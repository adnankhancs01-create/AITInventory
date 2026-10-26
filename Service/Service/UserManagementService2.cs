using Common;
using Common.Helpers;
using Common.Users2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.Users2;
using Service.IService;

namespace Service.Service
{
    /// <summary>Root-only administration of the business profile, users, roles and role permissions.</summary>
    public class UserManagementService2 : IUserManagementService2
    {
        private readonly IUserUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;
        private readonly ICurrentUser2 _currentUser;

        public UserManagementService2(IUserUnitOfWorkFactory2 uowFactory, ILogRepository logRepo, ICurrentUser2 currentUser)
        {
            _uowFactory = uowFactory;
            _logRepo = logRepo;
            _currentUser = currentUser;
        }

        private static string? Clean(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        /// <summary>Service-layer guard: every method here is for the business owner only.</summary>
        private BaseResponse<T>? RequireRoot<T>()
        {
            if (!_currentUser.IsAuthenticated) return UserResult2.Fail<T>("Please log in first.");
            if (!_currentUser.IsRoot) return UserResult2.Fail<T>("Only the business owner can do this.");
            return null;
        }

        // ================================================================== business

        public Task<BaseResponse<BusinessDto2>> GetBusinessAsync()
        {
            return UserResult2.RunAsync(async () =>
            {
                var denied = RequireRoot<BusinessDto2>();
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                var b = await uow.Accounts.GetBusinessAsync();
                if (b == null) return UserResult2.Fail<BusinessDto2>("The business has not been set up yet.");

                return UserResult2.Ok(new BusinessDto2
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address,
                    City = b.City,
                    Phone = b.Phone,
                    Email = b.Email,
                    TaxNumber = b.TaxNumber
                });
            }, _logRepo, nameof(GetBusinessAsync), _currentUser.UserId);
        }

        public Task<BaseResponse<bool>> SaveBusinessAsync(BusinessDto2 dto)
        {
            return UserResult2.RunAsync(async () =>
            {
                var denied = RequireRoot<bool>();
                if (denied != null) return denied;

                var errors = new List<string>();
                string name = (dto.Name ?? string.Empty).Trim();
                string address = (dto.Address ?? string.Empty).Trim();
                string? email = Clean(dto.Email, 150);

                if (name.Length == 0) errors.Add("Business name is required.");
                if (name.Length > 200) errors.Add("Business name can be at most 200 characters.");
                if (address.Length == 0) errors.Add("Business address is required.");
                if (address.Length > 300) errors.Add("Business address can be at most 300 characters.");
                if (email != null && !email.IsValidEmail()) errors.Add("Business email is not valid.");
                if (errors.Count > 0) return UserResult2.Fail<bool>(errors);

                await using var uow = _uowFactory.Create();
                await uow.BeginTransactionAsync();

                var b = await uow.Accounts.GetBusinessAsync();
                if (b == null) return UserResult2.Fail<bool>("The business has not been set up yet.");

                b.Name = name;
                b.Address = address;
                b.City = Clean(dto.City, 100);
                b.Phone = Clean(dto.Phone, 50);
                b.Email = email;
                b.TaxNumber = Clean(dto.TaxNumber, 50);

                // Keep the old fields in step: the existing slip printing still reads them from the owner's account.
                var root = await uow.Accounts.GetRootAsync();
                if (root != null)
                {
                    root.BusinessName = b.Name;
                    root.BusinessAddress = b.Address;
                    if (b.Phone != null) root.mobileNo = b.Phone;
                }

                await uow.SaveChangesAsync();
                await uow.CommitAsync();
                return UserResult2.Ok(true, "Business profile saved.");
            }, _logRepo, nameof(SaveBusinessAsync), _currentUser.UserId, dto);
        }

        // ================================================================== users

        public Task<BaseResponse<List<UserListItemDto2>>> GetUsersAsync(UserFilterDto2 filter)
        {
            return UserResult2.RunAsync(async () =>
            {
                var denied = RequireRoot<List<UserListItemDto2>>();
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                var list = await uow.Accounts.GetUserListAsync(filter ?? new UserFilterDto2());
                return UserResult2.Ok(list);
            }, _logRepo, nameof(GetUsersAsync), _currentUser.UserId, filter);
        }

        public Task<BaseResponse<int>> SaveUserAsync(UserSaveDto2 dto)
        {
            return UserResult2.RunAsync(async () =>
            {
                var denied = RequireRoot<int>();
                if (denied != null) return denied;

                var errors = new List<string>();
                string userName = (dto.UserName ?? string.Empty).Trim();
                string email = (dto.Email ?? string.Empty).Trim();
                bool isNew = dto.Id <= 0;

                if (userName.Length == 0) errors.Add("User name is required.");
                if (userName.Length > 100) errors.Add("User name can be at most 100 characters.");
                if (email.Length == 0 || !email.IsValidEmail()) errors.Add("A valid email address is required.");
                if (email.Length > 256) errors.Add("Email can be at most 256 characters.");
                if (isNew)
                {
                    string? passwordError = PasswordRules2.Validate(dto.Password);
                    if (passwordError != null) errors.Add(passwordError);
                }
                if (errors.Count > 0) return UserResult2.Fail<int>(errors);

                await using var uow = _uowFactory.Create();
                await uow.BeginTransactionAsync();

                var business = await uow.Accounts.GetBusinessAsync();
                if (business == null) return UserResult2.Fail<int>("The business has not been set up yet.");

                UserMst? user;
                if (isNew)
                {
                    user = new UserMst { CreatedOn = DateTime.Now, BusinessId = business.Id };
                }
                else
                {
                    user = await uow.Accounts.GetUserAsync(dto.Id);
                    if (user == null) return UserResult2.Fail<int>("User not found.");
                }

                if (await uow.Accounts.UserNameExistsAsync(userName, isNew ? 0 : user.UserId))
                    return UserResult2.Fail<int>("This user name is already used.");
                if (await uow.Accounts.EmailExistsAsync(email, isNew ? 0 : user.UserId))
                    return UserResult2.Fail<int>("This email is already used.");

                if (user.IsRoot)
                {
                    // The owner always has every screen: no role, and never inactive.
                    if (!dto.IsActive) return UserResult2.Fail<int>("The owner account cannot be deactivated.");
                    user.RoleId = null;
                }
                else
                {
                    if (!dto.RoleId.HasValue) return UserResult2.Fail<int>("Choose a role for this user.");
                    var role = await uow.Roles.GetRoleAsync(dto.RoleId.Value);
                    if (role == null) return UserResult2.Fail<int>("Role not found.");
                    if (!role.IsActive && user.RoleId != role.Id) return UserResult2.Fail<int>("This role is inactive.");

                    user.RoleId = role.Id;
                    user.IsActive = dto.IsActive;
                    user.BusinessId ??= business.Id;
                }

                user.UserName = userName;
                user.Email = email;
                user.mobileNo = Clean(dto.MobileNo, 50);

                if (isNew)
                {
                    user.PasswordHash = PasswordRules2.Hash(dto.Password!);
                    user.Password = string.Empty;
                    uow.Accounts.AddUser(user);
                }

                await uow.SaveChangesAsync();
                await uow.CommitAsync();
                return UserResult2.Ok(user.UserId, "User saved.");
            }, _logRepo, nameof(SaveUserAsync), _currentUser.UserId, dto);
        }

        public Task<BaseResponse<bool>> ResetPasswordAsync(int userId, string newPassword)
        {
            return UserResult2.RunAsync(async () =>
            {
                var denied = RequireRoot<bool>();
                if (denied != null) return denied;

                string? passwordError = PasswordRules2.Validate(newPassword);
                if (passwordError != null) return UserResult2.Fail<bool>(passwordError);

                await using var uow = _uowFactory.Create();
                var user = await uow.Accounts.GetUserAsync(userId);
                if (user == null) return UserResult2.Fail<bool>("User not found.");

                user.PasswordHash = PasswordRules2.Hash(newPassword);
                user.Password = string.Empty;
                await uow.SaveChangesAsync();
                return UserResult2.Ok(true, "Password reset.");
            }, _logRepo, nameof(ResetPasswordAsync), _currentUser.UserId, new { userId });
        }

        public Task<BaseResponse<bool>> SetUserActiveAsync(int userId, bool isActive)
        {
            return UserResult2.RunAsync(async () =>
            {
                var denied = RequireRoot<bool>();
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                var user = await uow.Accounts.GetUserAsync(userId);
                if (user == null) return UserResult2.Fail<bool>("User not found.");
                if (user.IsRoot && !isActive) return UserResult2.Fail<bool>("The owner account cannot be deactivated.");

                user.IsActive = isActive;
                await uow.SaveChangesAsync();
                return UserResult2.Ok(true, isActive ? "User activated." : "User deactivated.");
            }, _logRepo, nameof(SetUserActiveAsync), _currentUser.UserId, new { userId, isActive });
        }

        // ================================================================== roles + permissions

        public Task<BaseResponse<List<RoleDto2>>> GetRolesAsync()
        {
            return UserResult2.RunAsync(async () =>
            {
                var denied = RequireRoot<List<RoleDto2>>();
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                return UserResult2.Ok(await uow.Roles.GetRoleDtosAsync());
            }, _logRepo, nameof(GetRolesAsync), _currentUser.UserId);
        }

        public Task<BaseResponse<int>> SaveRoleAsync(RoleDto2 dto)
        {
            return UserResult2.RunAsync(async () =>
            {
                var denied = RequireRoot<int>();
                if (denied != null) return denied;

                var errors = new List<string>();
                string name = (dto.Name ?? string.Empty).Trim();
                if (name.Length == 0) errors.Add("Role name is required.");
                if (name.Length > 50) errors.Add("Role name can be at most 50 characters.");
                if ((dto.Description?.Trim().Length ?? 0) > 200) errors.Add("Description can be at most 200 characters.");
                if (errors.Count > 0) return UserResult2.Fail<int>(errors);

                await using var uow = _uowFactory.Create();

                if (await uow.Roles.RoleNameExistsAsync(name, dto.Id))
                    return UserResult2.Fail<int>("A role with this name already exists.");

                UserRole2? role;
                if (dto.Id > 0)
                {
                    role = await uow.Roles.GetRoleAsync(dto.Id);
                    if (role == null) return UserResult2.Fail<int>("Role not found.");

                    if (role.IsActive && !dto.IsActive && await uow.Roles.RoleHasActiveUsersAsync(role.Id))
                        return UserResult2.Fail<int>("This role still has active users. Move them to another role first.");
                }
                else
                {
                    role = new UserRole2 { CreatedOn = DateTime.Now };
                    uow.Roles.AddRole(role);
                }

                role.Name = name;
                role.Description = Clean(dto.Description, 200);
                role.IsActive = dto.IsActive;

                await uow.SaveChangesAsync();
                return UserResult2.Ok(role.Id, "Role saved.");
            }, _logRepo, nameof(SaveRoleAsync), _currentUser.UserId, dto);
        }

        public Task<BaseResponse<List<ScreenDto2>>> GetScreensAsync()
        {
            return UserResult2.RunAsync(async () =>
            {
                var denied = RequireRoot<List<ScreenDto2>>();
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                var screens = (await uow.Roles.GetScreensAsync())
                    .Where(s => !s.IsRootOnly)
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new ScreenDto2
                    {
                        ScreenKey = s.ScreenKey,
                        DisplayName = s.DisplayName,
                        GroupName = s.GroupName,
                        Route = s.Route,
                        SortOrder = s.SortOrder
                    }).ToList();

                return UserResult2.Ok(screens);
            }, _logRepo, nameof(GetScreensAsync), _currentUser.UserId);
        }

        public Task<BaseResponse<List<string>>> GetRolePermissionsAsync(int roleId)
        {
            return UserResult2.RunAsync(async () =>
            {
                var denied = RequireRoot<List<string>>();
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                return UserResult2.Ok(await uow.Roles.GetPermittedScreenKeysAsync(roleId));
            }, _logRepo, nameof(GetRolePermissionsAsync), _currentUser.UserId, new { roleId });
        }

        public Task<BaseResponse<bool>> SaveRolePermissionsAsync(int roleId, List<string> screenKeys)
        {
            return UserResult2.RunAsync(async () =>
            {
                var denied = RequireRoot<bool>();
                if (denied != null) return denied;

                await using var uow = _uowFactory.Create();
                await uow.BeginTransactionAsync();

                var role = await uow.Roles.GetRoleAsync(roleId);
                if (role == null) return UserResult2.Fail<bool>("Role not found.");

                var assignable = (await uow.Roles.GetScreensAsync())
                    .Where(s => !s.IsRootOnly)
                    .Select(s => s.ScreenKey)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var wanted = (screenKeys ?? new List<string>())
                    .Where(k => assignable.Contains(k))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var existing = await uow.Roles.GetPermissionsAsync(roleId);
                foreach (var row in existing)
                {
                    if (wanted.Contains(row.ScreenKey)) row.CanView = true;
                    else uow.Roles.RemovePermission(row);
                }

                var existingKeys = existing.Select(p => p.ScreenKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var key in wanted.Where(k => !existingKeys.Contains(k)))
                    uow.Roles.AddPermission(new RoleScreenPermission2 { RoleId = roleId, ScreenKey = key, CanView = true });

                await uow.SaveChangesAsync();
                await uow.CommitAsync();
                return UserResult2.Ok(true, "Permissions saved. Users of this role see the change the next time they log in.");
            }, _logRepo, nameof(SaveRolePermissionsAsync), _currentUser.UserId, new { roleId });
        }
    }
}
