using System;
using System.Collections.Generic;
using System.Text;
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
        Description = "Executes spark sql query on hdfs data and exports result to table.",
        RuntimeDescription = "x => x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SparkHdfsQueryToTable : Activity
    {
        private SparkApiWrapper _sparkApi;
        private ILogger<SparkHdfsQueryToTable> _logger;

        public SparkHdfsQueryToTable(IConfiguration configuration, ILogger<SparkHdfsQueryToTable> logger)
        {
            _logger = logger;
            _sparkApi = new SparkApiWrapper(configuration);
        }

        [ActivityProperty(Hint = "The name of spark app")]
        public IWorkflowExpression<string> AppName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The file type for hdfs data"
        )]
        [SelectOptions("CSV", "PARQUET")]
        public string FileType
        {
            get => GetState(() => "CSV");
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The file path of hdfs data")]
        public IWorkflowExpression<string> FilePath
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The schema string of hdfs data (Recommanded if file type is csv)")]
        public IWorkflowExpression<string> SchemaString
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The alias name of hdfs data for spark sql")]
        public IWorkflowExpression<string> TempTable
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The spark sql query would applied on hdfs data")]
        public IWorkflowExpression<string> SqlQuery
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The database jdbc url of result data")]
        public IWorkflowExpression<string> JdbcUrl
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The export table name of result data")]
        public IWorkflowExpression<string> ExportTable
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }


        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = null;
            _logger.LogInformation($">> [START] of {nameof(SparkHdfsQueryToTable)} ...");

            var appName = await context.EvaluateAsync(AppName, cancellationToken);
            var fileType = context.CurrentActivity.State.GetState<string>(nameof(FileType));
            var filePath = await context.EvaluateAsync(FilePath, cancellationToken);
            var schemaString = await context.EvaluateAsync(SchemaString, cancellationToken);
            var tempTable = await context.EvaluateAsync(TempTable, cancellationToken);
            var sqlQuery = await context.EvaluateAsync(SqlQuery, cancellationToken);
            var jdbcUrl = await context.EvaluateAsync(JdbcUrl, cancellationToken);
            var exportTable = await context.EvaluateAsync(ExportTable, cancellationToken);

            _logger.LogInformation($">> Creating spark app ...");
            var model = new HdfsQueryToTableAppModel(appName, fileType, filePath, schemaString, tempTable, sqlQuery, jdbcUrl, exportTable);

            try
            {
                activityResult = Done();

                _logger.LogInformation($">> Posting spark app (API) ...");
                var result = await _sparkApi.CreateHdfsQueryAppAsync(model);
                _logger.LogInformation($">> Post result: {result}");

                // Set workflow CorrelationId as AppName
                context.Workflow.CorrelationId = result.AppId.ToString();
                if (!result.Status)
                    activityResult = new FaultWorkflowResult("Spark api status -> false");
            }
            catch (Exception ex) { activityResult = new FaultWorkflowResult(ex.Message); }

            _logger.LogInformation($">> [END] of {nameof(SparkHdfsQueryToTable)}");
            return activityResult;
        }
    }
}
