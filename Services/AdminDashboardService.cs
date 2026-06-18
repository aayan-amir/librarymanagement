using Npgsql;
using NpgsqlTypes;
using QrDigitalLibrary.Api.Contracts;

namespace QrDigitalLibrary.Api.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IConfiguration _configuration;

    public AdminDashboardService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<AdminDashboardResponse> GetSummaryAsync(string? branchCode, CancellationToken cancellationToken)
    {
        const string sql = """
            select
                count(*) filter (where t.transaction_status in ('ISSUED', 'OVERDUE'))::integer as total_issued_books,
                count(*) filter (where t.returned_at is null and t.due_at < now())::integer as overdue_books,
                count(distinct t.student_id) filter (where t.returned_at is null and t.transaction_status in ('ISSUED', 'OVERDUE'))::integer as active_borrowers,
                count(distinct bc.copy_id) filter (where bc.copy_status = 'AVAILABLE')::integer as active_inventory,
                coalesce(sum(f.amount) filter (where f.fine_status <> 'PAID'), 0) as outstanding_fines,
                coalesce(sum(fp.amount_paid), 0) as fine_collection_total
            from book_copies bc
            left join branches br on br.branch_id = bc.branch_id
            left join transactions t on t.copy_id = bc.copy_id
            left join fines f on f.transaction_id = t.transaction_id
            left join fine_payments fp on fp.fine_id = f.fine_id
            where (@branch_code is null or br.branch_code = @branch_code);
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("branch_code", NpgsqlDbType.Varchar)
        {
            Value = string.IsNullOrWhiteSpace(branchCode) ? DBNull.Value : branchCode.Trim()
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new AdminDashboardResponse
            {
                TotalIssuedBooks = 0,
                OverdueBooks = 0,
                ActiveBorrowers = 0,
                ActiveInventory = 0,
                OutstandingFines = 0,
                FineCollectionTotal = 0
            };
        }

        return new AdminDashboardResponse
        {
            TotalIssuedBooks = reader.GetInt32(0),
            OverdueBooks = reader.GetInt32(1),
            ActiveBorrowers = reader.GetInt32(2),
            ActiveInventory = reader.GetInt32(3),
            OutstandingFines = reader.GetDecimal(4),
            FineCollectionTotal = reader.GetDecimal(5)
        };
    }

    public async Task<AnalyticsDashboardResponse> GetAnalyticsAsync(string? branchCode, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        return new AnalyticsDashboardResponse
        {
            MostActiveStudents = await ReadItemsAsync(connection, """
                select s.full_name, s.university_id, count(*)::numeric
                from transactions t
                join students s on s.student_id = t.student_id
                join book_copies bc on bc.copy_id = t.copy_id
                left join branches br on br.branch_id = bc.branch_id
                where (@branch_code is null or br.branch_code = @branch_code)
                group by s.full_name, s.university_id
                order by count(*) desc, s.full_name
                limit 8;
                """, branchCode, cancellationToken),
            PopularBooks = await ReadItemsAsync(connection, """
                select b.title, b.author, count(*)::numeric
                from transactions t
                join book_copies bc on bc.copy_id = t.copy_id
                join books b on b.book_id = bc.book_id
                left join branches br on br.branch_id = bc.branch_id
                where (@branch_code is null or br.branch_code = @branch_code)
                group by b.title, b.author
                order by count(*) desc, b.title
                limit 8;
                """, branchCode, cancellationToken),
            PeakIssuingTimings = await ReadItemsAsync(connection, """
                select to_char(date_trunc('hour', issued_at), 'HH24:00'), 'Issue hour', count(*)::numeric
                from transactions
                where transaction_type = 'ISSUE'
                group by date_trunc('hour', issued_at)
                order by count(*) desc
                limit 8;
                """, null, cancellationToken),
            FineTrends = await ReadItemsAsync(connection, """
                select to_char(date_trunc('day', assessed_at), 'YYYY-MM-DD'), 'Daily assessed fines', coalesce(sum(amount), 0)
                from fines
                group by date_trunc('day', assessed_at)
                order by date_trunc('day', assessed_at) desc
                limit 8;
                """, null, cancellationToken),
            DepartmentWiseStatistics = await ReadItemsAsync(connection, """
                select d.department_name, d.department_code, count(*)::numeric
                from transactions t
                join students s on s.student_id = t.student_id
                join departments d on d.department_id = s.department_id
                group by d.department_name, d.department_code
                order by count(*) desc, d.department_name
                limit 8;
                """, null, cancellationToken),
            DefaultersList = await ReadItemsAsync(connection, """
                select s.full_name, s.university_id, count(distinct t.transaction_id)::numeric
                from students s
                join transactions t on t.student_id = s.student_id
                where t.returned_at is null
                  and t.due_at < now()
                  and t.transaction_status in ('ISSUED', 'OVERDUE')
                group by s.full_name, s.university_id
                order by count(distinct t.transaction_id) desc, s.full_name
                limit 8;
                """, null, cancellationToken),
            DailyTransactions = await ReadItemsAsync(connection, """
                select to_char(date_trunc('day', issued_at), 'YYYY-MM-DD'),
                       'Issues: ' || count(*) filter (where transaction_type = 'ISSUE')
                           || '  Returns: ' || count(*) filter (where returned_at is not null),
                       count(*)::numeric
                from transactions
                where issued_at >= now() - interval '14 days'
                group by date_trunc('day', issued_at)
                order by date_trunc('day', issued_at) desc
                limit 8;
                """, null, cancellationToken),
            ActiveBorrowers = await ReadItemsAsync(connection, """
                select s.full_name, s.university_id, count(*)::numeric
                from students s
                join transactions t on t.student_id = s.student_id
                where t.returned_at is null and t.transaction_status in ('ISSUED', 'OVERDUE')
                group by s.full_name, s.university_id
                order by count(*) desc, s.full_name
                limit 8;
                """, null, cancellationToken),
            FineReports = await ReadItemsAsync(connection, """
                select fine_status, 'Total Amount: Rs. ' || total_amount, fine_count::numeric
                from vw_fine_reports
                order by total_amount desc;
                """, null, cancellationToken)
        };
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Supabase PostgreSQL connection string is missing on the API server.");
        }

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<IReadOnlyList<DashboardListItemDto>> ReadItemsAsync(
        NpgsqlConnection connection,
        string sql,
        string? branchCode,
        CancellationToken cancellationToken)
    {
        var items = new List<DashboardListItemDto>();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("branch_code", NpgsqlDbType.Varchar)
        {
            Value = string.IsNullOrWhiteSpace(branchCode) ? DBNull.Value : branchCode.Trim()
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new DashboardListItemDto
            {
                Label = reader.GetString(0),
                Detail = reader.GetString(1),
                Value = reader.GetDecimal(2)
            });
        }

        return items;
    }

    public async Task<ActivityLogsResponse> GetActivityLogsAsync(int limit, CancellationToken cancellationToken)
    {
        const string countSql = "select count(*)::integer from activity_logs;";
        const string logsSql = """
            select activity_log_id, actor_university_id, actor_role, action_type,
                   action_status, endpoint, failure_reason, created_at
            from activity_logs
            order by created_at desc
            limit @limit;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);

        int totalCount;
        await using (var countCmd = new NpgsqlCommand(countSql, connection))
        {
            totalCount = (int)(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0);
        }

        var logs = new List<ActivityLogDto>();
        await using var command = new NpgsqlCommand(logsSql, connection);
        command.Parameters.AddWithValue("limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            logs.Add(new ActivityLogDto
            {
                ActivityLogId = reader.GetGuid(0),
                ActorUniversityId = reader.IsDBNull(1) ? null : reader.GetString(1),
                ActorRole = reader.IsDBNull(2) ? null : reader.GetString(2),
                ActionType = reader.GetString(3),
                ActionStatus = reader.GetString(4),
                Endpoint = reader.IsDBNull(5) ? null : reader.GetString(5),
                FailureReason = reader.IsDBNull(6) ? null : reader.GetString(6),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(7)
            });
        }

        return new ActivityLogsResponse
        {
            Logs = logs,
            TotalCount = totalCount
        };
    }

    public async Task<bool> UpdateUserRoleAsync(string targetUniversityId, string newRole, string actorUniversityId, CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Supabase PostgreSQL connection string is missing on the API server.");

        const string sql = """
            do $$
            declare
                v_actor_role varchar;
            begin
                select role into v_actor_role from students where university_id = @actor_id;

                update students
                set role = @new_role
                where university_id = @target_id;

                if not found then
                    insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason)
                    values (@actor_id, v_actor_role, 'ROLE UPDATE', 'FAILURE', '/api/admin/roles', 'User not found');
                    raise exception 'User not found';
                end if;

                insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint)
                values (@actor_id, v_actor_role, 'ROLE UPDATE', 'SUCCESS', '/api/admin/roles');
            end;
            $$;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("target_id", NpgsqlDbType.Varchar) { Value = targetUniversityId.Trim() });
        command.Parameters.Add(new NpgsqlParameter("new_role", NpgsqlDbType.Varchar) { Value = newRole.Trim() });
        command.Parameters.Add(new NpgsqlParameter("actor_id", NpgsqlDbType.Varchar) { Value = actorUniversityId.Trim() });

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (PostgresException)
        {
            return false;
        }
    }

    private string? ResolveConnectionString()
    {
        return _configuration.GetConnectionString("SupabasePostgres")
            ?? _configuration["Supabase:ConnectionString"]
            ?? _configuration["SUPABASE_POSTGRES_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_POSTGRES_CONNECTION_STRING");
    }
}
