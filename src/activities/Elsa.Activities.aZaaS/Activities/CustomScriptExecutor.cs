using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis.Scripting;
using Elsa.Activities.aZaaS.Models;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace Elsa.Activities.aZaaS.Activities
{

    [ActivityDefinition(
        Category = "aZaaS",
        Description = "Executes custom cshare code by scripting api.",
        RuntimeDescription = "x => !!x.state.connectorName ? `Connector Name: <strong>${ x.state.connectorName.expression }</strong>.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class CustomScriptExecutor : Activity
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<MongoFilterToHttpResponse> _logger;

        string[] defaultNamespaces = new string[] {
            "System",
            "System.IO",
            "System.Linq",
            "System.Collections.Generic",
            "Newtonsoft.Json",
            "Elsa.Activities.aZaaS.Models" };

        public CustomScriptExecutor(
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache memoryCache,
            ILogger<MongoFilterToHttpResponse> logger)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
        }

        [ActivityProperty(Hint = "The import namespaces, Eg: System,System.IO ...")]
        public IWorkflowExpression<string> Imports
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "The custom code editor")]
        [ExpressionOptions(Multiline = true)]
        public IWorkflowExpression<string> CodeEditor
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            var activityResult = Done();
            var response = _httpContextAccessor.HttpContext.Response;
            _logger.LogInformation($">> [START] of {nameof(CustomScriptExecutor)} ...");

            //var lastResult = context.CurrentScope.LastResult;
            //var inputs = lastResult.Value as Dictionary<string, object>;
            //if (inputs == null)
            //    return Fault("Invalid last result conversion (target type: Dictionary<string, object>)");

            var code = await context.EvaluateAsync(CodeEditor, cancellationToken) ?? string.Empty;
            var imports = await context.EvaluateAsync(Imports, cancellationToken) ?? string.Empty;

            //if (string.IsNullOrWhiteSpace(code))
            //    return activityResult;

            if (string.IsNullOrWhiteSpace(code))
                code = @" ;
                var items = JsonConvert.DeserializeObject<List<dynamic>>(Inputs[""aggrs_result""] as string);
                var totals = items.Select(m => m._id).ToList();
                var result = new { user_profile = ""this is user profile"" , products = totals};
                var json = JsonConvert.SerializeObject(result);
                json ";

            var codeHashKey = GetStringHash(code);
            var inputs = new Dictionary<string, object>();
            foreach (var item in context.CurrentScope.Variables)
                inputs.Add(item.Key, item.Value.Value);

            var globalModel = new GlobalModel() { Inputs = inputs };
            var codeScript = _memoryCache.Get(codeHashKey) as Script<object>;
            if (codeScript == null)
            {
                // References & Imports
                var options = ScriptOptions.Default
                    .WithReferences(
                        typeof(JsonConvert).Assembly,
                        typeof(GlobalModel).Assembly,
                        typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly);

                var namespaces = new HashSet<string>(defaultNamespaces);
                var inputNamespaces = imports.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                inputNamespaces.ToList().ForEach(item => namespaces.Add(item));
                options = options.WithImports(namespaces);

                // Inputs & Compile
                codeScript = CSharpScript.Create(code, options: options, globalsType: typeof(GlobalModel));
                var info = codeScript.Compile();
                _memoryCache.Set(codeHashKey, codeScript);
            }

            try
            {
                // Execution
                var codeResult = (await codeScript.RunAsync(globalModel)).ReturnValue;

                // Write HttpResponse
                if (response.HasStarted)
                    return Fault("Response has already started");

                response.StatusCode = 200;
                response.ContentType = "application/json";
                if (codeResult != null)
                    await response.WriteAsync(codeResult.ToString(), cancellationToken);
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ContentType = "text/plain";
                await response.WriteAsync(ex.ToString());

                activityResult = new FaultWorkflowResult(ex.Message);
            }

            _logger.LogInformation($">> [END] of {nameof(CustomScriptExecutor)}");
            return activityResult;
        }


        private string GetStringHash(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                var data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                var strBuilder = new StringBuilder();

                // Loop through each byte of the hashed data
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    strBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return strBuilder.ToString();
            }

        }
    }
}
