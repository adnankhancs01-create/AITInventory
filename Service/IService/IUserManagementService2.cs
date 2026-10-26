using Common;
using Common.Users2;

namespace Service.IService
{
    /// <summary>Root-only administration: business profile, users, roles and screen permissions.</summary>
    public interface IUserManagementService2
    {
        Task<BaseResponse<BusinessDto2>> GetBusinessAsync();
        Task<BaseResponse<bool>> SaveBusinessAsync(BusinessDto2 dto);

        Task<BaseResponse<List<UserListItemDto2>>> GetUsersAsync(UserFilterDto2 filter);
        Task<BaseResponse<int>> SaveUserAsync(UserSaveDto2 dto);
        Task<BaseResponse<bool>> ResetPasswordAsync(int userId, string newPassword);
        Task<BaseResponse<bool>> SetUserActiveAsync(int userId, bool isActive);

        Task<BaseResponse<List<RoleDto2>>> GetRolesAsync();
        Task<BaseResponse<int>> SaveRoleAsync(RoleDto2 dto);

        /// <summary>Screens that can be assigned to roles (administration screens are root-only and not listed).</summary>
        Task<BaseResponse<List<ScreenDto2>>> GetScreensAsync();
        Task<BaseResponse<List<string>>> GetRolePermissionsAsync(int roleId);
        Task<BaseResponse<bool>> SaveRolePermissionsAsync(int roleId, List<string> screenKeys);
    }
}
