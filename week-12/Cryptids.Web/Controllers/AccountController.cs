using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Cryptids.Web.Models;
using Cryptids.Web.ViewModels;

namespace Cryptids.Web.Controllers;

public class AccountController : Controller
{
    // Identity's two workers. UserManager creates and finds members;
    // SignInManager signs them in and out — it is what sets the cookie.
    private readonly UserManager<Member> _userManager;
    private readonly SignInManager<Member> _signInManager;

    public AccountController(UserManager<Member> userManager, SignInManager<Member> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET /Account/Register
    public IActionResult Register()
    {
        return View();
    }

    // POST /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var member = new Member { UserName = form.Email, Email = form.Email, FieldName = form.FieldName };

        // The password goes in; a hash is what gets saved.
        var result = await _userManager.CreateAsync(member, form.Password);

        if (!result.Succeeded)
        {
            // Identity's own reasons — a weak password, an email already taken.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(form);
        }

        await _signInManager.SignInAsync(member, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    // GET /Account/Login — and where [Authorize] sends a visitor, with the
    // page they were trying to reach in ?ReturnUrl=
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel form, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await _signInManager.PasswordSignInAsync(
            form.Email, form.Password, form.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            // One message for both mistakes. Saying "wrong password" would tell
            // a stranger that the email has an account.
            ModelState.AddModelError("", "That email and password don't match a member.");
            return View(form);
        }

        // LocalRedirect refuses a ReturnUrl that points at another site.
        return LocalRedirect(returnUrl ?? "/");
    }

    // POST /Account/Logout — a POST, never a link. Any page on the internet
    // can make a browser send a GET.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
