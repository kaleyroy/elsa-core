using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aZaaS.KafkaEventBus.Events
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EventAttribute : Attribute
    {
        public string Topic { get; set; }
        public string GroupId { get; set; }

        public EventAttribute()
        {
        }
    }
}
