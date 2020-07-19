using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using aZaaS.KafkaEventBus;
using Elsa.Activities.aZaaS.Activities;
using Elsa.Models;
using Elsa.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample16.Events;

namespace Sample16.Subscribers
{
    public class SparkAppSubscriber : KafkaConsumer<SparkAppStatus>
    {
        //private readonly IWorkflowInvoker _workflowInvoker;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KafkaConsumer<SparkAppStatus>> _logger;

        public SparkAppSubscriber(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumer<SparkAppStatus>> logger) : base(configuration, logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public override async Task Handle(SparkAppStatus @event)
        {
            var appId = @event.AppId.ToString();
            if (@event.AppStatus == AppStatus.Completed)
            {
                var scope = _serviceProvider.CreateScope();
                var workflowInvoker = scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>();
                var triggeredExecutionContexts = await workflowInvoker.TriggerAsync(nameof(SparkAppWaiter), new Variables() { ["Signal"] = new Variable("Success") }, correlationId: appId); ;
                if (triggeredExecutionContexts != null && triggeredExecutionContexts.Any())
                    _logger.LogInformation($"Workflow Subscriber -> Spark App: [{appId}] is triggered successfully!");
                else
                    _logger.LogInformation($"Workflow Subscriber -> Spark App: [{appId}] nothing changed!");
            }

            // TODO: Handle other status of Workflow
        }
    }
}
