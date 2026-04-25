using AmiyaDbaasManager.Services.Interfaces;

namespace AmiyaDbaasManager.Services
{
    public class UserSubscriptionCheckWorker : BackgroundService
    {
        private readonly ILogger<UserSubscriptionCheckWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public UserSubscriptionCheckWorker(
            ILogger<UserSubscriptionCheckWorker> logger,
            IServiceScopeFactory scopeFactory
        )
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Checking user subscriptions");

                    using var scope = _scopeFactory.CreateScope();
                    var UserSubService =
                        scope.ServiceProvider.GetRequiredService<IUserSubscriptionService>();

                    await UserSubService.checkUserPlan();
                    _logger.LogInformation("Done check user plan");
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error while checking {e.Message}");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
