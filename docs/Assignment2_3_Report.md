# QR-Based Digital Library System: Assignment 2 and 3 Upgrade

## Overview

The project upgrades the original QR issuing module into a circulation and analytics system. Assignment 2 requirements are covered through return processing, overdue detection, fine calculation, notifications, admin reporting, and circulation dashboards. Assignment 3 requirements are covered through JWT authentication, role-based access, activity logs, branch-ready inventory, analytics, API documentation, deployment guidance, and a rule-based recommendation module.

## Assignment 2 Implementation

The return module lets a logged-in student return a book by scanning or entering the book QR code. The database routine `return_book_via_qr` validates the student, checks the scanned copy, finds the active issue transaction, records the return time, marks the copy available, and prevents duplicate returns.

The fine system calculates overdue fines automatically. If the return date is later than the due date, the system calculates late days and creates an unpaid fine record. Notifications are created for successful returns and overdue fine assessments.

The dashboard and reporting system gives librarians visibility into total issued books, overdue books, active borrowers, active inventory, fine totals, popular books, defaulters through overdue/fine views, and daily transaction activity.

## Assignment 3 Implementation

Authentication was upgraded to JWT tokens. Login responses include an access token, expiry time, user role, university ID, and display name. Student endpoints use the logged-in identity through `/api/students/me/...`, while dashboard routes require `Admin` or `Librarian` role.

Every major action is logged in `activity_logs`, including login, issue, return, and failed attempts. This supports enterprise monitoring and viva explanation of auditability.

The schema includes `branches` and `book_copies.branch_id`, so the system is ready for multi-branch inventory and branch-scoped dashboard filters.

The analytics dashboard reports most active students, popular books, peak issuing timings, fine trends, and department-wise statistics. The recommendation module is rule-based and scores available books using borrowing history, category match, semester hints, popularity, and availability.

## API Documentation Summary

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/transactions/issue`
- `POST /api/transactions/return`
- `GET /api/students/me/borrowed-books`
- `GET /api/students/me/transactions`
- `GET /api/students/me/fines`
- `GET /api/students/me/notifications`
- `GET /api/students/me/recommendations`
- `GET /api/admin/dashboard/summary`
- `GET /api/admin/dashboard/analytics`

## Security Summary

The project uses BCrypt password hashing, parameterized database commands, JWT token validation, role checks, rate limiting, activity logging, and HTTPS deployment guidance. A separate `SecurityTestingReport.md` file lists the planned security tests and expected results.

## Conclusion

The upgraded QR Digital Library now supports the full circulation lifecycle and adds enterprise features expected from a modern digital library system. It remains lightweight enough for a university project while demonstrating database design, stored procedures, secure APIs, analytics, and student-facing usability.
