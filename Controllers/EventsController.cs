using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventZax.Data;
using System.Threading.Tasks;
using System.Linq;
using EventZax.Models;
using System;

namespace EventZax.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _context.Events.Include(e => e.Venue)
                .Where(e => e.IsPublished)
                .ToListAsync();
            return View(events);
        }

        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.Events.Include(e => e.Venue).FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();
            return View(ev);
        }

        [HttpPost]
        public async Task<IActionResult> Register(int eventId, string FullName, string Address, string Tel, string Email)
        {
            // Save registration as Attendance
            var attendance = new Attendance
            {
                EventId = eventId,
                UserId = Email, // For demo, use email as UserId
                FullName = FullName,
                Address = Address,
                Tel = Tel,
                IsCheckedIn = false,
                CheckInTime = null
            };
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Registration successful!";
            return RedirectToAction("Details", new { id = eventId });
        }
    }
}