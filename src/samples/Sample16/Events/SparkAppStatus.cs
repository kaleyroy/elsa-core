
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using aZaaS.KafkaEventBus.Events;

namespace Sample16.Events
{
    [Event(GroupId = "elsa-subscriber", Topic = "spark-app-status")]
    public class SparkAppStatus : IEvent
    {
        public SparkAppStatus() { }
        public SparkAppStatus(Guid appId, AppStatus appStatus)
        {
            AppId = appId;
            AppStatus = appStatus;
        }

        public Guid AppId { get; set; }
        public AppStatus AppStatus { get; set; }

        public string Key => AppId.ToString();
    }

    public enum AppStatus
    {
        None,
        Created,
        Started,
        Running,
        Exporting,
        Completed,
        Error,
        Deleted
    }
}
