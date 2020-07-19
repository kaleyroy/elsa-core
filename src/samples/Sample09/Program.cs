using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using System.Reflection;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample09
{
    /// <summary>
    /// A simple console program that loads & executes a workflow designed with the HTML5 workflow designer.
    /// </summary>
    internal class Program
    {
        private static async Task Main()
        {

            var services = BuildServices();
            var serializer = services.GetRequiredService<IWorkflowSerializer>();

            var jsonDefinition = File.ReadAllText("workflow.incremental.json");
            var workflowDefinition = serializer.Deserialize<WorkflowDefinitionVersion>(jsonDefinition, JsonTokenFormatter.FormatName);


            var json = await ReadEmbeddedResourceAsync("Sample09.calculator.json");
            var workflow = serializer.Deserialize<WorkflowDefinitionVersion>(json, JsonTokenFormatter.FormatName);
            var invoker = services.GetRequiredService<IWorkflowInvoker>();
            await invoker.StartAsync(workflow);
        }

        private static IServiceProvider BuildServices()
        {
            return new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .AddSingleton(Console.In)
                .BuildServiceProvider();
        }

        private static async Task<string> ReadEmbeddedResourceAsync(string resourceName)
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            using var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName));
            return await reader.ReadToEndAsync();
        }
    }
}