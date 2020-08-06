using Elsa.Activities.aZaaS.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.aZaaS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddaZaaSActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<SqoopImportJob>()
                .AddActivity<SqoopJobExecutor>()
                .AddActivity<SqoopJobWaiter>()
                .AddActivity<SparkHdfsQueryToTable>()
                .AddActivity<SparkStreamQueryToKafka>()
                .AddActivity<SentimentAnalysisModel>()
                .AddActivity<SentimentAnalysisTrainer>()
                .AddActivity<SqoopIncrementalImportJob>()
                .AddActivity<SqoopJobScheduler>()
                .AddActivity<SparkAppWaiter>()
                .AddActivity<KafkaJdbcSourceConnector>()
                .AddActivity<KafkaMongoSinkConnector>()
                .AddActivity<MongoFilterToHttpResponse>()
                .AddActivity<MongoAggregationToHttpResponse>();
        }
    }
}
