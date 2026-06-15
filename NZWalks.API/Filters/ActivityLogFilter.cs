using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NZWalks.API.Data;
using NZWalks.API.Models.Domain.Inventory;

namespace NZWalks.API.Filters
{
    public class ActivityLogFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "POST", "PUT", "PATCH", "DELETE"
        };

        private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "passwordhash", "currentpassword", "newpassword",
            "nic", "token", "refreshtoken", "accesstoken", "secret"
        };

        private readonly InventoryDbContext _db;
        private readonly ILogger<ActivityLogFilter> _logger;

        public ActivityLogFilter(InventoryDbContext db, ILogger<ActivityLogFilter> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpMethod = context.HttpContext.Request.Method;

            if (!MutatingMethods.Contains(httpMethod))
            {
                await next();
                return;
            }

            var executedContext = await next();

            try
            {
                await WriteActivityLogAsync(context, executedContext, httpMethod);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write ActivityLog row for {Method} {Path}",
                    httpMethod, context.HttpContext.Request.Path);
            }
        }

        private async Task WriteActivityLogAsync(
            ActionExecutingContext context,
            ActionExecutedContext executedContext,
            string httpMethod)
        {
            var httpContext = context.HttpContext;

            string module = "Unknown";
            string action = "Unknown";
            if (context.ActionDescriptor is ControllerActionDescriptor cad)
            {
                module = cad.ControllerName;
                action = cad.ActionName;
            }

            var (userId, userName, userRole) = ExtractUser(httpContext.User);
            var (statusCode, isSuccess, message, dataObj) = ExtractResult(executedContext);
            var entityId = ExtractEntityId(context.ActionArguments, dataObj);
            var summary = BuildSummary(module, action, httpMethod, isSuccess, message, entityId);
            var details = BuildDetails(context.ActionArguments, executedContext.Exception);
            var ip = httpContext.Connection.RemoteIpAddress?.ToString();

            var log = new ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                Module = Truncate(module, 100),
                Action = Truncate(action, 100),
                HttpMethod = httpMethod.ToUpperInvariant(),
                Path = Truncate($"{httpContext.Request.Path}{httpContext.Request.QueryString}", 500),
                EntityId = entityId,
                Summary = Truncate(summary, 1000),
                StatusCode = statusCode,
                IsSuccess = isSuccess,
                Details = details,
                IpAddress = ip,
                CreatedAt = DateTime.UtcNow
            };

            _db.ActivityLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        private static (Guid? userId, string? userName, string? userRole) ExtractUser(ClaimsPrincipal user)
        {
            if (user.Identity?.IsAuthenticated != true)
                return (null, null, null);

            var subClaim = user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            Guid? userId = Guid.TryParse(subClaim, out var id) ? id : null;

            var userName = user.FindFirst(ClaimTypes.Name)?.Value
                ?? user.FindFirst("email")?.Value
                ?? user.FindFirst(ClaimTypes.Email)?.Value;

            var userRole = user.FindFirst("roleName")?.Value;

            return (userId, userName, userRole);
        }

        private static (int statusCode, bool isSuccess, string? message, object? data) ExtractResult(
            ActionExecutedContext executedContext)
        {
            if (executedContext.Exception != null && !executedContext.ExceptionHandled)
                return (500, false, executedContext.Exception.Message, null);

            if (executedContext.Result is ObjectResult objectResult)
            {
                var value = objectResult.Value;
                var statusCode = objectResult.StatusCode
                    ?? GetIntProperty(value, "StatusCode")
                    ?? 200;

                if (value is ValidationProblemDetails vpd)
                {
                    var errorSummary = string.Join("; ", vpd.Errors
                        .SelectMany(e => e.Value.Select(m => $"{e.Key}: {m}")));
                    return (vpd.Status ?? statusCode, false,
                        string.IsNullOrWhiteSpace(errorSummary) ? vpd.Title : errorSummary, null);
                }

                var hasShape = value?.GetType().GetProperty("IsSuccess") != null
                    && value?.GetType().GetProperty("StatusCode") != null;

                if (hasShape)
                {
                    var isSuccess = GetBoolProperty(value, "IsSuccess") ?? (statusCode is >= 200 and < 300);
                    var message = GetStringProperty(value, "Message");
                    var data = GetObjectProperty(value, "Data");
                    return (statusCode, isSuccess, message, data);
                }

                var rawMessage = GetStringProperty(value, "Message") ?? GetStringProperty(value, "message");
                return (statusCode, statusCode is >= 200 and < 300, rawMessage, value);
            }

            if (executedContext.Result is StatusCodeResult statusCodeResult)
            {
                var sc = statusCodeResult.StatusCode;
                return (sc, sc is >= 200 and < 300, null, null);
            }

            return (200, true, null, null);
        }

        private static Guid? ExtractEntityId(IDictionary<string, object?> actionArguments, object? data)
        {
            if (actionArguments.TryGetValue("id", out var idArg))
            {
                if (idArg is Guid g) return g;
            }

            var fromData = TryGetGuidProperty(data, "Id");
            if (fromData.HasValue) return fromData;

            foreach (var arg in actionArguments.Values)
            {
                if (arg == null) continue;

                var fromArg = TryGetGuidProperty(arg, "Id");
                if (fromArg.HasValue) return fromArg;

                foreach (var prop in arg.GetType().GetProperties())
                {
                    if (prop.Name.EndsWith("Id", StringComparison.Ordinal) &&
                        (prop.PropertyType == typeof(Guid) || prop.PropertyType == typeof(Guid?)))
                    {
                        var val = prop.GetValue(arg);
                        if (val is Guid gv) return gv;
                    }
                }
            }

            return null;
        }

        private static Guid? TryGetGuidProperty(object? obj, string propertyName)
        {
            if (obj == null) return null;
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null) return null;

            var val = prop.GetValue(obj);
            if (val is Guid g) return g;
            return null;
        }

        private static string BuildSummary(
            string module, string action, string httpMethod, bool isSuccess, string? message, Guid? entityId)
        {
            var verb = httpMethod.ToUpperInvariant() switch
            {
                "POST" => "created",
                "PUT" or "PATCH" => "updated",
                "DELETE" => "deleted",
                _ => "processed"
            };

            var outcome = isSuccess ? "succeeded" : "failed";
            var entityPart = entityId.HasValue ? $" (Id: {entityId})" : string.Empty;
            var msgPart = string.IsNullOrWhiteSpace(message) ? string.Empty : $" — {message}";

            return $"{module}.{action}: {verb} request {outcome}{entityPart}{msgPart}";
        }

        private string BuildDetails(IDictionary<string, object?> actionArguments, Exception? exception)
        {
            try
            {
                var redacted = new Dictionary<string, object?>();
                foreach (var (key, value) in actionArguments)
                {
                    redacted[key] = RedactValue(value);
                }

                var payload = new Dictionary<string, object?> { ["arguments"] = redacted };
                if (exception != null)
                    payload["exception"] = exception.Message;

                return JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                    MaxDepth = 4
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not serialize ActivityLog details.");
                return "{}";
            }
        }

        private static object? RedactValue(object? value)
        {
            if (value == null) return null;

            var type = value.GetType();
            if (type.IsPrimitive || value is string || value is Guid || value is DateTime
                || value is decimal || type.IsEnum)
            {
                return value;
            }

            var result = new Dictionary<string, object?>();
            foreach (var prop in type.GetProperties())
            {
                if (SensitiveKeys.Contains(prop.Name))
                {
                    result[prop.Name] = "***REDACTED***";
                    continue;
                }

                object? raw;
                try { raw = prop.GetValue(value); }
                catch { continue; }

                if (raw == null || raw is string || raw is Guid || raw is DateTime
                    || raw is bool || raw is decimal || raw.GetType().IsPrimitive || raw.GetType().IsEnum)
                {
                    result[prop.Name] = raw;
                }
                else
                {
                    result[prop.Name] = raw.GetType().Name;
                }
            }
            return result;
        }

        private static int? GetIntProperty(object? obj, string name)
        {
            var v = obj?.GetType().GetProperty(name)?.GetValue(obj);
            return v is int i ? i : null;
        }

        private static bool? GetBoolProperty(object? obj, string name)
        {
            var v = obj?.GetType().GetProperty(name)?.GetValue(obj);
            return v is bool b ? b : null;
        }

        private static string? GetStringProperty(object? obj, string name)
        {
            var v = obj?.GetType().GetProperty(name)?.GetValue(obj);
            return v as string;
        }

        private static object? GetObjectProperty(object? obj, string name)
        {
            return obj?.GetType().GetProperty(name)?.GetValue(obj);
        }

        private static string Truncate(string value, int maxLength)
            => string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
    }
}
