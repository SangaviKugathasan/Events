using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using EventZax.Models;

namespace EventZax.Controllers
{
    public class AuthController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "No user found with this email.";
                return View();
            }
            if (await _userManager.IsInRoleAsync(user, "Organizer"))
            {
                if (!user.IsApproved)
                {
                    ViewBag.Error = "Your organizer account is pending admin approval.";
                    return View();
                }
            }
            // Use username for sign in
            var result = await _signInManager.PasswordSignInAsync(user.UserName ?? string.Empty, password, false, false);
            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Index", "Admin");
                if (await _userManager.IsInRoleAsync(user, "Organizer"))
                    return RedirectToAction("Index", "Organizer");
                return RedirectToAction("Index", "Home");
            }
            if (result.IsLockedOut)
                ViewBag.Error = "Account is locked out.";
            else if (result.IsNotAllowed)
                ViewBag.Error = "Login is not allowed for this user.";
            else if (result.RequiresTwoFactor)
                ViewBag.Error = "Two-factor authentication is required.";
            else
                ViewBag.Error = "Invalid password.";
            return View();
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string role)
        {
            var user = new ApplicationUser { UserName = email, Email = email, IsApproved = role == "Organizer" ? false : true };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                if (role == "Organizer")
                {
                    ViewBag.Error = "Registration successful. Your organizer account is pending admin approval.";
                    return View();
                }
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }
            // Show detailed errors
            ViewBag.Error = "Registration failed: " + string.Join("; ", result.Errors.Select(e => e.Description));
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
