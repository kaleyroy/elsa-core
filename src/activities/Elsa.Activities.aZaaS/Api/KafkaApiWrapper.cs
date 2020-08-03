using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.aZaaS
{
    public sealed class KafkaApiWrapper
    {
        private readonly ILogger _logger;
        private readonly string _connectApiServer;

        public KafkaApiWrapper(IConfiguration configuration, ILogger logger)
        {
            _logger = logger;
            _connectApiServer = configuration["KafkaConnectApiServer"];
        }

        public async Task<KafkaConnectorResult> CreateConnectorAsync(KafkaConnectorModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var requestUri = $"{_connectApiServer}/connectors";
            //var accessToken = await AccessToken.GetTokenAsync(_apiClient, SparkApiName);

            using (var client = new HttpClient())
            {
                //client.SetBearerToken(accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var postJson = JsonConvert.SerializeObject(model);
                var postContent = new StringContent(postJson, Encoding.UTF8, "application/json");

                _logger.LogInformation($"POST: {postJson}");

                var response = await client.PostAsync(requestUri, postContent);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<KafkaConnectorResult>(responseContent);

                return result;
            }
        }



    }

    public class KafkaConnectorModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("config")]
        public Dictionary<string, string> Properties { get; set; }

        public KafkaConnectorModel()
        {
            Properties = new Dictionary<string, string>();
        }
        public KafkaConnectorModel(string name) : this()
        {
            Name = name;
        }

        protected KafkaConnectorModel SetProperty(string name, string value)
        {
            Properties[name] = value;
            return this;
        }
    }

    public class KafkaJdbcSourceConnectorModel : KafkaConnectorModel
    {
        public KafkaJdbcSourceConnectorModel(
            string name, string jdbcUrl, string userName, string password, string sqlQuery,
            string kafkaTopic, string timestampColumn, string mode = "timestamp", int pollIntervalMs = 5000, int maxTasks = 1)
            : base(name)
        {
            SetProperty("connection.url", jdbcUrl);
            SetProperty("connection.user", userName);
            SetProperty("connection.password", password);

            SetProperty("query", sqlQuery);
            SetProperty("topic.prefix", kafkaTopic);

            SetProperty("connector.class", "io.confluent.connect.jdbc.JdbcSourceConnector");
            SetProperty("numeric.mapping", "best_fit");
            SetProperty("validate.non.null", "false");
            SetProperty("value.converter.schemas.enable", "false");
            SetProperty("value.converter", "org.apache.kafka.connect.json.JsonConverter");

            SetProperty("mode", mode);
            SetProperty("timestamp.column.name", timestampColumn);
            SetProperty("tasks.max", maxTasks.ToString());

            SetProperty("poll.interval.ms", pollIntervalMs.ToString());
        }
    }

    public class KafkaMongoSinkConnectorModel : KafkaConnectorModel
    {
        public KafkaMongoSinkConnectorModel(
            string name, string kafkaTopic, string mongodbUri, string databaseName, string collectionName, int maxTasks = 1)
            : base(name)
        {
            SetProperty("connector.class", "com.mongodb.kafka.connect.MongoSinkConnector");
            SetProperty("connection.uri", mongodbUri);
            SetProperty("topics", kafkaTopic);
            SetProperty("database", databaseName);
            SetProperty("collection", collectionName);

            SetProperty("max.num.retries", "3");
            SetProperty("retries.defer.timeout", "5000");
            SetProperty("tasks.max", maxTasks.ToString());

            SetProperty("key.converter", "org.apache.kafka.connect.json.JsonConverter");
            SetProperty("key.converter.schemas.enable", "false");
            SetProperty("value.converter", "org.apache.kafka.connect.json.JsonConverter");
            SetProperty("value.converter.schemas.enable", "false");

            SetProperty("delete.on.null.values", "false");
            SetProperty("document.id.strategy", "com.mongodb.kafka.connect.sink.processor.id.strategy.ProvidedInValueStrategy");
            SetProperty("post.processor.chain", "com.mongodb.kafka.connect.sink.processor.DocumentIdAdder");
            SetProperty("writemodel.strategy", "com.mongodb.kafka.connect.sink.writemodel.strategy.ReplaceOneDefaultStrategy");
        }
    }

    public class KafkaConnectorResult : KafkaConnectorModel
    {
        [JsonProperty("tasks")]
        public List<KafkaConnectorTask> Tasks { get; set; }
    }
    public class KafkaConnectorTask
    {
        [JsonProperty("connector")]
        public string Connector { get; set; }

        [JsonProperty("task")]
        public string Task { get; set; }
    }
}
