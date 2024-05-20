using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace Instagram69bot.Downloader.Dapper;

public class DownloadMetricsService : IDisposable
{
    private readonly IDbConnection _connection;


    public DownloadMetricsService(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
    }

    public async Task AddAsync(long chatId, DownloadMetricsStatus status, long downloadSizeInBytes, string? instagramLink, string? messageId)
    {
        const string addNewMetricQuery = """
                                            INSERT INTO DownloadMetrics (Time, ChatId, Status, DownloadSizeInBytes, MessageId, InstagramLink)
                                            VALUES (@Time, @ChatId, @Status, @DownloadSizeInBytes, @MessageId, @InstagramLink)
                                         """;
        var parameters = new
        {
            ChatId = chatId,
            Status = (int)status,
            DownloadSizeInBytes = downloadSizeInBytes,
            MessageId = messageId,
            InstagramLink = instagramLink,
            Time = DateTimeOffset.Now
        };
        await _connection.ExecuteAsync(addNewMetricQuery, parameters);
    }

    public async Task AddAsync(long chatId, DownloadMetricsStatus status, long downloadSizeInBytes, string? instagramLink, int? messageId)
    {
        var messageIdText = messageId is null ? "" : messageId.ToString();
        await AddAsync(chatId, status, downloadSizeInBytes, instagramLink, messageIdText);
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}