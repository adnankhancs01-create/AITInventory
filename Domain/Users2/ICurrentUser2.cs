using Common.Users2;

namespace Domain.Users2
{
    /// <summary>The logged-in user of this app session. Services read it; only the login/logout flow changes it.</summary>
    public interface ICurrentUser2
    {
        bool IsAuthenticated { get; }
        int? UserId { get; }
        string UserName { get; }
        string RoleName { get; }
        string BusinessName { get; }
        bool IsRoot { get; }

        /// <summary>Raised after sign-in or sign-out so the menu can refresh.</summary>
        event Action? Changed;

        /// <summary>True for the root user, or when the role has the screen.</summary>
        bool CanView(string screenKey);

        /// <summary>Route-level check used by the page guard: public paths are open, root opens everything, others need a permitted screen.</summary>
        bool CanAccessPath(string path);

        void SignIn(SessionDto2 session);
        void SignOut();
    }
}
