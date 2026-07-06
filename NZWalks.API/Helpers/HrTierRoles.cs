using System.Security.Claims;

namespace NZWalks.API.Helpers
{
    // Central definition of the HR access tiers so role names aren't hardcoded
    // in multiple places across attributes/controllers.
    public static class HrTierRoles
    {
        public const string SuperAdmin = "Super Admin";
        public const string Ceo = "CEO";
        public const string HrAdmin = "HR Admin";
        public const string HrAssistant = "HR Assistant";
        public const string Employee = "Employee";

        // Unrestricted access to every feature, present and future.
        public static readonly string[] FullAccess = { SuperAdmin, Ceo };

        // Full HR-module visibility, and the only roles allowed to create users.
        public static readonly string[] HrManagement = { SuperAdmin, Ceo, HrAdmin, HrAssistant };

        public static bool IsFullAccess(ClaimsPrincipal user) => HasRole(user, FullAccess);

        public static bool IsHrManagement(ClaimsPrincipal user) => HasRole(user, HrManagement);

        private static bool HasRole(ClaimsPrincipal user, string[] roleNames)
        {
            var roleName = user.FindFirst("roleName")?.Value;
            if (string.IsNullOrEmpty(roleName)) return false;
            return roleNames.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
