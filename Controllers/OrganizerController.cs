using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EventZax.Data;
using EventZax.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EventZax.Controllers
{
    [Authorize(Roles = "Organizer,Admin")]
    public class OrganizerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrganizerController> _logger;

        public OrganizerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<OrganizerController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var events = await _context.Events.Where(e => e.OrganizerId == userId).ToListAsync();
            return View(events);
        }

        public async Task<IActionResult> CreateEvent()
        {
            // Pass venues to the view for dropdown
            var venues = await _context.Venues.ToListAsync();
            ViewBag.Venues = venues;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event model, IFormFile ImageFile)
        {
            // Pass venues to the view for dropdown on error
            var venues = await _context.Venues.ToListAsync();
            ViewBag.Venues = venues;
            try
            {
                if (!ModelState.IsValid)
                {
                    // Log validation errors
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogError("Validation error: {ErrorMessage}", error.ErrorMessage);
                    }
                    ModelState.AddModelError("", "Please correct the highlighted errors and try again.");
                    return View(model);
                }

                model.OrganizerId = _userManager.GetUserId(User);
                model.IsPublished = false;

                // Handle image upload
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/event-images");
                    Directory.CreateDirectory(uploads);
                    var fileName = Path.GetFileNameWithoutExtension(ImageFile.FileName) + "_" + Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(uploads, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    model.ImagePath = "/event-images/" + fileName;
                }
                else
                {
                    model.ImagePath = string.Empty;
                }

                // If venue name is empty but VenueId is provided, set VenueName from DB
                if (string.IsNullOrWhiteSpace(model.VenueName) && model.VenueId != 0)
                {
                    var venue = await _context.Venues.FindAsync(model.VenueId);
                    if (venue != null) model.VenueName = venue.Name;
                }

                _context.Events.Add(model);
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    _logger.LogInformation("Event created successfully with ID: {EventId}", model.Id);
                }
                else
                {
                    _logger.LogError("Failed to save the event to the database.");
                    ModelState.AddModelError("", "An error occurred while saving the event. Please try again.");
                    return View(model);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log error and show error view
                _logger.LogError(ex, "An error occurred while creating the event.");
                ModelState.AddModelError("", "An error occurred while creating the event: " + ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> EditEvent(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userId);
            if (ev == null) return NotFound();
            return View(ev);
        }

        [HttpPost]
        public async Task<IActionResult> EditEvent(Event model)
        {
            var userId = _userManager.GetUserId(User);
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == model.Id && e.OrganizerId == userId);
            if (ev == null) return NotFound();
            if (ModelState.IsValid)
            {
                ev.Title = model.Title;
                ev.Category = model.Category;
                ev.VenueId = model.VenueId;
                ev.StartDate = model.StartDate;
                ev.EndDate = model.EndDate;
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PublishEvent(int id)
        {
            var userId = _userManager.GetUserId(User);
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userId);
            if (ev == null) return NotFound();
            ev.IsPublished = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Attendance(int eventId)
        {
            var userId = _userManager.GetUserId(User);
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.OrganizerId == userId);
            if (ev == null) return NotFound();
            var attendees = await _context.Attendances.Where(a => a.EventId == eventId).ToListAsync();
            ViewBag.Event = ev;
            return View(attendees);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInAttendee(int attendanceId)
        {
            var attendance = await _context.Attendances.FindAsync(attendanceId);
            if (attendance != null)
            {
                attendance.IsCheckedIn = true;
                attendance.CheckInTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Attendance", new { eventId = attendance?.EventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UncheckAttendee(int attendanceId)
        {
            var attendance = await _context.Attendances.FindAsync(attendanceId);
            if (attendance != null)
            {
                attendance.IsCheckedIn = false;
                attendance.CheckInTime = null;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Attendance", new { eventId = attendance?.EventId });
        }
    }
}
