using System.Security.Claims;

namespace CondotelManagement.Helpers
{
    public static class ClaimsPrincipalHelper
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            return int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));
        }
    }
}
