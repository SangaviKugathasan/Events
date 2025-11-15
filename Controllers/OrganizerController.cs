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

        public IActionResult CreateEvent()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event model, IFormFile ImageFile)
        {
            try
            {
                // Minimal validation: Title, Category and StartDate required
                if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Category) || model.StartDate == default)
                {
                    ModelState.AddModelError("", "Title, Category and Start Date are required.");
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.VenueName))
                {
                    ModelState.AddModelError("VenueName", "Venue is required.");
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

                if (model.EndDate.HasValue && model.EndDate.Value == default)
                {
                    model.EndDate = null;
                }

                _context.Events.Add(model);
                try
                {
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
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "DB update error while saving event.");
                    ModelState.AddModelError("", "Database error: " + (dbEx.InnerException?.Message ?? dbEx.Message));
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
                ev.VenueName = model.VenueName;
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
