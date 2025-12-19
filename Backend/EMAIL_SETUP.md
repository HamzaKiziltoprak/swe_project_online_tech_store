# Email Setup Guide

## SMTP Configuration

The application uses SMTP to send emails. You need to configure your email provider in `appsettings.json`.

### Gmail Configuration (Recommended)

1. **Create an App Password:**
   - Go to your Google Account: https://myaccount.google.com/
   - Navigate to Security
   - Enable 2-Step Verification if not already enabled
   - Go to "App passwords"
   - Select app: Mail
   - Select device: Other (Custom name) - e.g., "Online Tech Store"
   - Click "Generate"
   - Copy the 16-character password (spaces will be removed automatically)

2. **Update appsettings.json:**
   ```json
   "EmailSettings": {
     "SmtpServer": "smtp.gmail.com",
     "SmtpPort": 587,
     "SenderEmail": "your-email@gmail.com",
     "SenderName": "Online Tech Store",
     "Username": "your-email@gmail.com",
     "Password": "your-16-char-app-password",
     "EnableSsl": true
   },
   "AppUrl": "http://localhost:5173"
   ```

3. **Update AppUrl:**
   - For development: `http://localhost:5173` (Vite default)
   - For production: Your actual frontend URL

### Other Email Providers

#### Outlook/Hotmail
```json
"EmailSettings": {
  "SmtpServer": "smtp-mail.outlook.com",
  "SmtpPort": 587,
  "SenderEmail": "your-email@outlook.com",
  "SenderName": "Online Tech Store",
  "Username": "your-email@outlook.com",
  "Password": "your-password",
  "EnableSsl": true
}
```

#### Yahoo Mail
```json
"EmailSettings": {
  "SmtpServer": "smtp.mail.yahoo.com",
  "SmtpPort": 587,
  "SenderEmail": "your-email@yahoo.com",
  "SenderName": "Online Tech Store",
  "Username": "your-email@yahoo.com",
  "Password": "your-app-password",
  "EnableSsl": true
}
```

## Email Features

### 1. Email Confirmation (Registration)
When a user registers, they receive a confirmation email with a link to verify their email address.

**Endpoint:** `POST /api/accounts/register`

**Response:**
```json
{
  "success": true,
  "message": "Please check your email to confirm your account",
  "data": null
}
```

**Email Template:** Blue button with "Confirm Your Email" call-to-action

### 2. Resend Confirmation Email
If the user didn't receive the confirmation email, they can request a new one.

**Endpoint:** `POST /api/accounts/resend-confirmation`

**Request Body:**
```json
"user@example.com"
```

**Response:**
```json
{
  "success": true,
  "message": "Confirmation email has been resent.",
  "data": null
}
```

### 3. Forgot Password
User can request a password reset link to be sent to their email.

**Endpoint:** `POST /api/accounts/forgot-password`

**Request Body:**
```json
"user@example.com"
```

**Response:**
```json
{
  "success": true,
  "message": "Password reset link has been sent to your email.",
  "data": null
}
```

**Email Template:** Red button with "Reset Your Password" call-to-action (1-hour validity)

### 4. Reset Password
After clicking the reset link, user can set a new password.

**Endpoint:** `POST /api/accounts/reset-password`

**Request Body:**
```json
{
  "userId": 123,
  "token": "encoded-token-from-email",
  "newPassword": "NewSecureP@ss123"
}
```

**Password Requirements:**
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character (@$!%*?&)

### 5. Confirm Email
Verifies the user's email address using the token from the confirmation email.

**Endpoint:** `POST /api/accounts/confirm-email?userId=123&token=encoded-token`

**Response:**
```json
{
  "success": true,
  "message": "Email confirmed successfully! You can now login.",
  "data": null
}
```

## Testing Emails Locally

### Option 1: Use a Real Email Account (Recommended for Development)
- Configure Gmail with an app password
- Emails will be sent to real email addresses
- Best for testing the complete user flow

### Option 2: Use a Test Email Service
- **Mailtrap.io** (Free tier available): https://mailtrap.io/
- **Ethereal Email**: https://ethereal.email/
- These services capture emails without sending them to real addresses

### Option 3: SMTP4Dev (Local SMTP Server)
1. Install SMTP4Dev:
   ```bash
   dotnet tool install -g Rnwood.Smtp4dev
   ```

2. Run SMTP4Dev:
   ```bash
   smtp4dev
   ```

3. Update appsettings.json:
   ```json
   "EmailSettings": {
     "SmtpServer": "localhost",
     "SmtpPort": 25,
     "SenderEmail": "noreply@techstore.com",
     "SenderName": "Online Tech Store",
     "Username": "",
     "Password": "",
     "EnableSsl": false
   }
   ```

4. View emails at: http://localhost:5000

## Troubleshooting

### "Authentication failed" Error
- **Gmail:** Make sure you're using an App Password, not your regular password
- **Outlook:** Enable "Less secure app access" or use OAuth2
- Check if 2FA is enabled and configured correctly

### "Connection refused" Error
- Verify the SMTP server address and port
- Check if your firewall is blocking the SMTP port
- Try port 465 with SSL instead of port 587 with TLS

### Emails Going to Spam
- Configure SPF, DKIM, and DMARC records for your domain
- Use a verified sender email address
- Keep email content professional and avoid spam trigger words

### Token Expired Errors
- Email confirmation tokens are valid for 24 hours
- Password reset tokens are valid for 1 hour
- User needs to request a new link if token expired

## Security Best Practices

1. **Never commit credentials:**
   - Add `appsettings.json` to `.gitignore`
   - Use environment variables or Azure Key Vault in production

2. **Use App Passwords:**
   - Don't use your main email password
   - Generate app-specific passwords for better security

3. **Enable SSL/TLS:**
   - Always set `EnableSsl: true` in production
   - Use port 587 (TLS) or 465 (SSL)

4. **Limit Email Rate:**
   - Consider implementing rate limiting for email sending
   - Prevent abuse of forgot password and resend confirmation endpoints

5. **Validate Email Addresses:**
   - Use email validation on both frontend and backend
   - Check for disposable email addresses if needed

## Production Deployment

### Environment Variables
Set these in your production environment:
- `EmailSettings__SmtpServer`
- `EmailSettings__SmtpPort`
- `EmailSettings__SenderEmail`
- `EmailSettings__Username`
- `EmailSettings__Password`
- `AppUrl`

### Example (Docker):
```dockerfile
ENV EmailSettings__SmtpServer=smtp.gmail.com
ENV EmailSettings__SmtpPort=587
ENV EmailSettings__SenderEmail=${SENDER_EMAIL}
ENV EmailSettings__Username=${SMTP_USERNAME}
ENV EmailSettings__Password=${SMTP_PASSWORD}
ENV AppUrl=https://yourdomain.com
```

### Example (Azure App Service):
Configure in Application Settings:
- `EmailSettings:SmtpServer` → `smtp.gmail.com`
- `EmailSettings:SmtpPort` → `587`
- `EmailSettings:SenderEmail` → `your-email@gmail.com`
- `EmailSettings:Username` → `your-email@gmail.com`
- `EmailSettings:Password` → `your-app-password`
- `AppUrl` → `https://yourdomain.com`

## Frontend Integration

### Email Confirmation Page
Create a page at `/confirm-email` that:
1. Extracts `userId` and `token` from URL query parameters
2. Calls `POST /api/accounts/confirm-email` with these parameters
3. Shows success/error message
4. Redirects to login page on success

### Password Reset Page
Create a page at `/reset-password` that:
1. Extracts `userId` and `token` from URL query parameters
2. Shows a form for new password
3. Calls `POST /api/accounts/reset-password` with userId, token, and newPassword
4. Shows success/error message
5. Redirects to login page on success

### Forgot Password Page
Create a page at `/forgot-password` that:
1. Shows a form for email input
2. Calls `POST /api/accounts/forgot-password` with email
3. Shows message about email being sent
4. Provides link to resend if not received

## Email Templates

The application includes three HTML email templates:

1. **Confirmation Email** (Blue Theme)
   - Clean, professional design
   - Clear call-to-action button
   - 24-hour validity notice

2. **Password Reset Email** (Red Theme)
   - Security-focused messaging
   - Prominent reset button
   - 1-hour validity notice
   - Warning about unsolicited requests

3. **Order Confirmation Email** (Green Theme)
   - Order details summary
   - Customer information
   - Professional branding

All templates use inline CSS for maximum email client compatibility.
