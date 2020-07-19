using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aZaaS.KafkaEventBus.Events
{
    public interface IEventHandler<T> where T : IEvent
    {
        Task Handle(T @event);
    }
}
