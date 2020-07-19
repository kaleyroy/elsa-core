using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using aZaaS.KafkaEventBus.Events;

namespace Sample16.Events
{
    [Event(GroupId = "elsa-subscriber", Topic = "sqoop-job-status")]
    public class SqoopJobStatus : IEvent
    {
        public string JobName { get; set; }
        public JobStatus JobStatus { get; set; }
        public string Remarks { get; set; }

        public string Key => string.Empty;

        public SqoopJobStatus() { }
        public SqoopJobStatus(string jobName, JobStatus jobStatus, string remarks = "")
        {
            JobName = jobName;
            JobStatus = jobStatus;
            Remarks = remarks;
        }
    }

    public enum JobStatus
    {
        None,
        Created,
        Started,
        Running,
        Completed,
        Error,
        Deleted

    }
}
