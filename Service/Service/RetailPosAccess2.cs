using Common;
using Domain.Users2;

namespace Service.Service
{
    /// <summary>Login, screen-permission and owner-only checks for the Retail POS services (the service-layer guard).</summary>
    public static class RetailPosAccess2
    {
        public static BaseResponse<T>? RequireLogin<T>(ICurrentUser2 user)
        {
            if (!user.IsAuthenticated)
                return RetailPosResult2.Fail<T>("Please log in first.");
            return null;
        }

        /// <summary>The user must be logged in and the role must have the screen that performs this action.</summary>
        public static BaseResponse<T>? Check<T>(ICurrentUser2 user, string screenKey)
        {
            var login = RequireLogin<T>(user);
            if (login != null) return login;

            if (!user.CanView(screenKey))
                return RetailPosResult2.Fail<T>("You do not have permission to perform this action.");
            return null;
        }

        /// <summary>Sensitive financial actions (credit limit, reversals, balance adjustments) are for the business owner only.</summary>
        public static BaseResponse<T>? RequireOwner<T>(ICurrentUser2 user, string action)
        {
            var login = RequireLogin<T>(user);
            if (login != null) return login;

            if (!user.IsRoot)
                return RetailPosResult2.Fail<T>($"Only the business owner can {action}.");
            return null;
        }
    }
}
