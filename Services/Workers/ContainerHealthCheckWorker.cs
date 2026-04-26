using AmiyaDbaasManager.Services.interfaces;

namespace AmiyaDbaasManager.Services.Workers
{
    public class ContainerHealthCheckWorker : BackgroundService
    {
        private readonly ILogger<ContainerHealthCheckWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ContainerHealthCheckWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<ContainerHealthCheckWorker> logger
        )
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Checking container status");

                    using var scope = _scopeFactory.CreateScope();
                    var dockerService = scope.ServiceProvider.GetRequiredService<IDockerService>();
                    await dockerService.ContainerHealthCheck();
                    _logger.LogInformation("Done checking!");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error while doing health check {e.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
