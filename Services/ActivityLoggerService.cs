using API_DASM.Data;
using API_DASM.Models;

namespace API_DASM.Services
{
    public class ActivityLoggerService
    {
        private readonly AppDbContext _context;

        private readonly IHttpContextAccessor _http;

        public ActivityLoggerService(
            AppDbContext context,
            IHttpContextAccessor http)
        {
            _context = context;

            _http = http;
        }

        public async Task LogActivity(
            Guid userId,
            string action,
            string? entityName,
            string? entityId,
            string? description,
            string? appType = null)
        {
            var ip =
                _http.HttpContext?
                .Connection?
                .RemoteIpAddress?
                .ToString();

            var userAgent =
                _http.HttpContext?
                .Request?
                .Headers["User-Agent"]
                .ToString();

            var log = new ActivityLog
            {
                UserId = userId,

                Action = action,

                EntityName = entityName,

                EntityId = entityId,

                Description = description,

                AppType = appType,

                IpAddress = ip,

                UserAgent = userAgent,

                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);

            await _context.SaveChangesAsync();
        }
    }
}

