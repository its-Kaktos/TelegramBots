using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace TelegramBots.Web.Meditr.UsersChart.GetUserUnBlockedPerDay;

public class GetUserUnBlockedPerDayQuery : IRequest<IEnumerable<GetUserUnBlockedPerDayDto>>
{
}

public class GetUserUnBlockedPerDayQueryHandler : IRequestHandler<GetUserUnBlockedPerDayQuery, IEnumerable<GetUserUnBlockedPerDayDto>>
{
    private readonly IDbConnection _connection;

    public GetUserUnBlockedPerDayQueryHandler(IOptions<ConnectionStringsDto> connectionString)
    {
        _connection = new SqlConnection(connectionString.Value.InstagramBot);
    }

    public async Task<IEnumerable<GetUserUnBlockedPerDayDto>> Handle(GetUserUnBlockedPerDayQuery request, CancellationToken cancellationToken)
    {
        const string query = """
                             SELECT CONVERT(date, SWITCHOFFSET(DateEventHappened,0)) AS Date, COUNT(*) AS Count
                             FROM UserEvents
                             WHERE EventType = 1
                             GROUP BY CONVERT(date, SWITCHOFFSET(DateEventHappened,0))
                             ORDER BY Date
                             """;

        var results = await _connection.QueryAsync<GetUserUnBlockedPerDayDto>(query);

        return results;
    }
}