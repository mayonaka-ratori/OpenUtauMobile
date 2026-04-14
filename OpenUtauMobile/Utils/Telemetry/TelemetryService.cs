using System.IO.Compression;
using Newtonsoft.Json;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using Serilog;
using Preferences = OpenUtau.Core.Util.Preferences;

namespace OpenUtauMobile.Utils.Telemetry;

/// <summary>
/// アプリ全体のクラッシュ・性能テレメトリを管理するシングルトン。
/// 外部サーバーへの送信は行わず、ローカルログへの構造化出力と
/// サポートバンドル (zip) エクスポートのみを行う。
/// </summary>
public sealed class TelemetryService
{
    public static TelemetryService Inst { get; } = new TelemetryService();

    private Guid _sessionId;
    private DateTime _sessionStartUtc;
    private DateTime? _startupCompleteUtc;

    private int _exceptionCount;
    private readonly List<string> _exceptionSignatures = new(20);
    private readonly object _exceptionLock = new();

    private int _slowFrameCount;
    private double _worstFrameMs;
    private DateTime _lastSlowFrameFlush = DateTime.UtcNow;
    private int _pendingSlowFrames;
    private readonly object _frameLock = new();

    private bool OptIn => Preferences.Default.TelemetryOptIn;

    private TelemetryService() { }

    // ─── セッション制御 ──────────────────────────────────────────────────────

    /// <summary>
    /// アプリ起動時に一度呼び出す。セッション ID を生成してログに出力する。
    /// </summary>
    public void StartSession()
    {
        _sessionId = Guid.NewGuid();
        _sessionStartUtc = DateTime.UtcNow;

        var header = new
        {
            session_id = _sessionId,
            start_utc = _sessionStartUtc.ToString("o"),
            app_version = AppInfo.VersionString,
            os_platform = DeviceInfo.Current.Platform.ToString(),
            os_version = DeviceInfo.Current.VersionString,
            device_manufacturer = DeviceInfo.Current.Manufacturer,
            device_model = DeviceInfo.Current.Model,
        };
        Log.Information("[TEL] session_start {Json}", JsonConvert.SerializeObject(header));
    }

    /// <summary>
    /// SplashScreen が HomePage へ遷移した直後に呼び出す。起動時間を算出してログに出力する。
    /// </summary>
    public void MarkStartupComplete()
    {
        _startupCompleteUtc = DateTime.UtcNow;
        double ms = (_startupCompleteUtc.Value - _sessionStartUtc).TotalMilliseconds;
        Log.Information("[TEL] startup_ms {Ms}", ms);
    }

    // ─── 例外収集 ────────────────────────────────────────────────────────────

    /// <summary>
    /// グローバル例外ハンドラから呼び出す。PII を除去した署名のみを記録する。
    /// </summary>
    public void ReportException(Exception ex, string source)
    {
        try
        {
            string sig = BuildSignature(ex);
            Log.Error("[TEL] exception source={Source} type={Type} sig={Sig}", source, ex.GetType().Name, sig);

            lock (_exceptionLock)
            {
                _exceptionCount++;
                if (_exceptionSignatures.Count < 20)
                    _exceptionSignatures.Add($"{source}:{ex.GetType().Name}:{sig}");
            }
        }
        catch
        {
            // テレメトリ自身が例外を発生させないようにする
        }
    }

    private static string BuildSignature(Exception ex)
    {
        // スタックトレースの最初の有効フレームのファイル名のみ残す (パスは削除)
        var frames = ex.StackTrace?.Split('\n') ?? [];
        string top = frames.FirstOrDefault(f => f.Contains(".cs:")) ?? frames.FirstOrDefault() ?? string.Empty;
        // ファイルパスを除去: "in C:\...\Foo.cs:line N" → "Foo.cs:line N"
        int inIdx = top.LastIndexOf(" in ", StringComparison.Ordinal);
        if (inIdx >= 0)
        {
            string filePart = top[(inIdx + 4)..];
            string fileName = Path.GetFileName(filePart.Split(':')[0]);
            string line = filePart.Contains(':') ? filePart[filePart.LastIndexOf(':')..] : string.Empty;
            top = fileName + line;
        }
        return top.Trim();
    }

    // ─── 性能メトリクス ──────────────────────────────────────────────────────

    /// <summary>
    /// 遅延フレームを記録する。60 秒毎に集約ログを出力する。
    /// </summary>
    public void ReportSlowFrame(double ms)
    {
        if (!OptIn) return;
        lock (_frameLock)
        {
            _slowFrameCount++;
            _pendingSlowFrames++;
            if (ms > _worstFrameMs) _worstFrameMs = ms;

            var now = DateTime.UtcNow;
            if ((now - _lastSlowFrameFlush).TotalSeconds >= 60)
            {
                Log.Information("[TEL] slow_frame_summary count={Count} worst={Worst}ms pending_interval={Pending}",
                    _slowFrameCount, _worstFrameMs, _pendingSlowFrames);
                _pendingSlowFrames = 0;
                _lastSlowFrameFlush = now;
            }
        }
    }

    // ─── プロジェクトスナップショット ─────────────────────────────────────────

    /// <summary>
    /// AutoSave などのタイミングで呼び出し、プロジェクト規模をログに記録する。
    /// </summary>
    public void CaptureProjectSnapshot()
    {
        if (!OptIn) return;
        try
        {
            var project = DocManager.Inst?.Project;
            if (project == null) return;

            int trackCount = project.tracks?.Count ?? 0;
            int totalNotes = project.parts?
                .OfType<OpenUtau.Core.Ustx.UVoicePart>()
                .Sum(p => p.notes?.Count ?? 0) ?? 0;
            int singerCount = SingerManager.Inst?.Singers?.Count ?? 0;
            long cacheSizeBytes = GetDirectorySizeBytes(PathManager.Inst.CachePath);

            var snap = new ProjectSnapshot
            {
                CapturedUtc = DateTime.UtcNow.ToString("o"),
                TrackCount = trackCount,
                TotalNotes = totalNotes,
                SingerCount = singerCount,
                CacheSizeBytes = cacheSizeBytes,
            };
            Log.Information("[TEL] project_snapshot {Json}", JsonConvert.SerializeObject(snap));

            lock (_exceptionLock)
            {
                // セッションメトリクス用にも保持 (最大5件)
                if (_snapshots.Count < 5) _snapshots.Add(snap);
            }
        }
        catch
        {
            // テレメトリ自身が例外を発生させないようにする
        }
    }

    private readonly List<ProjectSnapshot> _snapshots = [];

    private static long GetDirectorySizeBytes(string path)
    {
        if (!Directory.Exists(path)) return 0;
        try
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });
        }
        catch { return 0; }
    }

    // ─── サポートバンドルエクスポート ─────────────────────────────────────────

    /// <summary>
    /// ログファイル群とセッション情報を zip にまとめ、ファイルパスを返す。
    /// 呼び出し元 (SettingsPage) が Share API で共有シートを表示する。
    /// </summary>
    public string ExportSupportBundle()
    {
        string zipPath = Path.Combine(
            PathManager.Inst.CachePath,
            $"support-bundle-{DateTime.Now:yyyyMMdd-HHmmss}.zip");

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        // ログファイル群を追加
        string? logsDir = Path.GetDirectoryName(PathManager.Inst.LogFilePath);
        if (!string.IsNullOrEmpty(logsDir) && Directory.Exists(logsDir))
        {
            foreach (string logFile in Directory.GetFiles(logsDir))
            {
                try { archive.CreateEntryFromFile(logFile, Path.GetFileName(logFile)); }
                catch { /* ロック中ファイルはスキップ */ }
            }
        }

        // セッション JSON を追加
        string sessionJson = JsonConvert.SerializeObject(BuildSessionMetrics(), Formatting.Indented);
        var entry = archive.CreateEntry("session.json");
        using (var writer = new StreamWriter(entry.Open()))
        {
            writer.Write(sessionJson);
        }

        Log.Information("[TEL] support_bundle_exported path={Path}", zipPath);
        return zipPath;
    }

    private SessionMetrics BuildSessionMetrics()
    {
        lock (_exceptionLock)
        {
            return new SessionMetrics
            {
                SessionId = _sessionId.ToString(),
                StartUtc = _sessionStartUtc.ToString("o"),
                StartupMs = _startupCompleteUtc.HasValue
                    ? (_startupCompleteUtc.Value - _sessionStartUtc).TotalMilliseconds
                    : null,
                AppVersion = AppInfo.VersionString,
                OsPlatform = DeviceInfo.Current.Platform.ToString(),
                OsVersion = DeviceInfo.Current.VersionString,
                DeviceManufacturer = DeviceInfo.Current.Manufacturer,
                DeviceModel = DeviceInfo.Current.Model,
                ExceptionCount = _exceptionCount,
                ExceptionSignatures = new List<string>(_exceptionSignatures),
                SlowFrameCount = _slowFrameCount,
                WorstFrameMs = _worstFrameMs,
                ProjectSnapshots = new List<ProjectSnapshot>(_snapshots),
            };
        }
    }
}
