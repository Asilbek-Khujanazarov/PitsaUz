using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AuthController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var redirectUrl = Url.Action("GoogleResponse", "Auth", null, Request.Scheme);
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync();
        if (!result.Succeeded)
            return BadRequest("Google autentifikatsiyasi muvaffaqiyatsiz.");

        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var userId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(userId))
            return BadRequest("Foydalanuvchi ma'lumotlari topilmadi.");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return BadRequest(createResult.Errors);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        // Frontend’ga redirect qilish
        return Redirect("http://localhost:4200");
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
            return Unauthorized("Foydalanuvchi autentifikatsiya qilinmagan.");

        return Ok(new
        {
            Id = userId,
            Email = email
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { Message = "Muvaffaqiyatli chiqildi." });
    }
}