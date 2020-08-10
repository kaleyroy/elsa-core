using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Http.RequestHandlers.Results;
using Elsa.Activities.Http.Services;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace Elsa.Activities.Http.RequestHandlers.Handlers
{
    public class TriggerRequestHandler : IRequestHandler
    {
        private readonly HttpContext httpContext;
        private readonly IWorkflowInvoker workflowInvoker;
        private readonly IWorkflowRegistry registry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly CancellationToken cancellationToken;

        private readonly IConfiguration configuration;
        private readonly bool authorizationEnabled = true;

        public TriggerRequestHandler(
            IHttpContextAccessor httpContext,
            IWorkflowInvoker workflowInvoker,
            IWorkflowRegistry registry,
            IWorkflowInstanceStore workflowInstanceStore,
            IConfiguration configuration)
        {
            this.httpContext = httpContext.HttpContext;
            this.workflowInvoker = workflowInvoker;
            this.registry = registry;
            this.workflowInstanceStore = workflowInstanceStore;
            cancellationToken = httpContext.HttpContext.RequestAborted;

            this.configuration = configuration;
            bool.TryParse(configuration["Elsa:Introspect:Enabled"], out authorizationEnabled);
        }

        public async Task<IRequestHandlerResult> HandleRequestAsync()
        {
            // TODO: Optimize this by building up a hash of routes and workflows to execute.
            var requestPath = new Uri(httpContext.Request.Path.ToString(), UriKind.Relative);
            var method = httpContext.Request.Method;
            var httpWorkflows = await registry.ListByStartActivityAsync(nameof(ReceiveHttpRequest), cancellationToken);
            var workflowsToStart = Filter(httpWorkflows, requestPath, method).ToList();
            var haltedHttpWorkflows = await workflowInstanceStore.ListByBlockingActivityAsync<ReceiveHttpRequest>(
                cancellationToken: cancellationToken);

            var hasCorrelationIdHeader = httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationIdHeaderValues);
            var correlationIds = hasCorrelationIdHeader ? correlationIdHeaderValues.ToArray() : Array.Empty<string>();

            var workflowsToResume = Filter(haltedHttpWorkflows, requestPath, method, correlationIds).ToList();

            if (!workflowsToStart.Any() && !workflowsToResume.Any())
                return new NextResult();

            // If authentication enabled, we'll check the authentication info in Header or oidc cookies
            if (authorizationEnabled)
            {
                var accessToken = httpContext.Request.Headers[HeaderNames.Authorization].ToString();
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    var authResult = await httpContext.AuthenticateAsync("oidc");
                    if (authResult == null || !authResult.Succeeded)
                        return new UnAuthrorizedResult();
                }
                else
                {
                    var tokenValidated = await ValidateAccessTokenAsync(accessToken);
                    if(!tokenValidated)
                         return new UnAuthrorizedResult();
                }
            }

            await InvokeWorkflowsToStartAsync(workflowsToStart);
            await InvokeWorkflowsToResumeAsync(workflowsToResume);

            return !httpContext.Items.ContainsKey(WorkflowHttpResult.Instance)
                ? (IRequestHandlerResult)new AcceptedResult()
                : new EmptyResult();
        }

        private IEnumerable<(WorkflowInstance, ActivityInstance)> Filter(
            IEnumerable<(WorkflowInstance, ActivityInstance)> items,
            Uri path,
            string method,
            string[] correlationIds)
        {
            var correlatedItems = correlationIds.Any() ? items.Where(x => correlationIds.Contains(x.Item1.CorrelationId)) : items;
            return correlatedItems.Where(x => IsMatch(x.Item2.State, path, method));
        }

        private IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)> Filter(
            IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)> items,
            Uri path,
            string method)
        {
            return items.Where(x => IsMatch(x.Item2.State, path, method));
        }

        private bool IsMatch(JObject state, Uri path, string method)
        {
            var m = ReceiveHttpRequest.GetMethod(state);
            var p = ReceiveHttpRequest.GetPath(state);
            return (string.IsNullOrWhiteSpace(m) || m == method) && p == path;
        }

        private async Task InvokeWorkflowsToStartAsync(
            IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)> items)
        {
            foreach (var item in items)
            {
                await workflowInvoker.StartAsync(
                    item.Item1,
                    Variables.Empty,
                    new[] { item.Item2.Id },
                    cancellationToken: cancellationToken);
            }
        }

        private async Task InvokeWorkflowsToResumeAsync(IEnumerable<(WorkflowInstance, ActivityInstance)> items)
        {
            foreach (var (workflowInstance, activity) in items)
            {
                await workflowInvoker.ResumeAsync(
                    workflowInstance,
                    Variables.Empty,
                    new[] { activity.Id },
                    cancellationToken);
            }
        }


        private async Task<bool> ValidateAccessTokenAsync(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentNullException(nameof(accessToken));

            accessToken = accessToken.Replace("Bearer ", string.Empty);

            var client = new HttpClient();
            var response = await client.IntrospectTokenAsync(new TokenIntrospectionRequest
            {
                Address = $"{configuration["AuthServer:Authority"]}/connect/introspect",
                ClientId = configuration["Elsa:Introspect:ClientId"],
                ClientSecret = configuration["Elsa:Introspect:ClientSecret"],
                Token = accessToken
            });

            if (response.IsError)
                throw new Exception($"Introspect Token Error: {response.Error}");

            return response.IsActive;
        }
    }
}