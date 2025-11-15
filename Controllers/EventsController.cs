using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventZax.Data;
using System.Threading.Tasks;
using System.Linq;
using EventZax.Models;
using System;
using System.Collections.Generic;

namespace EventZax.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Support search by name and filter by category
        public async Task<IActionResult> Index(string searchTerm, string category)
        {
            var query = _context.Events.Where(e => e.IsPublished).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var st = searchTerm.Trim();
                query = query.Where(e => e.Title.Contains(st) || e.Category.Contains(st) || e.VenueName.Contains(st));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(e => e.Category == category);
            }

            var events = await query.ToListAsync();

            // Provide list for category filter
            var categories = await _context.Events.Select(e => e.Category).Distinct().OrderBy(c => c).ToListAsync();

            ViewBag.Categories = categories;

            // Preserve filter values in ViewBag for form population
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedCategory = category;

            return View(events);
        }

        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
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