using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssetFlow.Api.Contracts;
using AssetFlow.Api.Data;
using AssetFlow.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AssetFlow.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
}

public class JwtOptions
{
    public string Issuer { get; set; } = "AssetFlow";
    public string Audience { get; set; } = "AssetFlow";
    public string Key { get; set; } = "";
    public int ExpiryHours { get; set; } = 8;
}

public class AuthService(AppDbContext db, IPasswordHasher<User> hasher, JwtOptions jwt, TimeProvider clock) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = User.NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new ConflictException("An account with this email already exists.");

        // Self-registration always creates Staff. Admins are created by the system.
        var user = new User(request.Name, email, UserRole.Staff, request.Department, Now);
        user.SetPasswordHash(hasher.HashPassword(user, request.Password));
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return CreateToken(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var email = User.NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null) return null;

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        return result == PasswordVerificationResult.Failed ? null : CreateToken(user);
    }

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    private AuthResponse CreateToken(User user)
    {
        var expires = Now.AddHours(jwt.ExpiryHours);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
            new Claim("role", user.Role.ToString()),
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, Now, expires, credentials);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.ToDto());
    }
}
