using API_DASM.Data;
using Microsoft.EntityFrameworkCore;

namespace API_DASM.Services
{
    public class DocumentPermissionService
    {
        private readonly AppDbContext _context;

        public DocumentPermissionService(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanView(Guid userId)
        {
            return await HasPermission(
                userId,
                x => x.CanView);
        }

        public async Task<bool> CanEdit(Guid userId)
        {
            return await HasPermission(
                userId,
                x => x.CanEdit);
        }

        public async Task<bool> CanShare(Guid userId)
        {
            return await HasPermission(
                userId,
                x => x.CanShare);
        }

        public async Task<bool> CanDownload(Guid userId)
        {
            return await HasPermission(
                userId,
                x => x.CanDownload);
        }

        public async Task<bool> CanDelete(Guid userId)
        {
            return await HasPermission(
                userId,
                x => x.CanDelete);
        }

        private async Task<bool> HasPermission(
            Guid userId,
            Func<Models.AccessLevel, bool> permission)
        {
            var user =
                await _context.Users
                    .Include(x => x.AccessLevel)
                    .FirstOrDefaultAsync(x =>
                        x.Id == userId);

            if (user?.AccessLevel == null)
            {
                return false;
            }

            return permission(user.AccessLevel);
        }
    }
}
