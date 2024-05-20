using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace TelegramBots.Web.Meditr.Home.Statistics;

public class GetStatisticsQuery : IRequest<StatisticsDto>
{
}

public class GetStatisticsQueryHandler : IRequestHandler<GetStatisticsQuery, StatisticsDto>
{
    private readonly IDbConnection _connection;

    public GetStatisticsQueryHandler(IOptions<ConnectionStringsDto> connectionString)
    {
        _connection = new SqlConnection(connectionString.Value.InstagramBot);
    }

    public async Task<StatisticsDto> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
    {
        var result = new StatisticsDto();

        const string usersCountQuery = "SELECT COUNT(*) FROM Users";
        const string usersBlockedCountQuery = """
                                              SELECT COUNT(*) As Count FROM Users
                                              WHERE IsBotBlocked = 1
                                              """;
        const string usersJoinedChannelsQuery = """
                                                SELECT COUNT(*) As Count FROM Users
                                                WHERE IsInJoinedMandatoryChannels = 1
                                                """;
        const string usersJoinedByBotQuery = """
                                                 SELECT COUNT(*) As Count FROM UserJoinedChannels
                                             """;
        const string usersJoinedChannelsAndDidNotBlockedCountQuery = """
                                                                     SELECT COUNT(*) As Count FROM Users
                                                                     WHERE IsBotBlocked = 0 AND IsInJoinedMandatoryChannels = 1
                                                                     """;

        result.UsersCount = await _connection.QuerySingleAsync<int>(usersCountQuery);
        result.UsersBlockedBotCount = await _connection.QuerySingleAsync<int>(usersBlockedCountQuery);
        result.UsersJoinedChannelsCount = await _connection.QuerySingleAsync<int>(usersJoinedChannelsQuery);
        result.UsersJoinedChannelByBot = await _connection.QuerySingleAsync<int>(usersJoinedByBotQuery);
        result.UsersJoinedChannelsAndDidNotBlockBot = await _connection.QuerySingleAsync<int>(usersJoinedChannelsAndDidNotBlockedCountQuery);

        return result;
    }
}