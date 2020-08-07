using System;
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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Elsa.Activities.aZaaS.Activities
{
    [ActivityDefinition(
        Category = "aZaaS",
        Description = "Creates or updates mongo aggregation pipeline.",
        RuntimeDescription = "x => !!x.state.database ? ` <strong>${ x.state.database.expression }</strong> - <strong>${ x.state.collection.expression }</strong>` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class MongoAggregationPipeline : Activity
    {
        private readonly MongoApiWrapper _mongoApi;
        private readonly ILogger<MongoFilterToHttpResponse> _logger;

        public MongoAggregationPipeline(
            IConfiguration configuration,
            ILogger<MongoFilterToHttpResponse> logger)
        {
            _logger = logger;
            _mongoApi = new MongoApiWrapper(configuration, logger);
        }


        [ActivityProperty(Hint = "The name of mongodb database")]
        public IWorkflowExpression<string> Database
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The name of mongodb collection")]
        public IWorkflowExpression<string> Collection
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The aggregation expression of mongodb")]
        [ExpressionOptions(Multiline = true)]
        public IWorkflowExpression<string> Expression
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = Done();
            _logger.LogInformation($">> [START] of {nameof(MongoAggregationPipeline)} ...");

            //var lastResult = context.CurrentScope.LastResult;
            //if (lastResult == null)
            //    return Fault("Can not retrive http request, make sure [ReceiveHttpRequest] activity exists");

            var database = await context.EvaluateAsync(Database, cancellationToken);
            var collection = await context.EvaluateAsync(Collection, cancellationToken);
            var expression = await context.EvaluateAsync(Expression, cancellationToken);

            try
            {
                // Fetch aggregration result 
                _logger.LogInformation($">> Creating mongo aggregation (API) ...");
                await _mongoApi.CreateAggregationAsync(database, collection, expression);
            }
            catch (Exception ex) { activityResult = new FaultWorkflowResult(ex.Message); }

            _logger.LogInformation($">> [END] of {nameof(MongoAggregationPipeline)}");
            return activityResult;
        }

    }
}
