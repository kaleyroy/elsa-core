using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Elsa.Models;
using Elsa.Services;
using Elsa.Extensions;
using Elsa.Activities.Workflows.Activities;
using Elsa.Activities.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Sample01.Activities;
using Elsa.Persistence;

namespace Sample01
{
    /// <summary>
    /// A minimal workflows program defined in code with a strongly-typed workflow class.
    /// </summary>
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .AddActivity<HelloWorld>()
                .AddActivity<GoodByeWorld>()
                .AddWorkflowActivities()
                .BuildServiceProvider();

            var registry = services.GetService<IWorkflowRegistry>();
            var definitionStore = services.GetRequiredService<IWorkflowDefinitionStore>();

            // Variables
            var correlationId = "Test";
            var inputs = new Variables(new List<KeyValuePair<string, Variable>>()
            {
                //new KeyValuePair<string, Variable>("Folio",new Variable("This is my input folio"))
            });

            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            var executionContext = await invoker.StartAsync<HelloWorldWorkflow>(inputs, correlationId: correlationId);

            // IMPORTANT: 
            await definitionStore.AddAsync(executionContext.Workflow.Definition);

            Console.WriteLine("Wait...");

            //var acivitiyContexts = await invoker.TriggerAsync(nameof(Signaled), new Variables() { ["Signal"] = new Variable("Good") }, correlationId: correlationId);

            var workflowInstance = executionContext.Workflow.ToInstance();
            var acivitiyContexts = await invoker.ResumeAsync(workflowInstance, new Variables() { ["Folio"] = new Variable("Good to go") }, new string[] { executionContext.CurrentActivity.Id });
            Console.ReadLine();
        }
    }
}