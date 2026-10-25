using MySqlConnector;
using pizza_core_api.Core.Models;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Interfaces
{
    public interface IAdressService
    {
        Task<Adress> GetAdress(long id, string token);
        Task<Adress> AdressSql(long id, MySqlConnection connection);
        Task<Adress> CreateAdress(AdressCreateRequest request, string token);
        Task<Adress> UpdateAdress(long id, AdressUpdateRequest request, string token);
        Task<bool> DeleteAdress(long id, string token);
    }
}