using Common.Users2;
using Domain.Users2;

namespace Service.Service
{
    /// <summary>
    /// Holds the logged-in user for the running app. The desktop app has one user at a time, so this is a plain
    /// in-memory object registered as scoped (the Blazor Hybrid app uses a single scope for its whole run).
    /// </summary>
    public class CurrentUser2 : ICurrentUser2
    {
        private HashSet<string> _allowed = new(StringComparer.OrdinalIgnoreCase);
        private SessionDto2? _session;

        public event Action? Changed;

        public bool IsAuthenticated => _session != null;
        public int? UserId => _session?.UserId;
        public string UserName => _session?.UserName ?? string.Empty;
        public string RoleName => _session?.RoleName ?? string.Empty;
        public string BusinessName => _session?.BusinessName ?? string.Empty;
        public bool IsRoot => _session?.IsRoot ?? false;

        public bool CanView(string screenKey)
        {
            if (_session == null) return false;
            if (_session.IsRoot) return true;
            return _allowed.Contains(screenKey);
        }

        public bool CanAccessPath(string path)
        {
            if (ScreenCatalog2.IsPublicPath(path)) return true;
            if (_session == null) return false;
            if (_session.IsRoot) return true;

            var screen = ScreenCatalog2.FindByPath(path);
            return screen != null && !screen.IsRootOnly && _allowed.Contains(screen.ScreenKey);
        }

        public void SignIn(SessionDto2 session)
        {
            _session = session;
            _allowed = new HashSet<string>(session.AllowedScreens, StringComparer.OrdinalIgnoreCase);
            Changed?.Invoke();
        }

        public void SignOut()
        {
            _session = null;
            _allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Changed?.Invoke();
        }
    }
}
