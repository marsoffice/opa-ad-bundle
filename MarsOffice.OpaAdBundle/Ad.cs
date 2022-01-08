using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace MarsOffice.Opa.AdBundle
{
    public class Ad
    {
        private readonly GraphServiceClient _graphClient;

        public Ad(GraphServiceClient graphClient)
        {
            _graphClient = graphClient;
        }

        [FunctionName("Ad")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "/api/ad/data")] HttpRequest req,
            ILogger log)
        {


            return new OkObjectResult("test");
        }
    }
}
