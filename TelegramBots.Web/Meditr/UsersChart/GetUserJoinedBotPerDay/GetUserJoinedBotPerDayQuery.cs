using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace TelegramBots.Web.Meditr.UsersChart.GetUserJoinedBotPerDay;

public class GetUserJoinedBotPerDayQuery : IRequest<IEnumerable<GetUserJoinedBotPerDayDto>>
{
}

public class GetUserJoinedBotPerDayQueryHandler : IRequestHandler<GetUserJoinedBotPerDayQuery, IEnumerable<GetUserJoinedBotPerDayDto>>
{
    private readonly IDbConnection _connection;

    public GetUserJoinedBotPerDayQueryHandler(IOptions<ConnectionStringsDto> connectionString)
    {
        _connection = new SqlConnection(connectionString.Value.InstagramBot);
    }

    public async Task<IEnumerable<GetUserJoinedBotPerDayDto>> Handle(GetUserJoinedBotPerDayQuery request, CancellationToken cancellationToken)
    {
        const string query = """
                             SELECT CONVERT(date, SWITCHOFFSET(JoinedDate,0)) AS Date, COUNT(*) AS Count
                             FROM Users
                             GROUP BY CONVERT(date, SWITCHOFFSET(JoinedDate,0))
                             ORDER BY Date
                             """;

        var results = await _connection.QueryAsync<GetUserJoinedBotPerDayDto>(query);

        return results;
    }
}