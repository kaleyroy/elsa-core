using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

using Newtonsoft.Json;

namespace Elsa.Activities.aZaaS
{
    public sealed class MongoApiWrapper
    {
        private readonly ILogger _logger;
        private readonly MongoServerCredential _serverCredential;

        public MongoApiWrapper(IConfiguration configuration, ILogger logger)
        {
            _logger = logger;
            _serverCredential = configuration.GetSection("MongoRestApiServer").Get<MongoServerCredential>();
        }

        public async Task<string> FilterCollectionAsync(MongoFilterModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var requestUri = $"{_serverCredential.Url}/{model.UriPath()}";

            using (var client = new HttpClient())
            {
                // Basic Authorization
                var bytes = Encoding.ASCII.GetBytes($"{_serverCredential.Username}:{_serverCredential.Password}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));

                var response = await client.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
        }

        public async Task<string> AggrsCollectionAsync(MongoAggregationModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var requestUri = $"{_serverCredential.Url}/{model.AggrsPath()}";
            if (model.Updated) //Update or create Aggregation Pipeline
            {
                var result = await CreateAggregationAsync(model);
                return JsonConvert.SerializeObject(new { Updated = result });
            }

            using (var client = new HttpClient())
            {
                // Basic Authorization
                var bytes = Encoding.ASCII.GetBytes($"{_serverCredential.Username}:{_serverCredential.Password}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));

                var response = await client.GetAsync(requestUri);

                // If Aggregation Pipeline not exits would create firstly
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var result = await CreateAggregationAsync(model);
                    return JsonConvert.SerializeObject(new { Created = result });
                }               

                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
        }

        public async Task<bool> CreateAggregationAsync(MongoAggregationModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var requestUri = $"{_serverCredential.Url}/{model.BasePath()}";

            using (var client = new HttpClient())
            {
                // Basic Authorization
                var bytes = Encoding.ASCII.GetBytes($"{_serverCredential.Username}:{_serverCredential.Password}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));

                var content = JsonConvert.SerializeObject(new
                {
                    aggrs = new List<dynamic>()
                    {
                        new
                        {
                            type = model.Type,
                            uri = model.Uri,
                            stages = model.Stages
                        }
                    }
                });
                var putContent = new StringContent(content, Encoding.UTF8, "application/json");

                _logger.LogInformation($"POST: {content}");

                var response = await client.PutAsync(requestUri, putContent);
                response.EnsureSuccessStatusCode();

                //var responseContent = await response.Content.ReadAsStringAsync();
                //var result = JsonConvert.DeserializeObject<KafkaConnectorResult>(responseContent);

                return true;
            }
        }

    }



    internal class MongoServerCredential
    {
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class MongoFilterModel
    {

        public MongoFilterModel(string database, string collection, string filter = null, int? page = null, int? pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentNullException(nameof(database));
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException(nameof(collection));

            Database = database;
            Collection = collection;

            Filter = filter;
            Page = page;
            PageSize = pageSize;
        }

        public string Database { get; set; }
        public string Collection { get; set; }

        public string Filter { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; } = 10;


        public string UriPath()
        {
            var path = $"{Database}/{Collection}";
            var parameters = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(Filter))
                parameters.Add("filter", Filter);
            if (Page != null && Page > 0)
                parameters.Add("page", Page.ToString());
            if (PageSize != null && PageSize > 0)
                parameters.Add("pagesize", PageSize.ToString());

            if (parameters.Any())
            {
                var queryString = string.Join("&", parameters.Select(item => $"{item.Key}={item.Value}"));
                path = $"{path}?{queryString}";
            }

            return path;
        }
    }

    public class MongoAggregationModel
    {

        public MongoAggregationModel(
            string database, string collection,
            string expression, string uri,
            int? page, int? pageSize, string variables = null,
            bool updated = false, string type = "pipeline")

        {
            Database = database;
            Collection = collection;
            Type = type;
            Uri = uri;
            Stages = JsonConvert.DeserializeObject<List<dynamic>>(expression);

            Page = page;
            PageSize = pageSize;
            Variables = variables;

            Updated = updated;
        }

        public string Database { get; set; }
        public string Collection { get; set; }
        public string Type { get; set; }
        public string Uri { get; set; }
        public List<dynamic> Stages { get; set; }

        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string Variables { get; set; }

        public bool Updated { get; set; }

        public string BasePath()
        {
            return $"{Database}/{Collection}";
        }
        public string AggrsPath()
        {
            if (string.IsNullOrWhiteSpace(Uri))
                throw new ArgumentNullException(nameof(Uri));

            var path = $"{BasePath()}/_aggrs/{Uri}";
            var parameters = new Dictionary<string, string>();

            if (Page != null && Page > 0)
                parameters.Add("page", Page.ToString());
            if (PageSize != null && PageSize > 0)
                parameters.Add("pagesize", PageSize.ToString());
            if (!string.IsNullOrWhiteSpace(Variables))
                parameters.Add("avars", Variables);

            if (parameters.Any())
            {
                var queryString = string.Join("&", parameters.Select(item => $"{item.Key}={item.Value}"));
                path = $"{path}?{queryString}";
            }

            return path;
        }
    }
}
