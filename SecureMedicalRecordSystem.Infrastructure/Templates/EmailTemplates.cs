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

    public static string GetPatientInvitationTemplate(string patientName, string email, string temporaryPassword, string resetLink)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);'>
                <div style='background-color: #10b981; color: white; padding: 24px; text-align: center;'>
                    <h1 style='margin: 0; font-size: 24px;'>Welcome to Medical Record System</h1>
                </div>
                <div style='padding: 32px;'>
                    <p style='font-size: 18px; font-weight: bold;'>Dear {patientName},</p>
                    <p>Your healthcare provider has created a secure patient portal account for you in the Medical Record Management System.</p>
                    
                    <div style='background-color: #f0fdf4; padding: 24px; border-radius: 8px; border: 1px solid #bbf7d0; margin: 24px 0;'>
                        <h2 style='font-size: 16px; margin-top: 0; color: #065f46;'>Your Login Credentials</h2>
                        <p style='margin: 8px 0;'><strong>Email:</strong> {email}</p>
                        <p style='margin: 8px 0;'><strong>Temporary Password:</strong> <code style='background: #e2e8f0; padding: 4px 8px; border-radius: 4px; font-weight: bold; color: #333;'>{temporaryPassword}</code></p>
                    </div>

                    <p>To access your medical records and communicate securely with your doctor, please set your password by clicking the button below:</p>
                    <div style='text-align: center; margin: 32px 0;'>
                        <a href='{resetLink}' style='background-color: #10b981; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Set My Password to Access My Account</a>
                    </div>
                    <p style='font-size: 12px; color: #64748b; text-align: center;'>This link expires in 24 hours.</p>

                    <div style='margin-top: 32px; border-top: 1px solid #e2e8f0; padding-top: 24px;'>
                        <p style='margin: 0;'>If you have any questions, please contact your clinic.</p>
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

    public static string GetAppointmentScheduledTemplate(string patientName, string doctorName, DateTime date, string reason)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                <div style='background-color: #3b82f6; color: white; padding: 24px; text-align: center;'>
                    <h1 style='margin: 0; font-size: 20px;'>Appointment Requested</h1>
                </div>
                <div style='padding: 32px;'>
                    <p>Dear {patientName},</p>
                    <p>Your appointment request has been received and is waiting for confirmation from the doctor.</p>
                    <div style='background-color: #f8fafc; padding: 20px; border-radius: 8px; border: 1px solid #e2e8f0; margin: 24px 0;'>
                        <p style='margin: 5px 0;'><strong>Doctor:</strong> Dr. {doctorName}</p>
                        <p style='margin: 5px 0;'><strong>Date & Time:</strong> {date:f}</p>
                        <p style='margin: 5px 0;'><strong>Reason:</strong> {reason}</p>
                    </div>
                    <p>We will notify you once the doctor confirms the appointment.</p>
                </div>
                <div style='background-color: #f1f5f9; padding: 16px; text-align: center; font-size: 11px; color: #94a3b8;'>
                    <p>© {DateTime.Now.Year} Medical Record System</p>
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetAppointmentConfirmedTemplate(string patientName, string doctorName, DateTime date, string department)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                <div style='background-color: #10b981; color: white; padding: 24px; text-align: center;'>
                    <h1 style='margin: 0; font-size: 20px;'>Appointment Confirmed!</h1>
                </div>
                <div style='padding: 32px;'>
                    <p>Dear {patientName},</p>
                    <p>Your appointment with Dr. {doctorName} has been <strong>confirmed</strong>.</p>
                    <div style='background-color: #f0fdf4; padding: 20px; border-radius: 8px; border: 1px solid #bbf7d0; margin: 24px 0;'>
                        <p style='margin: 5px 0;'><strong>Doctor:</strong> Dr. {doctorName}</p>
                        <p style='margin: 5px 0;'><strong>Department:</strong> {department}</p>
                        <p style='margin: 5px 0;'><strong>Date & Time:</strong> {date:f}</p>
                    </div>
                    <p>Please arrive 10 minutes before your scheduled time. You can find more details in your dashboard.</p>
                </div>
                <div style='background-color: #f1f5f9; padding: 16px; text-align: center; font-size: 11px; color: #94a3b8;'>
                    <p>© {DateTime.Now.Year} Medical Record System</p>
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetAppointmentCancelledTemplate(string userName, DateTime date, string reason, string role)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                <div style='background-color: #ef4444; color: white; padding: 24px; text-align: center;'>
                    <h1 style='margin: 0; font-size: 20px;'>Appointment Cancelled</h1>
                </div>
                <div style='padding: 32px;'>
                    <p>Dear {userName},</p>
                    <p>The appointment scheduled for <strong>{date:f}</strong> has been cancelled.</p>
                    <div style='background-color: #fef2f2; padding: 20px; border-radius: 8px; border: 1px solid #fecaca; margin: 24px 0;'>
                        <p style='margin: 5px 0;'><strong>Cancellation Reason:</strong> {reason}</p>
                    </div>
                    { (role == "Patient" ? "<p>You can book a new appointment through your dashboard.</p>" : "") }
                </div>
                <div style='background-color: #f1f5f9; padding: 16px; text-align: center; font-size: 11px; color: #94a3b8;'>
                    <p>© {DateTime.Now.Year} Medical Record System</p>
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetAppointmentRescheduledTemplate(string patientName, string doctorName, DateTime oldDate, DateTime newDate)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                <div style='background-color: #f59e0b; color: white; padding: 24px; text-align: center;'>
                    <h1 style='margin: 0; font-size: 20px;'>Appointment Rescheduled</h1>
                </div>
                <div style='padding: 32px;'>
                    <p>Dear {patientName},</p>
                    <p>Your appointment with Dr. {doctorName} has been rescheduled.</p>
                    <div style='background-color: #fffbeb; padding: 20px; border-radius: 8px; border: 1px solid #fef3c7; margin: 24px 0;'>
                        <p style='margin: 5px 0; color: #92400e;'><strong>Old Date:</strong> <strike>{oldDate:f}</strike></p>
                        <p style='margin: 5px 0; font-size: 16px;'><strong>New Date:</strong> {newDate:f}</p>
                    </div>
                    <p>Please log in to your dashboard to confirm this new time slot.</p>
                </div>
                <div style='background-color: #f1f5f9; padding: 16px; text-align: center; font-size: 11px; color: #94a3b8;'>
                    <p>© {DateTime.Now.Year} Medical Record System</p>
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetAppointmentReminderTemplate(string patientName, string doctorName, DateTime date)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                <div style='background-color: #6366f1; color: white; padding: 24px; text-align: center;'>
                    <h1 style='margin: 0; font-size: 20px;'>Appointment Reminder</h1>
                </div>
                <div style='padding: 32px;'>
                    <p>Dear {patientName},</p>
                    <p>This is a friendly reminder of your upcoming appointment tomorrow.</p>
                    <div style='background-color: #eef2ff; padding: 20px; border-radius: 8px; border: 1px solid #e0e7ff; margin: 24px 0;'>
                        <p style='margin: 5px 0;'><strong>Doctor:</strong> Dr. {doctorName}</p>
                        <p style='margin: 5px 0;'><strong>Date & Time:</strong> {date:f}</p>
                    </div>
                    <p>If you need to cancel or reschedule, please do so at least 24 hours in advance through your dashboard.</p>
                </div>
                <div style='background-color: #f1f5f9; padding: 16px; text-align: center; font-size: 11px; color: #94a3b8;'>
                    <p>© {DateTime.Now.Year} Medical Record System</p>
                </div>
            </div>
        </body>
        </html>";
    }
    public static string GetDoctorNewAppointmentTemplate(string doctorName, string patientName, DateTime date, string reason)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                <div style='background-color: #0f172a; color: white; padding: 24px; text-align: center;'>
                    <h1 style='margin: 0; font-size: 20px;'>New Appointment Scheduled</h1>
                </div>
                <div style='padding: 32px;'>
                    <p>Dear Dr. {doctorName},</p>
                    <p>A new appointment has been scheduled by a patient.</p>
                    <div style='background-color: #f8fafc; padding: 20px; border-radius: 8px; border: 1px solid #e2e8f0; margin: 24px 0;'>
                        <p style='margin: 5px 0;'><strong>Patient:</strong> {patientName}</p>
                        <p style='margin: 5px 0;'><strong>Date & Time:</strong> {date:f}</p>
                        <p style='margin: 5px 0;'><strong>Reason:</strong> {reason}</p>
                    </div>
                    <p>You can view the patient's history and prepare for the visit through your dashboard.</p>
                </div>
                <div style='background-color: #f1f5f9; padding: 16px; text-align: center; font-size: 11px; color: #94a3b8;'>
                    <p>© {DateTime.Now.Year} Medical Record System</p>
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetFollowUpScheduledTemplate(
        string patientName,
        string doctorName,
        DateTime appointmentDate)
    {
        var formattedDate = appointmentDate.ToLocalTime().ToString("dddd, MMMM d, yyyy");
        var formattedTime = appointmentDate.ToLocalTime().ToString("h:mm tt");

        return $@"<!DOCTYPE html>
        <html>
        <head><meta charset='UTF-8'></head>
        <body style='font-family: Inter, system-ui, sans-serif; background: #f8fafc; margin: 0; padding: 24px;'>
            <div style='max-width: 560px; margin: 0 auto; background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.06);'>
                <div style='background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); padding: 32px; text-align: center;'>
                    <div style='display: inline-block; background: rgba(16,185,129,0.15); border-radius: 50%; padding: 16px; margin-bottom: 16px;'>
                        <span style='font-size: 32px;'>📅</span>
                    </div>
                    <h1 style='color: white; margin: 0; font-size: 22px; font-weight: 900; letter-spacing: -0.5px;'>Follow-Up Appointment Confirmed</h1>
                    <p style='color: #94a3b8; margin: 8px 0 0; font-size: 13px;'>Scheduled by Dr. {doctorName}</p>
                </div>

                <div style='padding: 32px;'>
                    <p style='color: #374151; font-size: 15px; margin: 0 0 24px;'>Hello <strong>{patientName}</strong>,</p>
                    <p style='color: #6b7280; font-size: 14px; line-height: 1.6; margin: 0 0 24px;'>
                        Your doctor has scheduled a follow-up appointment for you as part of your recent consultation. Please find the details below.
                    </p>

                    <div style='background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 12px; padding: 20px; margin-bottom: 24px;'>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0; color: #6b7280; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.1em; width: 40%;'>Date</td>
                                <td style='padding: 8px 0; color: #111827; font-size: 14px; font-weight: 700;'>{formattedDate}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #6b7280; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.1em;'>Time</td>
                                <td style='padding: 8px 0; color: #111827; font-size: 14px; font-weight: 700;'>{formattedTime}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #6b7280; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.1em;'>Doctor</td>
                                <td style='padding: 8px 0; color: #111827; font-size: 14px; font-weight: 700;'>Dr. {doctorName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #6b7280; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.1em;'>Type</td>
                                <td style='padding: 8px 0; color: #059669; font-size: 14px; font-weight: 700;'>Follow-Up Visit</td>
                            </tr>
                        </table>
                    </div>

                    <p style='color: #6b7280; font-size: 13px; line-height: 1.6; margin: 0 0 8px;'>
                        A calendar invite (.ics file) is attached to this email. You can import it into Google Calendar, Outlook, or Apple Calendar.
                    </p>
                    <p style='color: #6b7280; font-size: 13px; line-height: 1.6; margin: 0;'>
                        If you need to reschedule, please contact the clinic as soon as possible.
                    </p>
                </div>

                <div style='background-color: #f1f5f9; padding: 16px; text-align: center; font-size: 11px; color: #94a3b8;'>
                    <p>© {DateTime.Now.Year} Medical Record System &nbsp;|&nbsp; This appointment was booked by your doctor</p>
                </div>
            </div>
        </body>
        </html>";
    }
}
