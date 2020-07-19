using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aZaaS.KafkaEventBus.Events
{
    public interface IEvent
    {
        string Key { get; } 
    }
}
