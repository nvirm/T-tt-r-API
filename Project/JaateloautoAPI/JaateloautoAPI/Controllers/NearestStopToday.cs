using JaateloautoAPI.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

namespace JaateloautoAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NearestStopTodayController : ControllerBase
    {

        private readonly ILogger<NearestStopTodayController> _logger;

        public NearestStopTodayController(ILogger<NearestStopTodayController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<string> Get([FromQuery] QueryParameters parameters)
        {
            var jHelper = new JaateloHelper();
            var locArr = new double[] { parameters.Long, parameters.Lat };
            var getNearme = await jHelper.getNearestStop(locArr, parameters.Range,true);
            
            return JsonSerializer.Serialize(getNearme);

        }
    }

}
