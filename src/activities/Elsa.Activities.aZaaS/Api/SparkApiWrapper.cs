using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.aZaaS
{
    /// <summary>
    /// Spark application utils
    /// </summary>
    public sealed class SparkApiWrapper
    {
        const string SparkApiName = "sparkApiService";

        private readonly string _apiServer;
        private readonly ApiClient _apiClient;

        public SparkApiWrapper(IConfiguration configuration)
        {
            _apiServer = configuration["SparkApiServer"];
            _apiClient = configuration.GetSection("ApiClient").Get<ApiClient>();

        }

        public async Task<SparkAppResult> CreateHdfsQueryAppAsync(SparkAppModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var requestUri = $"{_apiServer}/api/spark/App/create";
            var accessToken = await AccessToken.GetTokenAsync(_apiClient, SparkApiName);

            using (var client = new HttpClient())
            {
                client.SetBearerToken(accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var postJson = JsonConvert.SerializeObject(model);
                var postContent = new StringContent(postJson, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(requestUri, postContent);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<SparkAppResult>(responseContent);

                return result;
            }
        }

    }

    public abstract class SparkAppModel
    {
        public string AppName { get; set; }
        public string FileName { get; set; }
        public List<dynamic> Arguments { get; set; }
        public bool Start { get; set; } = true;
        public string Description { get; set; } = "desc";


        public SparkAppModel(string appName, string fileName, bool start = true, string description = "desc")
        {
            AppName = appName;
            FileName = fileName;
            Start = start;
            Description = description;
            Arguments = new List<dynamic>();
        }


        protected void AddArgument(string name, string value)
        {
            Arguments.Add(new { Name = name, Value = value });
        }
    }

    public class HdfsQueryToTableAppModel : SparkAppModel
    {
        public HdfsQueryToTableAppModel(
            string appName, string fileType, string filePath,
            string schemaString, string tempTable, string sqlQuery, string jdbcUrl, string exportTable,
            bool start = true, string description = "desc")
            : base(appName, "HdfsQueryApp.zip", start, description)
        {
            AddArgument("--file-type", fileType);
            AddArgument("--file-path", filePath);
            if (!string.IsNullOrWhiteSpace(schemaString))
                AddArgument("--schema-string", schemaString);
            AddArgument("--temp-table", tempTable);
            AddArgument("--sql-query", sqlQuery);
            AddArgument("--export-type", "table");
            AddArgument("--jdbc-url", jdbcUrl);
            AddArgument("--export-table", exportTable);

            AddArgument("--total-records", "True");
        }
    }
    public class HdfsQueryToJsonAppModel : SparkAppModel
    {
        public HdfsQueryToJsonAppModel(
            string appName, string fileType, string filePath,
            string schemaString, string tempTable, string sqlQuery, string exportFile,
            bool start = true, string description = "desc")
            : base(appName, "HdfsQueryApp.zip", start, description)
        {
            AddArgument("--file-type", fileType);
            AddArgument("--file-path", filePath);
            if (!string.IsNullOrWhiteSpace(schemaString))
                AddArgument("--schema-string", schemaString);
            AddArgument("--temp-table", tempTable);
            AddArgument("--sql-query", sqlQuery);
            AddArgument("--export-type", "json");
            AddArgument("--export-file", exportFile);

            AddArgument("--total-records", "True");
        }
    }

    public class SparkAppResult
    {
        public bool Status { get; set; }
        public Guid AppId { get; set; }

        public override string ToString()
        {
            return $"Status: {Status}, AppId: {AppId}";
        }
    }
}
