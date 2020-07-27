using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger _logger;

        public SparkApiWrapper(IConfiguration configuration, ILogger logger)
        {
            _logger = logger;
            _apiServer = configuration["SparkApiServer"];
            _apiClient = configuration.GetSection("ApiClient").Get<ApiClient>();

        }

        public async Task<SparkAppResult> CreateSparkAppAsync(SparkAppModel model)
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

                _logger.LogInformation($"POST: {postJson}");

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
        public Dictionary<string, string> Properties { get; set; }


        public SparkAppModel(string appName, string fileName, bool start = true, string description = "desc")
        {
            AppName = appName;
            FileName = fileName;
            Start = start;
            Description = description;
            Arguments = new List<dynamic>();
            Properties = new Dictionary<string, string>();
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


    public class SentimentAnalysisAppModel : SparkAppModel
    {
        public SentimentAnalysisAppModel(
            string appName, string modelFile,
            string jdbcUrl, string inputTable, string inputColumn, string labelColumn,
            string outputColumns, string outputTable,
            bool start = true, string description = "desc")
            : base(appName, "SentimentAnalysisApp.zip", start, description)
        {
            AddArgument("--model-file", modelFile);
            AddArgument("--jdbc-url", jdbcUrl);
            AddArgument("--input-table", inputTable);
            AddArgument("--input-column", inputColumn);

            if (!string.IsNullOrWhiteSpace(labelColumn))
                AddArgument("--label-Column", labelColumn);
            if (!string.IsNullOrWhiteSpace(outputColumns))
                AddArgument("--output-columns", outputColumns);

            AddArgument("--output-table", outputTable);

            Properties = new Dictionary<string, string>()
            {
                {"spark.files","hdfs://master:9000/publish/linux/Apache.Arrow.dll,hdfs://master:9000/publish/linux/aZaaS.Spark.dll,hdfs://master:9000/publish/linux/IdentityModel.dll,hdfs://master:9000/publish/linux/Microsoft.ML.Core.dll,hdfs://master:9000/publish/linux/Microsoft.ML.CpuMath.dll,hdfs://master:9000/publish/linux/Microsoft.ML.Data.dll,hdfs://master:9000/publish/linux/Microsoft.ML.DataView.dll,hdfs://master:9000/publish/linux/Microsoft.ML.KMeansClustering.dll,hdfs://master:9000/publish/linux/Microsoft.ML.PCA.dll,hdfs://master:9000/publish/linux/Microsoft.ML.StandardTrainers.dll,hdfs://master:9000/publish/linux/Microsoft.ML.Transforms.dll,hdfs://master:9000/publish/linux/Microsoft.Spark.dll,hdfs://master:9000/publish/linux/Microsoft.Win32.Registry.dll,hdfs://master:9000/publish/linux/Newtonsoft.Json.dll,hdfs://master:9000/publish/linux/Razorvine.Pyrolite.dll,hdfs://master:9000/publish/linux/Razorvine.Serpent.dll,hdfs://master:9000/publish/linux/SentimentAnalysisApp.dll,hdfs://master:9000/publish/linux/System.CodeDom.dll,hdfs://master:9000/publish/linux/System.CommandLine.dll,hdfs://master:9000/publish/linux/System.CommandLine.DragonFruit.dll,hdfs://master:9000/publish/linux/System.CommandLine.Rendering.dll,hdfs://master:9000/publish/linux/System.Data.SqlClient.dll,hdfs://master:9000/publish/linux/System.Diagnostics.DiagnosticSource.dll,hdfs://master:9000/publish/linux/System.Runtime.CompilerServices.Unsafe.dll,hdfs://master:9000/publish/linux/System.Security.AccessControl.dll,hdfs://master:9000/publish/linux/System.Security.Principal.Windows.dll,hdfs://master:9000/publish/linux/System.Text.Encoding.CodePages.dll,hdfs://master:9000/publish/linux/System.Threading.Channels.dll,hdfs://master:9000/publish/linux/libCpuMathNative.so,hdfs://master:9000/publish/linux/libLdaNative.so" }
            };
        }
    }

    public class SentimentAnalysisTrainerModel : SparkAppModel
    {
        public SentimentAnalysisTrainerModel(
            string appName, string jdbcUrl, string inputTable,
            string inputColumn, string labelColumn,string modelFile,
            bool start = true, string description = "desc")
            : base(appName, "SentimentAnaylysisTrainer.zip", start, description)
        {
            AddArgument("--jdbc-url", jdbcUrl);
            AddArgument("--input-table", inputTable);
            AddArgument("--input-column", inputColumn);
            AddArgument("--label-Column", labelColumn);

            AddArgument("--model-file", modelFile);
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
