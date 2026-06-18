using Npgsql;
using NpgsqlTypes;
using QrDigitalLibrary.Api.Contracts;

namespace QrDigitalLibrary.Api.Services;

public sealed class LibraryIssueService : ILibraryIssueService
{
    private readonly IConfiguration _configuration;

    public LibraryIssueService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IssueBookDbResult> IssueBookViaQrAsync(
        IssueBookRequest request,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                success,
                transaction_id,
                status_code,
                message,
                issued_at,
                due_at
            from issue_book_via_qr(@p_university_id, @p_qr_code_id);
            """;

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new IssueBookDbResult
            {
                Success = false,
                TransactionId = null,
                StatusCode = "DATABASE_NOT_CONFIGURED",
                Message = "Supabase PostgreSQL connection string is missing on the API server.",
                IssuedAt = null,
                DueAt = null
            };
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(new NpgsqlParameter("p_university_id", NpgsqlDbType.Varchar)
        {
            Value = request.UniversityId.Trim()
        });
        command.Parameters.Add(new NpgsqlParameter("p_qr_code_id", NpgsqlDbType.Varchar)
        {
            Value = request.QrCodeId.Trim()
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Database routine issue_book_via_qr returned no result.");
        }

        return new IssueBookDbResult
        {
            Success = reader.GetBoolean(0),
            TransactionId = reader.IsDBNull(1) ? null : reader.GetGuid(1),
            StatusCode = reader.GetString(2),
            Message = reader.GetString(3),
            IssuedAt = reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
            DueAt = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5)
        };
    }

    public async Task<ReturnBookDbResult> ReturnBookViaQrAsync(
        string universityId,
        ReturnBookRequest request,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                success,
                transaction_id,
                status_code,
                message,
                returned_at,
                fine_amount
            from return_book_via_qr(@p_university_id, @p_qr_code_id);
            """;

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new ReturnBookDbResult
            {
                Success = false,
                TransactionId = null,
                StatusCode = "DATABASE_NOT_CONFIGURED",
                Message = "Supabase PostgreSQL connection string is missing on the API server.",
                ReturnedAt = null,
                FineAmount = 0
            };
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(new NpgsqlParameter("p_university_id", NpgsqlDbType.Varchar)
        {
            Value = universityId.Trim()
        });
        command.Parameters.Add(new NpgsqlParameter("p_qr_code_id", NpgsqlDbType.Varchar)
        {
            Value = request.QrCodeId.Trim()
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Database routine return_book_via_qr returned no result.");
        }

        return new ReturnBookDbResult
        {
            Success = reader.GetBoolean(0),
            TransactionId = reader.IsDBNull(1) ? null : reader.GetGuid(1),
            StatusCode = reader.GetString(2),
            Message = reader.GetString(3),
            ReturnedAt = reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
            FineAmount = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5)
        };
    }

    private string? ResolveConnectionString()
    {
        var connectionString =
            _configuration.GetConnectionString("SupabasePostgres")
            ?? _configuration["Supabase:ConnectionString"]
            ?? _configuration["SUPABASE_POSTGRES_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_POSTGRES_CONNECTION_STRING");

        return connectionString;
    }
}
