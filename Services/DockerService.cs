using System.Security.Cryptography;
using System.Threading.Tasks;
using AmiyaDbaasManager.DTOs.Request.DbInstance;
using AmiyaDbaasManager.DTOs.Response;
using AmiyaDbaasManager.DTOs.Response.DbInstance;
using AmiyaDbaasManager.Enums;
using AmiyaDbaasManager.Hubs;
using AmiyaDbaasManager.Mappers;
using AmiyaDbaasManager.Models;
using AmiyaDbaasManager.Repositories.Interfaces;
using AmiyaDbaasManager.Services.Interfaces;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.SignalR;

namespace AmiyaDbaasManager.Services;

public class DockerService : IDockerService
{
    private readonly DockerClient _dockerClient;
    private readonly IPortManagerService _portService;
    private readonly IDbInstanceRepo _dbInstanceRepo;
    private readonly IAuthService _authService;
    private readonly IUserSubscriptionService _subscriptionService;
    private readonly IEncryptionService _encryptionService;

    public DockerService(
        IPortManagerService portService,
        IDbInstanceRepo dbInstanceRepo,
        IAuthService authService,
        IUserSubscriptionService subscriptionService,
        IEncryptionService encryptionService
    )
    {
        _dockerClient = new DockerClientConfiguration(
            new Uri("unix:///var/run/docker.sock")
        ).CreateClient();

        _portService = portService;
        _dbInstanceRepo = dbInstanceRepo;
        _authService = authService;
        _subscriptionService = subscriptionService;
        _encryptionService = encryptionService;
    }

    public async Task<ApiResponse<DbInstanceResponseDto>> createDbImage(
        CreateDbInstanceRequestDto request
    )
    {
        // 1. Validate request & user
        var validationError = ValidateRequest(request);
        if (validationError != null)
            return ApiResponse<DbInstanceResponseDto>.Fail(validationError);

        if (!await _authService.isUserValid(request.UserId))
            return ApiResponse<DbInstanceResponseDto>.Fail("Invalid user");

        // 2. Kiểm tra plan limit trước khi tạo instance
        try
        {
            await _subscriptionService.ValidatePlanLimit(request.UserId);
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<DbInstanceResponseDto>.Fail(ex.Message);
        }

        // 3. Parse engine
        if (!TryParseEngine(request.Engine, out DbEngine engine, out string engineError))
            return ApiResponse<DbInstanceResponseDto>.Fail(engineError);

        try
        {
            await PullImageAsync(engine);

            await EnsureNetworkExistsAsync(request.UserId);

            var bytes = RandomNumberGenerator.GetBytes(16);
            string rawPassword = Convert.ToBase64String(bytes)[..16];

            int newPort = await _portService.NewPort();
            string containerId = await CreateAndStartContainerAsync(
                request,
                engine,
                newPort,
                rawPassword
            );

            string encryptedPassword = _encryptionService.Encrypt(rawPassword);

            var dbInstance = await SaveDbInstanceAsync(
                request,
                engine,
                newPort,
                containerId,
                encryptedPassword
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
            Name = $"tenant_{request.UserId}_{Guid.NewGuid()}",
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
            DockerContainerId = containerId,
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
                instance.DockerContainerId,
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
                instance.DockerContainerId,
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
                instance.DockerContainerId,
                new ContainerStopParameters()
            );

            await _dockerClient.Containers.RemoveContainerAsync(
                instance.DockerContainerId,
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
        int page = 1;
        int pageSize = 50;

        while (true)
        {
            var dbInstances = await _dbInstanceRepo.GetPagedAsync(page, pageSize);

            if (dbInstances == null || dbInstances.Count == 0)
            {
                break;
            }

            var tasks = dbInstances.Select(async instance =>
            {
                await semaphore.WaitAsync();

                try
                {
                    try
                    {
                        var container = await _dockerClient.Containers.InspectContainerAsync(
                            instance.DockerContainerId
                        );
                        instance.Status =
                            container.State.Status == "running" ? "Running" : "Stopped";
                    }
                    catch (Exception)
                    {
                        // Nếu không tìm thấy container hoặc có lỗi gọi đến Docker daemon
                        instance.Status = "Error";
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            await _dbInstanceRepo.UpdateRangeAsync(dbInstances);
            page++;
        }
    }

    public async Task SendLogsToGroup(string containerId, IHubContext<InstanceLogs> hubContext)
    {
        using var stream = await _dockerClient.Containers.GetContainerLogsAsync(
            containerId,
            false, // tty
            new ContainerLogsParameters
            {
                ShowStdout = true,
                Follow = true,
                ShowStderr = true,
                Timestamps = true,
            },
            CancellationToken.None
        );

        var buffer = new byte[81920];
        while (true)
        {
            var result = await stream.ReadOutputAsync(
                buffer,
                0,
                buffer.Length,
                CancellationToken.None
            );
            if (result.EOF)
                break;

            string line = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (!string.IsNullOrWhiteSpace(line))
            {
                await hubContext
                    .Clients.Group(containerId)
                    .SendAsync("ReceiveLog", line.TrimEnd('\r', '\n'));
            }
        }
    }

    private async Task<string> CreateExecAsync(
        string containerId,
        IEnumerable<string> cmd,
        IEnumerable<string>? envVars,
        CancellationToken ct
    )
    {
        var result = await _dockerClient.Exec.ExecCreateContainerAsync(
            containerId,
            new ContainerExecCreateParameters
            {
                Cmd = cmd.ToList(),
                AttachStdout = true,
                AttachStderr = true,
                Env = envVars?.ToList(),
            },
            ct
        );

        return result.ID;
    }

    public async Task ExecAsync(
        string containerId,
        IEnumerable<string> cmd,
        IEnumerable<string>? envVars,
        CancellationToken ct = default
    )
    {
        var execId = await CreateExecAsync(containerId, cmd, envVars, ct);
        using var stream = await _dockerClient.Exec.StartAndAttachContainerExecAsync(
            execId,
            false,
            ct
        );

        var (stdout, stderr) = await stream.ReadOutputToEndAsync(ct);

        if (!string.IsNullOrWhiteSpace(stderr))
            Console.WriteLine($"{stderr}");
    }

    public async Task GetFileStreamFromContainer(
        string containerId,
        string contentPath,
        Func<Stream, long, Task> action,
        CancellationToken ct = default
    )
    {
        var response = await _dockerClient.Containers.GetArchiveFromContainerAsync(
            containerId,
            new GetArchiveFromContainerParameters(),
            false,
            ct
        );

        var reader = new System.Formats.Tar.TarReader(response.Stream);
        var entry = await reader.GetNextEntryAsync();

        if (entry?.DataStream is null)
            throw new Exception($"Không đọc được file từ container: {contentPath}");

        await action(entry.DataStream, entry.Length);
    }

    public async Task CopyToContainer(
        string containerId,
        Stream fileStream,
        string contentPath,
        CancellationToken ct = default
    )
    {
        var filename = Path.GetFileName(contentPath);
        var dir = Path.GetDirectoryName(contentPath);

        var pipe = new System.IO.Pipelines.Pipe();

        var writeTask = Task.Run(async () =>
        {
            using (
                var tarWriter = new System.Formats.Tar.TarWriter(
                    pipe.Writer.AsStream(),
                    leaveOpen: false
                )
            )
            {
                var entry = new System.Formats.Tar.PaxTarEntry(
                    System.Formats.Tar.TarEntryType.RegularFile,
                    filename
                );
                entry.DataStream = fileStream;
                tarWriter.WriteEntry(entry);
                await pipe.Writer.CompleteAsync();
            }
        });

        var readTask = _dockerClient.Containers.ExtractArchiveToContainerAsync(
            containerId,
            new ContainerPathStatParameters { Path = dir },
            pipe.Reader.AsStream(),
            ct
        );

        await Task.WhenAll(writeTask, readTask);
    }
}
