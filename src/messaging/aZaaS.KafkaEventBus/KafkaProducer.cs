using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using aZaaS.KafkaEventBus.Events;

namespace aZaaS.KafkaEventBus
{
    public class KafkaProducer
    {
        private readonly ILogger<KafkaProducer> _logger;
        private readonly IConfiguration _configuration;
        private readonly bool _producerEnabled = true;

        public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _producerEnabled = bool.Parse(_configuration["Kafka:Producer:Enabled"]);
        }

        public Task Produce(IEvent @event)
        {
            if(!_producerEnabled)
            {
                _logger.LogWarning($"Kafka Producer is disabled, ignoring the operation ...");
                return Task.CompletedTask;
            }

            var attribute = @event.GetType().GetCustomAttributes(typeof(EventAttribute), false).FirstOrDefault();
            var eventAttribute = (attribute ?? throw new InvalidOperationException(nameof(EventAttribute))) as EventAttribute;

            var topic = eventAttribute.Topic;
            _logger.LogInformation($"Produce Topic: ---- {@event.GetType().Name}[{topic}] ----");

            var producerConfig = new ProducerConfig
            { 
                BootstrapServers = _configuration["Kafka:Producer:BootstrapServers"]
            };
            using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
            {
                try
                {
                    var jsonData = JsonConvert.SerializeObject(@event);
                    var dr = producer.ProduceAsync(topic, new Message<string, string> { Key = @event.Key, Value = jsonData }).GetAwaiter().GetResult();

                    _logger.LogInformation("Event Data: {0} , Partition: {1} => SUCCESS", dr.Value, dr.TopicPartitionOffset);
                }
                catch (ProduceException<string, string> ex)
                {
                    _logger.LogError(ex, "Event: {0} failed，Reason: {1} ", topic, ex.Error.Reason);
                }
            }

            return Task.CompletedTask;
        }
    }
}
