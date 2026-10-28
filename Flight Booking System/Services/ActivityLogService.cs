
using System.Security.Claims;

using Flight_Booking_System.Models;
using Flight_Booking_System.ModelsServices;
using Flight_Booking_System.Repositories;

namespace Flight_Booking_System.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IRepository<ActivityLog> _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(
            IRepository<ActivityLog> repository,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string control,
            string action,
            string? entityName = null,
            string? entityId = null,
            string? description = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var userId = httpContext?.User?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            var ipAddress = httpContext?
                .Connection
                .RemoteIpAddress?
                .ToString();

            var activityLog = new ActivityLog
            {
                UserId = userId,
                Control = control,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                IpAddress = ipAddress,
                CreatedAt = DateTime.Now
            };

            await _repository.InsertAsync(activityLog);
            await _repository.CommitAsync();
        }
    }
}
