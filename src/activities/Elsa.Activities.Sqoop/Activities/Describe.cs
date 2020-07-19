using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Sqoop.Activities
{
    [ActivityDefinition(
        Category = "aZaaS",
        Description = "Describe the sqoop job.",
        Icon = "fas fa-terminal",
        RuntimeDescription = "x => x.definition.description",
        Outcomes = new[] { OutcomeNames.True }
    )]
    public class Describe : Activity
    {
        private ILogger<Describe> _logger;

        public Describe(ILogger<Describe> logger)
        {
            _logger = logger;
        }

        protected override Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            _logger.LogInformation($">>>>>>>>>>>>>>>>>>>> Describe job ...");

            var jobName = context.GetVariable<string>("JobName");
            _logger.LogInformation(" *********************************************");
            _logger.LogInformation($"   JobName: {jobName  }");
            _logger.LogInformation(" *********************************************");
           

            _logger.LogInformation($">>>>>>>>>>>>>>>>>>>> job is described");
            return Task.FromResult(Outcomes(new string[] { OutcomeNames.True }));
        }
    }
}
