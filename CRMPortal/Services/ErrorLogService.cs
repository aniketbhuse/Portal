using CRMPortal.Data;
using CRMPortal.Models;

namespace CRMPortal.Services
{
    public class ErrorLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ErrorLogService(AppDbContext context,
                               IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogErrorAsync(
            Exception ex,
            string controller,
            string action)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var log = new ErrorLogs
            {
                ControllerName = controller,
                ActionName = action,

                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                InnerException = ex.InnerException?.Message,

                UserId = httpContext?.Session.GetInt32("UserId"),
                UserName = httpContext?.Session.GetString("FullName"),

                IPAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),

                Browser = httpContext?
                    .Request.Headers["User-Agent"].ToString(),

                CreatedDate = DateTime.Now
            };

            _context.ErrorLogs.Add(log);

            await _context.SaveChangesAsync();
        }
    }
}