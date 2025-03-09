using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using IdentityTest.Models;
using System.Text.RegularExpressions;

namespace IdentityTest.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!IsInputSafe(model.Username) || !IsInputSafe(model.Email) || !IsInputSafe(model.Password))
            {
                return BadRequest("Invalid input.");
            }

            model.Username = SanitizeInput(model.Username);
            model.Email = SanitizeInput(model.Email);

            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Username, Email = model.Email };
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
                user.PasswordHash = hashedPassword;
                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["ErrorMessage"] = "Registration failed. Please try again.";
            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!IsInputSafe(model.Email) || !IsInputSafe(model.Password))
            {
                return BadRequest("Invalid input.");
            }

            model.Email = SanitizeInput(model.Email);

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
                    if (isPasswordValid)
                    {
                        await _signInManager.SignInAsync(user, model.RememberMe);
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        return BadRequest("Invalid login attempt.");
                    }
                }
                else
                {
                    return BadRequest("Invalid login attempt.");
                }
            }

            return BadRequest("Invalid login attempt.");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private bool IsInputSafe(string input)
        {
            if (string.IsNullOrEmpty(input)) return true;
            return !Regex.IsMatch(input, @"[<>'"";]");
        }

        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return Regex.Replace(input, @"[<>'"";]", "");
        }
    }
}
