using Microsoft.AspNetCore.Mvc;
using QRCoder;
using EventZax.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using EventZax.Models;

namespace EventZax.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public OrdersController(ApplicationDbContext context) { _context = context; }

        [HttpPost("{orderId}/ticket/generate")]
        public async Task<IActionResult> GenerateTicket(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();
            var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode($"Order:{order.Id}", QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCoder.BitmapByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(20);
            // In production, save to file or cloud storage
            order.QrCodePath = $"/tickets/qr_{order.Id}.png";
            await _context.SaveChangesAsync();
            return File(qrBytes, "image/png");
        }
    }
}