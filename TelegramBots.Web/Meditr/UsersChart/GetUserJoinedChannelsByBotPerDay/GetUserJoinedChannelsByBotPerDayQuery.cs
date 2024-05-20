using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace TelegramBots.Web.Meditr.UsersChart.GetUserJoinedChannelsByBotPerDay;

public class GetUserJoinedChannelsByBotPerDayQuery : IRequest<IEnumerable<GetUserJoinedChannelsByBotPerDayDto>>
{
}

public class GetUserJoinedChannelsByBotPerDayQueryHandler : IRequestHandler<GetUserJoinedChannelsByBotPerDayQuery, IEnumerable<GetUserJoinedChannelsByBotPerDayDto>>
{
    private readonly IDbConnection _connection;

    public GetUserJoinedChannelsByBotPerDayQueryHandler(IOptions<ConnectionStringsDto> connectionString)
    {
        _connection = new SqlConnection(connectionString.Value.InstagramBot);
    }

    public async Task<IEnumerable<GetUserJoinedChannelsByBotPerDayDto>> Handle(GetUserJoinedChannelsByBotPerDayQuery request, CancellationToken cancellationToken)
    {
        const string query = """
                             SELECT CONVERT(date, SWITCHOFFSET(JoinedDate,0)) AS Date, COUNT(*) AS Count
                             FROM UserJoinedChannels
                             GROUP BY CONVERT(date, SWITCHOFFSET(JoinedDate,0))
                             ORDER BY Date
                             """;

        var results = await _connection.QueryAsync<GetUserJoinedChannelsByBotPerDayDto>(query);

        return results;
    }
}