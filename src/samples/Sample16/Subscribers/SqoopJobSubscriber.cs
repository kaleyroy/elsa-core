using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using aZaaS.KafkaEventBus;
using Elsa.Activities.aZaaS.Activities;
using Elsa.Models;
using Elsa.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sample16.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Sample16.Subscribers
{
    public class SqoopJobSubscriber : KafkaConsumer<SqoopJobStatus>
    {
        //private readonly IWorkflowInvoker _workflowInvoker;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KafkaConsumer<SqoopJobStatus>> _logger;

        public SqoopJobSubscriber(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<KafkaConsumer<SqoopJobStatus>> logger) : base(configuration, logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public override async Task Handle(SqoopJobStatus @event)
        {
            var jobName = @event.JobName;
            if (@event.JobStatus == JobStatus.Completed)
            {
                var scope = _serviceProvider.CreateScope();
                var workflowInvoker = scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>();
                var triggeredExecutionContexts = await workflowInvoker.TriggerAsync(nameof(SqoopJobWaiter), new Variables() { ["Signal"] = new Variable("Success") }, correlationId: jobName);
                if (triggeredExecutionContexts != null && triggeredExecutionContexts.Any())
                    _logger.LogInformation($"Workflow Subscriber -> Sqoop Job: [{jobName}] is triggered successfully!");
                else
                    _logger.LogInformation($"Workflow Subscriber -> Sqoop Job: [{jobName}] nothing changed!");
            }

            // TODO: Handle other status of Workflow
        }
    }
}
