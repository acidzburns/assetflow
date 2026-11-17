using System.Security.Claims;

namespace AssetFlow.Api.Infrastructure;

public static class ClaimsExtensions
{
    /// <summary>The logged-in user's ID, read from the JWT "sub" claim.</summary>
    public static int UserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue("sub") ?? throw new UnauthorizedAccessException());

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole("Admin");
}
