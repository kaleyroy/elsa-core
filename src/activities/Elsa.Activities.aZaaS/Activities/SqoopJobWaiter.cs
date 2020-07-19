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

namespace Elsa.Activities.aZaaS.Activities
{
    [ActivityDefinition(
        Category = "aZaaS",
        Description = "Waits sqoop import or export job execution.",
        RuntimeDescription = "x => x.definition.description",
        Outcomes = new[] { "Success" }
    )]
    public class SqoopJobWaiter : Activity
    {
        const string SuccessSignal = "Success";


        [ActivityProperty(Hint = "The correlationId (sqoop job name) of workflow")]
        public IWorkflowExpression<string> CorrelationId
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        //protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        //{
        //    return Halt(true);
        //}

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var correlationId = await context.EvaluateAsync(CorrelationId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(correlationId))
                context.Workflow.CorrelationId = correlationId;

            return Halt(true);
        }

        protected override Task<bool> OnCanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var flag = context.Workflow.Input.GetVariable<string>("Signal") == SuccessSignal;
            return Task.FromResult(flag);
        }

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            return Outcome("Success");
        }
    }
}
