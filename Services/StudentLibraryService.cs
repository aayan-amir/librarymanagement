using Npgsql;
using NpgsqlTypes;
using QrDigitalLibrary.Api.Contracts;

namespace QrDigitalLibrary.Api.Services;

public sealed class StudentLibraryService : IStudentLibraryService
{
    private readonly IConfiguration _configuration;
    private readonly IJwtTokenService _jwtTokenService;

    public StudentLibraryService(IConfiguration configuration, IJwtTokenService jwtTokenService)
    {
        _configuration = configuration;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<BorrowedBooksResponse> GetBorrowedBooksAsync(
        string universityId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                t.transaction_id,
                b.title,
                b.author,
                bc.accession_no,
                t.issued_at,
                t.due_at,
                t.transaction_status,
                greatest(0, extract(day from (now() - t.due_at)))::numeric * 20 as estimated_fine
            from students s
            join transactions t on t.student_id = s.student_id
            join book_copies bc on bc.copy_id = t.copy_id
            join books b on b.book_id = bc.book_id
            where s.university_id = @university_id
              and t.returned_at is null
              and t.transaction_status in ('ISSUED', 'OVERDUE')
            order by t.due_at asc, t.issued_at desc;
            """;

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Supabase PostgreSQL connection string is missing on the API server.");
        }

        var books = new List<BorrowedBookDto>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("university_id", NpgsqlDbType.Varchar)
        {
            Value = universityId.Trim()
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            books.Add(new BorrowedBookDto
            {
                TransactionId = reader.GetGuid(0),
                Title = reader.GetString(1),
                Author = reader.GetString(2),
                AccessionNo = reader.GetString(3),
                IssuedAt = reader.GetFieldValue<DateTimeOffset>(4),
                DueAt = reader.GetFieldValue<DateTimeOffset>(5),
                Status = reader.GetString(6),
                IsOverdue = reader.GetFieldValue<DateTimeOffset>(5) < DateTimeOffset.UtcNow,
                EstimatedFine = reader.GetDecimal(7)
            });
        }

        return new BorrowedBooksResponse
        {
            UniversityId = universityId.Trim(),
            ActiveBorrowCount = books.Count,
            Books = books
        };
    }

    public async Task<TransactionHistoryResponse> GetTransactionHistoryAsync(
        string universityId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                t.transaction_id,
                b.title,
                b.author,
                bc.accession_no,
                t.transaction_type,
                t.transaction_status,
                t.issued_at,
                t.due_at,
                t.returned_at,
                coalesce(f.amount, 0) as fine_amount
            from students s
            join transactions t on t.student_id = s.student_id
            join book_copies bc on bc.copy_id = t.copy_id
            join books b on b.book_id = bc.book_id
            left join fines f on f.transaction_id = t.transaction_id
            where s.university_id = @university_id
            order by t.created_at desc;
            """;

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Supabase PostgreSQL connection string is missing on the API server.");
        }

        var transactions = new List<TransactionHistoryDto>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("university_id", NpgsqlDbType.Varchar)
        {
            Value = universityId.Trim()
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            transactions.Add(new TransactionHistoryDto
            {
                TransactionId = reader.GetGuid(0),
                Title = reader.GetString(1),
                Author = reader.GetString(2),
                AccessionNo = reader.GetString(3),
                TransactionType = reader.GetString(4),
                Status = reader.GetString(5),
                IssuedAt = reader.GetFieldValue<DateTimeOffset>(6),
                DueAt = reader.GetFieldValue<DateTimeOffset>(7),
                ReturnedAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
                FineAmount = reader.GetDecimal(9)
            });
        }

        return new TransactionHistoryResponse
        {
            UniversityId = universityId.Trim(),
            Transactions = transactions
        };
    }

    public async Task<FinesResponse> GetFinesAsync(
        string universityId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                f.fine_id,
                f.transaction_id,
                b.title,
                bc.accession_no,
                f.amount,
                f.fine_status,
                f.assessed_at
            from students s
            join transactions t on t.student_id = s.student_id
            join book_copies bc on bc.copy_id = t.copy_id
            join books b on b.book_id = bc.book_id
            join fines f on f.transaction_id = t.transaction_id
            where s.university_id = @university_id
            order by f.assessed_at desc;
            """;

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Supabase PostgreSQL connection string is missing on the API server.");
        }

        var fines = new List<FineDto>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("university_id", NpgsqlDbType.Varchar)
        {
            Value = universityId.Trim()
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            fines.Add(new FineDto
            {
                FineId = reader.GetGuid(0),
                TransactionId = reader.GetGuid(1),
                Title = reader.GetString(2),
                AccessionNo = reader.GetString(3),
                Amount = reader.GetDecimal(4),
                Status = reader.GetString(5),
                AssessedAt = reader.GetFieldValue<DateTimeOffset>(6)
            });
        }

        return new FinesResponse
        {
            UniversityId = universityId.Trim(),
            OutstandingTotal = fines.Where(f => f.Status != "PAID").Sum(f => f.Amount),
            Fines = fines
        };
    }

    public async Task<bool> PayFineAsync(string universityId, Guid fineId, CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Supabase PostgreSQL connection string is missing on the API server.");

        const string sql = """
            do $$
            declare
                v_student_id uuid;
                v_fine_amount numeric;
                v_role varchar;
            begin
                select s.student_id, f.amount, s.role into v_student_id, v_fine_amount, v_role
                from fines f
                join transactions t on t.transaction_id = f.transaction_id
                join students s on s.student_id = t.student_id
                where f.fine_id = @fine_id and s.university_id = @university_id and f.fine_status = 'UNPAID';

                if not found then
                    insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason)
                    values (@university_id, 'Unknown', 'FINE PAYMENT', 'FAILURE', '/api/students/me/fines/pay', 'Fine not found or already paid');
                    raise exception 'Fine not found or already paid';
                end if;

                update fines set fine_status = 'PAID', paid_at = now() where fine_id = @fine_id;

                insert into fine_payments (fine_id, amount_paid, payment_reference)
                values (@fine_id, v_fine_amount, 'SYS-MOCK-PAY');

                insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint)
                values (@university_id, v_role, 'FINE PAYMENT', 'SUCCESS', '/api/students/me/fines/pay');
            end;
            $$;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("university_id", NpgsqlDbType.Varchar) { Value = universityId.Trim() });
        command.Parameters.Add(new NpgsqlParameter("fine_id", NpgsqlDbType.Uuid) { Value = fineId });

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

    public async Task<NotificationsResponse> GetNotificationsAsync(
        string universityId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select n.notification_id, n.notification_type, n.title, n.message, n.is_read, n.created_at
            from students s
            join notifications n on n.student_id = s.student_id
            where s.university_id = @university_id
            order by n.created_at desc
            limit 25;
            """;

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Supabase PostgreSQL connection string is missing on the API server.");
        }

        var notifications = new List<NotificationDto>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("university_id", NpgsqlDbType.Varchar)
        {
            Value = universityId.Trim()
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            notifications.Add(new NotificationDto
            {
                NotificationId = reader.GetGuid(0),
                NotificationType = reader.GetString(1),
                Title = reader.GetString(2),
                Message = reader.GetString(3),
                IsRead = reader.GetBoolean(4),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(5)
            });
        }

        return new NotificationsResponse
        {
            UniversityId = universityId.Trim(),
            Notifications = notifications
        };
    }

    public async Task<RecommendationsResponse> GetRecommendationsAsync(
        string universityId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            with current_student as (
                select s.student_id, s.semester, d.department_name
                from students s
                join departments d on d.department_id = s.department_id
                where s.university_id = @university_id
            ),
            borrowed_categories as (
                select b.category, count(*) as borrow_count
                from current_student cs
                join transactions t on t.student_id = cs.student_id
                join book_copies bc on bc.copy_id = t.copy_id
                join books b on b.book_id = bc.book_id
                where b.category is not null
                group by b.category
            ),
            availability as (
                select book_id, count(*) filter (where copy_status = 'AVAILABLE')::integer as available_count
                from book_copies
                group by book_id
            ),
            popularity as (
                select bc.book_id, count(*)::integer as borrow_count
                from transactions t
                join book_copies bc on bc.copy_id = t.copy_id
                group by bc.book_id
            )
            select
                b.book_id,
                b.title,
                b.author,
                b.category,
                coalesce(a.available_count, 0) as available_count,
                (
                    coalesce(p.borrow_count, 0)
                    + case when exists (select 1 from borrowed_categories c where c.category = b.category) then 8 else 0 end
                    + case when b.category ilike '%' || (select semester::text from current_student) || '%' then 3 else 0 end
                    + case when b.category ilike '%' || (select department_name from current_student) || '%' then 5 else 0 end
                    + coalesce(a.available_count, 0)
                )::integer as score
            from books b
            cross join current_student cs
            left join availability a on a.book_id = b.book_id
            left join popularity p on p.book_id = b.book_id
            where coalesce(a.available_count, 0) > 0
              and not exists (
                  select 1
                  from transactions t
                  join book_copies bc on bc.copy_id = t.copy_id
                  where t.student_id = cs.student_id
                    and bc.book_id = b.book_id
                    and t.returned_at is null
                    and t.transaction_status in ('ISSUED', 'OVERDUE')
              )
            order by score desc, b.title
            limit 8;
            """;

        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Supabase PostgreSQL connection string is missing on the API server.");
        }

        var recommendations = new List<RecommendationDto>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("university_id", NpgsqlDbType.Varchar)
        {
            Value = universityId.Trim()
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rank = 1;
        while (await reader.ReadAsync(cancellationToken))
        {
            var category = reader.IsDBNull(3) ? null : reader.GetString(3);
            var score = reader.GetInt32(5);
            recommendations.Add(new RecommendationDto
            {
                Rank = rank++,
                BookId = reader.GetGuid(0),
                Title = reader.GetString(1),
                Author = reader.GetString(2),
                Category = category,
                AvailabilityCount = reader.GetInt32(4),
                Score = score,
                Reason = category is null
                    ? "Recommended from current availability and library popularity."
                    : $"Recommended from your borrowing pattern, {category} interest, and library popularity."
            });
        }

        return new RecommendationsResponse
        {
            UniversityId = universityId.Trim(),
            Recommendations = recommendations
        };
    }

    public async Task<RegisterStudentResponse> RegisterStudentAsync(
        RegisterStudentRequest request,
        CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new RegisterStudentResponse
            {
                Success = false,
                StatusCode = "DATABASE_NOT_CONFIGURED",
                Message = "Supabase PostgreSQL connection string is missing."
            };
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        const string sql = """
            insert into students (university_id, full_name, email, password_hash, department_id, semester)
            select
                @university_id,
                @full_name,
                @email,
                @password_hash,
                d.department_id,
                @semester
            from departments d
            where d.department_code = @department_code;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Check department exists
        await using var checkCmd = new NpgsqlCommand(
            "select 1 from departments where department_code = @dc", connection);
        checkCmd.Parameters.Add(new NpgsqlParameter("dc", NpgsqlDbType.Varchar)
            { Value = request.DepartmentCode.Trim() });
        var deptExists = await checkCmd.ExecuteScalarAsync(cancellationToken);
        if (deptExists is null)
        {
            return new RegisterStudentResponse
            {
                Success = false,
                StatusCode = "DEPARTMENT_NOT_FOUND",
                Message = "The selected department does not exist."
            };
        }

        try
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("university_id", NpgsqlDbType.Varchar)
                { Value = request.UniversityId.Trim() });
            command.Parameters.Add(new NpgsqlParameter("full_name", NpgsqlDbType.Varchar)
                { Value = request.FullName.Trim() });
            command.Parameters.Add(new NpgsqlParameter("email", NpgsqlDbType.Varchar)
                { Value = request.Email.Trim() });
            command.Parameters.Add(new NpgsqlParameter("password_hash", NpgsqlDbType.Varchar)
                { Value = passwordHash });
            command.Parameters.Add(new NpgsqlParameter("department_code", NpgsqlDbType.Varchar)
                { Value = request.DepartmentCode.Trim() });
            command.Parameters.Add(new NpgsqlParameter("semester", NpgsqlDbType.Smallint)
                { Value = (short)request.Semester });

            await command.ExecuteNonQueryAsync(cancellationToken);

            return new RegisterStudentResponse
            {
                Success = true,
                StatusCode = "REGISTERED",
                Message = "Student registered successfully."
            };
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            var field = ex.ConstraintName switch
            {
                "students_email_key" => "email",
                _ => "university ID"
            };

            return new RegisterStudentResponse
            {
                Success = false,
                StatusCode = "DUPLICATE",
                Message = $"A student with this {field} already exists."
            };
        }
    }

    public async Task<LoginStudentResponse> VerifyLoginAsync(
        LoginStudentRequest request,
        CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new LoginStudentResponse
            {
                Success = false,
                StatusCode = "DATABASE_NOT_CONFIGURED",
                Message = "Supabase PostgreSQL connection string is missing."
            };
        }

        const string sql = """
            select password_hash, full_name, is_active, role
            from students
            where university_id = @university_id;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter("university_id", NpgsqlDbType.Varchar)
            { Value = request.UniversityId.Trim() });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            await LogActivityAsync(connectionString, null, "LOGIN", "FAILED", "Unknown university ID.", cancellationToken);
            return new LoginStudentResponse
            {
                Success = false,
                StatusCode = "INVALID_CREDENTIALS",
                Message = "Invalid university ID or password."
            };
        }

        var storedHash = reader.GetString(0);
        var fullName = reader.GetString(1);
        var isActive = reader.GetBoolean(2);
        var role = reader.IsDBNull(3) ? "Student" : reader.GetString(3);

        if (!BCrypt.Net.BCrypt.Verify(request.Password, storedHash))
        {
            await LogActivityAsync(connectionString, request.UniversityId.Trim(), "LOGIN", "FAILED", "Invalid password.", cancellationToken);
            return new LoginStudentResponse
            {
                Success = false,
                StatusCode = "INVALID_CREDENTIALS",
                Message = "Invalid university ID or password."
            };
        }

        if (!isActive)
        {
            await LogActivityAsync(connectionString, request.UniversityId.Trim(), "LOGIN", "FAILED", "Inactive account.", cancellationToken);
            return new LoginStudentResponse
            {
                Success = false,
                StatusCode = "STUDENT_INACTIVE",
                Message = "This student account is not active."
            };
        }

        var token = _jwtTokenService.CreateToken(request.UniversityId.Trim(), fullName, role, out var expiresAt);
        await LogActivityAsync(connectionString, request.UniversityId.Trim(), "LOGIN", "SUCCESS", null, cancellationToken);

        return new LoginStudentResponse
        {
            Success = true,
            StatusCode = "AUTHENTICATED",
            Message = "Login successful.",
            UniversityId = request.UniversityId.Trim(),
            FullName = fullName,
            Role = role,
            AccessToken = token,
            ExpiresAt = expiresAt
        };
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync(
        CancellationToken cancellationToken)
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return [];
        }

        const string sql = "select department_code, department_name from departments order by department_name;";

        var departments = new List<DepartmentDto>();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            departments.Add(new DepartmentDto
            {
                DepartmentCode = reader.GetString(0),
                DepartmentName = reader.GetString(1)
            });
        }

        return departments;
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

    private static async Task LogActivityAsync(
        string connectionString,
        string? universityId,
        string actionType,
        string actionStatus,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        const string sql = """
            insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason)
            values (@actor_university_id, 'Student', @action_type, @action_status, '/api/auth/login', @failure_reason);
            """;

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("actor_university_id", NpgsqlDbType.Varchar)
            {
                Value = string.IsNullOrWhiteSpace(universityId) ? DBNull.Value : universityId
            });
            command.Parameters.Add(new NpgsqlParameter("action_type", NpgsqlDbType.Varchar) { Value = actionType });
            command.Parameters.Add(new NpgsqlParameter("action_status", NpgsqlDbType.Varchar) { Value = actionStatus });
            command.Parameters.Add(new NpgsqlParameter("failure_reason", NpgsqlDbType.Varchar)
            {
                Value = string.IsNullOrWhiteSpace(failureReason) ? DBNull.Value : failureReason
            });
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // Activity logging must not block authentication in development/demo databases.
        }
    }
}
