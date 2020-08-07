using System;
using System.Linq;
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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Elsa.Activities.Http.Models;

namespace Elsa.Activities.aZaaS.Activities
{
    [ActivityDefinition(
        Category = "aZaaS",
        Description = "Executes mongo aggregation and writes result to http response.",
        RuntimeDescription = "x => !!x.state.database ? ` <strong>${ x.state.database.expression }</strong>/<strong>${ x.state.collection.expression }</strong>/<strong>${ x.state.pipelineUri.expression }</strong>` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class MongoAggregationToHttpResponse : Activity
    {
        private readonly MongoApiWrapper _mongoApi;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<MongoFilterToHttpResponse> _logger;

        public MongoAggregationToHttpResponse(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<MongoFilterToHttpResponse> logger)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
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

        //[ActivityProperty(Hint = "The aggregation expression of mongodb")]
        //[ExpressionOptions(Multiline = true)]
        //public IWorkflowExpression<string> Expression
        //{
        //    get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
        //    set => SetState(value);
        //}

        [ActivityProperty(Hint = "The pre-defined uri of aggregation pipeline")]
        public IWorkflowExpression<string> PipelineUri
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = Done();
            _logger.LogInformation($">> [START] of {nameof(MongoAggregationToHttpResponse)} ...");

            var lastResult = context.CurrentScope.LastResult;
            if (lastResult == null)
                return Fault("Failed to retrive http request, make sure [ReceiveHttpRequest] activity exists");

            var database = await context.EvaluateAsync(Database, cancellationToken);
            var collection = await context.EvaluateAsync(Collection, cancellationToken);
            var uriAlias = await context.EvaluateAsync(PipelineUri, cancellationToken);
            if (uriAlias.StartsWith("/"))
                uriAlias = uriAlias.Trim().Split('/').Last();

            int page = 0, pageSize = 0;
            string variables = string.Empty, mode = string.Empty;
            var request = lastResult.Value as HttpRequestModel;
            foreach (var item in request.QueryString)
            {
                switch (item.Key.ToLower())
                {
                    case "avars":
                        variables = item.Value.Value;
                        break;
                    case "page":
                        page = int.Parse(item.Value.Value);
                        break;
                    case "pagesize":
                        pageSize = int.Parse(item.Value.Value);
                        break;
                    case "mode":
                        mode = item.Value.Value.ToLower();
                        break;
                }
            }

            var model = new MongoAggregationModel(database, collection, uriAlias, page, pageSize, variables);
            var response = _httpContextAccessor.HttpContext.Response;
            response.ContentType = "application/json";

            if (response.HasStarted)
                return Fault("Response has already started");

            try
            {
                // Fetch aggregration result 
                _logger.LogInformation($">> Executing mongo aggregation (API) ...");
                var aggrsResult = await _mongoApi.AggrsCollectionAsync(model);

                // Write HttpResponse
                response.StatusCode = 200;
                if (!string.IsNullOrWhiteSpace(aggrsResult))
                    await response.WriteAsync(aggrsResult, cancellationToken);
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ContentType = "text/plain";
                await response.WriteAsync(ex.ToString());

                activityResult = new FaultWorkflowResult(ex.Message);
            }

            _logger.LogInformation($">> [END] of {nameof(MongoAggregationToHttpResponse)}");
            return activityResult;
        }
    }
}
