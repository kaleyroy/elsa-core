using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.aZaaS.Activities
{
    [ActivityDefinition(
        Category = "aZaaS",
        Description = "Imports sql table to hdfs incrementally using sqoop",
        RuntimeDescription = "x => !!x.state.jobName ? `Job Name: <strong>${ x.state.jobName.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { "Created" }
    )]
    public class SqoopIncrementalImportJob : Activity
    {
        const string INCREMEANTAL_MODE = "lastmodified";

        private SqoopApiWrapper _sqoopApi;
        private ILogger<SqoopImportJob> _logger;

        public SqoopIncrementalImportJob(IConfiguration configuration, ILogger<SqoopImportJob> logger)
        {
            _logger = logger;
            _sqoopApi = new SqoopApiWrapper(configuration);
        }


        #region Properties

        /// <summary>
        /// The name of import job
        /// </summary>
        [ActivityProperty(Hint = "The name of import job")]
        public WorkflowExpression<string> JobName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        /// <summary>
        /// The jdbc url of database
        /// </summary>
        [ActivityProperty(Hint = "The jdbc url of database")]
        public WorkflowExpression<string> JdbcUrl
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        /// <summary>
        /// The user name of database
        /// </summary>
        [ActivityProperty(Hint = "The user name of database")]
        public IWorkflowExpression<string> UserName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        /// <summary>
        /// The password of database
        /// </summary>
        [ActivityProperty(Hint = "The password of database")]
        public IWorkflowExpression<string> Password
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        /// <summary>
        /// The import table name
        /// </summary>
        [ActivityProperty(Hint = "The import table name")]
        public IWorkflowExpression<string> TableName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        /// <summary>
        /// The incremental import column name
        /// </summary>
        [ActivityProperty(Hint = "The table incremental column name")]
        public IWorkflowExpression<string> IncrementalColumn
        {
            get => GetState(() => new LiteralExpression<string>(string.Empty));
            set => SetState(value);
        }

        /// <summary>
        /// The import incremental column init value
        /// </summary>
        [ActivityProperty(Hint = "The table incremental column initial value")]
        public IWorkflowExpression<string> IncrementalValue
        {
            get => GetState(() => new LiteralExpression<string>(string.Empty));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The table primary key column name")]
        public IWorkflowExpression<string> PrimaryKeyColumn
        {
            get => GetState(() => new LiteralExpression<string>(string.Empty));
            set => SetState(value);
        }

        /// <summary>
        /// The mapper number for import job
        /// </summary>
        [ActivityProperty(Hint = "The mapper number for import job")]
        public IWorkflowExpression<int> Mapper
        {
            get => GetState(() => new LiteralExpression<int>("1"));
            set => SetState(value);
        }

        //public IWorkflowExpression<bool> 

        #endregion

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = null;
            _logger.LogInformation($">> [START] of {nameof(SqoopIncrementalImportJob)} ...");

            var jobName = await context.EvaluateAsync(JobName, cancellationToken);
            var jdbcUrl = await context.EvaluateAsync(JdbcUrl, cancellationToken);
            var userName = await context.EvaluateAsync(UserName, cancellationToken);
            var password = await context.EvaluateAsync(Password, cancellationToken);
            var tableName = await context.EvaluateAsync(TableName, cancellationToken);
            var incrementalColumn = await context.EvaluateAsync(IncrementalColumn, cancellationToken);
            var incrementalValue = await context.EvaluateAsync(IncrementalValue, cancellationToken);
            var primaryKeyColumn = await context.EvaluateAsync(PrimaryKeyColumn, cancellationToken);
            var mapper = await context.EvaluateAsync(Mapper, cancellationToken);

            _logger.LogInformation($">> Creating import job ...");

            var database = JdbcUrlHelper.GetDatabase(jdbcUrl);
            var hiveDatabase = FormatHiveProperty(database);
            var hiveTableName = FormatHiveProperty(tableName);

            var importJob = new ImportJob(jobName, jdbcUrl, userName, password, tableName, mapper);
            importJob.UseIncremental(incrementalColumn, INCREMEANTAL_MODE, incrementalValue, primaryKeyColumn)
                     .ToOrcFile(hiveDatabase, hiveTableName);

            // Set workflow CorrelationId as JobName
            context.Workflow.CorrelationId = jobName;

            try
            {
                activityResult = new OutcomeResult(new string[] { "Created" });

                _logger.LogInformation($">> Posting import job (API) ...");
                var result = await _sqoopApi.CreateImportJobAsync(importJob);
                _logger.LogInformation($">> Post result: {result}");

                // Set JobName variable that will be used in SqoopJobExecutor after job created
                context.CurrentScope.SetVariable("JobName", jobName);

                if (!result)
                    activityResult = new FaultWorkflowResult("Sqoop import job status -> false");
            }
            catch (Exception ex) { activityResult = new FaultWorkflowResult(ex.Message); }

            _logger.LogInformation($">> [END] of {nameof(SqoopIncrementalImportJob)}");
            return activityResult;
        }


        private string FormatHiveProperty(string input)
        {
            return (input ?? string.Empty).Replace("-", "_").Replace(".", "_").ToLower();
        }
    }
}
