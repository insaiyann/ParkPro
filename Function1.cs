using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Configuration;
using System.Linq;

namespace EmailCopilotNew
{
    public class Function1
    {
        private readonly ILogger _logger;

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
        }

        [Function("ParkPro")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "ParkPro")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("C# HTTP trigger function processed a request.");
                //IDictionary<string, string> queryParams = req.GetQueryParameterDictionary().ToDictionary(param => param.Key, param => param.Value);
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var input = query["inputText"];
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                var summary = await SemanticKernelForParkPro(input);

                response.WriteString(summary.ToString());

                return response;
            }
            catch
            {
                throw;
            }
        }

        private static async Task<Microsoft.SemanticKernel.Orchestration.SKContext> SemanticKernelForParkPro(string input)
        {
            //Semantic kernel implementation
            IKernel kernel = Microsoft.SemanticKernel.Kernel.Builder.Build();
            // Grab the locally stored credentials from the settings.json file. 
            // Name the service as "davinci" — assuming that you're using one of the davinci completion models. 
            var (useAzureOpenAI, model, azureEndpoint, apiKey, orgId) = Settings.LoadFromFile();

            if (useAzureOpenAI)
                kernel.Config.AddAzureOpenAITextCompletion("davinci", model, azureEndpoint, apiKey);
            else
                kernel.Config.AddOpenAITextCompletion("davinci", model, apiKey, orgId);

            string mySemanticFunctionInline = """ 
                    {{$userInput}}
                    Based on the above input, extract the occupancy number from below json data.
                    {{$referenceData}}
                """;
            var promptConfig = new PromptTemplateConfig
            {
                Completion =
                {
                    MaxTokens = 1000, Temperature = 0.2, TopP = 0.5,
                }
            };

            var promptTemplate = new PromptTemplate(
                mySemanticFunctionInline, promptConfig, kernel
            );

            var functionConfig = new SemanticFunctionConfig(promptConfig, promptTemplate);

            var summaryFunction = kernel.RegisterSemanticFunction("ParkingSkill", "Occupancy", functionConfig);
            var contextData = kernel.CreateNewContext();
            var history = "";
            var data = File.ReadAllText(@"C:\Users\tussinghal\personal projects\ParkPro\config\OccupancyData.json");
            contextData.Variables["history"] = history;
            contextData.Variables["referenceData"]=data;
            contextData.Variables["userInput"] = input;
            //contextData.Variables.Set("history", history);
            //contextData.Variables.Set("referenceData", data);

            Console.WriteLine("A semantic function has been registered.");
            // Text source: https://www.microsoft.com/en-us/worklab/kevin-scott-on-5-ways-generative-ai-will-transform-work-in-2023

           

            var summary = await summaryFunction.InvokeAsync(contextData);


            Console.WriteLine(summary);
            return summary;
        }
    }
}
