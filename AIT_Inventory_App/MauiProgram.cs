using Data;
using Data.SupportiveEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Service;
namespace AIT_Inventory_App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            // Load appsettings.json
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            // Connection string (SQLite or SQL Server LocalDB for desktop)
            var cs = builder.Configuration.GetConnectionString("DefaultConnection");

            //DbContext
            builder.Services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(cs),
            ServiceLifetime.Scoped);
            
            // Identity
            builder.Services.AddIdentity<ApplicationUserIdentity, IdentityRole>(options =>
            {
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<InventoryDbContext>()
            .AddDefaultTokenProviders();

            // Data layer
            builder.Services.AddRepositoryResolution(cs);
            // Service layer
            builder.Services.AddServiceResolution();
            return builder.Build();
        }
    }
}
