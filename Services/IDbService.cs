using TravelAgency.Models.DTOs;
using TravelAgency.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TravelAgency.Services
{
    public interface IDbService
    {
        Task<IEnumerable<TripDTO>> GetAllTripsAsync();
        Task<IEnumerable<TripDTO>> GetClientTripsAsync(int clientId);
        Task<int> AddClientAsync(ClientDTO client);
        Task RegisterClientToTripAsync(int clientId, int tripId);
        Task RemoveClientFromTripAsync(int clientId, int tripId);
    }
}