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
        Category = "Sqoop",
        Description = "Execute the sqoop job.",
        Icon = "fas fa-terminal",
        RuntimeDescription = "x => x.definition.description",
        Outcomes = new[] { OutcomeNames.True }
    )]
    public class ExecuteJob : Activity
    {
        private ILogger<ExecuteJob> _logger;

        public ExecuteJob(ILogger<ExecuteJob> logger)
        {
            _logger = logger;
        }

        protected override Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            return base.OnExecuteAsync(context, cancellationToken);
        }

    }
}
