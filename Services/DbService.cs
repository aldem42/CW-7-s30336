using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TravelAgency.Models.DTOs;
using TravelAgency.Models;
using TravelAgency.Exceptions;

namespace TravelAgency.Services
{
    public class DbService : IDbService
    {
        private readonly string _connectionString;

        public DbService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        //pobieramy wszystkie dostepne wycieczki oraz kraje, ktore one objemuja
        public async Task<IEnumerable<TripDTO>> GetAllTripsAsync()
        {
            var trips = new List<TripDTO>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                       c.IdCountry, c.Name AS CountryName
                FROM Trip t
                LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                LEFT JOIN Country c ON ct.IdCountry = c.IdCountry";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            var tripDict = new Dictionary<int, TripDTO>();

            while (await reader.ReadAsync())
            {
                var tripId = reader.GetInt32(reader.GetOrdinal("IdTrip"));

                if (!tripDict.ContainsKey(tripId))
                {
                    tripDict[tripId] = new TripDTO
                    {
                        IdTrip = tripId,
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                        DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                        MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                        Countries = new List<CountryDTO>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("IdCountry")))
                {
                    tripDict[tripId].Countries.Add(new CountryDTO
                    {
                        IdCountry = reader.GetInt32(reader.GetOrdinal("IdCountry")),
                        Name = reader.GetString(reader.GetOrdinal("CountryName"))
                    });
                }
            }

            return tripDict.Values;
        }
        //tutaj zwraca wszystkie wycieczki, na ktore zapisany jest nasz klient
        public async Task<IEnumerable<TripDTO>> GetClientTripsAsync(int clientId)
        {
            var trips = new List<TripDTO>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // czy istnieje klient, chcemy to sprawdzic...
            var checkClientQuery = "SELECT 1 FROM Client WHERE IdClient = @ClientId";
            using (var checkCommand = new SqlCommand(checkClientQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@ClientId", clientId);
                var exists = await checkCommand.ExecuteScalarAsync();
                if (exists == null)
                    throw new NotFoundException($"Client with ID {clientId} does not exist.");
            }

            var query = @"
                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                       ct.RegisteredAt, ct.PaymentDate
                FROM Trip t
                INNER JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip
                WHERE ct.IdClient = @ClientId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ClientId", clientId);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                trips.Add(new TripDTO
                {
                    IdTrip = reader.GetInt32(reader.GetOrdinal("IdTrip")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                    DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                    MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                    RegisteredAt = reader.GetDateTime(reader.GetOrdinal("RegisteredAt")),
                    PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                        ? (DateTime?)null
                        : reader.GetDateTime(reader.GetOrdinal("PaymentDate"))
                });
            }

            return trips;
        }
        //dodajemy nowego klienta do bazy danych i zwracamy jego id
        public async Task<int> AddClientAsync(ClientDTO client)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);
                SELECT SCOPE_IDENTITY();";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@FirstName", client.FirstName);
            command.Parameters.AddWithValue("@LastName", client.LastName);
            command.Parameters.AddWithValue("@Email", client.Email);
            command.Parameters.AddWithValue("@Telephone", client.Telephone);
            command.Parameters.AddWithValue("@Pesel", client.Pesel);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        //bedziemy rejestrowac naszego klienta jesli beda istnialy dane i odpowiedni limit miejsc
        public async Task RegisterClientToTripAsync(int clientId, int tripId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // czy istneje klient
            var checkClientQuery = "SELECT 1 FROM Client WHERE IdClient = @ClientId";
            using (var checkCommand = new SqlCommand(checkClientQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@ClientId", clientId);
                var exists = await checkCommand.ExecuteScalarAsync();
                if (exists == null)
                    throw new NotFoundException($"Client with ID {clientId} does not exist.");
            }

            // czy istnieje wycieczka
            var checkTripQuery = "SELECT 1 FROM Trip WHERE IdTrip = @TripId";
            using (var checkCommand = new SqlCommand(checkTripQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@TripId", tripId);
                var exists = await checkCommand.ExecuteScalarAsync();
                if (exists == null)
                    throw new NotFoundException($"Trip with ID {tripId} does not exist.");
            }

            // liczba zapisow
            var countQuery = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @TripId";
            using (var countCommand = new SqlCommand(countQuery, connection))
            {
                countCommand.Parameters.AddWithValue("@TripId", tripId);
                var count = (int)await countCommand.ExecuteScalarAsync();

                var maxPeopleQuery = "SELECT MaxPeople FROM Trip WHERE IdTrip = @TripId";
                using (var maxCommand = new SqlCommand(maxPeopleQuery, connection))
                {
                    maxCommand.Parameters.AddWithValue("@TripId", tripId);
                    var maxPeople = (int)await maxCommand.ExecuteScalarAsync();

                    if (count >= maxPeople)
                        throw new InvalidOperationException("Maximum number of participants reached.");
                }
            }

            // tutaj dodamy naszego klienta do wycieczki
            var insertQuery = @"
                INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
                VALUES (@ClientId, @TripId, @RegisteredAt)";

            using var insertCommand = new SqlCommand(insertQuery, connection);
            insertCommand.Parameters.AddWithValue("@ClientId", clientId);
            insertCommand.Parameters.AddWithValue("@TripId", tripId);
            insertCommand.Parameters.AddWithValue("@RegisteredAt", DateTime.UtcNow);

            await insertCommand.ExecuteNonQueryAsync();
        }
        //tutaj usuwamy klienta, jezeli jakoby byl juz zapisany
        public async Task RemoveClientFromTripAsync(int clientId, int tripId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // sprawdzamy odnosnie rejestracji
            var checkQuery = @"
                SELECT 1 FROM Client_Trip
                WHERE IdClient = @ClientId AND IdTrip = @TripId";

            using (var checkCommand = new SqlCommand(checkQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@ClientId", clientId);
                checkCommand.Parameters.AddWithValue("@TripId", tripId);
                var exists = await checkCommand.ExecuteScalarAsync();
                if (exists == null)
                    throw new NotFoundException("Registration does not exist.");
            }

            // usuwamy rejestracje
            var deleteQuery = @"
                DELETE FROM Client_Trip
                WHERE IdClient = @ClientId AND IdTrip = @TripId";

            using var deleteCommand = new SqlCommand(deleteQuery, connection);
            deleteCommand.Parameters.AddWithValue("@ClientId", clientId);
            deleteCommand.Parameters.AddWithValue("@TripId", tripId);

            await deleteCommand.ExecuteNonQueryAsync();
        }
    }
}
