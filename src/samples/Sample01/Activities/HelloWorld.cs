using System;
using Elsa.Services;
using Elsa.Results;
using Elsa.Services.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Sample01.Activities
{
    public class HelloWorld : Activity
    {

        protected override Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var folio = context.Workflow.Input.GetVariable<string>("Folio");
            if (folio == null)
            {
                Console.WriteLine("Empty folio -> Halt");
                return Task.FromResult<ActivityExecutionResult>(Fault("Empty folio"));
            }


            Console.WriteLine($"Folio: {folio}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("Hello world!");
            return Task.FromResult(Done());
        }

        //protected override Task<bool> OnCanExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        //{
        //    var folio = context.Workflow.Input.GetVariable<string>("Folio");
        //    return Task.FromResult(!string.IsNullOrEmpty(folio));
        //}

        protected override ActivityExecutionResult OnResume(WorkflowExecutionContext context)
        {
            Console.WriteLine("Resume ...");

            return Done();
        }
    }
}