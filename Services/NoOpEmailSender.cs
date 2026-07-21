using Microsoft.AspNetCore.Identity.UI.Services;

namespace FamilyHub.Services;

public class FamilyHubNoOpEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return Task.CompletedTask;
    }
}
