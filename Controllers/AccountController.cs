using AutoRent.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AutoRent.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    // GET: Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        // Если пользователь уже авторизован, перенаправляем на главную
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    // POST: Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User created a new account with password.");

            // Назначение роли Customer
            var roleResult = await _userManager.AddToRoleAsync(user, "Customer");

            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign Customer role to user {Email}", model.Email);
            }

            // Автоматический вход после регистрации
            await _signInManager.SignInAsync(user, isPersistent: false);

            _logger.LogInformation("User logged in after registration.");

            return RedirectToAction("Index", "Home");
        }

        // Добавление ошибок в ModelState
        foreach (var error in result.Errors)
        {
            // Не показываем детальную информацию о существующих пользователях
            if (error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail")
            {
                ModelState.AddModelError(string.Empty, "Пользователь с таким email уже существует");
            }
            else
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    // GET: Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Если пользователь уже авторизован, перенаправляем на главную
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Проверяем, существует ли пользователь (но не сообщаем об этом клиенту)
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user != null)
        {
            // Попытка входа
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");

                // Перенаправление на returnUrl или на главную
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                ModelState.AddModelError(string.Empty, "Учётная запись заблокирована из-за множественных неудачных попыток входа. Попробуйте позже.");
                return View(model);
            }
        }

        // Общее сообщение об ошибке (не раскрываем, существует ли пользователь)
        ModelState.AddModelError(string.Empty, "Неверный email или пароль");
        return View(model);
    }

    // POST: Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    // GET: Account/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
