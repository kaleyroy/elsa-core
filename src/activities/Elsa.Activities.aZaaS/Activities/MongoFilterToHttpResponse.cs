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
        Description = "Executes mongo filter and writes result to http response.",
        RuntimeDescription = "x => !!x.state.database ? ` <strong>${ x.state.database.expression }</strong> - <strong>${ x.state.collection.expression }</strong>` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class MongoFilterToHttpResponse : Activity
    {
        private readonly MongoApiWrapper _mongoApi;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<MongoFilterToHttpResponse> _logger;

        public MongoFilterToHttpResponse(
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

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = Done();
            _logger.LogInformation($">> [START] of {nameof(MongoFilterToHttpResponse)} ...");

            var lastResult = context.CurrentScope.LastResult;
            if (lastResult == null)
                return Fault("Can not retrive http request, make sure [ReceiveHttpRequest] activity exists");

            var database = await context.EvaluateAsync(Database, cancellationToken);
            var collection = await context.EvaluateAsync(Collection, cancellationToken);

            var filter = string.Empty;
            int page = 0, pageSize = 0;

            var request = lastResult.Value as HttpRequestModel;
            foreach (var item in request.QueryString)
            {
                switch (item.Key.ToLower())
                {
                    case "filter":
                        filter = item.Value.Value;
                        break;
                    case "page":
                        page = int.Parse(item.Value.Value);
                        break;
                    case "pagesize":
                        pageSize = int.Parse(item.Value.Value);
                        break;
                }
            }
            var model = new MongoFilterModel(database, collection, filter, page, pageSize); // TODO:

            var response = _httpContextAccessor.HttpContext.Response;
            response.ContentType = "application/json";

            if (response.HasStarted)
                return Fault("Response has already started");

            try
            {
                // Fetch filter 
                _logger.LogInformation($">> Executing mongo filter (API) ...");
                var filerResult = await _mongoApi.FilterCollectionAsync(model);

                // Write HttpResponse
                response.StatusCode = 200;
                if (!string.IsNullOrWhiteSpace(filerResult))
                    await response.WriteAsync(filerResult, cancellationToken);
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ContentType = "text/plain";
                await response.WriteAsync(ex.ToString());

                activityResult = new FaultWorkflowResult(ex.Message);
            }

            _logger.LogInformation($">> [END] of {nameof(MongoFilterToHttpResponse)}");
            return activityResult;
        }
    }
}
