using System.Security.Claims;

namespace Lab1224_Identity.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            // 優先找 sub (JWT 標準)，再找 NameIdentifier
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return 0; // 或拋出異常，視你們的邏輯而定
            }

            return int.Parse(userIdClaim);
        }
    }
}