using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.aZaaS.Activities
{
    [ActivityDefinition(
        Category = "aZaaS",
        Description = "Schedules sqoop import or export job",
        RuntimeDescription = "x => !!x.state.schedule ? `Schedule: <strong>${ x.state.schedule.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { "Done" }
    )]
    public class SqoopJobScheduler : Activity
    {
        private SqoopApiWrapper _sqoopApi;
        private ILogger<SqoopImportJob> _logger;

        public SqoopJobScheduler(IConfiguration configuration, ILogger<SqoopImportJob> logger)
        {
            _logger = logger;
            _sqoopApi = new SqoopApiWrapper(configuration);
        }

        [ActivityProperty(Hint = "The cron expression for schedule")]
        public IWorkflowExpression<string> Schedule
        {
            get => GetState(() => new LiteralExpression<string>("* * * * *"));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = null;
            _logger.LogInformation($">> [START] of {nameof(SqoopJobScheduler)} ...");

            _logger.LogInformation($">> Reading job name ...");
            var jobName = context.GetVariable<string>("JobName");
            _logger.LogInformation($">> JobName: {jobName}");

            if (string.IsNullOrWhiteSpace(jobName))
                return Fault("Job name was not specified");

            try
            {
                activityResult = Done();
                var schedule = await context.EvaluateAsync(Schedule, cancellationToken);

                _logger.LogInformation($">> Post job scheduler (API) ...");
                var result = await _sqoopApi.ScheduleJobAsync(jobName, schedule);
                _logger.LogInformation($">> Post result: {result}");

                if (!result)
                    activityResult = new FaultWorkflowResult("Sqoop job scheduler status -> false");
            }
            catch (Exception ex) { activityResult = new FaultWorkflowResult(ex.Message); }

            _logger.LogInformation($">> [END] of {nameof(SqoopJobScheduler)}");
            return activityResult;
        }
    }
}
