using System;
using System.Collections.Generic;
using System.Text;
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
        Description = "Creates jdbc source connector for kafka connect.",
        RuntimeDescription = "x => !!x.state.connectorName ? `Connector Name: <strong>${ x.state.connectorName.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class KafkaJdbcSourceConnector : Activity
    {
        private readonly KafkaApiWrapper _kafkaApi;
        private readonly ILogger<KafkaJdbcSourceConnector> _logger;

        public KafkaJdbcSourceConnector(IConfiguration configuration, ILogger<KafkaJdbcSourceConnector> logger)
        {
            _logger = logger;
            _kafkaApi = new KafkaApiWrapper(configuration, logger);
        }


        [ActivityProperty(Hint = "The name of jdbc connector")]
        public IWorkflowExpression<string> ConnectorName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }
        [ActivityProperty(Hint = "The jdbc url of database")]
        public IWorkflowExpression<string> JdbcUrl
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The user name of database")]
        public IWorkflowExpression<string> UserName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The password of database")]
        public IWorkflowExpression<string> Password
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The sql query of table or view")]
        public IWorkflowExpression<string> SqlQuery
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The time column name of table or view")]
        public IWorkflowExpression<string> TimeColumn
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The kafka topic name of poll data")]
        public IWorkflowExpression<string> KafkaTopic
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The interval ms of poll data, Default: 5000")]
        public IWorkflowExpression<int?> PollInterval
        {
            get => GetState(() => new WorkflowExpression<int?>(LiteralEvaluator.SyntaxName, "5000"));
            set => SetState(value);
        }
        [ActivityProperty(Hint = "The max tasks of poll data, Default: 1")]
        public IWorkflowExpression<int?> MaxTasks
        {
            get => GetState(() => new WorkflowExpression<int?>(LiteralEvaluator.SyntaxName, "1"));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = null;
            _logger.LogInformation($">> [START] of {nameof(KafkaJdbcSourceConnector)} ...");

            var connectorName = await context.EvaluateAsync(ConnectorName, cancellationToken);
            var jdbcUrl = await context.EvaluateAsync(JdbcUrl, cancellationToken);
            var userName = await context.EvaluateAsync(UserName, cancellationToken);
            var password = await context.EvaluateAsync(Password, cancellationToken);

            var sqlQuery = await context.EvaluateAsync(SqlQuery, cancellationToken);
            var timeColumn = await context.EvaluateAsync(TimeColumn, cancellationToken);
            var kafkaTopic = await context.EvaluateAsync(KafkaTopic, cancellationToken);

            var pollInterval = await context.EvaluateAsync(PollInterval, cancellationToken) ?? 5000;
            var maxTasks = await context.EvaluateAsync(MaxTasks, cancellationToken) ?? 1;

            // Set KafkaTopic to current scope variable
            context.CurrentScope.Variables.SetVariable(nameof(kafkaTopic), KafkaTopic);

            _logger.LogInformation($">> Creating kafka connector ...");
            var model = new KafkaJdbcSourceConnectorModel(connectorName, jdbcUrl, userName, password, sqlQuery, kafkaTopic, timeColumn, pollIntervalMs: pollInterval, maxTasks: maxTasks);

            try
            {
                activityResult = Done();

                _logger.LogInformation($">> Posting kafka connector (API) ...");
                var result = await _kafkaApi.CreateConnectorAsync(model);
                _logger.LogInformation($">> Post result: {result}");

                if (result == null)
                    activityResult = new FaultWorkflowResult("Kafka connector status -> empty");
            }
            catch (Exception ex) { activityResult = new FaultWorkflowResult(ex.Message); }

            _logger.LogInformation($">> [END] of {nameof(KafkaJdbcSourceConnector)}");
            return activityResult;
        }
    }
}
