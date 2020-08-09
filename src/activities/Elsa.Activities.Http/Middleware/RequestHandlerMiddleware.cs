using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace Elsa.Activities.Http.Middleware
{
    public class RequestHandlerMiddleware<THandler> where THandler : IRequestHandler
    {
        private readonly RequestDelegate next;
        private readonly IConfiguration configuration;
        private readonly bool authorizationEnabled = true;

        public RequestHandlerMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            this.next = next;
            this.configuration = configuration;
            bool.TryParse(configuration["Elsa:Introspect:Enabled"], out authorizationEnabled);
        }

        public async Task InvokeAsync(HttpContext httpContext, THandler handler)
        {
            var accessToken = httpContext.Request.Headers[HeaderNames.Authorization].ToString();
            if (authorizationEnabled 
                && (string.IsNullOrWhiteSpace(accessToken) || !await ValidateAccessTokenAsync(accessToken)))
            {
                httpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                await httpContext.Response.WriteAsync("Unauthorized");
                return;
            }

            var result = await handler.HandleRequestAsync();

            if (result != null && !httpContext.Response.HasStarted)
                await result.ExecuteResultAsync(httpContext, next);
        }


        private async Task<bool> ValidateAccessTokenAsync(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentNullException(nameof(accessToken));

            accessToken = accessToken.Replace("Bearer ", string.Empty);

            var client = new HttpClient();
            var response = await client.IntrospectTokenAsync(new TokenIntrospectionRequest
            {
                Address = configuration["AuthServer:Authority"],
                ClientId = configuration["Elsa:Introspect:ClientId"],
                ClientSecret = configuration["Elsa:Introspect:ClientSecret"],
                Token = accessToken
            });

            if (response.IsError)
                throw new Exception($"Introspect Token Error: {response.Error}");

            return response.IsActive;
        }
    }
}