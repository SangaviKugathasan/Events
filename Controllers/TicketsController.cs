using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventZax.Data;
using System.Threading.Tasks;
using System.Linq;

namespace EventZax.Controllers
{
    [Route("api/tickets")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public TicketsController(ApplicationDbContext context) { _context = context; }

        [HttpGet("tiers/{eventId}")]
        public async Task<IActionResult> GetTicketTiers(int eventId)
        {
            var tiers = await _context.TicketTiers.Where(t => t.EventId == eventId).ToListAsync();
            return Ok(tiers);
        }
    }
}