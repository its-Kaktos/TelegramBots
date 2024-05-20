using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace TelegramBots.Web.Meditr.UsersChart.GetUserUsagesPerDay;

public class GetUserUsagesPerDayQuery : IRequest<IEnumerable<GetUserUsagesPerDayDto>>
{
}

public class GetUserUsagesPerDayQueryHandler : IRequestHandler<GetUserUsagesPerDayQuery, IEnumerable<GetUserUsagesPerDayDto>>
{
    private readonly IDbConnection _connection;

    public GetUserUsagesPerDayQueryHandler(IOptions<ConnectionStringsDto> connectionString)
    {
        _connection = new SqlConnection(connectionString.Value.Metrics);
    }

    public async Task<IEnumerable<GetUserUsagesPerDayDto>> Handle(GetUserUsagesPerDayQuery request, CancellationToken cancellationToken)
    {
        const string query = """
                             SELECT CONVERT(date, Time) AS Date,
                                    COUNT(CASE WHEN Status = 0 THEN 0 END) AS RequestsCount,
                                    COUNT(CASE WHEN Status = 1 THEN 1 END) AS SuccessfulCount,
                                    COUNT(CASE WHEN Status = 2 THEN 1 END) AS ErrorCount
                             FROM DownloadMetrics
                             GROUP BY CONVERT(date, Time)
                             ORDER BY Date
                             """;

        var results = await _connection.QueryAsync<GetUserUsagesPerDayDto>(query);

        return results;
    }
}