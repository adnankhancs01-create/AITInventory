using Common.Users2;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class UserAccountRepo2 : IUserAccountRepo2
    {
        private readonly InventoryDbContext _db;

        public UserAccountRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        public Task<bool> AnyRootAsync()
        {
            return _db.UserMst.AnyAsync(u => u.IsRoot);
        }

        public Task<int> CountUsersAsync()
        {
            return _db.UserMst.CountAsync();
        }

        public async Task<UserMst?> FindLegacyOwnerAsync()
        {
            var withBusiness = await _db.UserMst
                .Where(u => u.BusinessName != null && u.BusinessName != "")
                .OrderBy(u => u.UserId)
                .FirstOrDefaultAsync();

            return withBusiness ?? await _db.UserMst.OrderBy(u => u.UserId).FirstOrDefaultAsync();
        }

        public Task<List<UserMst>> GetUsersWithoutBusinessAsync()
        {
            return _db.UserMst.Where(u => u.BusinessId == null).ToListAsync();
        }

        public Task<Business2?> GetBusinessAsync()
        {
            return _db.Set<Business2>().OrderBy(b => b.Id).FirstOrDefaultAsync();
        }

        public void AddBusiness(Business2 business)
        {
            _db.Set<Business2>().Add(business);
        }

        public Task<UserMst?> FindByLoginAsync(string identifier)
        {
            return _db.UserMst
                .Include(u => u.Role)
                .Include(u => u.Business)
                .FirstOrDefaultAsync(u => u.Email == identifier || u.UserName == identifier);
        }

        public Task<UserMst?> GetUserAsync(int userId)
        {
            return _db.UserMst.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public Task<UserMst?> GetRootAsync()
        {
            return _db.UserMst.FirstOrDefaultAsync(u => u.IsRoot);
        }

        public Task<bool> EmailExistsAsync(string email, int excludeUserId)
        {
            return _db.UserMst.AnyAsync(u => u.Email == email && u.UserId != excludeUserId);
        }

        public Task<bool> UserNameExistsAsync(string userName, int excludeUserId)
        {
            return _db.UserMst.AnyAsync(u => u.UserName == userName && u.UserId != excludeUserId);
        }

        public void AddUser(UserMst user)
        {
            _db.UserMst.Add(user);
        }

        public async Task<List<UserListItemDto2>> GetUserListAsync(UserFilterDto2 filter)
        {
            var query = _db.UserMst.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string s = filter.Search.Trim();
                query = query.Where(u => (u.UserName != null && u.UserName.Contains(s))
                    || (u.Email != null && u.Email.Contains(s))
                    || (u.mobileNo != null && u.mobileNo.Contains(s)));
            }
            if (filter.RoleId.HasValue) query = query.Where(u => u.RoleId == filter.RoleId.Value);
            if (filter.IsActive.HasValue) query = query.Where(u => u.IsActive == filter.IsActive.Value);

            return await query
                .OrderByDescending(u => u.IsRoot).ThenBy(u => u.UserName)
                .Select(u => new UserListItemDto2
                {
                    Id = u.UserId,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    MobileNo = u.mobileNo ?? string.Empty,
                    RoleId = u.RoleId,
                    RoleName = u.IsRoot ? "Owner" : (u.Role != null ? u.Role.Name : string.Empty),
                    IsRoot = u.IsRoot,
                    IsActive = u.IsActive,
                    LastLoginOn = u.LastLoginOn
                }).ToListAsync();
        }
    }
}
