using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity.UI.Services;
using FinanceApp.Application.Interfaces;

namespace FinanceApp.Infrastructure.Services
{
    public class IdentityEmailSender : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
    {
        private readonly IEmailService _emailService;

        public IdentityEmailSender(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Simply delegate to your EmailService
            await _emailService.SendEmailAsync(email, subject, htmlMessage);
        }
    }
}
