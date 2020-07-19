using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Sqoop.Activities
{
    /// <summary>
    /// Writes a text string to the console.
    /// </summary>
    [ActivityDefinition(
        Category = "Sqoop",
        Description = "Import table to hdfs.",
        Icon = "fas fa-terminal",
        RuntimeDescription = "x => !!x.state.jobName ? `Job Name: <strong>${ x.state.jobName.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ImportJob : Activity
    {
        private ILogger<ImportJob> _logger;

        public ImportJob(ILogger<ImportJob> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// The name of import job
        /// </summary>
        [ActivityProperty(
            Hint = "The name of import job"
        )]
        public WorkflowExpression<string> JobName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        /// <summary>
        /// The source database type
        /// </summary>
        [ActivityProperty(
            Type = ActivityPropertyTypes.Select,
            Hint = "The source database type"
        )]
        [SelectOptions("SQL SERVER", "MYSQL")]
        public string DatabaseType
        {
            get => GetState(() => "SQL SERVER");
            set => SetState(value);
        }

        /// <summary>
        /// The jdbc url of data source
        /// </summary>
        [ActivityProperty(Hint = "The jdbc url of data base.")]
        public WorkflowExpression<string> JdbcUrl
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var jobName = await context.EvaluateAsync(JobName, cancellationToken);
            var jdbcUrl = await context.EvaluateAsync(JdbcUrl, cancellationToken);

            _logger.LogInformation($">>>>>>>>>>>>>>>>>>>> Importing data ...");
            _logger.LogInformation($">> JdbUrl: {jdbcUrl}");

            context.CurrentScope.SetVariable("JobName", jobName);


            _logger.LogInformation($"<<<<<<<<<<<<<<<<<<<< data imported.");
            return Outcomes(new[] { OutcomeNames.Done });
        }
    }
}
