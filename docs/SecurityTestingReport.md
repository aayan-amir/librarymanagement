# Security Testing Report

## Controls Implemented

- Password hashing with BCrypt.
- Parameterized SQL commands and stored functions.
- JWT login token with issuer, audience, expiry, role, and HMAC-SHA256 signature.
- Role checks for admin dashboard endpoints.
- Rate limiting for authentication and transaction APIs.
- Activity logging for login, book issue, book return, and failed attempts.
- HTTPS deployment guidance.

## Test Cases

| Area | Test | Expected Result |
| --- | --- | --- |
| Password security | Inspect database password field | Password is stored as a BCrypt hash, not plaintext. |
| JWT required | Call `/api/students/me/borrowed-books` without token | API returns unauthorized. |
| Role management | Call admin dashboard as `Student` | API returns forbidden. |
| SQL injection | Send `' or '1'='1` in login and QR fields | Query remains parameterized and request fails safely. |
| Rate limiting | Repeatedly call login endpoint | API throttles excessive requests. |
| Failed attempt logging | Login with invalid password | `activity_logs` receives a failed `LOGIN` entry. |
| Return safety | Return the same QR twice | Second return fails with no duplicate fine/return. |

## Notes

The project uses a small built-in JWT helper to avoid extra package dependencies in the local assignment environment. Production deployment should rotate `QR_LIBRARY_JWT_SECRET`, enforce HTTPS, and keep database credentials outside source control.
