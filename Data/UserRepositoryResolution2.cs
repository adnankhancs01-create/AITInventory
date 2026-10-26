using Data.Repositories;
using Domain.IRepositories;
using Microsoft.Extensions.DependencyInjection;

namespace Data
{
    /// <summary>Registers the user-management repositories. Call builder.Services.AddUserRepositories2() once in MauiProgram.cs.</summary>
    public static class UserRepositoryResolution2
    {
        public static IServiceCollection AddUserRepositories2(this IServiceCollection services)
        {
            services.AddScoped<IUserAccountRepo2, UserAccountRepo2>();
            services.AddScoped<IUserRoleRepo2, UserRoleRepo2>();
            services.AddScoped<IUserUnitOfWorkFactory2, UserUnitOfWorkFactory2>();
            return services;
        }
    }
}
