using Microsoft.AspNetCore.Mvc;
using TravelAgency.Exceptions;
using TravelAgency.Models.DTOs;
using TravelAgency.Services;

namespace TravelAgency.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController(IDbService dbService) : ControllerBase
{
    //tutaj zwraca nam wypisane wycieczki,ktore beda przypisane do klienta na podstawie jego ID
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetClientTrips([FromRoute] int id)
    {
        try
        {
            return Ok(await dbService.GetClientTripsAsync(id));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

   //tutaj bedzie tworzyc nowy klient i zapisywac nastepnie go do bazy danych
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientDTO body)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = await dbService.AddClientAsync(body);
        return Created($"/api/clients/{id}", new { Id = id });
    }
    //nastepnie rejestrujemy klienta na wycieczke, przy czym sprawdzimy czy klient i wycieczka istnieje
    [HttpPut("{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientToTrip([FromRoute] int id, [FromRoute] int tripId)
    {
        try
        {
            await dbService.RegisterClientToTripAsync(id, tripId);
            return Ok($"Client {id} registered for trip {tripId}");
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }
    //tutaj nastepuje usuwanie klienta z wycieczki
    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> DeleteClientTrip([FromRoute] int id, [FromRoute] int tripId)
    {
        try
        {
            await dbService.RemoveClientFromTripAsync(id, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}
