using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Runtime;
using Elsa.Services;
using Elsa.Services.Models;

using Elsa.Activities.aZaaS.Extensions;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Workflows.Extensions;

using Elsa.Activities.aZaaS.Activities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;


namespace WorkflowTrigger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = BuildServices();

            var scope = services.CreateScope();
            var definitionStore = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
            var instanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var workflowInvoker = scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>();

            //var instances = await instanceStore.ListAllAsync();
            
            while (true)
            {
                Console.WriteLine("Which activity to trigger?");
                Console.WriteLine(">>   Sqoop = 1, Spark = 2");
                var input = Console.ReadLine();
                if (input.ToLower().Equals("exit"))
                    break;

                var activityType = 1;
                var correlationId = string.Empty;
                if (!int.TryParse(input, out activityType))
                    Console.WriteLine("Invalid activity type!");
                else
                {
                    if (activityType == 1)
                        Console.WriteLine("Activity type: Sqoop");
                    else if (activityType == 2)
                        Console.WriteLine("Activity type: Spark");
                    else
                    {
                        Console.WriteLine("Unidentified activity type!");
                        continue;
                    }
                }
                Console.WriteLine("Input workflow correlationId");
                correlationId = Console.ReadLine();
                Console.WriteLine($"CorrelationId: {correlationId}");

                Console.WriteLine("Triggering activity with signal ...");
                IEnumerable<WorkflowExecutionContext> triggeredExecutionContexts = null;
                try
                {
                    switch (activityType)
                    {
                        case 1:
                            Console.WriteLine("Sqoop activity triggering ...");
                            triggeredExecutionContexts = await workflowInvoker.TriggerAsync(nameof(SqoopJobWaiter), new Variables() { ["Signal"] = new Variable("Success") }, correlationId: correlationId);
                            break;
                        case 2:
                            Console.WriteLine("Spark activity triggering ...");
                            triggeredExecutionContexts = await workflowInvoker.TriggerAsync(nameof(SparkAppWaiter), new Variables() { ["Signal"] = new Variable("Success") }, correlationId: correlationId);
                            break;
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Error: {ex.ToString()}"); }

                if (triggeredExecutionContexts != null && triggeredExecutionContexts.Any())
                    Console.WriteLine("Operation successful!");
                else
                    Console.WriteLine("Nothing changed!");

                Console.WriteLine();
                Console.WriteLine("PRESS ANY KEY TO CONTINUE");
                Console.ReadLine();
            }

            // Load the workflow definition.
            //var definitionId = "9ca512601cdd433ea9772d52e01cce02";
            //var correlationId = Guid.NewGuid().ToString();
            //var workflowDefinition = await definitionStore.GetByIdAsync(definitionId, VersionOptions.Latest);

            // Execute the workflow.
            //var executionContext = await workflowInvoker.StartAsync(workflowDefinition, correlationId: correlationId);
            //var triggeredExecutionContexts = await invoker.TriggerAsync(nameof(Signaled), new Variables() { ["Signal"] = new Variable("Approved") }, correlationId: correlationId);
        }



        private static IServiceProvider BuildServices()
        {

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var sqlServerConnectionStrings = configuration["ConnectionStrings:SqlServer"];

            return new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)

                .AddElsa(x => x.AddEntityFrameworkStores<SqlServerContext>(
                        options => options.UseSqlServer(sqlServerConnectionStrings)))
                .AddStartupRunner()
                .AddConsoleActivities()
                .AddHttpActivities()
                .AddWorkflowActivities()
                .AddaZaaSActivities()
                .BuildServiceProvider();
        }
    }
}
