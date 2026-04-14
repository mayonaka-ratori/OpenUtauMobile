using Newtonsoft.Json;

namespace OpenUtauMobile.Utils.Telemetry;

/// <summary>
/// 1セッション分のテレメトリデータ (JSON シリアライズ用 POCO)。
/// 外部送信は行わず、サポートバンドルとして手動エクスポートする。
/// </summary>
public class SessionMetrics
{
    [JsonProperty("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [JsonProperty("start_utc")]
    public string StartUtc { get; set; } = string.Empty;

    [JsonProperty("startup_ms")]
    public double? StartupMs { get; set; }

    [JsonProperty("app_version")]
    public string AppVersion { get; set; } = string.Empty;

    [JsonProperty("os_platform")]
    public string OsPlatform { get; set; } = string.Empty;

    [JsonProperty("os_version")]
    public string OsVersion { get; set; } = string.Empty;

    [JsonProperty("device_manufacturer")]
    public string DeviceManufacturer { get; set; } = string.Empty;

    [JsonProperty("device_model")]
    public string DeviceModel { get; set; } = string.Empty;

    [JsonProperty("exception_count")]
    public int ExceptionCount { get; set; }

    [JsonProperty("exception_signatures")]
    public List<string> ExceptionSignatures { get; set; } = [];

    [JsonProperty("slow_frame_count")]
    public int SlowFrameCount { get; set; }

    [JsonProperty("worst_frame_ms")]
    public double WorstFrameMs { get; set; }

    [JsonProperty("project_snapshots")]
    public List<ProjectSnapshot> ProjectSnapshots { get; set; } = [];
}

/// <summary>
/// AutoSave 等のタイミングで記録するプロジェクト状態のスナップショット。
/// </summary>
public class ProjectSnapshot
{
    [JsonProperty("captured_utc")]
    public string CapturedUtc { get; set; } = string.Empty;

    [JsonProperty("track_count")]
    public int TrackCount { get; set; }

    [JsonProperty("total_notes")]
    public int TotalNotes { get; set; }

    [JsonProperty("singer_count")]
    public int SingerCount { get; set; }

    [JsonProperty("cache_size_bytes")]
    public long CacheSizeBytes { get; set; }
}
