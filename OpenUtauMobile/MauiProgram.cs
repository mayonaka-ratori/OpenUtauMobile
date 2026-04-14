using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using OpenUtau.Core;
using OpenUtauMobile.Resources.Strings;
using OpenUtauMobile.Utils.Permission;
using Serilog;
using System.Text;
#if ANDROID
using OpenUtauMobile.Platforms.Android.Utils.Permission;
#endif
#if WINDOWS
using OpenUtauMobile.Platforms.Windows.Utils.Permission;
#endif
using OpenUtauMobile.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Globalization;

namespace OpenUtauMobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Register encoding provider
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit() // Use MauiCommunityToolkit
                .UseSkiaSharp() // Use SkiaSharp for rendering
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                ;

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            InitLogging(); //

            Log.Information("OS: " + DeviceInfo.Current.Platform.ToString());
            Log.Information("OS version: " + DeviceInfo.Current.VersionString);
            Log.Information("Manufacturer: " + DeviceInfo.Current.Manufacturer);
            Log.Information("Model: " + DeviceInfo.Current.Model);

            return builder.Build();
        }

        /// <summary>
        /// Initialises the Serilog logger and registers the global unhandled-exception handler.
        /// </summary>
        public static void InitLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Debug()
                .WriteTo.Logger(lc => lc
                    .MinimumLevel.Information() // 
                    .WriteTo.File(PathManager.Inst.LogFilePath, rollingInterval: RollingInterval.Day, encoding: Encoding.UTF8)) // write to log file
                //.WriteTo.Logger(lc => lc
                //    .MinimumLevel.ControlledBy(DebugViewModel.Sink.Inst.LevelSwitch)
                //    .WriteTo.Sink(DebugViewModel.Sink.Inst))
                .CreateLogger();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((sender, args) => {
                Log.Error((Exception)args.ExceptionObject, "Unhandled exception"); // unhandled exception
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification((Exception)args.ExceptionObject));
            });
            Log.Information("==========开始记录日志==========");
        }
    }
}
