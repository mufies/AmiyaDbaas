using System.Security.Cryptography;
using AmiyaDbaasManager.DTOs.Request.DbInstance;
using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.DTOs.Response.DbInstance;
using AmiyaDbaasManager.Enums;
using AmiyaDbaasManager.Mappers;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories.Interfaces;
using AmiyaDbaasManager.Services.interfaces;
using AmiyaDbaasManager.Services.Interfaces;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace AmiyaDbaasManager.Services;

public class DockerService : IDockerService
{
    private readonly DockerClient _dockerClient;
    private readonly IPortManagerService _portService;
    private readonly IDbInstanceRepo _dbInstanceRepo;
    private readonly IAuthService _authService;

    public DockerService(
        IPortManagerService portService,
        IDbInstanceRepo dbInstanceRepo,
        IAuthService authService
    )
    {
        _dockerClient = new DockerClientConfiguration(
            new Uri("unix:///var/run/docker.sock")
        ).CreateClient();

        _portService = portService;
        _dbInstanceRepo = dbInstanceRepo;
        _authService = authService;
    }

    public async Task<ApiResponse<DbInstanceResponseDto>> createDbImage(
        CreateDbInstanceRequestDto request
    )
    {
        // 1. Validate
        var validationError = ValidateRequest(request);
        if (validationError != null)
            return ApiResponse<DbInstanceResponseDto>.Fail(validationError);

        if (!await _authService.isUserValid(request.UserId))
            return ApiResponse<DbInstanceResponseDto>.Fail("Invalid user");

        // 2. Parse engine
        if (!TryParseEngine(request.Engine, out DbEngine engine, out string engineError))
            return ApiResponse<DbInstanceResponseDto>.Fail(engineError);

        try
        {
            // 3. Pull image
            await PullImageAsync(engine);

            // 4. Tạo network cho tenant
            await EnsureNetworkExistsAsync(request.UserId);

            // Generate password
            var bytes = RandomNumberGenerator.GetBytes(16);
            string rawPassword = Convert.ToBase64String(bytes)[..16];

            // 5. Tạo và start container
            int newPort = await _portService.NewPort();
            string containerId = await CreateAndStartContainerAsync(
                request,
                engine,
                newPort,
                rawPassword
            );

            // Hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(rawPassword);

            // 6. Lưu vào DB
            var dbInstance = await SaveDbInstanceAsync(
                request,
                engine,
                newPort,
                containerId,
                hashedPassword
            );

            var responseDto = dbInstance.ToDto();
            responseDto.Password = rawPassword;
            responseDto.ConnectionString = DbEngineConfig.BuildConnectionString(
                engine,
                dbInstance.Host,
                dbInstance.AllocatedPort,
                rawPassword
            );

            return ApiResponse<DbInstanceResponseDto>.Ok(responseDto, "Tao db thanh cong");
        }
        catch (Exception e)
        {
            return ApiResponse<DbInstanceResponseDto>.Fail($"loi he thong {e.Message}");
        }
    }

    private bool TryParseEngine(string engineStr, out DbEngine engine, out string error)
    {
        error = string.Empty;
        try
        {
            engine = DbEngineConfig.ParseEngine(engineStr);
            return true;
        }
        catch (ArgumentException ex)
        {
            engine = default;
            error = ex.Message;
            return false;
        }
    }

    private string? ValidateRequest(CreateDbInstanceRequestDto request)
    {
        if (request == null)
            return "Invalid request";
        if (string.IsNullOrEmpty(request.InstanceName))
            return "Invalid instance name";
        return null;
    }

    private async Task PullImageAsync(DbEngine engine)
    {
        string imageName = DbEngineConfig.GetImageName(engine);
        string imageTag = DbEngineConfig.GetImageTag(engine);

        await _dockerClient.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = imageName, Tag = imageTag },
            null,
            new Progress<JSONMessage>()
        );
    }

    private async Task<string> CreateAndStartContainerAsync(
        CreateDbInstanceRequestDto request,
        DbEngine engine,
        int newPort,
        string rawPassword
    )
    {
        string imageName = DbEngineConfig.GetImageName(engine);
        string imageTag = DbEngineConfig.GetImageTag(engine);
        string containerPort = DbEngineConfig.GetContainerPort(engine);
        string networkName = $"network_tenant_{request.UserId}";

        var createParam = new CreateContainerParameters
        {
            Name = request.InstanceName,
            Image = $"{imageName}:{imageTag}",
            Env = DbEngineConfig.GetEnvVars(engine, rawPassword),
            HostConfig = new HostConfig
            {
                Memory = request.RamMb * 1024L * 1024L,
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {
                        containerPort,
                        new List<PortBinding> { new PortBinding { HostPort = newPort.ToString() } }
                    },
                },
            },
            NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    { networkName, new EndpointSettings() },
                },
            },
        };

        var response = await _dockerClient.Containers.CreateContainerAsync(createParam);
        await _dockerClient.Containers.StartContainerAsync(
            response.ID,
            new ContainerStartParameters()
        );
        return response.ID;
    }

    private async Task<DbInstance> SaveDbInstanceAsync(
        CreateDbInstanceRequestDto request,
        DbEngine engine,
        int newPort,
        string containerId,
        string hashedPassword
    )
    {
        var dbInstance = new DbInstance
        {
            Id = Guid.NewGuid(),
            InstanceName = request.InstanceName,
            Engine = request.Engine,
            Status = "Running",
            Description = request.Description,
            CpuCores = request.CpuCores,
            RamMb = request.RamMb,
            StorageGb = request.StorageGb,
            AllocatedPort = newPort,
            Host = request.Host,
            Password = hashedPassword,
            CreatedAt = DateTime.UtcNow,
            UserId = request.UserId,
        };

        await _dbInstanceRepo.Create(dbInstance);
        return dbInstance;
    }

    public async Task EnsureNetworkExistsAsync(Guid userId)
    {
        var networkName = $"network_tenant_{userId}";

        var network = await _dockerClient.Networks.ListNetworksAsync(
            new NetworksListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "name",
                        new Dictionary<string, bool> { { networkName, true } }
                    },
                },
            }
        );
        if (!network.Any())
        {
            await _dockerClient.Networks.CreateNetworkAsync(
                new NetworksCreateParameters { Name = networkName, Driver = "bridge" }
            );
        }
    }

    public async Task<ApiResponse<DbInstanceResponseDto>> StartInstanceAsync(
        Guid instanceId,
        string userId
    )
    {
        var instance = await _dbInstanceRepo.GetById(instanceId);
        if (instance == null || instance.UserId.ToString() != userId)
            return ApiResponse<DbInstanceResponseDto>.Fail("Instance not found or unauthorized");

        if (instance.Status == "Running")
            return ApiResponse<DbInstanceResponseDto>.Fail("Instance is already running");

        try
        {
            await _dockerClient.Containers.StartContainerAsync(
                instance.InstanceName,
                new ContainerStartParameters()
            );

            instance = await _dbInstanceRepo.UpdateStatus(instanceId, "Running");
            return ApiResponse<DbInstanceResponseDto>.Ok(
                instance!.ToDto(),
                "Instance started successfully"
            );
        }
        catch (Exception e)
        {
            return ApiResponse<DbInstanceResponseDto>.Fail(
                $"Failed to start instance: {e.Message}"
            );
        }
    }

    public async Task<ApiResponse<DbInstanceResponseDto>> StopInstanceAsync(
        Guid instanceId,
        string userId
    )
    {
        var instance = await _dbInstanceRepo.GetById(instanceId);
        if (instance == null || instance.UserId.ToString() != userId)
            return ApiResponse<DbInstanceResponseDto>.Fail("Instance not found or unauthorized");

        if (instance.Status == "Stopped")
            return ApiResponse<DbInstanceResponseDto>.Fail("Instance is already stopped");

        try
        {
            await _dockerClient.Containers.StopContainerAsync(
                instance.InstanceName,
                new ContainerStopParameters()
            );

            instance = await _dbInstanceRepo.UpdateStatus(instanceId, "Stopped");
            return ApiResponse<DbInstanceResponseDto>.Ok(
                instance!.ToDto(),
                "Instance stopped successfully"
            );
        }
        catch (Exception e)
        {
            return ApiResponse<DbInstanceResponseDto>.Fail($"Failed to stop instance: {e.Message}");
        }
    }

    public async Task<ApiResponse<string>> DeleteInstanceAsync(Guid instanceId, string userid)
    {
        var instance = await _dbInstanceRepo.GetById(instanceId);
        if (instance == null || !instance.UserId.ToString().Equals(userid))
            return ApiResponse<string>.Fail("Instance not found or unauthorized");

        try
        {
            await _dockerClient.Containers.StopContainerAsync(
                instance.InstanceName,
                new ContainerStopParameters()
            );

            await _dockerClient.Containers.RemoveContainerAsync(
                instance.InstanceName,
                new ContainerRemoveParameters()
            );
            await _dbInstanceRepo.DeleteInstance(instanceId, userid);
            await DeleteNetworkIfNeeded(Guid.Parse(userid));
            return ApiResponse<string>.Ok("Success", "Instance deleted!");
        }
        catch (Exception e)
        {
            return ApiResponse<string>.Fail($"Failed to deleted instance; {e.Message}");
        }
    }

    public async Task DeleteNetworkIfNeeded(Guid userid)
    {
        var networkName = $"network_tenant_{userid}";

        var network = await _dockerClient.Networks.ListNetworksAsync(
            new NetworksListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "name",
                        new Dictionary<string, bool> { { networkName, true } }
                    },
                },
            }
        );
        var networkDetails = await _dockerClient.Networks.InspectNetworkAsync(network.First().ID);

        if (networkDetails.Containers?.Count == 0)
            await _dockerClient.Networks.DeleteNetworkAsync(networkName);
    }

    public async Task ContainerHealthCheck()
    {
        var semaphore = new SemaphoreSlim(10);
        var dbInstance = await _dbInstanceRepo.GetAll();
        var tasks = dbInstance.Select(async instance =>
        {
            await semaphore.WaitAsync();

            try
            {
                var container = await _dockerClient.Containers.InspectContainerAsync(
                    instance.Id.ToString()
                );
                instance.Status = container.State.Status == "running" ? "Running" : "Stopped";
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);

        // Sau đó mới save 1 lần duy nhất
        await _dbInstanceRepo.UpdateRangeAsync(dbInstance);
    }
}
