using System.Data;
using System.Data.SqlClient;
using Dapper;
using Youtube69bot.Downloader.DTOs;
using Youtube69bot.Downloader.Services;

namespace Youtube69bot.Downloader.Dapper;

public class YoutubeFileCacheService
{
    private readonly IDbConnection _connection;

    public YoutubeFileCacheService(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
    }

    public async Task<FileCacheInfo?> GetFileCacheInfoAsync(string userSentLink)
    {
        const string query = """
                             SELECT f.*, a.*, v.* FROM FileCacheInfos f
                             LEFT OUTER JOIN AudioCaches a ON f.Id = a.FileCacheInfoId
                             LEFT OUTER JOIN VideoCaches v ON f.Id = v.FileCacheInfoId
                             WHERE f.UserSentLinkKey = @UserSentLinkKey OR f.UserSentLink = @UserSentLink
                             """;

        var fileCacheInfoDictionary = new Dictionary<long, FileCacheInfo>();

        var results = (await _connection.QueryAsync<FileCacheInfo, AudioCache?, VideoCache?, FileCacheInfo>(
                query,
                (f, audioCache, videoCache) =>
                {
                    if (!fileCacheInfoDictionary.TryGetValue(f.Id, out var fatherEntry))
                    {
                        fatherEntry = f;
                        fileCacheInfoDictionary.Add(fatherEntry.Id, fatherEntry);
                    }

                    if (audioCache is not null) fatherEntry.Audios.Add(audioCache);
                    if (videoCache is not null) fatherEntry.Videos.Add(videoCache);
                    return fatherEntry;
                }, new { UserSentLink = userSentLink, UserSentLinkKey = LinkKeyGenerator.Generate(userSentLink) }))
            .Distinct()
            .ToList();

        return results.Count > 0 ? results[0] : null;
    }
}