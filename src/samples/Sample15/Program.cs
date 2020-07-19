using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.MongoDb.Extensions;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Elsa.Activities.Workflows.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Workflows.Activities;

namespace Sample15
{
    /// <summary>
    /// A simple demonstration of using the MongoDB persistence providers.
    /// If you don't have MongoDB installed but you do have Docker, run `docker-compose up` to run a container with MongoDB (see the 'docker-compose.yaml' file). 
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            var services = BuildServices();

            //// Create a workflow definition.
            //var registry = services.GetService<IWorkflowRegistry>();
            //var workflowDefinition = await registry.GetWorkflowDefinitionAsync<HelloWorldWorkflow>();

            //// Mark this definition as the "latest" version.
            //workflowDefinition.IsLatest = true;
            //workflowDefinition.Version = 1;

            //using var scope = services.CreateScope();
            //// Persist the workflow definition.
            //var definitionStore = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
            //await definitionStore.SaveAsync(workflowDefinition);

            //// Load the workflow definition.
            //workflowDefinition = await definitionStore.GetByIdAsync(
            //    workflowDefinition.DefinitionId,
            //    VersionOptions.Latest);

            //// Execute the workflow.
            //var invoker = scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>();
            //var executionContext = await invoker.StartAsync(workflowDefinition);

            //// Persist the workflow instance.
            //var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            //var workflowInstance = executionContext.Workflow.ToInstance();
            //await instanceStore.SaveAsync(workflowInstance);

            using var scope = services.CreateScope();
            var definitionStore = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
            var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();

            // Load the workflow definition.
            var definitionId = "9ca512601cdd433ea9772d52e01cce02";
            var correlationId = Guid.NewGuid().ToString();
            var workflowDefinition = await definitionStore.GetByIdAsync(definitionId, VersionOptions.Latest);

            // Execute the workflow.
            var invoker = scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>();
            var executionContext = await invoker.StartAsync(workflowDefinition, correlationId: correlationId);

            // Persist the workflow instance.
            var workflowInstance = executionContext.Workflow.ToInstance();
            await instanceStore.SaveAsync(workflowInstance);

            Console.WriteLine("Trigger Signal?");
            Console.ReadLine();

            var triggeredExecutionContexts = await invoker.TriggerAsync(nameof(Signaled), new Variables() { ["Signal"] = new Variable("Approved") }, correlationId: correlationId);
        }

        private static IServiceProvider BuildServices()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string>
                    {
                        ["ConnectionStrings:MongoDb"] = "mongodb://mongodb"
                    }
                )
                .Build();

            return new ServiceCollection()
                .AddElsa(x => x.AddMongoDbStores(configuration, "Elsa", "MongoDb"))
                .AddStartupRunner()
                .AddConsoleActivities()
                .AddWorkflow<HelloWorldWorkflow>()
                .AddHttpActivities()
                .AddWorkflowActivities()
                .BuildServiceProvider();
        }
    }
}