using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NZWalks.API.Helpers
{
    // Grants access if the user's role is one of the HR-tier management roles
    // (Super Admin, CEO, HR Admin, HR Assistant), or if the user holds the
    // given "{module}.{action}" permission claim.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireAdminOrPermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _module;
        private readonly string _action;

        public RequireAdminOrPermissionAttribute(string module, string action)
        {
            _module = module;
            _action = action;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (HrTierRoles.IsHrManagement(user)) return;

            var requiredPerm = $"{_module}.{_action}";
            var hasPerm = user.Claims
                .Where(c => c.Type == "permission")
                .Any(c => c.Value.Equals(requiredPerm, StringComparison.OrdinalIgnoreCase));

            if (!hasPerm)
            {
                context.Result = new ObjectResult(new
                {
                    StatusCode = 403,
                    IsSuccess = false,
                    Message = $"Access denied. Requires an HR-tier management role or permission: {requiredPerm}"
                })
                { StatusCode = 403 };
            }
        }
    }
}
