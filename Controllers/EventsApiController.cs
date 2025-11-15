using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventZax.Data;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace EventZax.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SearchEvents(string? search, string? category, string? dateFrom, string? location)
        {
            var query = _context.Events.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.Title.Contains(search));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category == category);
            }

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var parsedDate))
            {
                query = query.Where(e => e.StartDate >= parsedDate);
            }

            if (!string.IsNullOrEmpty(location))
            {
                // match against VenueName now that Event no longer has a Venue navigation
                query = query.Where(e => e.VenueName != null && e.VenueName.Contains(location));
            }

            var events = await query.ToListAsync();
            return Ok(events);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventDetails(int id)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();
            return Ok(ev);
        }
    }
}