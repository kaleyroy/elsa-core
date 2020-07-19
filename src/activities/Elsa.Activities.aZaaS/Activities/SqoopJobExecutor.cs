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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.aZaaS.Activities
{
    [ActivityDefinition(
        Category = "aZaaS",
        Description = "Executes sqoop import or export job.",
        RuntimeDescription = "x => x.definition.description",
        Outcomes = new[] { "Submitted" }
    )]
    public class SqoopJobExecutor : Activity
    {
        private SqoopApiWrapper _sqoopApi;
        private ILogger<SqoopJobExecutor> _logger;


        public SqoopJobExecutor(IConfiguration configuration, ILogger<SqoopJobExecutor> logger)
        {
            _logger = logger;
            _sqoopApi = new SqoopApiWrapper(configuration);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = null;
            _logger.LogInformation($">> [START] of {nameof(SqoopJobExecutor)} ...");

            _logger.LogInformation($">> Reading job name ...");
            var jobName = context.GetVariable<string>("JobName");
            _logger.LogInformation($">> JobName: {jobName}");

            if (string.IsNullOrWhiteSpace(jobName))
                return Fault("Job name was not specified");

            try
            {
                activityResult = new OutcomeResult(new string[] { "Submitted" });

                _logger.LogInformation($">> Post job execution (API) ...");
                var result = await _sqoopApi.ExecuteJobAsync(jobName);
                _logger.LogInformation($">> Post result: {result}");

                if (!result)
                    activityResult = new FaultWorkflowResult("Sqoop execute job status -> false");
            }
            catch (Exception ex) { activityResult = new FaultWorkflowResult(ex.Message); }

            _logger.LogInformation($">> [END] of {nameof(SqoopJobExecutor)}");
            return activityResult;
        }

    }
}
