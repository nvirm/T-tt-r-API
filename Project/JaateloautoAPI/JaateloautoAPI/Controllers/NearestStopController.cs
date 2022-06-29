using JaateloautoAPI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

namespace JaateloautoAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NearestStopController : ControllerBase
    {

        private readonly ILogger<NearestStopController> _logger;

        public NearestStopController(ILogger<NearestStopController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<string> Get([FromQuery] QueryParameters parameters)
        {
            var jHelper = new JaateloHelper();
            var locArr = new double[] { parameters.Long, parameters.Lat };
            var getNearme = new SingleStopDetails();
            if (parameters.SuppliedZip != null)
            {
                if (parameters.SuppliedZip.Length > 0)
                {
                    getNearme = await jHelper.getNearestStop(locArr, parameters.Range, false, parameters.SuppliedZip);
                }
            }
            else
            {
                getNearme = await jHelper.getNearestStop(locArr, parameters.Range, false);
            }


            return JsonSerializer.Serialize(getNearme);

        }
    }
    public class QueryParameters
    {
        public double Lat { get; set; }
        public double Long { get; set; }
        public int Range { get; set; }
        public string? SuppliedZip { get; set; }
    }
}
