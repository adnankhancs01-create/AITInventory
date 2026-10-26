using Common;
using Common.Helpers;
using Common.Users2;
using Domain.Entities;
using Domain.IRepositories;
using Domain.Users2;
using Service.IService;

namespace Service.Service
{
    public class AuthService2 : IAuthService2
    {
        private const string GenericLoginError = "Invalid user name/email or password.";

        private static bool _seeded;
        private static readonly SemaphoreSlim SeedLock = new(1, 1);
        // Verified against when the user does not exist, so a wrong user name and a wrong password take the same time.
        private static readonly Lazy<string> DummyHash = new(() => PasswordRules2.Hash("not-a-real-password"));

        private readonly IUserUnitOfWorkFactory2 _uowFactory;
        private readonly ILogRepository _logRepo;
        private readonly ICurrentUser2 _currentUser;

        public AuthService2(IUserUnitOfWorkFactory2 uowFactory, ILogRepository logRepo, ICurrentUser2 currentUser)
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

        // ================================================================== startup + seed

        public Task<BaseResponse<StartupStateDto2>> GetStartupStateAsync()
        {
            return UserResult2.RunAsync(async () =>
            {
                await EnsureSeededAsync();

                await using var uow = _uowFactory.Create();
                bool hasRoot = await uow.Accounts.AnyRootAsync();
                return UserResult2.Ok(new StartupStateDto2 { SetupRequired = !hasRoot });
            }, _logRepo, nameof(GetStartupStateAsync));
        }

        /// <summary>
        /// Runs once per app start and is safe to repeat: syncs the screen registry, creates the default roles only when
        /// there are no roles at all, and (for databases created before user management) promotes the oldest account to owner.
        /// </summary>
        private async Task EnsureSeededAsync()
        {
            if (_seeded) return;

            await SeedLock.WaitAsync();
            try
            {
                if (_seeded) return;

                await using var uow = _uowFactory.Create();
                await uow.BeginTransactionAsync();

                await SyncScreensAsync(uow);
                await SeedDefaultRolesAsync(uow);
                await PromoteLegacyOwnerAsync(uow);

                await uow.SaveChangesAsync();
                await uow.CommitAsync();
                _seeded = true;
            }
            finally
            {
                SeedLock.Release();
            }
        }

        private static async Task SyncScreensAsync(IUserUnitOfWork2 uow)
        {
            var existing = (await uow.Roles.GetScreensAsync()).ToDictionary(s => s.ScreenKey, StringComparer.OrdinalIgnoreCase);

            foreach (var def in ScreenCatalog2.All)
            {
                if (existing.TryGetValue(def.ScreenKey, out var row))
                {
                    row.DisplayName = def.DisplayName;
                    row.GroupName = def.GroupName;
                    row.Route = def.Route;
                    row.SortOrder = def.SortOrder;
                    row.IsRootOnly = def.IsRootOnly;
                }
                else
                {
                    uow.Roles.AddScreen(new AppScreen2
                    {
                        ScreenKey = def.ScreenKey,
                        DisplayName = def.DisplayName,
                        GroupName = def.GroupName,
                        Route = def.Route,
                        SortOrder = def.SortOrder,
                        IsRootOnly = def.IsRootOnly
                    });
                }
            }
        }

        private static async Task SeedDefaultRolesAsync(IUserUnitOfWork2 uow)
        {
            // Only on the very first start. After that the owner decides which roles exist.
            if (await uow.Roles.CountRolesAsync() > 0) return;

            foreach (var def in ScreenCatalog2.DefaultRoles)
            {
                var role = new UserRole2 { Name = def.Name, Description = def.Description, IsActive = true, CreatedOn = DateTime.Now };
                foreach (var key in def.ScreenKeys.Distinct())
                    role.Permissions.Add(new RoleScreenPermission2 { ScreenKey = key, CanView = true });

                uow.Roles.AddRole(role);
            }
        }

        private static async Task PromoteLegacyOwnerAsync(IUserUnitOfWork2 uow)
        {
            if (await uow.Accounts.AnyRootAsync()) return;
            if (await uow.Accounts.CountUsersAsync() == 0) return; // fresh database: the Business Setup screen will create the owner

            var owner = await uow.Accounts.FindLegacyOwnerAsync();
            if (owner == null) return;

            var business = await uow.Accounts.GetBusinessAsync();
            if (business == null)
            {
                business = new Business2
                {
                    Name = Clean(owner.BusinessName, 200) ?? "My Business",
                    Address = Clean(owner.BusinessAddress, 300) ?? "-",
                    Phone = Clean(owner.mobileNo, 50),
                    Email = Clean(owner.Email, 150),
                    CreatedOn = DateTime.Now
                };
                uow.Accounts.AddBusiness(business);
            }

            owner.IsRoot = true;
            owner.IsActive = true;
            owner.RoleId = null;
            owner.CreatedOn ??= DateTime.Now;

            foreach (var user in await uow.Accounts.GetUsersWithoutBusinessAsync())
            {
                if (business.Id > 0) user.BusinessId = business.Id;
                else user.Business = business;
            }
        }

        // ================================================================== business setup

        public Task<BaseResponse<SessionDto2>> SetupBusinessAsync(BusinessSetupRequestDto2 request)
        {
            return UserResult2.RunAsync(async () =>
            {
                var errors = new List<string>();

                string businessName = (request.BusinessName ?? string.Empty).Trim();
                string address = (request.Address ?? string.Empty).Trim();
                string userName = (request.OwnerUserName ?? string.Empty).Trim();
                string email = (request.OwnerEmail ?? string.Empty).Trim();
                string? businessEmail = Clean(request.BusinessEmail, 150);

                if (businessName.Length == 0) errors.Add("Business name is required.");
                if (businessName.Length > 200) errors.Add("Business name can be at most 200 characters.");
                if (address.Length == 0) errors.Add("Business address is required.");
                if (address.Length > 300) errors.Add("Business address can be at most 300 characters.");
                if (businessEmail != null && !businessEmail.IsValidEmail()) errors.Add("Business email is not valid.");

                if (userName.Length == 0) errors.Add("Your user name is required.");
                if (userName.Length > 100) errors.Add("User name can be at most 100 characters.");
                if (email.Length == 0 || !email.IsValidEmail()) errors.Add("A valid email address is required.");
                if (email.Length > 256) errors.Add("Email can be at most 256 characters.");

                string? passwordError = PasswordRules2.Validate(request.Password);
                if (passwordError != null) errors.Add(passwordError);
                else if (request.Password != request.ConfirmPassword) errors.Add("Password and confirmation do not match.");

                if (errors.Count > 0) return UserResult2.Fail<SessionDto2>(errors);

                await EnsureSeededAsync();

                await using var uow = _uowFactory.Create();
                await uow.BeginTransactionAsync();

                if (await uow.Accounts.AnyRootAsync())
                    return UserResult2.Fail<SessionDto2>("Setup is already complete. Please log in.");
                if (await uow.Accounts.UserNameExistsAsync(userName, 0))
                    return UserResult2.Fail<SessionDto2>("This user name is already used.");
                if (await uow.Accounts.EmailExistsAsync(email, 0))
                    return UserResult2.Fail<SessionDto2>("This email is already used.");

                var business = await uow.Accounts.GetBusinessAsync();
                if (business == null)
                {
                    business = new Business2 { CreatedOn = DateTime.Now };
                    uow.Accounts.AddBusiness(business);
                }

                business.Name = businessName;
                business.Address = address;
                business.City = Clean(request.City, 100);
                business.Phone = Clean(request.Phone, 50);
                business.Email = businessEmail;
                business.TaxNumber = Clean(request.TaxNumber, 50);

                var owner = new UserMst
                {
                    UserName = userName,
                    Email = email,
                    mobileNo = business.Phone,
                    PasswordHash = PasswordRules2.Hash(request.Password),
                    Password = string.Empty,
                    // The existing slip printing reads the business name and address from the first user that has them.
                    BusinessName = business.Name,
                    BusinessAddress = business.Address,
                    Business = business,
                    IsRoot = true,
                    IsActive = true,
                    CreatedOn = DateTime.Now,
                    LastLoginOn = DateTime.Now
                };
                uow.Accounts.AddUser(owner);

                await uow.SaveChangesAsync();
                await uow.CommitAsync();

                var session = BuildSession(owner, "Owner", business.Name, ScreenCatalog2.All.Select(s => s.ScreenKey).ToList());
                _currentUser.SignIn(session);
                return UserResult2.Ok(session, "Business created. Welcome!");
            }, _logRepo, nameof(SetupBusinessAsync));
        }

        // ================================================================== login / logout

        public Task<BaseResponse<SessionDto2>> LoginAsync(LoginRequestDto2 request)
        {
            return UserResult2.RunAsync(async () =>
            {
                string identifier = (request.Identifier ?? string.Empty).Trim();
                string password = request.Password ?? string.Empty;
                if (identifier.Length == 0 || password.Length == 0)
                    return UserResult2.Fail<SessionDto2>(GenericLoginError);

                await EnsureSeededAsync();

                await using var uow = _uowFactory.Create();
                var user = await uow.Accounts.FindByLoginAsync(identifier);

                if (user == null)
                {
                    PasswordRules2.Verify(password, DummyHash.Value);
                    return UserResult2.Fail<SessionDto2>(GenericLoginError);
                }

                bool passwordOk;
                bool upgradeLegacy = false;
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    passwordOk = PasswordRules2.Verify(password, user.PasswordHash);
                }
                else
                {
                    // Account created before password hashing existed: accept the old password once and replace it by a hash.
                    passwordOk = PasswordRules2.LegacyPlainMatches(password, user.Password);
                    upgradeLegacy = passwordOk;
                }

                if (!passwordOk || !user.IsActive)
                    return UserResult2.Fail<SessionDto2>(GenericLoginError);

                List<string> allowed;
                string roleName;
                if (user.IsRoot)
                {
                    allowed = ScreenCatalog2.All.Select(s => s.ScreenKey).ToList();
                    roleName = "Owner";
                }
                else
                {
                    if (user.RoleId == null || user.Role == null || !user.Role.IsActive)
                        return UserResult2.Fail<SessionDto2>("Your account has no active role. Ask the business owner to assign one.");

                    allowed = await uow.Roles.GetPermittedScreenKeysAsync(user.RoleId.Value);
                    roleName = user.Role.Name;
                }

                if (upgradeLegacy)
                {
                    user.PasswordHash = PasswordRules2.Hash(password);
                    user.Password = string.Empty;
                }
                user.LastLoginOn = DateTime.Now;
                await uow.SaveChangesAsync();

                string businessName = user.Business?.Name ?? user.BusinessName ?? string.Empty;
                var session = BuildSession(user, roleName, businessName, allowed);
                _currentUser.SignIn(session);
                return UserResult2.Ok(session, "Login successful");
            }, _logRepo, nameof(LoginAsync));
        }

        public Task LogoutAsync()
        {
            _currentUser.SignOut();
            return Task.CompletedTask;
        }

        private static SessionDto2 BuildSession(UserMst user, string roleName, string businessName, List<string> allowed)
        {
            return new SessionDto2
            {
                UserId = user.UserId,
                UserName = user.UserName ?? user.Email ?? string.Empty,
                Email = user.Email ?? string.Empty,
                RoleId = user.RoleId,
                RoleName = roleName,
                IsRoot = user.IsRoot,
                BusinessId = user.BusinessId,
                BusinessName = businessName,
                AllowedScreens = allowed
            };
        }
    }
}
