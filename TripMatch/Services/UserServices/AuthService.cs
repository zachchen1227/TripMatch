using Lab1224_Identity.Models;
using Lab1224_Identity.Models.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;



public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<JwtSettings> _jwtSettings;
    public AuthService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings;
    }
    // 產生 JWT 字串（使用 HMAC-SHA256）
    // 會使用設定節點: Jwt:Key, Jwt:Issuer, Jwt:Audience
    // 在 AuthService.cs 內
    public string GenerateJwtToken(ApplicationUser user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        // 直接使用建構子注入的 _jwtSettings.Value
        var settings = _jwtSettings.Value;

        var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
    };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // 寫入 PendingEmail Cookie (這是在註冊/驗證流程中使用)
    public void SetPendingCookie(HttpContext context, string? email)
    {
        if (string.IsNullOrEmpty(email)) return;
        context.Response.Cookies.Append("PendingEmail", email, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // 正式環境請改 true
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddMinutes(30)
        });
    }

    // 寫入正式 AuthToken Cookie (登入成功後)
    public void SetAuthCookie(HttpContext context, string token)
    {
        context.Response.Cookies.Append("AuthToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(30)
        });
    }
    // 在 AuthService.cs 內修改
    public string GenerateConfirmUrl(HttpContext ctx, object userId, string code)
    {
        return $"{ctx.Request.Scheme}://{ctx.Request.Host}/api/auth/confirm-email?userId={userId}&code={System.Net.WebUtility.UrlEncode(code)}";
    }

}
