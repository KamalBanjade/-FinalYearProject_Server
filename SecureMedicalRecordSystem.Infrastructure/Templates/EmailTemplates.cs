namespace SecureMedicalRecordSystem.Infrastructure.Templates;

public static class EmailTemplates
{
    public static string GetDoctorInvitationTemplate(string doctorName, string email, string temporaryPassword, string resetLink)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);'>
                <div style='background-color: #0f172a; color: white; padding: 24px; text-align: center;'>
                    <h1 style='margin: 0; font-size: 24px;'>Welcome to Medical Record System</h1>
                </div>
                <div style='padding: 32px;'>
                    <p style='font-size: 18px; font-weight: bold;'>Dear Dr. {doctorName},</p>
                    <p>Your doctor account has been successfully created in our Medical Record Management System. We're excited to have you join our healthcare network.</p>
                    
                    <div style='background-color: #f8fafc; padding: 24px; border-radius: 8px; border: 1px solid #cbd5e1; margin: 24px 0;'>
                        <h2 style='font-size: 16px; margin-top: 0; color: #64748b;'>Your Login Credentials</h2>
                        <p style='margin: 8px 0;'><strong>Email:</strong> {email}</p>
                        <p style='margin: 8px 0;'><strong>Temporary Password:</strong> <code style='background: #e2e8f0; padding: 4px 8px; border-radius: 4px; font-weight: bold;'>{temporaryPassword}</code></p>
                        <p style='font-size: 12px; color: #ef4444; margin-top: 12px;'>⚠️ For security reasons, you will be required to change this password on your first login.</p>
                    </div>

                    <p>To set your password immediately before your first login, click the button below:</p>
                    <div style='text-align: center; margin: 32px 0;'>
                        <a href='{resetLink}' style='background-color: #3b82f6; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Set My Password Now</a>
                    </div>
                    <p style='font-size: 12px; color: #64748b; text-align: center;'>This link expires in 24 hours.</p>

                    <h3 style='font-size: 16px; border-bottom: 1px solid #e2e8f0; padding-bottom: 8px; margin-top: 32px;'>Quick Start Guide</h3>
                    <ol style='padding-left: 20px;'>
                        <li>Visit our login page</li>
                        <li>Enter your email and temporary password</li>
                        <li>Create your new permanent password</li>
                        <li>Start managing patient records securely</li>
                    </ol>

                    <div style='margin-top: 32px; border-top: 1px solid #e2e8f0; padding-top: 24px;'>
                        <p style='margin: 0;'>Need help? Contact us at <a href='mailto:admin@medicalrecord.com' style='color: #3b82f6;'>admin@medicalrecord.com</a></p>
                    </div>
                </div>
                <div style='background-color: #f1f5f9; padding: 16px; text-align: center; font-size: 12px; color: #94a3b8;'>
                    <p style='margin: 0;'>© {DateTime.Now.Year} Medical Record System. All rights reserved.</p>
                    <p style='margin: 4px 0;'>This is an automated message. Please do not reply to this email.</p>
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetPasswordResetTemplate(string resetLink)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; padding: 32px;'>
                <h2 style='color: #0f172a; margin-top: 0;'>Password Reset Request</h2>
                <p>We received a request to reset your password for your Medical Record System account.</p>
                <div style='text-align: center; margin: 32px 0;'>
                    <a href='{resetLink}' style='background-color: #3b82f6; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Reset Password</a>
                </div>
                <p>This link will expire in 1 hour.</p>
                <p style='color: #64748b; font-size: 14px;'>If you didn't request this, please ignore this email. Your password will remain unchanged.</p>
                <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 32px 0;' />
                <p style='font-size: 12px; color: #94a3b8;'>© {DateTime.Now.Year} Medical Record System</p>
            </div>
        </body>
        </html>";
    }

    public static string GetEmailConfirmationTemplate(string confirmationLink)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; padding: 32px;'>
                <h2 style='color: #0f172a; margin-top: 0;'>Verify Your Email Address</h2>
                <p>Welcome to Medical Record System! Thank you for registering. Please verify your email address to complete your account setup.</p>
                <div style='text-align: center; margin: 32px 0;'>
                    <a href='{confirmationLink}' style='background-color: #10b981; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Verify Email Address</a>
                </div>
                <p>This link will expire in 24 hours.</p>
                <hr style='border: 0; border-top: 1px solid #e2e8f0; margin: 32px 0;' />
                <p style='font-size: 12px; color: #94a3b8;'>© {DateTime.Now.Year} Medical Record System</p>
            </div>
        </body>
        </html>";
    }
}
