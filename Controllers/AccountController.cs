using LooseNotes.Models.ViewModels;
using LooseNotes.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

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

        var authProps = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

        Response.Cookies.Append("UserId", user.Id.ToString());
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

        if (await _userService.EmailExistsAsync(model.Email))
        {
            ModelState.AddModelError("Email", "Email address is already registered.");
            return View(model);
        }

        var user = await _userService.RegisterAsync(model.Username, model.Email, model.Password);
        await _userService.SetSecurityQuestionAsync(user.Id, model.SecurityQuestion, model.SecurityAnswer);
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
        if (user == null || string.IsNullOrEmpty(user.SecurityQuestion))
        {
            TempData["Error"] = "No account with that email address was found.";
            return View(model);
        }

        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.SecurityAnswer));
        Response.Cookies.Append("sqval", encoded, new CookieOptions { HttpOnly = false, Secure = false, SameSite = SameSiteMode.None });

        return RedirectToAction("SecurityQuestion", new { email = model.Email });
    }

    [HttpGet]
    public async Task<IActionResult> SecurityQuestion(string email)
    {
        var user = await _userService.GetByEmailAsync(email);
        if (user == null) return RedirectToAction("ForgotPassword");

        return View(new SecurityQuestionViewModel
        {
            Email = email,
            Question = user.SecurityQuestion
        });
    }

    [HttpPost]
    public async Task<IActionResult> SecurityQuestion(SecurityQuestionViewModel model)
    {
        var user = await _userService.GetByEmailAsync(model.Email);
        if (user == null) return RedirectToAction("ForgotPassword");

        model.Question = user.SecurityQuestion;

        if (!ModelState.IsValid) return View(model);

        Request.Cookies.TryGetValue("sqval", out var encoded);
        var expected = string.IsNullOrEmpty(encoded)
            ? string.Empty
            : Encoding.UTF8.GetString(Convert.FromBase64String(encoded));

        if (!string.Equals(expected, model.Answer.Trim().ToLowerInvariant(), StringComparison.Ordinal))
        {
            ModelState.AddModelError("Answer", "The answer you provided is incorrect.");
            return View(model);
        }

        ViewBag.RecoveredPassword = user.Password;
        return View("PasswordRecovered", user.Password);
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

        Request.Cookies.TryGetValue("UserId", out var cookieUserId);
        var userId = int.Parse(cookieUserId ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        return View(new ProfileViewModel
        {
            Username = user.Username,
            Email = user.Email,
            SecurityQuestion = user.SecurityQuestion,
            StoredPassword = user.Password
        });
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login");

        if (!ModelState.IsValid) return View(model);

        Request.Cookies.TryGetValue("UserId", out var cookieUserId);
        var userId = int.Parse(cookieUserId ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        string? newPassword = null;
        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            if (model.NewPassword != model.ConfirmNewPassword)
            {
                ModelState.AddModelError("ConfirmNewPassword", "Passwords do not match.");
                return View(model);
            }
            newPassword = model.NewPassword;
        }

        await _userService.UpdateProfileAsync(userId, model.Username, model.Email, newPassword);

        if (!string.IsNullOrWhiteSpace(model.SecurityQuestion) && !string.IsNullOrWhiteSpace(model.SecurityAnswer))
            await _userService.SetSecurityQuestionAsync(userId, model.SecurityQuestion, model.SecurityAnswer);

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

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> EmailAutocomplete(string? prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return Json(new List<string>());

        var emails = await _userService.SearchEmailsAsync(prefix);
        return Json(emails);
    }

    [HttpGet]
    public IActionResult Diagnostics()
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToAction("Login");

        var headerString = string.Join("&", Request.Headers.Select(h => h.Key + ": " + h.Value));
        ViewBag.HeaderOutput = headerString.Replace("&", "<br>");
        return View();
    }
}
