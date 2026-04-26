using AmiyaDbaasManager.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AmiyaDbaasManager.Hubs
{
    [Authorize]
    public class InstanceLogs : Hub
    {
        private readonly IDbInstanceRepo _dbInstanceRepo;
        private readonly IServiceProvider _serviceProvider;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<
            string,
            bool
        > _activeStreams = new();

        public InstanceLogs(IDbInstanceRepo dbInstanceRepo, IServiceProvider serviceProvider)
        {
            _dbInstanceRepo = dbInstanceRepo;
            _serviceProvider = serviceProvider;
        }

        public async Task JoinInstanceLogsChat(string instanceId)
        {
            if (!Guid.TryParse(instanceId, out var parsedId))
            {
                throw new HubException("Invalid instance ID format.");
            }

            var userIdClaim = Context
                .User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new HubException("Unauthorized.");
            }

            var instance = await _dbInstanceRepo.GetById(parsedId);

            if (instance == null || instance.UserId.ToString() != userIdClaim)
            {
                throw new HubException("Instance not found or access denied.");
            }

            if (string.IsNullOrEmpty(instance.DockerContainerId))
            {
                throw new HubException("Container ID is missing for this instance.");
            }

            string containerId = instance.DockerContainerId;
            await Groups.AddToGroupAsync(Context.ConnectionId, containerId);

            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var dockerService =
                            scope.ServiceProvider.GetRequiredService<AmiyaDbaasManager.Services.interfaces.IDockerService>();
                        var hubContext = scope.ServiceProvider.GetRequiredService<
                            IHubContext<InstanceLogs>
                        >();

                        await dockerService.SendLogsToGroup(containerId, hubContext);
                    }
                    catch (Exception) { }
                    finally
                    {
                        _activeStreams.TryRemove(containerId, out _);
                    }
                });
            }
        }

        public async Task LeaveInstanceLogsChat(string instanceId)
        {
            if (!Guid.TryParse(instanceId, out var parsedId))
            {
                return;
            }

            var instance = await _dbInstanceRepo.GetById(parsedId);
            if (instance != null && !string.IsNullOrEmpty(instance.DockerContainerId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, instance.DockerContainerId);
            }
        }
    }
}
