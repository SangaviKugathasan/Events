using Microsoft.AspNetCore.Mvc;
using EventZax.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using EventZax.Models;

namespace EventZax.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public AttendanceController(ApplicationDbContext context) => _context = context;

        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromForm] string qr)
        {
            if (string.IsNullOrWhiteSpace(qr)) return BadRequest("Missing qr payload.");

            Order? order = null;
            // Try parse Order:{id} pattern
            if (qr.StartsWith("Order:", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(qr.Substring("Order:".Length), out var id))
                {
                    order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
                }
            }

            // If not found, try matching by stored QrCodePath
            if (order == null)
            {
                order = await _context.Orders.FirstOrDefaultAsync(o => o.QrCodePath == qr || ("/tickets/qr_" + o.Id + ".png") == qr);
            }

            if (order == null) return NotFound("Order not found for QR code.");

            var attendance = await _context.Attendances.FirstOrDefaultAsync(a => a.EventId == order.EventId && a.UserId == order.UserId);
            if (attendance == null)
            {
                attendance = new Attendance
                {
                    EventId = order.EventId,
                    UserId = order.UserId,
                    IsCheckedIn = true,
                    CheckInTime = DateTime.UtcNow
                };
                _context.Attendances.Add(attendance);
            }
            else
            {
                attendance.IsCheckedIn = true;
                attendance.CheckInTime = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Check-in successful" });
        }
    }
}
