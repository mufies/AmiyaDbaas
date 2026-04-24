using AmiyaDbaasManager.Repositories.Interfaces;
using AmiyaDbaasManager.Services.Interfaces;

namespace AmiyaDbaasManager.Services
{
    public class PortManagerService : IPortManagerService
    {
        private readonly IDbInstanceRepo dbInstanceRepo;

        public PortManagerService(IDbInstanceRepo _dbInstanceRepo)
        {
            dbInstanceRepo = _dbInstanceRepo;
        }

        public async Task<int> NewPort()
        {
            var ListAllowcatedPort = await dbInstanceRepo.GetPortList();
            var rng = new Random();

            int newPort;
            do
            {
                newPort = rng.Next(49152, 65535); // random 5 chữ số
            } while (ListAllowcatedPort.Contains(newPort));
            return newPort;
        }
    }
}
