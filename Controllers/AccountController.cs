using LooseNotes.Models.ViewModels;
using LooseNotes.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LooseNotes.Controllers;

public class AccountController : Controller
{
    private readonly UserService _userService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserService userService, ILogger<AccountController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userService.AuthenticateAsync(model.Username, model.Password);
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("IsAdmin", user.IsAdmin.ToString()),
            new("Email", user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

        Response.Cookies.Append("IsAdmin", user.IsAdmin.ToString());
        Response.Cookies.Append("Username", user.Username);

        TempData["Success"] = $"Welcome back, {user.Username}!";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Notes");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _userService.UsernameExistsAsync(model.Username))
        {
            ModelState.AddModelError("Username", "Username is already taken.");
            return View(model);
        }

        await _userService.RegisterAsync(model.Username, model.Email, model.Password);
        TempData["Success"] = "Account created successfully. Please log in.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userService.GetByEmailAsync(model.Email);
        if (user != null)
        {
            var token = await _userService.GeneratePasswordResetTokenAsync(user.Id);
            TempData["ResetToken"] = token;
            TempData["Info"] = $"Password reset token: {token} (In production this would be sent via email)";
        }
        else
        {
            TempData["Info"] = "If that email is registered, you will receive reset instructions.";
        }

        return RedirectToAction("ResetPassword");
    }

    [HttpGet]
    public IActionResult ResetPassword(string? token)
    {
        return View(new ResetPasswordViewModel { Token = token ?? "" });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var success = await _userService.ResetPasswordAsync(model.Token, model.NewPassword);
        if (!success)
        {
            ModelState.AddModelError("", "Invalid or expired reset token.");
            return View(model);
        }

        TempData["Success"] = "Password reset successfully. Please log in.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login");

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        return View(new ProfileViewModel
        {
            Username = user.Username,
            Email = user.Email
        });
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login");

        if (!ModelState.IsValid) return View(model);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        string? newPassword = null;
        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            if (model.CurrentPassword != user.Password)
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }
            if (model.NewPassword != model.ConfirmNewPassword)
            {
                ModelState.AddModelError("ConfirmNewPassword", "Passwords do not match.");
                return View(model);
            }
            newPassword = model.NewPassword;
        }

        await _userService.UpdateProfileAsync(userId, model.Username, model.Email, newPassword);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, model.Username),
            new("IsAdmin", user.IsAdmin.ToString()),
            new("Email", model.Email)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction("Profile");
    }
}
