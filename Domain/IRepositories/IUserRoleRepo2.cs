using Common.Users2;
using Domain.Entities;

namespace Domain.IRepositories
{
    public interface IUserRoleRepo2
    {
        Task<int> CountRolesAsync();
        Task<List<RoleDto2>> GetRoleDtosAsync();
        Task<UserRole2?> GetRoleAsync(int roleId);
        Task<bool> RoleNameExistsAsync(string name, int excludeRoleId);
        Task<bool> RoleHasActiveUsersAsync(int roleId);
        void AddRole(UserRole2 role);

        Task<List<AppScreen2>> GetScreensAsync();
        void AddScreen(AppScreen2 screen);

        /// <summary>Screen keys the role may view (root-only screens are never returned).</summary>
        Task<List<string>> GetPermittedScreenKeysAsync(int roleId);
        Task<List<RoleScreenPermission2>> GetPermissionsAsync(int roleId);
        void AddPermission(RoleScreenPermission2 permission);
        void RemovePermission(RoleScreenPermission2 permission);
    }
}
