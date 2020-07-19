using Elsa.Services;
using Elsa.Services.Models;

using Elsa.Activities.Workflows.Activities;
using Sample01.Activities;
using Elsa.Expressions;

namespace Sample01
{
    public class HelloWorldWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<HelloWorld>()
                //.Then<Signaled>(x => x.Signal = new LiteralExpression("Good"))
                .Then<GoodByeWorld>();
        }
    }
}