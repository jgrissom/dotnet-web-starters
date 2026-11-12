using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cryptids.Web.Data;
using Cryptids.Web.Dtos;
using Cryptids.Web.Models;

namespace Cryptids.Web.Controllers;

// Week 13: the public half, as JSON. No [Authorize]: anybody can file a report,
// the same as SightingsController.
[ApiController]
[Route("api/sightings")]
public class SightingsApiController : ControllerBase
{
    private readonly CryptidContext _context;

    public SightingsApiController(CryptidContext context)
    {
        _context = context;
    }

    // POST /api/sightings
    [HttpPost]
    public async Task<ActionResult<SightingDto>> Create(SightingInput input)
    {
        // The rules on SightingInput can't know which creatures exist. Without
        // this, a made-up id reaches the database and comes back as a 500.
        if (!await _context.Cryptids.AnyAsync(c => c.Id == input.CryptidId))
        {
            ModelState.AddModelError(nameof(input.CryptidId), "There is no creature with that id.");
            return ValidationProblem(ModelState);
        }

        var sighting = new Sighting
        {
            CryptidId = input.CryptidId,
            Witness = input.Witness,
            Location = input.Location,
            SeenOn = input.SeenOn!.Value,
            Account = input.Account
        };

        _context.Sightings.Add(sighting);
        await _context.SaveChangesAsync();

        var filed = new SightingDto
        {
            Id = sighting.Id,
            CryptidId = sighting.CryptidId,
            Witness = sighting.Witness,
            Location = sighting.Location,
            SeenOn = sighting.SeenOn,
            Account = sighting.Account
        };

        return Created($"/api/cryptids/{sighting.CryptidId}", filed);
    }
}
