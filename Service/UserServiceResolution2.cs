using Domain.Users2;
using Microsoft.Extensions.DependencyInjection;
using Service.IService;
using Service.Service;

namespace Service
{
    /// <summary>Registers the user-management services. Call builder.Services.AddUserServices2() once in MauiProgram.cs.</summary>
    public static class UserServiceResolution2
    {
        public static IServiceCollection AddUserServices2(this IServiceCollection services)
        {
            // One CurrentUser2 per app scope. The same instance serves the pages and the services.
            services.AddScoped<ICurrentUser2, CurrentUser2>();
            services.AddScoped<IAuthService2, AuthService2>();
            services.AddScoped<IUserManagementService2, UserManagementService2>();
            return services;
        }
    }
}
