using System;

using Newtonsoft.Json;
using Confluent.Kafka;

namespace KafkaProducer
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            var topicName = "banana-topic"; //"test-topic";
            var bootstrapServers = "kafka:9092"; //,kafka-2:29092

            var config = new ProducerConfig(new ClientConfig()
            {
                BootstrapServers = bootstrapServers
            });

            //Action<DeliveryReport<Null, string>> handler = r =>
            //   Console.WriteLine(!r.Error.IsError
            //   ? $"Delivered message to {r.TopicPartitionOffset}"
            //   : $"Delivery Error: {r.Error.Reason}");

            while (true)
            {
                Console.WriteLine("Which type message to publish?");
                Console.WriteLine(">>   Sqoop = 1, Spark = 2");
                var input = Console.ReadLine();
                if (input.ToLower().Equals("exit"))
                    break;

                var activityType = 1;
                var correlationId = string.Empty;
                var jsonData = string.Empty;
                if (!int.TryParse(input, out activityType))
                    Console.WriteLine("Invalid message type!");
                else
                {
                    if (activityType == 1)
                    {
                        topicName = "sqoop-job-status";
                        Console.WriteLine("Message type: Sqoop");
                        Console.WriteLine($"Topic name: {topicName}");
                    }
                    else if (activityType == 2)
                    {
                        topicName = "spark-app-status";
                        Console.WriteLine("Message type: Spark");
                        Console.WriteLine($"Topic name: {topicName}");
                    }
                    else
                    {
                        Console.WriteLine("Unidentified message type!");
                        continue;
                    }
                }
                Console.WriteLine("Input workflow correlationId");
                correlationId = Console.ReadLine();
                Console.WriteLine($"Input correlationId: {correlationId}");

                using (var producer = new ProducerBuilder<string, string>(config).Build())
                {
                    //var topicPartion = new TopicPartition(topicName,new Partition(2));
                    switch (activityType)
                    {
                        case 1:
                            jsonData = JsonConvert.SerializeObject(new { JobName = correlationId, JobStatus = 4, Remarks = string.Empty });
                            break;
                        case 2:
                            jsonData = JsonConvert.SerializeObject(new { AppId = correlationId, AppStatus = 5 });
                            break;
                    }

                    Console.WriteLine($"Sending CorrelationId: {correlationId} -> Data: {jsonData}");

                    await producer.ProduceAsync(topicName, new Message<string, string> { Key = string.Empty, Value = jsonData });
                    producer.Flush(TimeSpan.FromSeconds(10));

                    Console.WriteLine($"CorrelationId: {correlationId} Sent!");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("Messages have been sent!");
        }
    }
}
