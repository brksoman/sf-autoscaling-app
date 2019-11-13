namespace Worker.Controllers
{
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    public class LoadDataController : Controller
    {
        private readonly StatelessServiceContext context;
        private readonly SyncValue frequency;

        public LoadDataController(StatelessServiceContext context, SyncValue frequency)
        {
            this.context = context;
            this.frequency = frequency;
        }

        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            return Json(new KeyValuePair<string, int>("frequency", this.frequency.Value));
        }

        [HttpPut("{name}")]
        public async Task<IActionResult> Put(string name)
        {
            this.frequency.Value = int.Parse(name);

            return new OkResult();
        }
    }
}
