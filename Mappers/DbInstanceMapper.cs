using AmiyaDbaasManager.DTOs.Response.DbInstance;
using AmiyaDbaasManager.Models;

namespace AmiyaDbaasManager.Mappers;

public static class DbInstanceMapper
{
    public static DbInstanceResponseDto ToDto(this DbInstance instance)
    {
        return new DbInstanceResponseDto
        {
            Id             = instance.Id,
            InstanceName   = instance.InstanceName,
            DockerContainerId = instance.DockerContainerId,
            Engine         = instance.Engine,
            Status         = instance.Status,
            Description    = instance.Description,
            CpuCores       = instance.CpuCores,
            RamMb          = instance.RamMb,
            StorageGb      = instance.StorageGb,
            AllocatedPort  = instance.AllocatedPort,
            Host           = instance.Host,
            ConnectionString = AmiyaDbaasManager.Enums.DbEngineConfig.BuildConnectionString(
                AmiyaDbaasManager.Enums.DbEngineConfig.ParseEngine(instance.Engine), 
                instance.Host, 
                instance.AllocatedPort, 
                "********"
            ),
            CreatedAt      = instance.CreatedAt,
            UserId         = instance.UserId,
        };
    }

    public static List<DbInstanceResponseDto> ToDtoList(this IEnumerable<DbInstance> instances)
        => instances.Select(i => i.ToDto()).ToList();
}
