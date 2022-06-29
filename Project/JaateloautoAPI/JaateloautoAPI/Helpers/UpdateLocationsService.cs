using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace JaateloautoAPI.Helpers
{
    public class UpdateLocationsService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<UpdateLocationsService> _logger;
        private Timer? _timer = null;

        public UpdateLocationsService(ILogger<UpdateLocationsService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(60));

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation(
                "Timed Hosted Service is working. Count: {Count}", count);
            if (VRoutes.Maintenance == false)
            {
                var jHelper = new JaateloHelper();
                var parse = Task.Run(async () => await jHelper.parseVehiclesToRoutes(40)).Result;
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
