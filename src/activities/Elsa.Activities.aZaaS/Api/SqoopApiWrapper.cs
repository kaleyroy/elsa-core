
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
    public sealed class SqoopApiWrapper
    {
        const string SqoopApiName = "sqoopApiService";

        private readonly string _apiServer;
        private readonly ApiClient _apiClient;

        public SqoopApiWrapper(IConfiguration configuration)
        {
            _apiServer = configuration["SqoopApiServer"];
            _apiClient = configuration.GetSection("ApiClient").Get<ApiClient>();
        }

        public async Task<bool> CreateImportJobAsync(ImportJob importJob)
        {
            if (string.IsNullOrWhiteSpace(_apiServer))
                throw new ArgumentNullException(nameof(_apiServer));
            if (importJob == null)
                throw new ArgumentNullException(nameof(importJob));

            var requestUri = $"{_apiServer}/api/sqoop/Import/create";
            var accessToken = await AccessToken.GetTokenAsync(_apiClient, SqoopApiName);

            using (var client = new HttpClient())
            {
                client.SetBearerToken(accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var postJson = JsonConvert.SerializeObject(importJob);
                var postContent = new StringContent(postJson, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(requestUri, postContent);

                //TODO: Logging the response
                return response.IsSuccessStatusCode;
            }
        }

        public async Task<bool> ExecuteJobAsync(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName))
                throw new ArgumentNullException(nameof(jobName));

            var requestUri = $"{_apiServer}/api/sqoop/Import/execute?name={jobName}";
            var accessToken = await AccessToken.GetTokenAsync(_apiClient, SqoopApiName);

            using (var client = new HttpClient())
            {
                client.SetBearerToken(accessToken);

                var postContent = new StringContent(string.Empty);
                var response = await client.PostAsync(requestUri, postContent);

                //TODO: Logging the response
                return response.IsSuccessStatusCode;
            }
        }

        public async Task<bool> ScheduleJobAsync(string jobName, string schedule)
        {
            if (string.IsNullOrWhiteSpace(jobName))
                throw new ArgumentNullException(nameof(jobName));
            if (string.IsNullOrWhiteSpace(schedule))
                throw new ArgumentNullException(nameof(schedule));

            var requestUri = $"{_apiServer}/api/sqoop/Import/schedule?name={jobName}&schedule={schedule}";
            var accessToken = await AccessToken.GetTokenAsync(_apiClient, SqoopApiName);

            using (var client = new HttpClient())
            {
                client.SetBearerToken(accessToken);

                var postContent = new StringContent(string.Empty);
                var response = await client.PostAsync(requestUri, postContent);

                //TODO: Logging the response
                return response.IsSuccessStatusCode;
            }
        }
    }


    public class ImportJob
    {
        public ImportJob()
        {
            Mapper = 1;
            Description = "desc";
        }

        public ImportJob(string name, string jdbcUrl, string userName, string password, string tableName, int mapper = 1) : this()
        {
            Name = name;
            JdbcUrl = jdbcUrl;
            UserName = userName;
            Password = password;
            TableName = tableName;
            Mapper = mapper <= 0 ? 1 : mapper;
        }

        public string Name { get; set; }
        public string JdbcUrl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string TableName { get; set; }


        public string FilePath { get; set; }
        public string FileFormat { get; set; }

        public string CheckColumn { get; set; }
        public string Incremental { get; set; }
        public string InitialValue { get; set; }
        public string MergeColumn { get; set; }

        public string HiveDbName { get; set; }
        public string HiveTableName { get; set; }

        public string Description { get; set; }

        public int Mapper { get; set; }
        public int MapMemoryMb { get; set; }
        public int ReduceMemoryMb { get; set; }
        public int ChildJavaOptsMb { get; set; }
        public int TaskIOSortMb { get; set; }


        public ImportJob TargetPath(string path)
        {
            FilePath = path;

            return this;
        }
        public ImportJob UseIncremental(string checkColumn, string incremental, string initialValue, string mergeColumn = "")
        {
            CheckColumn = checkColumn;
            Incremental = incremental;
            InitialValue = initialValue;
            MergeColumn = mergeColumn;

            return this;
        }

        public ImportJob ToCsvFile()
        {
            FileFormat = "csv";

            return this;
        }
        public ImportJob ToParquetFile()
        {
            FileFormat = "parquet";

            return this;
        }
        public ImportJob ToOrcFile(string dbName, string tableName)
        {
            FileFormat = "orc";
            HiveDbName = dbName;
            HiveTableName = tableName;

            return this;
        }

    }
}
