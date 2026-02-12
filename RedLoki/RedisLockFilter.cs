using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace RedLoki
{
    public class RedisLockFilter : IAsyncActionFilter
    {
        private readonly string _cacheParameter;
        private readonly TimeSpan _lockExpiry;

        public RedisLockFilter(string cacheParameter, TimeUnit timeUnit = TimeUnit.Minutes, int value = 1)
        {
            _cacheParameter = cacheParameter ?? throw new ArgumentNullException($"{cacheParameter}");

            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException($"{value} must be greater than zero");
            }

            _lockExpiry = timeUnit switch
            {
                TimeUnit.Milliseconds => TimeSpan.FromMilliseconds(value),
                TimeUnit.Seconds => TimeSpan.FromSeconds(value),
                TimeUnit.Minutes => TimeSpan.FromMinutes(value),
                TimeUnit.Hours => TimeSpan.FromHours(value),
                TimeUnit.Days => TimeSpan.FromDays(value),
                _ => throw new ArgumentNullException($"{timeUnit}")
            };
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var _redis = context.HttpContext.RequestServices.GetService<IConnectionMultiplexer>()
                ?? throw new ArgumentNullException(nameof(context.HttpContext.RequestServices));

            var db = _redis.GetDatabase();
            var idempotencyKey = GetCacheParameterValue(context);

            if (string.IsNullOrEmpty(idempotencyKey))
            {
                context.Result = new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                    new { message = $"Cache parameter '{_cacheParameter}' is required" });
                return;
            }

            var lockKey = GenerateLockKey(idempotencyKey);
            var lockValue = Guid.NewGuid().ToString();

            // Try to acquire the lock
            var lockAcquired = await AcquireLock(db, lockKey, lockValue);

            if (!lockAcquired)
            {
                context.Result = new Microsoft.AspNetCore.Mvc.ConflictObjectResult(
                    new { message = "Resource is locked by another request" });
                return;
            }

            // Execute the action
            await next();
        }
        private async Task<bool> AcquireLock(IDatabase db, string lockKey, string lockValue)
        {
            return await db.StringSetAsync(
                lockKey,
                lockValue,
                _lockExpiry,
                When.NotExists);
        }
        private string? GetCacheParameterValue(ActionExecutingContext context)
        {
            // Check route values first
            if (context.RouteData.Values.TryGetValue(_cacheParameter, out var routeValue))
            {
                return routeValue?.ToString();
            }

            // Check action parameters
            if (context.ActionArguments.TryGetValue(_cacheParameter, out var actionArgumentValue))
            {
                return actionArgumentValue?.ToString();
            }

            // Check query string
            if (context.HttpContext.Request.Query.TryGetValue(_cacheParameter, out var queryValue))
            {
                return queryValue.ToString();
            }

            // Check request headers
            if (context.HttpContext.Request.Headers.TryGetValue(_cacheParameter, out var headerValue))
            {
                return headerValue.ToString();
            }

            // Check if any action parameter has a cacheParameter property
            foreach (var argument in context.ActionArguments.Values.Where(m => m != null))
            {
                var cacheParameterProperty = argument.GetType().GetProperty(_cacheParameter);
                if (cacheParameterProperty != null)
                {
                    return cacheParameterProperty.GetValue(argument)?.ToString();
                }
            }

            return null;
        }
        private static string GenerateLockKey(string key)
        {
            return $"lock:{key}";
        }
    }
    public enum TimeUnit
    {
        Milliseconds = 0,
        Seconds = 1,
        Minutes = 2,
        Hours = 3,
        Days = 4
    }
}