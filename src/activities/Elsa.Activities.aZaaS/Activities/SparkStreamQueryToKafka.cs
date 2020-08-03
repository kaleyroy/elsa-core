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
        Description = "Executes spark stream query on kafka topic and exports result back to kafka.",
        RuntimeDescription = "x => !!x.state.appName ? `App Name: <strong>${ x.state.appName.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SparkStreamQueryToKafka : Activity
    {
        private SparkApiWrapper _sparkApi;
        private readonly string _bootstrapServers;
        private ILogger<SparkStreamQueryToKafka> _logger;

        public SparkStreamQueryToKafka(IConfiguration configuration, ILogger<SparkStreamQueryToKafka> logger)
        {
            _logger = logger;
            _sparkApi = new SparkApiWrapper(configuration, logger);
            _bootstrapServers = configuration["KafkaBootstrapServers"] ?? "kafka:9092";
        }


        [ActivityProperty(Hint = "The names of spark app")]
        public IWorkflowExpression<string> AppName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        //[ActivityProperty(Hint = "The server names of kafka broker")]
        //public IWorkflowExpression<string> BootstrapServers
        //{
        //    get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
        //    set => SetState(value);
        //}

        [ActivityProperty(Hint = "The input topic name for stream data")]
        public IWorkflowExpression<string> InputTopic
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The ddl schema string of steam data")]
        public IWorkflowExpression<string> StreamSchema
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The temp view name of stream data, Default: stream")]
        public IWorkflowExpression<string> StreamName
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The timestamp colum format string, E.g: OldColumnName|yyyy-MM-dd|NewColumnName, ...")]
        public IWorkflowExpression<string> TimestampColumnFormat
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The spark sql query of stream data, Aka: Calculation")]
        public IWorkflowExpression<string> StreamQuery
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The output topic name of stream result data")]
        public IWorkflowExpression<string> OutputTopic
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }


        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            ActivityExecutionResult activityResult = null;
            _logger.LogInformation($">> [START] of {nameof(SparkStreamQueryToKafka)} ...");

            var appName = await context.EvaluateAsync(AppName, cancellationToken);

            //var bootstrapServers = await context.EvaluateAsync(BootstrapServers, cancellationToken);
            var inputTopic = await context.EvaluateAsync(InputTopic, cancellationToken);

            // If not specified would try to read from previous activity (E.g KafkaSourceConnector)
            if (inputTopic == null)
                inputTopic = context.CurrentScope.GetVariable<string>("KafkaTopic");

            var streamSchema = await context.EvaluateAsync(StreamSchema, cancellationToken);
            var streamName = await context.EvaluateAsync(StreamName, cancellationToken);
            var timestampColumnFormat = await context.EvaluateAsync(TimestampColumnFormat, cancellationToken);
            var streamQuery = await context.EvaluateAsync(StreamQuery, cancellationToken);
            var outputTopic = await context.EvaluateAsync(OutputTopic, cancellationToken);

            // Set OutputTopic to current scope variable
            context.CurrentScope.SetVariable(nameof(OutputTopic), OutputTopic);

            _logger.LogInformation($">> Creating spark app ...");
            var model = new SparkStreamQueryToKafkaAppModel(appName, _bootstrapServers, inputTopic, streamSchema, streamName, timestampColumnFormat, streamQuery, outputTopic);

            try
            {
                activityResult = Done();

                _logger.LogInformation($">> Posting spark app (API) ...");
                var result = await _sparkApi.CreateSparkAppAsync(model);
                _logger.LogInformation($">> Post result: {result}");

                // Set workflow CorrelationId as AppName
                context.Workflow.CorrelationId = result.AppId.ToString();
                if (!result.Status)
                    activityResult = new FaultWorkflowResult("Spark api status -> false");
            }
            catch (Exception ex) { activityResult = new FaultWorkflowResult(ex.Message); }

            _logger.LogInformation($">> [END] of {nameof(SparkStreamQueryToKafka)}");
            return activityResult;
        }

    }
}
