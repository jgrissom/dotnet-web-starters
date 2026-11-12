using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cryptids.Web.Data;
using Cryptids.Web.Dtos;
using Cryptids.Web.Models;

namespace Cryptids.Web.Controllers;

// Week 13: the archive, answered as JSON. Reading stays open to anybody, and
// changing it needs a curator, the same as the pages.
[ApiController]
[Route("api/cryptids")]
[Authorize(Roles = "Curator")]
public class CryptidsApiController : ControllerBase
{
    private readonly CryptidContext _context;

    public CryptidsApiController(CryptidContext context)
    {
        _context = context;
    }

    // GET /api/cryptids
    [AllowAnonymous]
    [HttpGet]
    public async Task<List<CryptidDto>> GetAll()
    {
        var cryptids = await _context.Cryptids
            .Include(c => c.Sightings)
            .ToListAsync();

        return cryptids.Select(c => new CryptidDto
        {
            Id = c.Id,
            Name = c.Name,
            Region = c.Region,
            FirstSighting = c.FirstSighting,
            IsDebunked = c.IsDebunked,
            LatinName = c.LatinName,
            ReportCount = c.Sightings.Count
        }).ToList();
    }

    // GET /api/cryptids/1
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CryptidDetailsDto>> GetOne(int id)
    {
        var cryptid = await _context.Cryptids
            .Include(c => c.Sightings)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cryptid == null)
        {
            return NotFound();
        }

        return new CryptidDetailsDto
        {
            Id = cryptid.Id,
            Name = cryptid.Name,
            Region = cryptid.Region,
            FirstSighting = cryptid.FirstSighting,
            IsDebunked = cryptid.IsDebunked,
            LatinName = cryptid.LatinName,
            Sightings = cryptid.Sightings
                .Select(s => new SightingDto
                {
                    Id = s.Id,
                    CryptidId = s.CryptidId,
                    Witness = s.Witness,
                    Location = s.Location,
                    SeenOn = s.SeenOn,
                    Account = s.Account
                })
                .ToList()
        };
    }

    // PUT /api/cryptids/1
    // CryptidInput has no ImageUrl, and nothing here touches it, so the plate stays.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CryptidInput input)
    {
        var cryptid = await _context.Cryptids.FindAsync(id);
        if (cryptid == null)
        {
            return NotFound();
        }

        cryptid.Name = input.Name;
        cryptid.Region = input.Region;
        cryptid.FirstSighting = input.FirstSighting;
        cryptid.IsDebunked = input.IsDebunked;
        cryptid.LatinName = input.LatinName;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/cryptids/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cryptid = await _context.Cryptids.FindAsync(id);
        if (cryptid == null)
        {
            return NotFound();
        }

        _context.Cryptids.Remove(cryptid);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
