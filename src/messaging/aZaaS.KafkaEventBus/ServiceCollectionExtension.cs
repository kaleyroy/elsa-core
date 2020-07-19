using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using aZaaS.KafkaEventBus.Events;

namespace aZaaS.KafkaEventBus
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddKafkaEventBus(this IServiceCollection services)
        {
            services.AddTransient<KafkaProducer>();
            foreach (Type mType in typeof(IEvent).GetAssemblies())
            {
                var hTypes = typeof(IEventHandler<>).GetMakeGenericType(mType);
                foreach (var hType in hTypes)
                    services.AddSingleton(typeof(IHostedService), hType);                
            }

            services.AddSingleton<IKafkaEventBus, KafkaEventBus>();

            return services;
        }
    }
}
