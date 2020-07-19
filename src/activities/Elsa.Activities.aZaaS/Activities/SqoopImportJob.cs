using System;
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
    /// <summary>
    /// Imports sql table to Hdfs.
    /// </summary>
    [ActivityDefinition(
        Category = "aZaaS",
        Description = "Imports sql table to hdfs by using sqoop",
        RuntimeDescription = "x => !!x.state.jobName ? `Job Name: <strong>${ x.state.jobName.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { "Created" }
    )]
    public class SqoopImportJob : Activity
    {
        private SqoopApiWrapper _sqoopApi;
        private ILogger<SqoopImportJob> _logger;

        public SqoopImportJob(IConfiguration configuration, ILogger<SqoopImportJob> logger)
        {
            _logger = logger;
            _sqoopApi = new SqoopApiWrapper(configuration);
        }

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
        /// The file format for import data
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The file format for import data"
        )]
        [SelectOptions("CSV", "PARQUET")]
        public string FileFormat
        {
            get => GetState(() => "CSV");
            set => SetState(value);
        }

        /// <summary>
        /// The target path for import data
        /// </summary>
        [ActivityProperty(Hint = "The target path for import data")]
        public IWorkflowExpression<string> TargetPath
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
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

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = null;
            _logger.LogInformation($">> [START] of {nameof(SqoopImportJob)} ...");

            var jobName = await context.EvaluateAsync(JobName, cancellationToken);
            var jdbcUrl = await context.EvaluateAsync(JdbcUrl, cancellationToken);
            var userName = await context.EvaluateAsync(UserName, cancellationToken);
            var password = await context.EvaluateAsync(Password, cancellationToken);
            var tableName = await context.EvaluateAsync(TableName, cancellationToken);
            var mapper = await context.EvaluateAsync(Mapper, cancellationToken);
            var fileFormat = context.CurrentActivity.State.GetState<string>(nameof(FileFormat));
            var targetPath = await context.EvaluateAsync(TargetPath, cancellationToken);

            _logger.LogInformation($">> Creating import job ...");
            var importJob = new ImportJob(jobName, jdbcUrl, userName, password, tableName, mapper);
            switch (fileFormat)
            {
                case "CSV":
                    importJob = importJob.ToCsvFile();
                    break;
                case "PARQUET":
                    importJob = importJob.ToParquetFile();
                    break;
            }
            importJob = importJob.TargetPath(targetPath);

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

            _logger.LogInformation($">> [END] of {nameof(SqoopImportJob)}");
            return activityResult;
        }
    }
}
