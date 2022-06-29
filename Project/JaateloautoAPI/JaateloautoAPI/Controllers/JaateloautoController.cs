using JaateloautoAPI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

namespace JaateloautoAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JaateloautoController : ControllerBase
    {

        private readonly ILogger<JaateloautoController> _logger;

        public JaateloautoController(ILogger<JaateloautoController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<string> Get([FromQuery] QAllParams parameters)
        {

            var jHelper = new JaateloHelper();
            if (parameters == null)
            {
                var getCurrent = await jHelper.getCurrentData();

                return JsonSerializer.Serialize(getCurrent);
            }
            if (parameters.Mode == "Routes")
            {
                var getCurrent = await jHelper.getCurrentData();

                return JsonSerializer.Serialize(getCurrent);
            }
            if (parameters.Mode == "Vehicles")
            {
                var getVehicles = await jHelper.getCurrentVehicles();

                return JsonSerializer.Serialize(getVehicles);
            }
            if (parameters.Mode == "Stops")
            {
                var getStops = await jHelper.getCurrentRouteStops();

                return JsonSerializer.Serialize(getStops);
            }

            return "";

        }
    }

    public class QAllParams
    {
        public string Mode { get; set; }
    }
}
