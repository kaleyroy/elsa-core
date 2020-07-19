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
                .AddActivity<SqoopIncrementalImportJob>()
                .AddActivity<SqoopJobScheduler>()
                .AddActivity<SparkAppWaiter>();
        }
    }
}
