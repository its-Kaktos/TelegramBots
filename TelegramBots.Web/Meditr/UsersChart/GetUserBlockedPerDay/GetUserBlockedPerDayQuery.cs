using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace TelegramBots.Web.Meditr.UsersChart.GetUserBlockedPerDay;

public class GetUserBlockedPerDayQuery : IRequest<IEnumerable<GetUserBlockedPerDayDto>>
{
}

public class GetUserBlockedPerDayAndCountQueryHandler : IRequestHandler<GetUserBlockedPerDayQuery, IEnumerable<GetUserBlockedPerDayDto>>
{
    private readonly IDbConnection _connection;

    public GetUserBlockedPerDayAndCountQueryHandler(IOptions<ConnectionStringsDto> connectionString)
    {
        _connection = new SqlConnection(connectionString.Value.InstagramBot);
    }

    public async Task<IEnumerable<GetUserBlockedPerDayDto>> Handle(GetUserBlockedPerDayQuery request, CancellationToken cancellationToken)
    {
        const string query = """
                             SELECT CONVERT(date, SWITCHOFFSET(DateEventHappened,0)) AS Date, COUNT(*) AS Count
                             FROM UserEvents
                             WHERE EventType = 0
                             GROUP BY CONVERT(date, SWITCHOFFSET(DateEventHappened,0))
                             ORDER BY Date
                             """;

        var results = await _connection.QueryAsync<GetUserBlockedPerDayDto>(query);

        return results;
    }
}