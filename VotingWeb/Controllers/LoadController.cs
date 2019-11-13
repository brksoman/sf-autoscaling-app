namespace VotingWeb.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json;

    [Produces("application/json")]
    [Route("api/[controller]")]
    public class LoadController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly StatelessServiceContext context;

        public LoadController(HttpClient httpClient, StatelessServiceContext context)
        {
            this.httpClient = httpClient;
            this.context = context;
        }

        // GET: api/Load
        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            var proxyUrl = GetWorkerProxyUrl();

            using var response = await httpClient.GetAsync(proxyUrl);
            
            return Json(
                response.StatusCode == System.Net.HttpStatusCode.OK
                    ? JsonConvert.DeserializeObject<KeyValuePair<string, int>>(
                        await response.Content.ReadAsStringAsync())
                    : new KeyValuePair<string, int>("frequency", 1));
        }

        [HttpPut("{value}")]
        public async Task<IActionResult> SetFrequency(string value)
        {
            var proxyUrl = $"{GetWorkerProxyUrl()}/{value}";

            var putContent = new StringContent($"{{ 'name' : '{value}' }}", Encoding.UTF8, "application/json");
            putContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await httpClient.PutAsync(proxyUrl, putContent);

            return new ContentResult()
            {
                StatusCode = (int)response.StatusCode,
                Content = await response.Content.ReadAsStringAsync()
            };
        }

        /// <summary>
        /// Constructs a reverse proxy URL for a given service.
        /// Example: http://localhost:19081/VotingApplication/Worker/
        /// </summary>
        private Uri GetProxyAddress(Uri serviceName)
        {
            return new Uri($"http://localhost:19081{serviceName.AbsolutePath}");
        }

        /// <summary>
        /// Constructs a specific proxy URL.
        /// Example: http://localhost:19081/VotingApplication/Worker/api/LoadData
        /// </summary>
        private string GetWorkerProxyUrl()
        {
            return VotingWeb.GetApiName(
                GetProxyAddress(VotingWeb.GetWorkerServiceName(context)),
                "LoadData");
        }
    }
}
