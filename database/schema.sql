-- QR-Based Digital Library System
-- Assignment 1, 2, and 3 database schema for Supabase PostgreSQL.

create extension if not exists pgcrypto;

create table if not exists branches (
    branch_id uuid primary key default gen_random_uuid(),
    branch_code varchar(30) not null unique,
    branch_name varchar(150) not null,
    created_at timestamptz not null default now(),
    constraint chk_branches_code_not_blank check (btrim(branch_code) <> ''),
    constraint chk_branches_name_not_blank check (btrim(branch_name) <> '')
);

insert into branches (branch_code, branch_name)
values ('MAIN', 'Main Library')
on conflict (branch_code) do nothing;

create table if not exists departments (
    department_id uuid primary key default gen_random_uuid(),
    department_code varchar(20) not null unique,
    department_name varchar(150) not null unique,
    created_at timestamptz not null default now(),
    constraint chk_departments_code_not_blank check (btrim(department_code) <> ''),
    constraint chk_departments_name_not_blank check (btrim(department_name) <> '')
);

create table if not exists students (
    student_id uuid primary key default gen_random_uuid(),
    university_id varchar(30) not null unique,
    full_name varchar(150) not null,
    email varchar(254) not null unique,
    password_hash varchar(255) not null,
    department_id uuid not null references departments(department_id) on update cascade on delete restrict,
    semester smallint not null,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint chk_students_university_id_not_blank check (btrim(university_id) <> ''),
    constraint chk_students_full_name_not_blank check (btrim(full_name) <> ''),
    constraint chk_students_semester_valid check (semester between 1 and 8)
);

alter table students
    add column if not exists role varchar(20) not null default 'Student';

create table if not exists books (
    book_id uuid primary key default gen_random_uuid(),
    isbn varchar(20) unique,
    title varchar(250) not null,
    author varchar(200) not null,
    publisher varchar(200),
    edition varchar(50),
    category varchar(100),
    created_at timestamptz not null default now(),
    constraint chk_books_title_not_blank check (btrim(title) <> ''),
    constraint chk_books_author_not_blank check (btrim(author) <> '')
);

create table if not exists book_copies (
    copy_id uuid primary key default gen_random_uuid(),
    book_id uuid not null references books(book_id) on update cascade on delete restrict,
    accession_no varchar(40) not null unique,
    qr_code_id varchar(100) not null unique,
    shelf_location varchar(80),
    copy_status varchar(20) not null default 'AVAILABLE',
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint chk_book_copies_accession_not_blank check (btrim(accession_no) <> ''),
    constraint chk_book_copies_status_valid check (
        copy_status in ('AVAILABLE', 'ISSUED', 'LOST', 'DAMAGED', 'MAINTENANCE')
    )
);

alter table book_copies
    add column if not exists branch_id uuid references branches(branch_id) on update cascade on delete restrict;

update book_copies
set branch_id = (select branch_id from branches where branch_code = 'MAIN')
where branch_id is null;

create table if not exists transactions (
    transaction_id uuid primary key default gen_random_uuid(),
    student_id uuid not null references students(student_id) on update cascade on delete restrict,
    copy_id uuid not null references book_copies(copy_id) on update cascade on delete restrict,
    transaction_type varchar(20) not null default 'ISSUE',
    transaction_status varchar(20) not null default 'ISSUED',
    issued_at timestamptz not null default now(),
    due_at timestamptz not null default (now() + interval '14 days'),
    returned_at timestamptz,
    created_at timestamptz not null default now(),
    constraint chk_transactions_type_valid check (transaction_type in ('ISSUE', 'RETURN')),
    constraint chk_transactions_status_valid check (transaction_status in ('ISSUED', 'RETURNED', 'OVERDUE', 'CANCELLED')),
    constraint chk_transactions_due_after_issue check (due_at > issued_at),
    constraint chk_transactions_return_after_issue check (returned_at is null or returned_at >= issued_at)
);

create table if not exists fines (
    fine_id uuid primary key default gen_random_uuid(),
    transaction_id uuid not null unique references transactions(transaction_id) on update cascade on delete restrict,
    amount numeric(10, 2) not null default 0,
    fine_status varchar(20) not null default 'UNPAID',
    assessed_at timestamptz not null default now(),
    paid_at timestamptz,
    constraint chk_fines_amount_non_negative check (amount >= 0),
    constraint chk_fines_status_valid check (fine_status in ('UNPAID', 'PAID', 'WAIVED'))
);

create table if not exists fine_payments (
    payment_id uuid primary key default gen_random_uuid(),
    fine_id uuid not null references fines(fine_id) on update cascade on delete restrict,
    amount_paid numeric(10, 2) not null,
    paid_at timestamptz not null default now(),
    payment_reference varchar(80),
    constraint chk_fine_payments_amount_positive check (amount_paid > 0)
);

create table if not exists notifications (
    notification_id uuid primary key default gen_random_uuid(),
    student_id uuid not null references students(student_id) on update cascade on delete cascade,
    notification_type varchar(40) not null,
    title varchar(150) not null,
    message text not null,
    is_read boolean not null default false,
    created_at timestamptz not null default now()
);

create table if not exists activity_logs (
    activity_log_id uuid primary key default gen_random_uuid(),
    actor_university_id varchar(30),
    actor_role varchar(20),
    action_type varchar(40) not null,
    action_status varchar(20) not null,
    endpoint varchar(150),
    failure_reason text,
    created_at timestamptz not null default now()
);

create unique index if not exists ux_transactions_one_active_issue_per_copy
    on transactions(copy_id)
    where returned_at is null and transaction_status in ('ISSUED', 'OVERDUE');

create index if not exists ix_students_university_id on students(university_id);
create index if not exists ix_book_copies_qr_code_id on book_copies(qr_code_id);
create index if not exists ix_book_copies_branch_status on book_copies(branch_id, copy_status);
create index if not exists ix_transactions_student_active
    on transactions(student_id)
    where returned_at is null and transaction_status in ('ISSUED', 'OVERDUE');
create index if not exists ix_transactions_due_active
    on transactions(due_at)
    where returned_at is null and transaction_status in ('ISSUED', 'OVERDUE');
create index if not exists ix_notifications_student_created on notifications(student_id, created_at desc);
create index if not exists ix_activity_logs_action_created on activity_logs(action_type, created_at desc);

create or replace function set_updated_at()
returns trigger
language plpgsql
as $$
begin
    new.updated_at = now();
    return new;
end;
$$;

drop trigger if exists trg_students_set_updated_at on students;
create trigger trg_students_set_updated_at
before update on students
for each row execute function set_updated_at();

drop trigger if exists trg_book_copies_set_updated_at on book_copies;
create trigger trg_book_copies_set_updated_at
before update on book_copies
for each row execute function set_updated_at();

create or replace function prevent_transaction_delete()
returns trigger
language plpgsql
as $$
begin
    raise exception 'Transaction records are permanent and cannot be deleted.'
        using errcode = 'P0001';
end;
$$;

drop trigger if exists trg_transactions_prevent_delete on transactions;
create trigger trg_transactions_prevent_delete
before delete on transactions
for each row execute function prevent_transaction_delete();

create or replace function refresh_overdue_transactions()
returns integer
language plpgsql
as $$
declare
    v_updated integer;
begin
    update transactions
    set transaction_status = 'OVERDUE'
    where returned_at is null
      and transaction_status = 'ISSUED'
      and due_at < now();

    get diagnostics v_updated = row_count;
    return v_updated;
end;
$$;

create or replace function issue_book_via_qr(
    p_university_id varchar,
    p_qr_code_id varchar
)
returns table (
    success boolean,
    transaction_id uuid,
    status_code text,
    message text,
    issued_at timestamptz,
    due_at timestamptz
)
language plpgsql
as $$
declare
    v_student_id uuid;
    v_is_active boolean;
    v_role varchar(20);
    v_copy_id uuid;
    v_copy_status varchar(20);
    v_active_borrows integer;
    v_transaction_id uuid;
    v_issued_at timestamptz;
    v_due_at timestamptz;
begin
    perform refresh_overdue_transactions();

    select s.student_id, s.is_active, s.role
    into v_student_id, v_is_active, v_role
    from students s
    where s.university_id = btrim(p_university_id);

    if v_student_id is null then
        insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason)
        values (p_university_id, 'Student', 'BOOK_ISSUE', 'FAILED', '/api/transactions/issue', 'Student not found');
        return query select false, null::uuid, 'STUDENT_NOT_FOUND', 'No student exists for the supplied university ID.', null::timestamptz, null::timestamptz;
        return;
    end if;

    if not v_is_active then
        insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason)
        values (p_university_id, v_role, 'BOOK_ISSUE', 'FAILED', '/api/transactions/issue', 'Inactive student');
        return query select false, null::uuid, 'STUDENT_INACTIVE', 'Only active students are allowed to issue books.', null::timestamptz, null::timestamptz;
        return;
    end if;

    perform pg_advisory_xact_lock(hashtextextended(v_student_id::text, 0));

    select count(*)::integer
    into v_active_borrows
    from transactions t
    where t.student_id = v_student_id
      and t.returned_at is null
      and t.transaction_status in ('ISSUED', 'OVERDUE');

    if v_active_borrows >= 3 then
        insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason)
        values (p_university_id, v_role, 'BOOK_ISSUE', 'FAILED', '/api/transactions/issue', 'Borrow limit reached');
        return query select false, null::uuid, 'BORROW_LIMIT_REACHED', 'Student already has 3 active borrowed books.', null::timestamptz, null::timestamptz;
        return;
    end if;

    select bc.copy_id, bc.copy_status
    into v_copy_id, v_copy_status
    from book_copies bc
    where bc.qr_code_id = btrim(p_qr_code_id)
    for update;

    if v_copy_id is null then
        insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason)
        values (p_university_id, v_role, 'BOOK_ISSUE', 'FAILED', '/api/transactions/issue', 'QR code not found');
        return query select false, null::uuid, 'QR_CODE_NOT_FOUND', 'No book copy is linked with the scanned QR code.', null::timestamptz, null::timestamptz;
        return;
    end if;

    if v_copy_status <> 'AVAILABLE' then
        insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason)
        values (p_university_id, v_role, 'BOOK_ISSUE', 'FAILED', '/api/transactions/issue', 'Copy not available');
        return query select false, null::uuid, 'COPY_NOT_AVAILABLE', 'The scanned book copy is not currently available for issue.', null::timestamptz, null::timestamptz;
        return;
    end if;

    update book_copies set copy_status = 'ISSUED' where copy_id = v_copy_id;

    insert into transactions (student_id, copy_id, transaction_type, transaction_status)
    values (v_student_id, v_copy_id, 'ISSUE', 'ISSUED')
    returning transactions.transaction_id, transactions.issued_at, transactions.due_at
    into v_transaction_id, v_issued_at, v_due_at;

    insert into notifications (student_id, notification_type, title, message)
    values (v_student_id, 'ISSUE', 'Book issued', 'Your book was issued successfully. Due date: ' || to_char(v_due_at, 'YYYY-MM-DD HH24:MI'));

    insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint)
    values (p_university_id, v_role, 'BOOK_ISSUE', 'SUCCESS', '/api/transactions/issue');

    return query select true, v_transaction_id, 'ISSUED', 'Book issued successfully through QR scan.', v_issued_at, v_due_at;
end;
$$;

create or replace function return_book_via_qr(
    p_university_id varchar,
    p_qr_code_id varchar
)
returns table (
    success boolean,
    transaction_id uuid,
    status_code text,
    message text,
    returned_at timestamptz,
    fine_amount numeric
)
language plpgsql
as $$
declare
    v_student_id uuid;
    v_role varchar(20);
    v_copy_id uuid;
    v_transaction_id uuid;
    v_due_at timestamptz;
    v_returned_at timestamptz := now();
    v_late_days integer;
    v_fine_amount numeric(10, 2);
begin
    perform refresh_overdue_transactions();

    select s.student_id, s.role
    into v_student_id, v_role
    from students s
    where s.university_id = btrim(p_university_id);

    if v_student_id is null then
        insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason)
        values (p_university_id, 'Student', 'BOOK_RETURN', 'FAILED', '/api/transactions/return', 'Student not found');
        return query select false, null::uuid, 'STUDENT_NOT_FOUND', 'No student exists for the supplied university ID.', null::timestamptz, 0::numeric;
        return;
    end if;

    select bc.copy_id
    into v_copy_id
    from book_copies bc
    where bc.qr_code_id = btrim(p_qr_code_id)
    for update;

    if v_copy_id is null then
        insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason)
        values (p_university_id, v_role, 'BOOK_RETURN', 'FAILED', '/api/transactions/return', 'QR code not found');
        return query select false, null::uuid, 'QR_CODE_NOT_FOUND', 'No book copy is linked with the scanned QR code.', null::timestamptz, 0::numeric;
        return;
    end if;

    select t.transaction_id, t.due_at
    into v_transaction_id, v_due_at
    from transactions t
    where t.student_id = v_student_id
      and t.copy_id = v_copy_id
      and t.returned_at is null
      and t.transaction_status in ('ISSUED', 'OVERDUE')
    order by t.issued_at desc
    limit 1
    for update;

    if v_transaction_id is null then
        insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason)
        values (p_university_id, v_role, 'BOOK_RETURN', 'FAILED', '/api/transactions/return', 'No active issue for this student and copy');
        return query select false, null::uuid, 'NO_ACTIVE_ISSUE', 'This copy is not actively issued to the logged-in student.', null::timestamptz, 0::numeric;
        return;
    end if;

    v_late_days := greatest(0, ceil(extract(epoch from (v_returned_at - v_due_at)) / 86400.0)::integer);
    v_fine_amount := (v_late_days * 20)::numeric(10, 2);

    update transactions
    set transaction_type = 'RETURN',
        transaction_status = 'RETURNED',
        returned_at = v_returned_at
    where transactions.transaction_id = v_transaction_id;

    update book_copies
    set copy_status = 'AVAILABLE'
    where copy_id = v_copy_id;

    if v_fine_amount > 0 then
        insert into fines (transaction_id, amount, fine_status)
        values (v_transaction_id, v_fine_amount, 'UNPAID')
        on conflict (transaction_id) do update
        set amount = excluded.amount,
            fine_status = 'UNPAID',
            assessed_at = now();

        insert into notifications (student_id, notification_type, title, message)
        values (v_student_id, 'FINE', 'Overdue fine assessed', 'A fine of Rs. ' || v_fine_amount || ' was added for late return.');
    else
        insert into notifications (student_id, notification_type, title, message)
        values (v_student_id, 'RETURN', 'Book returned', 'Your book was returned successfully with no fine.');
    end if;

    insert into activity_logs (actor_university_id, actor_role, action_type, action_status, endpoint)
    values (p_university_id, v_role, 'BOOK_RETURN', 'SUCCESS', '/api/transactions/return');

    return query select true, v_transaction_id, 'RETURNED', 'Book returned successfully through QR scan.', v_returned_at, v_fine_amount;
end;
$$;

create or replace view vw_overdue_books as
select s.university_id, s.full_name, b.title, bc.accession_no, t.due_at
from transactions t
join students s on s.student_id = t.student_id
join book_copies bc on bc.copy_id = t.copy_id
join books b on b.book_id = bc.book_id
where t.returned_at is null and t.due_at < now();

create or replace view vw_active_borrowers as
select s.university_id, s.full_name, count(*) as active_borrows
from students s
join transactions t on t.student_id = s.student_id
where t.returned_at is null and t.transaction_status in ('ISSUED', 'OVERDUE')
group by s.university_id, s.full_name;

create or replace view vw_daily_transactions as
select date_trunc('day', created_at)::date as transaction_date, count(*) as transaction_count
from transactions
group by date_trunc('day', created_at)::date;

create or replace view vw_active_inventory as
select br.branch_code, br.branch_name, count(*) filter (where bc.copy_status = 'AVAILABLE') as available_copies
from branches br
left join book_copies bc on bc.branch_id = br.branch_id
group by br.branch_code, br.branch_name;

create or replace view vw_fine_reports as
select f.fine_status, count(*) as fine_count, coalesce(sum(f.amount), 0) as total_amount
from fines f
group by f.fine_status;

create or replace view vw_most_borrowed_books as
select b.title, b.author, count(*) as borrow_count
from transactions t
join book_copies bc on bc.copy_id = t.copy_id
join books b on b.book_id = bc.book_id
group by b.title, b.author
order by count(*) desc;

-- Backup & Archival Systems
create table if not exists activity_logs_archive (
    log_id uuid primary key,
    actor_university_id varchar(30),
    actor_role varchar(30),
    action_type varchar(50) not null,
    action_status varchar(20) not null,
    endpoint varchar(200),
    failure_reason text,
    created_at timestamptz not null,
    archived_at timestamptz not null default now()
);

create or replace function archive_old_data()
returns void
language plpgsql
security definer
as $$
begin
    -- Archive logs older than 1 year
    insert into activity_logs_archive (log_id, actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason, created_at)
    select log_id, actor_university_id, actor_role, action_type, action_status, endpoint, failure_reason, created_at
    from activity_logs
    where created_at < now() - interval '1 year';

    -- Delete the archived records from the active table
    delete from activity_logs
    where created_at < now() - interval '1 year';
end;
$$;
