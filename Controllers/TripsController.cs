using Microsoft.AspNetCore.Mvc;
using TravelAgency.Exceptions;
using TravelAgency.Services;

namespace TravelAgency.Controllers;
//tutaj zwraca liste wszystkich wycieczek ktore sa dostepne i informacje o krajach
[ApiController]
[Route("api/trips")]
public class TripsController(IDbService dbService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllTrips()
    {
        return Ok(await dbService.GetAllTripsAsync());
    }
}