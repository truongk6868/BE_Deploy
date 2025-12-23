using HostModel = CondotelManagement.Models.Host;

namespace CondotelManagement.Repositories
{
    public interface IHostRepository
    {
        HostModel GetByUserId(int userId);

		Task<HostModel> GetHostProfileAsync(int userId);

		Task UpdateHostAsync(HostModel host);
	}
}
