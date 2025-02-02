using System.Diagnostics.CodeAnalysis;
using Haihv.Elis.Tool.ChuyenDvhc.Extensions;
using Haihv.Elis.Tool.ChuyenDvhc.Services;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc
{
    public static class MauiProgram
    {
        [Experimental("EXTEXP0018")]
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

            // Đăng ký FileService
            builder.Services.AddSingleton<IFileService, FileService>();
            // Đăng ký Serilog (Write to File)
            var logger = new LoggerConfiguration()
                .WriteTo.File(
                    Settings.FilePath.LogFile($"Log_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
                    rollingInterval: RollingInterval.Infinite,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .Enrich.FromLogContext()
                .CreateLogger();
            builder.Services.AddSerilog(logger);

            // Đăng ký HybridCaching
            builder.Services.AddHybridCaching();
            // Đăng ký BlazorWebView
            builder.Services.AddMauiBlazorWebView();

            // Đăng ký MudBlazor
            builder.Services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.NewestOnTop = false;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.VisibleStateDuration = 10000;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
                config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            });

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}