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
        Description = "Text sentiment analysis using pre-trained model.",
        RuntimeDescription = "x => !!x.state.appName ? `App Name: <strong>${ x.state.appName.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SentimentAnalysisModel : Activity
    {
        // Source (File/Table)
        // Model  (Zip File)
        // Input  (Column)
        // Output (Prediction + Custom Column)

        private SparkApiWrapper _sparkApi;
        private ILogger<SentimentAnalysisModel> _logger;

        public SentimentAnalysisModel(IConfiguration configuration, ILogger<SentimentAnalysisModel> logger)
        {
            _logger = logger;
            _sparkApi = new SparkApiWrapper(configuration, logger);
        }


        [ActivityProperty(Hint = "The name of spark app")]
        public IWorkflowExpression<string> AppName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, "Sentiment Analysis App"));
            set => SetState(value);
        }

        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The file name of pre-trained model"
        )]
        [SelectOptions("MLModel.zip", "MLModel_Custom.zip")]
        public string ModelFile
        {
            get => GetState(() => "MLModel.zip");
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The jdbc url of database")]
        public IWorkflowExpression<string> JdbcUrl
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The table name of input data")]
        public IWorkflowExpression<string> InputTable
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The column name of feature data")]
        public IWorkflowExpression<string> InputColumn
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The column name of prediction label")]
        public IWorkflowExpression<string> LabelColumn
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, "Prediction"));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The column names to export (OPTIONAL)")]
        public IWorkflowExpression<string> OutputColumns
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The table name of result data")]
        public IWorkflowExpression<string> OutputTable
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = null;
            _logger.LogInformation($">> [START] of {nameof(SentimentAnalysisModel)} ...");

            var appName = await context.EvaluateAsync(AppName, cancellationToken);
            var modelFile = context.CurrentActivity.State.GetState<string>(nameof(ModelFile));

            var jdbcUrl = await context.EvaluateAsync(JdbcUrl, cancellationToken);
            var inputTable = await context.EvaluateAsync(InputTable, cancellationToken);
            var inputColumn = await context.EvaluateAsync(InputColumn, cancellationToken);
            var labelColumn = await context.EvaluateAsync(LabelColumn, cancellationToken);
            var outputColumns = await context.EvaluateAsync(OutputColumns, cancellationToken);
            var outputTable = await context.EvaluateAsync(OutputTable, cancellationToken);

            _logger.LogInformation($">> Creating spark app ...");
            var model = new SentimentAnalysisAppModel(appName, modelFile, jdbcUrl, inputTable, inputColumn, labelColumn, outputColumns, outputTable);

            try
            {
                activityResult = Done();

                _logger.LogInformation($">> Posting spark app (API) ...");
                var result = await _sparkApi.CreateSparkAppAsync(model);
                _logger.LogInformation($">> Post result: {result}");

                // Set workflow CorrelationId as AppName
                context.Workflow.CorrelationId = result.AppId.ToString();
                if (!result.Status)
                    activityResult = new FaultWorkflowResult("Spark api status -> false");
            }
            catch (Exception ex) { activityResult = new FaultWorkflowResult(ex.Message); }

            _logger.LogInformation($">> [END] of {nameof(SentimentAnalysisModel)}");
            return activityResult;
        }
    }
}
