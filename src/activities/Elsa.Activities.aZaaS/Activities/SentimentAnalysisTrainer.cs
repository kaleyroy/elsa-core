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
        Description = "Text sentiment analysis prediction model trainer.",
        RuntimeDescription = "x => !!x.state.appName ? `App Name: <strong>${ x.state.appName.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SentimentAnalysisTrainer : Activity
    {

        private SparkApiWrapper _sparkApi;
        private ILogger<SentimentAnalysisTrainer> _logger;

        public SentimentAnalysisTrainer(IConfiguration configuration, ILogger<SentimentAnalysisTrainer> logger)
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

        [ActivityProperty(Hint = "The jdbc url of database")]
        public IWorkflowExpression<string> JdbcUrl
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The table name of train data")]
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
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The file name of output train model")]
        public IWorkflowExpression<string> ModelFile
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, "SentimentAnaylysis.zip"));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = null;
            _logger.LogInformation($">> [START] of {nameof(SentimentAnalysisTrainer)} ...");

            var appName = await context.EvaluateAsync(AppName, cancellationToken);

            var jdbcUrl = await context.EvaluateAsync(JdbcUrl, cancellationToken);
            var inputTable = await context.EvaluateAsync(InputTable, cancellationToken);
            var inputColumn = await context.EvaluateAsync(InputColumn, cancellationToken);
            var labelColumn = await context.EvaluateAsync(LabelColumn, cancellationToken);

            var modelFile = await context.EvaluateAsync(ModelFile, cancellationToken);

            _logger.LogInformation($">> Creating spark app ...");
            var model = new SentimentAnalysisTrainerModel(appName, jdbcUrl, inputTable, inputColumn, labelColumn, modelFile);

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

            _logger.LogInformation($">> [END] of {nameof(SentimentAnalysisTrainer)}");
            return activityResult;
        }
    }
}
