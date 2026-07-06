using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NZWalks.API.Helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _module;
        private readonly string _action;

        public RequirePermissionAttribute(string module, string action)
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

            // Super Admin / CEO have unrestricted access to every feature, present and future.
            if (HrTierRoles.IsFullAccess(user)) return;

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
                    Message = $"Access denied. Required permission: {requiredPerm}"
                })
                { StatusCode = 403 };
            }
        }
    }
}