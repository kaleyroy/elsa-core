
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Elsa.Activities.Http.Services;

namespace Elsa.Activities.Http.RequestHandlers.Results
{
    public class UnAuthrorizedResult : IRequestHandlerResult
    {
        public async Task ExecuteResultAsync(HttpContext httpContext, RequestDelegate next)
        {
            httpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
            await httpContext.Response.WriteAsync("Unauthorized - Http Activities");
        }
    }
}
