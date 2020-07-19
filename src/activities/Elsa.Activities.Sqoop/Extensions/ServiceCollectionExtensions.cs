using Elsa.Activities.Sqoop.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Sqoop.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSqoopActivities(this IServiceCollection services)
        {
            return services
                .AddActivity<ImportJob>()
                .AddActivity<ExecuteJob>()
                .AddActivity<Describe>();
        }
    }
}
