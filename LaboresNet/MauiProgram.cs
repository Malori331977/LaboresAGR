using CurrieTechnologies.Razor.SweetAlert2;
using JtSegEncrypta;
using LaboresNet.Data;
using LaboresNet.Data.Navigation;
using Microsoft.Extensions.Logging;
using PortalEmpleadoGeotime.Client.Data;
using PortalEmpleadoGeotime.Client.Data.DataBaseLocal;
using PortalEmpleadoGeotime.Client.Data.Interfaces;
using PortalEmpleadoGeotime.Client.Data.State;
using Radzen;

namespace LaboresNet
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


            builder.Services.AddScoped<IGeoTimeConnect, GeoTimeConnect>()
                .AddScoped<IEmailRepository, EmailRepository>()
                .AddScoped<IBrowserService, BrowserService>()
                .AddScoped<IGridExtensions, PortalEmpleadoGeotime.Client.Data.GridExtensions>()
                .AddScoped<IEncriptaService, EncriptaService>()
                .AddScoped<LoginState>()
                .AddScoped<IMarcaGrupalService, MarcaGrupalService>()
                .AddScoped<PeriodicExecutor>();

            builder.Services.AddSingleton<NavigationService>();
            builder.Services.AddScoped<BlazorNavigationService>();

            builder.Services.AddSweetAlert2();
            builder.Services.AddBlazorBootstrap();
            builder.Services.AddRadzenComponents();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif
#if ANDROID
#pragma warning disable CA1416 // Validate platform compatibility
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebView, CustomBlazorWebViewHandler>();
            });
#pragma warning restore CA1416 // Validate platform compatibility
#endif

            return builder.Build();
        }
    }
}


