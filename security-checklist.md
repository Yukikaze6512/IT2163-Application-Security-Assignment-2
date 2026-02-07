# Web Application Security Checklist - Ace Job Agency

## Registration and User Data Management
- [x] Implement successful saving of member info into the database
- [x] Check for duplicate email addresses and handle appropriately
- [x] Implement strong password requirements:
  - [x] Minimum 12 characters
  - [x] Combination of lowercase, uppercase, numbers, and special characters
  - [x] Provide feedback on password strength (progress bar indicator)
  - [x] Implement both client-side and server-side password checks
- [x] Encrypt sensitive user data in the database (NRIC encrypted using Data Protection API)
- [x] Implement proper password hashing and storage (ASP.NET Core Identity PBKDF2)
- [x] Implement file upload restrictions (.docx and .pdf only, max 10MB)
- [x] HTML encode text fields before saving to database (XSS prevention)

## Session Management
- [x] Create a secure session upon successful login (SessionId stored in DB + HTTP Session)
- [x] Implement session timeout (30 minutes idle timeout)
- [x] Route to login page after session timeout
- [x] Detect and handle multiple logins from different devices/browser tabs

## Login/Logout Security
- [x] Implement proper login functionality
- [x] Implement rate limiting (account lockout after 3 failed login attempts, 15 min lockout)
- [x] Perform proper and safe logout (clear session, clear DB SessionId, redirect to login)
- [x] Implement audit logging (save user activities: Registration, Login, Logout, Password Changes, 2FA events)
- [x] Redirect to homepage after successful login, displaying user info including decrypted NRIC

## Anti-Bot Protection
- [x] Implement Google reCAPTCHA v3 service (on Register and Login pages)

## Input Validation and Sanitization
- [x] Prevent injection attacks (parameterized queries via Entity Framework Core)
- [x] Implement Cross-Site Request Forgery (CSRF) protection (Anti-forgery tokens on all forms)
- [x] Prevent Cross-Site Scripting (XSS) attacks (HTML encoding, Razor auto-encoding)
- [x] Perform proper input sanitization, validation, and verification (email, NRIC, date, file type)
- [x] Implement both client-side and server-side input validation
- [x] Display error or warning messages for improper input
- [x] Perform proper encoding before saving data into the database (WebUtility.HtmlEncode)

## Error Handling
- [x] Implement graceful error handling on all pages
- [x] Create and display custom error pages (404, 403, 500, and other status codes)

## Software Testing and Security Analysis
- [ ] Perform source code analysis using external tools (e.g., GitHub CodeQL)
- [ ] Address security vulnerabilities identified in the source code

## Advanced Security Features
- [x] Implement automatic account recovery after lockout period (15 minutes)
- [x] Enforce password history (avoid password reuse, max 2 password history)
- [x] Implement change password functionality (with min age check of 5 minutes)
- [x] Implement reset password functionality (using email link)
- [x] Enforce minimum and maximum password age policies (min: 5 min, max: 90 days)
- [x] Implement Two-Factor Authentication (2FA) using TOTP with QR code

## General Security Best Practices
- [x] Use HTTPS for all communications (HSTS enabled)
- [x] Implement proper access controls and authorization ([Authorize] attribute)
- [x] Secure cookie settings (HttpOnly, SameSite=Strict)
- [x] Follow secure coding practices
- [x] Implement logging and monitoring for security events (AuditLog table)

## Documentation and Reporting
- [ ] Prepare a report on implemented security features
- [ ] Complete and submit the security checklist

## Registration Form Fields (IT2163-01 to ITN2163-03 - Ace Job Agency)
- [x] First Name
- [x] Last Name
- [x] Gender
- [x] NRIC (Encrypted in database)
- [x] Email address (Must be unique)
- [x] Password
- [x] Confirm Password
- [x] Date of Birth
- [x] Resume (.docx or .pdf file)
- [x] Who Am I (allows all special characters)
