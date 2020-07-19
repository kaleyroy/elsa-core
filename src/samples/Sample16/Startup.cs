using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Timers.Extensions;

using Elsa.Activities.aZaaS.Extensions;
using Elsa.Activities.Console.Extensions;
using Elsa.Dashboard.Extensions;

using Elsa.Persistence.MongoDb.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

using aZaaS.KafkaEventBus;

namespace Sample16
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var elsaSection = Configuration.GetSection("Elsa");

            var storageProvider = Configuration["StorageProvider"] ?? "MongoDb";

            if (storageProvider.Equals("MongoDb"))
                services.AddElsa(x => x.AddMongoDbStores(Configuration, "Elsa", "MongoDb"));
            else
            {
                var connectionString = Configuration["ConnectionStrings:SqlServer"];
                services.AddElsa(x => x.AddEntityFrameworkStores<SqlServerContext>(options => options.UseSqlServer(connectionString)));
            }


            services
                // Add workflow services.                

                //.AddElsa(x => x.AddMongoDbStores(Configuration, "Sample16", "MongoDb"))
                //.AddElsa(x => x.AddEntityFrameworkStores<SqlServerContext>(
                //        options => options.UseSqlServer(@"Server=127.0.0.1;Database=Elsa;User=sa;Password=sa;")))

                // Add activities we'd like to use.

                .AddaZaaSActivities()
                .AddHttpActivities(options => options.Bind(elsaSection.GetSection("Http")))
                .AddEmailActivities(options => options.Bind(elsaSection.GetSection("Smtp")))
                .AddTimerActivities(options => options.Bind(elsaSection.GetSection("BackgroundRunner")))
                .AddConsoleActivities()

                // Add Dashboard services.
                .AddElsaDashboard();

            services.AddKafkaEventBus();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app
                .UseStaticFiles()
                .UseHttpActivities()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapControllerRoute(
                           name: "default",
                           pattern: "{controller=Home}/{action=Index}/{id?}");
                })
                .UseWelcomePage();
        }
    }
}