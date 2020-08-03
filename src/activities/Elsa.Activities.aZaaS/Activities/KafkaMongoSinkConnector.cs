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
        Description = "Creates mongodb sink connector for kafka connect.",
        RuntimeDescription = "x => !!x.state.connectorName ? `Connector Name: <strong>${ x.state.connectorName.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class KafkaMongoSinkConnector : Activity
    {
        private readonly KafkaApiWrapper _kafkaApi;
        private readonly ILogger<KafkaMongoSinkConnector> _logger;

        public KafkaMongoSinkConnector(IConfiguration configuration, ILogger<KafkaMongoSinkConnector> logger)
        {
            _logger = logger;
            _kafkaApi = new KafkaApiWrapper(configuration, logger);
        }


        [ActivityProperty(Hint = "The name of mongodb connector")]
        public IWorkflowExpression<string> ConnectorName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The kafka topic name of sink data")]
        public IWorkflowExpression<string> KafkaTopic
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The connection uri of mongodb")]
        public IWorkflowExpression<string> MongodbUri
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The database name of sink data")]
        public IWorkflowExpression<string> DatabaseName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The collection name of sink data")]
        public IWorkflowExpression<string> CollectionName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The max tasks of sink data, Default: 1")]
        public IWorkflowExpression<int?> MaxTasks
        {
            get => GetState(() => new WorkflowExpression<int?>(LiteralEvaluator.SyntaxName, "1"));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = null;
            _logger.LogInformation($">> [START] of {nameof(KafkaMongoSinkConnector)} ...");

            var connectorName = await context.EvaluateAsync(ConnectorName, cancellationToken);

            var kafkaTopic = await context.EvaluateAsync(KafkaTopic, cancellationToken);
            if (kafkaTopic == null)
                kafkaTopic = context.CurrentScope.GetVariable<string>("OutputTopic");

            var mongodbUri = await context.EvaluateAsync(MongodbUri, cancellationToken);
            var databaseName = await context.EvaluateAsync(DatabaseName, cancellationToken);
            var collectionName = await context.EvaluateAsync(CollectionName, cancellationToken);

            var maxTasks = await context.EvaluateAsync(MaxTasks, cancellationToken) ?? 1;

            // Set KafkaTopic to current scope variable
            context.CurrentScope.Variables.SetVariable(nameof(kafkaTopic), KafkaTopic);

            _logger.LogInformation($">> Creating kafka connector ...");
            var model = new KafkaMongoSinkConnectorModel(connectorName, kafkaTopic, mongodbUri, databaseName, collectionName, maxTasks: maxTasks);

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

            _logger.LogInformation($">> [END] of {nameof(KafkaMongoSinkConnector)}");
            return activityResult;
        }
    }
}
