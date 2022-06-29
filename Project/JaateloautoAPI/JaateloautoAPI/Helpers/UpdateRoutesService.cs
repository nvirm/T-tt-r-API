using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JaateloautoAPI.Helpers
{
    public class UpdateRoutesService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<UpdateRoutesService> _logger;
        private Timer? _timer = null;

        public UpdateRoutesService(ILogger<UpdateRoutesService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(300)); //5 Minutes

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation(
                "Timed Hosted Service is working. Count: {Count}", count);
            if (VRoutes.InitialRunDone == false)
            {
                var jHelper = new JaateloHelper();
                _logger.LogInformation(DateTime.Now.ToLongDateString() + " -- Updating Base Data.");
                var init = Task.Run(async () => await jHelper.getBaseInfo()).Result;
            }
            else
            {
                bool checkTime = DateTime.Now.TimeOfDay.Hours > 10 && DateTime.Now.TimeOfDay.Hours < 11; //Reset data between 10 and 11 for now.
                bool dataUpdated = VRoutes.DataUpdated.Day < DateTime.Now.Day;
                if (checkTime == true && dataUpdated == true)
                {
                    var jHelper = new JaateloHelper();
                    _logger.LogInformation(DateTime.Now.ToLongDateString() + " -- Reset Base Data.");
                    var reset = Task.Run(async () => await jHelper.resetBaseInfo()).Result;
                    if (reset == "OK")
                    {
                        _logger.LogInformation(DateTime.Now.ToLongDateString() + " -- Reset Base Data OK.");
                        _logger.LogInformation(DateTime.Now.ToLongDateString() + " -- Updating Base Data.");
                        var init = Task.Run(async () => await jHelper.getBaseInfo()).Result;
                        if (init == "OK")
                        {
                            _logger.LogInformation(DateTime.Now.ToLongDateString() + " -- Update Base Data OK.");
                        }
                        else
                        {
                            _logger.LogInformation(DateTime.Now.ToLongDateString() + " -- Update Base Data ERR");
                        }
                    }
                    else
                    {
                        _logger.LogInformation(DateTime.Now.ToLongDateString() + " -- Reset Base Data ERR.");
                    }
                }
            }
            
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
