using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventZax.Data;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Stripe;
using EventZax.Models;

namespace EventZax.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public CartController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class AddToCartRequest
        {
            public int TicketTierId { get; set; }
            public int Quantity { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest req)
        {
            var userId = User.Identity?.Name ?? "guest";
            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId, Items = new List<CartItem>() };
                _context.Carts.Add(cart);
            }
            var item = cart.Items.FirstOrDefault(i => i.TicketTierId == req.TicketTierId);
            if (item != null)
                item.Quantity += req.Quantity;
            else
                cart.Items.Add(new CartItem { TicketTierId = req.TicketTierId, Quantity = req.Quantity });
            await _context.SaveChangesAsync();
            return Ok(cart);
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                // Get cart
                var userId = User.Identity?.Name ?? "guest";
                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.TicketTier)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.Items.Any())
                {
                    return BadRequest(new { message = "Cart is empty" });
                }

                // Calculate total
                var total = cart.Items.Sum(i => i.Quantity * i.TicketTier.Price);

                // In production: Create Stripe checkout session
                // For now, just create an order
                var order = new Order
                {
                    UserId = userId,
                    TotalPrice = total,
                    Status = "Pending"
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return Ok(new { orderId = order.Id, message = "Checkout session created." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during checkout", error = ex.Message });
            }
        }

        public IActionResult Index()
        {
            // TODO: Load cart items for the logged-in user
            return View();
        }
    }
}