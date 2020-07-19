
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using aZaaS.KafkaEventBus.Events;

namespace aZaaS.KafkaEventBus
{
    public interface IKafkaEventBus
    {
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;
    }
}
