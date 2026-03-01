using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceApp.Infrastructure.Services
{
    public class EmailSettings
    {
  
    public string? SmtpServer { get; set; }
    public int Port { get; set; }
    public bool EnableSsl { get; set; } = true;
    public int TimeoutMs { get; set; } = 10000;
    public string? SenderName { get; set; }
    public string? SenderEmail { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }

    }
}