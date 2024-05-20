namespace Youtube69bot.Dapper;

public class DownloadMetrics
{
    public long Id { get; set; }
    public DateTimeOffset Time { get; set; }
    public long ChatId { get; set; }
    public string? MessageId { get; set; }
    public string? YoutubeLink { get; set; }
    public DownloadMetricsStatus Status { get; set; }
    public long DownloadSizeInBytes { get; set; }
}

public enum DownloadMetricsStatus
{
    NewRequest = 0,
    Completed = 1,
    Failed = 2
}