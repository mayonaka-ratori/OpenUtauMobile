using Android.App;
using Android.Runtime;
using OpenUtauMobile.Utils.Telemetry;
using Serilog;

namespace OpenUtauMobile
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            // Android ネイティブ (JNI 経由) から投げられた未処理例外を捕捉する。
            // e.Handled は false のまま — クラッシュはさせつつ、ログだけ記録する。
            AndroidEnvironment.UnhandledExceptionRaiser += (s, e) =>
            {
                try
                {
                    Log.Error(e.Exception, "[TEL] AndroidEnvironment.UnhandledExceptionRaiser");
                    TelemetryService.Inst.ReportException(e.Exception, "AndroidEnvironment");
                }
                catch { }
            };
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
