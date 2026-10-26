using Common.Users2;
using Domain.Entities;
using Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Data.Repositories
{
    public class UserRoleRepo2 : IUserRoleRepo2
    {
        private readonly InventoryDbContext _db;

        public UserRoleRepo2(InventoryDbContext db)
        {
            _db = db;
        }

        public Task<int> CountRolesAsync()
        {
            return _db.Set<UserRole2>().CountAsync();
        }

        public Task<List<RoleDto2>> GetRoleDtosAsync()
        {
            return _db.Set<UserRole2>().AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new RoleDto2
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    UserCount = _db.UserMst.Count(u => u.RoleId == r.Id)
                }).ToListAsync();
        }

        public Task<UserRole2?> GetRoleAsync(int roleId)
        {
            return _db.Set<UserRole2>().FirstOrDefaultAsync(r => r.Id == roleId);
        }

        public Task<bool> RoleNameExistsAsync(string name, int excludeRoleId)
        {
            return _db.Set<UserRole2>().AnyAsync(r => r.Name == name && r.Id != excludeRoleId);
        }

        public Task<bool> RoleHasActiveUsersAsync(int roleId)
        {
            return _db.UserMst.AnyAsync(u => u.RoleId == roleId && u.IsActive);
        }

        public void AddRole(UserRole2 role)
        {
            _db.Set<UserRole2>().Add(role);
        }

        public Task<List<AppScreen2>> GetScreensAsync()
        {
            return _db.Set<AppScreen2>().OrderBy(s => s.SortOrder).ToListAsync();
        }

        public void AddScreen(AppScreen2 screen)
        {
            _db.Set<AppScreen2>().Add(screen);
        }

        public async Task<List<string>> GetPermittedScreenKeysAsync(int roleId)
        {
            var rootOnly = await _db.Set<AppScreen2>().Where(s => s.IsRootOnly).Select(s => s.ScreenKey).ToListAsync();

            return await _db.Set<RoleScreenPermission2>().AsNoTracking()
                .Where(p => p.RoleId == roleId && p.CanView && !rootOnly.Contains(p.ScreenKey))
                .Select(p => p.ScreenKey)
                .ToListAsync();
        }

        public Task<List<RoleScreenPermission2>> GetPermissionsAsync(int roleId)
        {
            return _db.Set<RoleScreenPermission2>().Where(p => p.RoleId == roleId).ToListAsync();
        }

        public void AddPermission(RoleScreenPermission2 permission)
        {
            _db.Set<RoleScreenPermission2>().Add(permission);
        }

        public void RemovePermission(RoleScreenPermission2 permission)
        {
            _db.Set<RoleScreenPermission2>().Remove(permission);
        }
    }
}
