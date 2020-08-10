using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Middleware
{
    public class RequestHandlerMiddleware<THandler> where THandler : IRequestHandler
    {
        private readonly RequestDelegate next;

        public RequestHandlerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, THandler handler)
        {
            var result = await handler.HandleRequestAsync();

            if (result != null && !httpContext.Response.HasStarted)
                await result.ExecuteResultAsync(httpContext, next);
        }


        //private async Task<bool> ValidateAccessTokenAsync(string accessToken)
        //{
        //    if (string.IsNullOrWhiteSpace(accessToken))
        //        throw new ArgumentNullException(nameof(accessToken));

        //    accessToken = accessToken.Replace("Bearer ", string.Empty);

        //    var client = new HttpClient();
        //    var response = await client.IntrospectTokenAsync(new TokenIntrospectionRequest
        //    {
        //        Address = configuration["AuthServer:Authority"],
        //        ClientId = configuration["Elsa:Introspect:ClientId"],
        //        ClientSecret = configuration["Elsa:Introspect:ClientSecret"],
        //        Token = accessToken
        //    });

        //    if (response.IsError)
        //        throw new Exception($"Introspect Token Error: {response.Error}");

        //    return response.IsActive;
        //}
    }
}