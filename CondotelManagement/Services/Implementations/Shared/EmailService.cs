using CondotelManagement.Services.Interfaces.Shared;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Mail;
using System.Net;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace CondotelManagement.Services.Implementations.Shared
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new System.Net.Mail.SmtpClient
            {
                Host = _config["Email:SmtpHost"],
                Port = int.Parse(_config["Email:SmtpPort"]),
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    _config["Email:Username"],
                    _config["Email:Password"]
                )
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["Email:From"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Reset Your Password - Condotel Management";

            var body = new BodyBuilder
            {
                HtmlBody = $"Please reset your password by <a href='{resetLink}'>clicking here</a>."
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        public class BookingEmailInfo
        {
           
            public string CustomerName { get; set; } = string.Empty;


            public string? GuestName { get; set; }

            
            public string CondotelName { get; set; } = string.Empty;


            public string RoomNumber { get; set; } = string.Empty;

            // Mã xác minh check-in
            public string CheckInToken { get; set; } = string.Empty;

            // Thời gian
            public DateTime CheckInAt { get; set; }
            public DateTime CheckOutAt { get; set; }
        }


        public async Task SendBookingConfirmedEmailAsync(
          string toEmail,
          BookingEmailInfo info
      )
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"Xác nhận đặt phòng – {info.CondotelName} ({info.RoomNumber})";

            var guestSection = string.IsNullOrWhiteSpace(info.GuestName)
                ? ""
                : $@"
            <tr>
                <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>👤 Người lưu trú:</strong></td>
                <td style='padding: 10px; border-bottom: 1px solid #eee;'>{info.GuestName}</td>
            </tr>";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .success-icon {{ font-size: 48px; margin-bottom: 20px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .token-box {{ background: #e3f2fd; border-left: 4px solid #2196f3; padding: 20px; margin: 20px 0; border-radius: 8px; }}
        .token {{ font-size: 28px; font-weight: bold; color: #1976d2; letter-spacing: 2px; text-align: center; }}
        table {{ width: 100%; border-collapse: collapse; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
        .warning-box {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='success-icon'>✓</div>
            <h1>Đặt phòng thành công!</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{info.CustomerName}</strong>,</p>
            
            <p>Chúng tôi xác nhận <strong>đặt phòng của bạn đã được thanh toán và xác nhận thành công</strong>.</p>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #667eea;'>🏨 THÔNG TIN CĂN HỘ</h3>
                <table>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Tên căn hộ:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{info.CondotelName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Phòng:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{info.RoomNumber}</td>
                    </tr>
                    {guestSection}
                </table>
            </div>

            <div class='info-box'>
                <h3 style='margin-top: 0; color: #667eea;'>⏰ THỜI GIAN LƯU TRÚ</h3>
                <table>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Check-in:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>Từ 14:00 – {info.CheckInAt:dd/MM/yyyy}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px;'><strong>Check-out:</strong></td>
                        <td style='padding: 10px;'>Trước 12:00 – {info.CheckOutAt:dd/MM/yyyy}</td>
                    </tr>
                </table>
            </div>

            <div class='token-box'>
                <h3 style='margin-top: 0; color: #1976d2; text-align: center;'>🔐 MÃ XÁC MINH CHECK-IN</h3>
                <div class='token'>{info.CheckInToken}</div>
                <p style='text-align: center; margin-bottom: 0; color: #1976d2;'>
                    <strong>Mã này là bằng chứng xác nhận hợp lệ để nhận phòng</strong>
                </p>
            </div>

            <div class='info-box'>
                <h3 style='margin-top: 0; color: #667eea;'>🧾 QUY TRÌNH NHẬN PHÒNG</h3>
                <p>Khi đến nhận phòng, khách vui lòng:</p>
                <ul style='padding-left: 20px;'>
                    <li>Cung cấp mã check-in</li>
                    <li>Xuất trình CCCD / Hộ chiếu hợp lệ</li>
                    <li>Thông tin giấy tờ phải trùng khớp với người lưu trú</li>
                </ul>
                <p style='margin-bottom: 0;'>
                    <em>Trường hợp không cung cấp được mã hoặc thông tin không hợp lệ, 
                    chủ nhà/lễ tân có quyền từ chối nhận phòng theo chính sách vận hành.</em>
                </p>
            </div>

            <div class='warning-box'>
                <h4 style='margin-top: 0; color: #856404;'>🔒 LƯU Ý QUAN TRỌNG</h4>
                <ul style='color: #856404; padding-left: 20px; margin-bottom: 0;'>
                    <li>Không chia sẻ mã check-in cho người không liên quan</li>
                    <li>Người sở hữu mã được xem là người được ủy quyền hợp lệ</li>
                    <li>Mã chỉ có hiệu lực trong thời gian lưu trú đã đăng ký</li>
                </ul>
            </div>
            
            <p>Cảm ơn bạn đã sử dụng dịch vụ.<br>
            Chúc bạn có kỳ nghỉ thuận lợi!</p>
            
            <p>Trân trọng,<br>
            <strong>Ban quản lý hệ thống</strong></p>
            
            <div class='footer'>
                <p>Email này được gửi tự động, vui lòng không trả lời email này.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            var body = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }


        public async Task SendPasswordResetOtpAsync(string toEmail, string otp)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Your Password Reset OTP - Condotel Management";

            // Sửa lại nội dung email để hiển thị OTP
            var body = new BodyBuilder
            {
                HtmlBody = $"<p>Your password reset OTP code is:</p>" +
                           $"<h1 style='font-size: 24px; font-weight: bold; color: #333;'>{otp}</h1>" +
                           $"<p>This code will expire in 10 minutes.</p>" +
                           $"<p>If you did not request this, please ignore this email.</p>"
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendVerificationOtpAsync(string toEmail, string otp)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Verify Your Email - Condotel Management";

            var body = new BodyBuilder
            {
                HtmlBody = $"<p>Thank you for registering. Your email verification OTP code is:</p>" +
                           $"<h1 style='font-size: 24px; font-weight: bold; color: #333;'>{otp}</h1>" +
                           $"<p>This code will expire in 10 minutes.</p>"
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendRefundConfirmationEmailAsync(string toEmail, string customerName, int bookingId, decimal refundAmount, string? bankCode = null, string? accountNumber = null)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"Xác nhận hoàn tiền thành công - Booking #{bookingId}";

            // Format số tiền
            var formattedAmount = refundAmount.ToString("N0").Replace(",", ".") + " VNĐ";
            
            // Tạo nội dung email đẹp
            var bankInfoHtml = "";
            if (!string.IsNullOrEmpty(bankCode) && !string.IsNullOrEmpty(accountNumber))
            {
                bankInfoHtml = $@"
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Ngân hàng:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{bankCode}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Số tài khoản:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{accountNumber}</td>
                    </tr>";
            }

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .success-icon {{ font-size: 48px; margin-bottom: 20px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .amount {{ font-size: 24px; font-weight: bold; color: #28a745; margin: 10px 0; }}
        table {{ width: 100%; border-collapse: collapse; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='success-icon'>✓</div>
            <h1>Hoàn tiền thành công!</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{customerName}</strong>,</p>
            
            <p>Chúng tôi xin thông báo rằng yêu cầu hoàn tiền cho booking <strong>#{bookingId}</strong> đã được xử lý thành công.</p>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #667eea;'>Thông tin hoàn tiền</h3>
                <table>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Mã booking:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>#{bookingId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Số tiền hoàn lại:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><span class='amount'>{formattedAmount}</span></td>
                    </tr>
                    {bankInfoHtml}
                    <tr>
                        <td style='padding: 10px;'><strong>Trạng thái:</strong></td>
                        <td style='padding: 10px;'><span style='color: #28a745; font-weight: bold;'>Đã hoàn tiền</span></td>
                    </tr>
                </table>
            </div>
            
            <p>Tiền hoàn lại sẽ được chuyển vào tài khoản của bạn trong vòng 1-3 ngày làm việc (tùy thuộc vào ngân hàng).</p>
            
            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email hoặc hotline.</p>
            
            <p>Trân trọng,<br>
            <strong>Đội ngũ Condotel Management</strong></p>
            
            <div class='footer'>
                <p>Email này được gửi tự động, vui lòng không trả lời email này.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            var body = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendRefundRejectionEmailAsync(string toEmail, string customerName, int bookingId, decimal refundAmount, string reason)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"Thông báo từ chối yêu cầu hoàn tiền - Booking #{bookingId}";

            // Format số tiền
            var formattedAmount = refundAmount.ToString("N0").Replace(",", ".") + " VNĐ";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .warning-icon {{ font-size: 48px; margin-bottom: 20px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .amount {{ font-size: 24px; font-weight: bold; color: #dc3545; margin: 10px 0; }}
        .reason-box {{ background: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 20px 0; border-radius: 4px; }}
        table {{ width: 100%; border-collapse: collapse; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='warning-icon'>⚠</div>
            <h1>Yêu cầu hoàn tiền đã bị từ chối</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{customerName}</strong>,</p>
            
            <p>Chúng tôi rất tiếc phải thông báo rằng yêu cầu hoàn tiền cho booking <strong>#{bookingId}</strong> đã bị từ chối.</p>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #dc3545;'>Thông tin yêu cầu hoàn tiền</h3>
                <table>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Mã booking:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>#{bookingId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Số tiền yêu cầu hoàn:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><span class='amount'>{formattedAmount}</span></td>
                    </tr>
                    <tr>
                        <td style='padding: 10px;'><strong>Trạng thái:</strong></td>
                        <td style='padding: 10px;'><span style='color: #dc3545; font-weight: bold;'>Đã từ chối</span></td>
                    </tr>
                </table>
            </div>

            <div class='reason-box'>
                <h4 style='margin-top: 0; color: #856404;'>Lý do từ chối:</h4>
                <p style='margin-bottom: 0; color: #856404;'>{reason}</p>
            </div>
            
            <p>Nếu bạn có bất kỳ thắc mắc nào về quyết định này, vui lòng liên hệ với chúng tôi qua email hoặc hotline để được hỗ trợ.</p>
            
            <p>Trân trọng,<br>
            <strong>Đội ngũ Condotel Management</strong></p>
            
            <div class='footer'>
                <p>Email này được gửi tự động, vui lòng không trả lời email này.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            var body = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendPayoutConfirmationEmailAsync(string toEmail, string hostName, int bookingId, string condotelName, decimal amount, DateTime paidAt, string? bankName = null, string? accountNumber = null, string? accountHolderName = null)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"Xác nhận thanh toán thành công - Booking #{bookingId}";

            // Format số tiền
            var formattedAmount = amount.ToString("N0").Replace(",", ".") + " VNĐ";
            var formattedDate = paidAt.ToString("dd/MM/yyyy HH:mm");
            
            // Tạo nội dung email đẹp
            var bankInfoHtml = "";
            if (!string.IsNullOrEmpty(bankName) && !string.IsNullOrEmpty(accountNumber))
            {
                bankInfoHtml = $@"
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Ngân hàng:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{bankName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Số tài khoản:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{accountNumber}</td>
                    </tr>";
                if (!string.IsNullOrEmpty(accountHolderName))
                {
                    bankInfoHtml += $@"
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Chủ tài khoản:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{accountHolderName}</td>
                    </tr>";
                }
            }

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .success-icon {{ font-size: 48px; margin-bottom: 20px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .amount {{ font-size: 24px; font-weight: bold; color: #28a745; margin: 10px 0; }}
        table {{ width: 100%; border-collapse: collapse; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='success-icon'>✓</div>
            <h1>Thanh toán thành công!</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{hostName}</strong>,</p>
            
            <p>Chúng tôi xin thông báo rằng thanh toán cho booking <strong>#{bookingId}</strong> đã được xử lý thành công.</p>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #28a745;'>Thông tin thanh toán</h3>
                <table>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Mã booking:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>#{bookingId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Tên condotel:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{condotelName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Số tiền đã thanh toán:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><span class='amount'>{formattedAmount}</span></td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Ngày thanh toán:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{formattedDate}</td>
                    </tr>
                    {bankInfoHtml}
                    <tr>
                        <td style='padding: 10px;'><strong>Trạng thái:</strong></td>
                        <td style='padding: 10px;'><span style='color: #28a745; font-weight: bold;'>Đã thanh toán</span></td>
                    </tr>
                </table>
            </div>
            
            <p>Tiền đã được chuyển vào tài khoản ngân hàng của bạn. Vui lòng kiểm tra tài khoản trong vòng 1-3 ngày làm việc (tùy thuộc vào ngân hàng).</p>
            
            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email hoặc hotline.</p>
            
            <p>Trân trọng,<br>
            <strong>Đội ngũ Condotel Management</strong></p>
            
            <div class='footer'>
                <p>Email này được gửi tự động, vui lòng không trả lời email này.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            var body = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendPayoutAccountErrorEmailAsync(string toEmail, string hostName, int bookingId, string condotelName, decimal amount, string? currentBankName = null, string? currentAccountNumber = null, string? currentAccountHolderName = null, string? errorMessage = null)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"⚠️ Thông báo lỗi thông tin tài khoản - Booking #{bookingId}";

            // Format số tiền
            var formattedAmount = amount.ToString("N0").Replace(",", ".") + " VNĐ";
            
            // Tạo nội dung email đẹp
            var bankInfoHtml = "";
            if (!string.IsNullOrEmpty(currentBankName) && !string.IsNullOrEmpty(currentAccountNumber))
            {
                bankInfoHtml = $@"
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Ngân hàng hiện tại:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{currentBankName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Số tài khoản hiện tại:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{currentAccountNumber}</td>
                    </tr>";
                if (!string.IsNullOrEmpty(currentAccountHolderName))
                {
                    bankInfoHtml += $@"
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Chủ tài khoản hiện tại:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{currentAccountHolderName}</td>
                    </tr>";
                }
            }

            var errorMessageHtml = "";
            if (!string.IsNullOrEmpty(errorMessage))
            {
                errorMessageHtml = $@"
                    <div style='background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                        <h4 style='margin-top: 0; color: #856404;'>Chi tiết lỗi:</h4>
                        <p style='color: #856404; margin: 0;'>{errorMessage}</p>
                    </div>";
            }

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .warning-icon {{ font-size: 48px; margin-bottom: 20px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .amount {{ font-size: 24px; font-weight: bold; color: #ff9800; margin: 10px 0; }}
        table {{ width: 100%; border-collapse: collapse; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
        .action-box {{ background: #e3f2fd; border-left: 4px solid #2196f3; padding: 15px; margin: 20px 0; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='warning-icon'>⚠️</div>
            <h1>Thông báo lỗi thông tin tài khoản</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{hostName}</strong>,</p>
            
            <p>Chúng tôi xin thông báo rằng khi thực hiện thanh toán cho booking <strong>#{bookingId}</strong>, chúng tôi phát hiện <strong style='color: #d32f2f;'>thông tin tài khoản ngân hàng không chính xác</strong>.</p>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #ff9800;'>Thông tin booking</h3>
                <table>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Mã booking:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>#{bookingId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Tên condotel:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{condotelName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Số tiền cần thanh toán:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><span class='amount'>{formattedAmount}</span></td>
                    </tr>
                </table>
            </div>

            {errorMessageHtml}

            <div class='info-box'>
                <h3 style='margin-top: 0; color: #ff9800;'>Thông tin tài khoản hiện tại trong hệ thống</h3>
                <table>
                    {bankInfoHtml}
                </table>
            </div>

            <div class='action-box'>
                <h4 style='margin-top: 0; color: #1976d2;'>Hành động cần thực hiện:</h4>
                <ol style='color: #1976d2; padding-left: 20px;'>
                    <li>Vui lòng đăng nhập vào hệ thống và kiểm tra lại thông tin tài khoản ngân hàng của bạn.</li>
                    <li>Cập nhật thông tin tài khoản ngân hàng chính xác (số tài khoản, tên chủ tài khoản, tên ngân hàng).</li>
                    <li>Sau khi cập nhật, vui lòng liên hệ với chúng tôi để chúng tôi có thể thực hiện lại thanh toán.</li>
                </ol>
            </div>
            
            <p><strong>Lưu ý:</strong> Thanh toán sẽ chỉ được thực hiện sau khi bạn cập nhật thông tin tài khoản chính xác.</p>
            
            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email hoặc hotline.</p>
            
            <p>Trân trọng,<br>
            <strong>Đội ngũ Condotel Management</strong></p>
            
            <div class='footer'>
                <p>Email này được gửi tự động, vui lòng không trả lời email này.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            var body = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendPayoutRejectionEmailAsync(string toEmail, string hostName, int bookingId, string condotelName, decimal amount, string reason)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"❌ Từ chối thanh toán - Booking #{bookingId}";

            // Format số tiền
            var formattedAmount = amount.ToString("N0").Replace(",", ".") + " VNĐ";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #d32f2f 0%, #c62828 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .rejection-icon {{ font-size: 48px; margin-bottom: 20px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .amount {{ font-size: 24px; font-weight: bold; color: #d32f2f; margin: 10px 0; }}
        table {{ width: 100%; border-collapse: collapse; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
        .reason-box {{ background: #ffebee; border-left: 4px solid #d32f2f; padding: 15px; margin: 20px 0; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='rejection-icon'>❌</div>
            <h1>Thông báo từ chối thanh toán</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{hostName}</strong>,</p>
            
            <p>Chúng tôi xin thông báo rằng yêu cầu thanh toán cho booking <strong>#{bookingId}</strong> đã bị <strong style='color: #d32f2f;'>từ chối</strong>.</p>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #d32f2f;'>Thông tin booking</h3>
                <table>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Mã booking:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>#{bookingId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Tên condotel:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{condotelName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Số tiền:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><span class='amount'>{formattedAmount}</span></td>
                    </tr>
                </table>
            </div>

            <div class='reason-box'>
                <h4 style='margin-top: 0; color: #c62828;'>Lý do từ chối:</h4>
                <p style='color: #c62828; margin: 0;'>{reason}</p>
            </div>
            
            <p><strong>Lưu ý:</strong> Booking này sẽ không được thanh toán cho đến khi vấn đề được giải quyết. Nếu bạn có bất kỳ câu hỏi nào về quyết định này, vui lòng liên hệ với chúng tôi.</p>
            
            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email hoặc hotline.</p>
            
            <p>Trân trọng,<br>
            <strong>Đội ngũ Condotel Management</strong></p>
            
            <div class='footer'>
                <p>Email này được gửi tự động, vui lòng không trả lời email này.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            var body = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendVoucherNotificationEmailAsync(string toEmail, string customerName, int bookingId, List<CondotelManagement.Services.Interfaces.Shared.VoucherInfo> vouchers)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"🎁 Bạn đã nhận được {vouchers.Count} voucher từ booking #{bookingId} - Condotel Management";

            // Tạo danh sách voucher HTML
            var vouchersHtml = "";
            foreach (var voucher in vouchers)
            {
                var discountText = voucher.DiscountAmount.HasValue && voucher.DiscountAmount > 0
                    ? $"{voucher.DiscountAmount:N0} VNĐ"
                    : voucher.DiscountPercentage.HasValue && voucher.DiscountPercentage > 0
                        ? $"{voucher.DiscountPercentage}%"
                        : "Không có giảm giá";

                vouchersHtml += $@"
                <div style='background: white; padding: 20px; border-radius: 8px; margin: 15px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); border-left: 4px solid #4caf50;'>
                    <h3 style='margin-top: 0; color: #4caf50;'>{voucher.CondotelName}</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <tr>
                            <td style='padding: 8px; border-bottom: 1px solid #eee;'><strong>Mã voucher:</strong></td>
                            <td style='padding: 8px; border-bottom: 1px solid #eee;'><strong style='color: #4caf50; font-size: 18px;'>{voucher.Code}</strong></td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border-bottom: 1px solid #eee;'><strong>Giảm giá:</strong></td>
                            <td style='padding: 8px; border-bottom: 1px solid #eee;'><span style='color: #d32f2f; font-weight: bold;'>{discountText}</span></td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border-bottom: 1px solid #eee;'><strong>Hiệu lực từ:</strong></td>
                            <td style='padding: 8px; border-bottom: 1px solid #eee;'>{voucher.StartDate:dd/MM/yyyy}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px;'><strong>Hết hạn:</strong></td>
                            <td style='padding: 8px;'>{voucher.EndDate:dd/MM/yyyy}</td>
                        </tr>
                    </table>
                </div>";
            }

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #4caf50 0%, #388e3c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .voucher-icon {{ font-size: 48px; margin-bottom: 20px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
        .highlight {{ background: #e8f5e9; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #4caf50; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='voucher-icon'>🎁</div>
            <h1>Chúc mừng! Bạn đã nhận được voucher</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{customerName}</strong>,</p>
            
            <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi! Chúng tôi xin gửi tặng bạn <strong>{vouchers.Count} voucher</strong> như một phần thưởng cho booking <strong>#{bookingId}</strong> đã hoàn thành.</p>
            
            <div class='highlight'>
                <p style='margin: 0;'><strong>💡 Lưu ý:</strong> Bạn có thể sử dụng các voucher này cho các booking tiếp theo tại các condotel tương ứng. Hãy nhập mã voucher khi đặt phòng để nhận được ưu đãi!</p>
            </div>

            <h2 style='color: #4caf50; margin-top: 30px;'>Chi tiết voucher:</h2>
            {vouchersHtml}
            
            <p><strong>Cách sử dụng:</strong></p>
            <ol>
                <li>Khi đặt phòng, nhập mã voucher vào ô 'Mã giảm giá'</li>
                <li>Hệ thống sẽ tự động áp dụng giảm giá cho booking của bạn</li>
                <li>Mỗi voucher chỉ có thể sử dụng một lần</li>
                <li>Voucher có thời hạn sử dụng, vui lòng sử dụng trước ngày hết hạn</li>
            </ol>
            
            <p>Chúc bạn có những trải nghiệm tuyệt vời với dịch vụ của chúng tôi!</p>
            
            <p>Trân trọng,<br>
            <strong>Đội ngũ Condotel Management</strong></p>
            
            <div class='footer'>
                <p>Email này được gửi tự động, vui lòng không trả lời email này.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            var body = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendBookingConfirmationEmailAsync(string toEmail, string customerName, int bookingId, string condotelName, DateOnly checkInDate, DateOnly checkOutDate, decimal totalAmount, DateTime confirmedAt, string? checkInToken = null, string? guestFullName = null, string? guestPhone = null, string? guestIdNumber = null)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"✅ Xác nhận đặt phòng thành công - Booking #{bookingId}";

            // Format dữ liệu
            var formattedAmount = totalAmount.ToString("N0").Replace(",", ".") + " VNĐ";
            var formattedConfirmDate = confirmedAt.ToString("dd/MM/yyyy HH:mm");
            var formattedCheckIn = checkInDate.ToString("dd/MM/yyyy");
            var formattedCheckOut = checkOutDate.ToString("dd/MM/yyyy");
            
            // Tính số đêm
            var nights = checkOutDate.DayNumber - checkInDate.DayNumber;

            // Tạo phần hiển thị CheckInToken nếu có
            var checkInTokenHtml = string.IsNullOrEmpty(checkInToken) ? "" : $@"
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Mã Check-in:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><span style='font-size: 18px; font-weight: bold; color: #ff6b6b; font-family: monospace;'>{checkInToken}</span></td>
                    </tr>";

            // Tạo phần hiển thị thông tin guest nếu có
            var guestInfoHtml = "";
            if (!string.IsNullOrEmpty(guestFullName) || !string.IsNullOrEmpty(guestPhone) || !string.IsNullOrEmpty(guestIdNumber))
            {
                guestInfoHtml = $@"
            <div class='info-box' style='background: #fff3cd; border-left: 4px solid #ffc107;'>
                <h3 style='margin-top: 0; color: #ff9800;'>🎫 Thông tin người nhận phòng (Đặt hộ)</h3>
                <table>";
                
                if (!string.IsNullOrEmpty(guestFullName))
                {
                    guestInfoHtml += $@"
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Họ và tên:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{guestFullName}</td>
                    </tr>";
                }
                
                if (!string.IsNullOrEmpty(guestPhone))
                {
                    guestInfoHtml += $@"
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Số điện thoại:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{guestPhone}</td>
                    </tr>";
                }
                
                if (!string.IsNullOrEmpty(guestIdNumber))
                {
                    guestInfoHtml += $@"
                    <tr>
                        <td style='padding: 10px;'><strong>CMND/CCCD:</strong></td>
                        <td style='padding: 10px;'>{guestIdNumber}</td>
                    </tr>";
                }
                
                guestInfoHtml += $@"
                </table>
                <p style='margin: 10px 0 0 0; color: #d35400;'><strong>⚠️ Lưu ý:</strong> Người nhận phòng cần mang theo CMND/CCCD và thông báo cho lễ tân biết họ được đặt hộ.</p>
            </div>";
            }

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .success-icon {{ font-size: 48px; margin-bottom: 20px; }}
        .content {{ background: #f8f9fa; padding: 30px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .amount {{ font-size: 24px; font-weight: bold; color: #667eea; margin: 10px 0; }}
        table {{ width: 100%; border-collapse: collapse; }}
        .highlight-box {{ background: #e3f2fd; padding: 15px; border-left: 4px solid #2196f3; border-radius: 4px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
        .btn {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='success-icon'>✅</div>
            <h1>Đặt phòng thành công!</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{customerName}</strong>,</p>
            
            <p>Cảm ơn bạn đã thanh toán! Đơn đặt phòng của bạn đã được xác nhận thành công.</p>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #667eea;'>Thông tin đặt phòng</h3>
                <table>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Mã booking:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>#{bookingId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Tên condotel:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{condotelName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Ngày nhận phòng:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{formattedCheckIn}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Ngày trả phòng:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{formattedCheckOut}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Số đêm:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{nights} đêm</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Tổng tiền:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><span class='amount'>{formattedAmount}</span></td>
                    </tr>
                    {checkInTokenHtml}
                    <tr>
                        <td style='padding: 10px;'><strong>Thời gian xác nhận:</strong></td>
                        <td style='padding: 10px;'>{formattedConfirmDate}</td>
                    </tr>
                </table>
            </div>

            {guestInfoHtml}

            <div class='highlight-box'>
                <p style='margin: 0;'><strong>📌 Lưu ý quan trọng:</strong></p>
                <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                    <li>Vui lòng mang theo giấy tờ tùy thân khi nhận phòng</li>
                    <li>Thời gian nhận phòng: từ 14:00 ngày {formattedCheckIn}</li>
                    <li>Thời gian trả phòng: trước 12:00 ngày {formattedCheckOut}</li>
                    <li>Nếu muốn hủy đặt phòng, vui lòng thực hiện trước ít nhất 2 ngày so với ngày nhận phòng</li>
                </ul>
            </div>
            
            <p>Chúng tôi rất mong được phục vụ bạn. Chúc bạn có một kỳ nghỉ vui vẻ!</p>
            
            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email hoặc hotline.</p>
            
            <p>Trân trọng,<br>
            <strong>Đội ngũ Condotel Management</strong></p>
            
            <div class='footer'>
                <p>Email này được gửi tự động, vui lòng không trả lời email này.</p>
                <p>© 2025 Condotel Management. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            var body = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendNewBookingNotificationToHostAsync(string toEmail, string hostName, int bookingId, string condotelName, string customerName, DateOnly checkInDate, DateOnly checkOutDate, decimal totalAmount, DateTime confirmedAt)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = $"🏠 Bạn có booking mới #{bookingId} - {condotelName}";

            // Format dữ liệu
            var formattedAmount = totalAmount.ToString("N0").Replace(",", ".") + " VNĐ";
            var formattedConfirmDate = confirmedAt.ToString("dd/MM/yyyy HH:mm");
            var formattedCheckIn = checkInDate.ToString("dd/MM/yyyy");
            var formattedCheckOut = checkOutDate.ToString("dd/MM/yyyy");
            
            // Tính số đêm
            var nights = checkOutDate.DayNumber - checkInDate.DayNumber;

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .notification-icon {{ font-size: 48px; margin-bottom: 20px; }}
        .content {{ background: #f8f9fa; padding: 30px; }}
        .info-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .amount {{ font-size: 24px; font-weight: bold; color: #f5576c; margin: 10px 0; }}
        table {{ width: 100%; border-collapse: collapse; }}
        .highlight-box {{ background: #fff3e0; padding: 15px; border-left: 4px solid #ff9800; border-radius: 4px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='notification-icon'>🏠</div>
            <h1>Bạn có booking mới!</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>{hostName}</strong>,</p>
            
            <p>Chúc mừng! Condotel <strong>{condotelName}</strong> của bạn vừa nhận được một booking mới từ khách hàng <strong>{customerName}</strong>.</p>
            
            <div class='info-box'>
                <h3 style='margin-top: 0; color: #f5576c;'>Thông tin booking</h3>
                <table>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Mã booking:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>#{bookingId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Tên condotel:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{condotelName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Tên khách hàng:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{customerName}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Ngày check-in:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{formattedCheckIn}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Ngày check-out:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{formattedCheckOut}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Số đêm:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'>{nights} đêm</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Tổng tiền:</strong></td>
                        <td style='padding: 10px; border-bottom: 1px solid #eee;'><span class='amount'>{formattedAmount}</span></td>
                    </tr>
                    <tr>
                        <td style='padding: 10px;'><strong>Thời gian đặt:</strong></td>
                        <td style='padding: 10px;'>{formattedConfirmDate}</td>
                    </tr>
                </table>
            </div>

            <div class='highlight-box'>
                <p style='margin: 0;'><strong>📋 Công việc cần làm:</strong></p>
                <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                    <li>Chuẩn bị phòng trước ngày khách check-in</li>
                    <li>Kiểm tra trang thiết bị và vệ sinh phòng</li>
                    <li>Đảm bảo mọi tiện nghi hoạt động tốt</li>
                    <li>Liên hệ với khách nếu cần thông tin bổ sung</li>
                </ul>
            </div>
            
            <p>Thanh toán sẽ được chuyển vào tài khoản của bạn sau khi khách hoàn tất kỳ nghỉ và không có vấn đề gì phát sinh.</p>
            
            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email hoặc hotline.</p>
            
            <p>Trân trọng,<br>
            <strong>Đội ngũ Condotel Management</strong></p>
            
            <div class='footer'>
                <p>Email này được gửi tự động, vui lòng không trả lời email này.</p>
                <p>© 2025 Condotel Management. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            var body = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            email.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpServer"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}

