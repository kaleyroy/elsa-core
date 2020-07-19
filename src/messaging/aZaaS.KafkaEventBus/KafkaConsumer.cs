

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using aZaaS.KafkaEventBus.Events;

namespace aZaaS.KafkaEventBus
{
    public abstract class KafkaConsumer<T> : BackgroundService, IEventHandler<T> where T : IEvent
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KafkaConsumer<T>> _logger;
        //private readonly CancellationTokenSource _stoppingToken = new CancellationTokenSource();

        public KafkaConsumer(IConfiguration configuration, ILogger<KafkaConsumer<T>> logger)
        {
            _logger = logger;
            _configuration = configuration;

            //Console.CancelKeyPress += (_, e) =>
            //{
            //    e.Cancel = true; _stoppingToken.Cancel();
            //};
        }


        protected ILogger<KafkaConsumer<T>> Logger { get { return _logger; } }

        //protected abstract void Handle(T @event);

        public abstract Task Handle(T @event);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Factory.StartNew(async () =>
            {
                var attribute = typeof(T).GetCustomAttributes(typeof(EventAttribute), false).FirstOrDefault();
                var eventAttribute = (attribute ?? throw new InvalidOperationException(nameof(EventAttribute))) as EventAttribute;

                var topic = eventAttribute.Topic;
                var consumerConfig = new ConsumerConfig
                {
                    GroupId = eventAttribute.GroupId,
                    BootstrapServers = _configuration["Kafka:Consumer:BootstrapServers"],
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = true
                };

                using (var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build())
                {
                    consumer.Subscribe(topic);
                    _logger.LogInformation($"Subscribe Topic: ---- {typeof(T).Name}[{topic}] ----");

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var result = consumer.Consume(stoppingToken);
                            var @event = JsonConvert.DeserializeObject<T>(result.Value);

                            _logger.LogInformation($"Consuming Topic: ---- {typeof(T).Name}[{topic}] ---- / Group: {eventAttribute.GroupId} /");

                            await Handle(@event); //DoWork(@event); //consumer.StoreOffset(result);
                        }
                        catch (OperationCanceledException ex)
                        {
                            consumer.Close();
                            _logger.LogError(ex, "Kafka consumer is closed");
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError(ex, "Kafka consumer error");
                        }
                        catch (KafkaException ex)
                        {
                            _logger.LogError(ex, "Kafka consumer exception");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Kafka consumer exception (ERROR)");
                        }
                    }
                }
            }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Kafka Consumer for [{typeof(T).Name}] is starting ");
            return base.StartAsync(cancellationToken);
        }

        //public override void Dispose()
        //{
        //    base.Dispose();
        //    _stoppingToken.Cancel();
        //}
    }
}
