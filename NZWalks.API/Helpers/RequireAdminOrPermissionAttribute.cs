using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NZWalks.API.Helpers
{
    // Grants access if the user's role is "HR Admin", or if the user holds the
    // given "{module}.{action}" permission claim.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireAdminOrPermissionAttribute : Attribute, IAuthorizationFilter
    {
        private const string AdminRoleName = "HR Admin";

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

            var isAdmin = user.Claims
                .Any(c => c.Type == "roleName" && c.Value.Equals(AdminRoleName, StringComparison.OrdinalIgnoreCase));

            if (isAdmin) return;

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
                    Message = $"Access denied. Requires '{AdminRoleName}' role or permission: {requiredPerm}"
                })
                { StatusCode = 403 };
            }
        }
    }
}
