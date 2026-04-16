//using Android.Renderscripts;
using AIT_Inventory_App.C_.Models;
using Common.Helpers;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Radzen;
using Service;
using System.Drawing.Printing;
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
            builder.Services.AddSingleton<NotificationServiceMessage>();
#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            // Load appsettings.json

            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            // Connection string (SQLite or SQL Server LocalDB for desktop)
            //builder.Services.AddBlazoredLocalStorage();
            var cs = builder.Configuration.GetConnectionString("DefaultConnection");

            // Data layer
            builder.Services.AddRepositoryResolution(cs);
            // Service layer
            builder.Services.AddServiceResolution();
            builder.Services.AddRadzenComponents();
            builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));
            builder.Services.AddScoped<ISlipPrinter, SlipPrinter>();
            builder.Services.Configure<Paths>(
    builder.Configuration.GetSection("Paths"));

            return builder.Build();
        }
    }
}
