using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Instagram69Bot.Dapper;

public class DownloadMetricsService : IDisposable
{
    private readonly IDbConnection _connection;


    public DownloadMetricsService(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
    }

    public async Task CreateTableIfDoesNotExistsAsync()
    {
        const string createTableQuery = """
                                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='DownloadMetrics')
                                        BEGIN
                                            CREATE TABLE DownloadMetrics (
                                                Id int PRIMARY KEY IDENTITY(1,1),
                                                Time DATETIMEOFFSET NOT NULL,
                                                ChatId BIGINT NOT NULL,
                                                MessageId NVARCHAR(100) NULL,
                                                InstagramLink NVARCHAR(750) NULL,
                                                Status INT NOT NULL,
                                                DownloadSizeInBytes BIGINT NOT NULL
                                            )
                                        END
                                        """;
        await _connection.ExecuteAsync(createTableQuery);
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