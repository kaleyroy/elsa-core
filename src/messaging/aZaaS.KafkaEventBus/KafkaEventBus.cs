using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using aZaaS.KafkaEventBus.Events;

namespace aZaaS.KafkaEventBus
{
    public class KafkaEventBus : IKafkaEventBus
    {
        private readonly KafkaProducer _kafkaProducer;

        public KafkaEventBus(KafkaProducer kafkaProducer)
        {
            _kafkaProducer = kafkaProducer;
        }

        public Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
        {
            return _kafkaProducer.Produce(@event);
        }
    }
}
