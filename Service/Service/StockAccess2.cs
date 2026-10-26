using Common;
using Common.Stock2;
using Domain.Users2;

namespace Service.Service
{
    /// <summary>Permission and user-id helpers shared by the Stock2 services.</summary>
    public static class StockAccess2
    {
        /// <summary>
        /// Returns a failure response when the logged-in user may not use the screen that performs this action,
        /// otherwise null. This is the service-layer check; the page guard alone is not enough.
        /// </summary>
        public static BaseResponse<T>? Check<T>(ICurrentUser2 user, string screenKey)
        {
            if (!user.IsAuthenticated)
                return StockResult2.Fail<T>("Please log in first.");
            if (!user.CanView(screenKey))
                return StockResult2.Fail<T>("You do not have permission to perform this action.");
            return null;
        }

        /// <summary>The logged-in user wins over any user id sent by the caller; the old system user is only a last fallback.</summary>
        public static int ActingUserId(ICurrentUser2 user, int? requested)
        {
            return user.UserId ?? requested ?? StockConstants2.SystemUserId;
        }
    }
}
