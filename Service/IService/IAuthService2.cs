using Common;
using Common.Users2;

namespace Service.IService
{
    /// <summary>First-run setup, login and logout.</summary>
    public interface IAuthService2
    {
        /// <summary>Syncs the screen registry (and first-run data) and reports whether the Business Setup screen is needed.</summary>
        Task<BaseResponse<StartupStateDto2>> GetStartupStateAsync();

        /// <summary>Creates the business and its root (owner) user, then signs the owner in.</summary>
        Task<BaseResponse<SessionDto2>> SetupBusinessAsync(BusinessSetupRequestDto2 request);

        Task<BaseResponse<SessionDto2>> LoginAsync(LoginRequestDto2 request);

        Task LogoutAsync();
    }
}
