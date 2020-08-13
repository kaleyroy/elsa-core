using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Timers.Extensions;

using Elsa.Activities.aZaaS.Extensions;
using Elsa.Activities.Console.Extensions;
using Elsa.Dashboard.Extensions;

using Elsa.Dashboard.Options;
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
using IdentityModel;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Sample16
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            HostEnvironment = hostEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostEnvironment { get; }

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
                .AddaZaaSActivities()
                .AddHttpActivities(options => options.Bind(elsaSection.GetSection("Http")))
                .AddEmailActivities(options => options.Bind(elsaSection.GetSection("Smtp")))
                //.AddTimerActivities(options => options.Bind(elsaSection.GetSection("BackgroundRunner")))
                .AddConsoleActivities()

                // Add Dashboard services.
                .AddElsaDashboard();

            services.AddKafkaEventBus();

            //services.Configure<CookiePolicyOptions>(options =>
            //{
            //    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            //    options.CheckConsentNeeded = context => true;
            //    options.MinimumSameSitePolicy = SameSiteMode.None;
            //});

            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies")
            .AddOpenIdConnect("oidc", options =>
            {
                options.SignInScheme = "Cookies";
                options.Authority = Configuration["AuthServer:Authority"];
                options.ClientId = Configuration["AuthServer:ClientId"];
                options.ClientSecret = Configuration["AuthServer:ClientSecret"];

                options.RequireHttpsMetadata = false;
                options.ResponseType = OpenIdConnectResponseType.CodeIdToken;

                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add("phone");
                options.Scope.Add("elsaHttpService");
                //foreach (var item in customApiResources)
                //    options.Scope.Add(item.Id);
                options.Scope.Add("offline_access");

                options.SaveTokens = true;

                // Don't enable the UserInfoEndpoint, otherwise this may happen
                // An error was encountered while handling the remote login. ---> System.Exception: Unknown response type: text/html
                options.GetClaimsFromUserInfoEndpoint = false;

                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                {
                    NameClaimType = JwtClaimTypes.Name
                };
                options.Events.OnSignedOutCallbackRedirect += context =>
                {
                    context.Response.Redirect(context.Options.SignedOutRedirectUri);
                    context.HandleResponse();

                    return Task.CompletedTask;
                };
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app
                .UseStaticFiles()
                .UseHttpActivities()
                .UseRouting()
                .UseAuthentication()
                .UseAuthorization()
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