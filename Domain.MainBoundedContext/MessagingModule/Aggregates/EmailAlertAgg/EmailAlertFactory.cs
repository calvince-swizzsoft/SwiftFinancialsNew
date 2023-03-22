using Domain.MainBoundedContext.ValueObjects;
using System;

namespace Domain.MainBoundedContext.MessagingModule.Aggregates.EmailAlertAgg
{
    public static class EmailAlertFactory
    {
        public static EmailAlert CreateEmailAlert(MailMessage mailMessage)
        {
            var emailAlert = new EmailAlert();

            emailAlert.GenerateNewIdentity();

            emailAlert.MailMessage = mailMessage;

            emailAlert.CreatedDate = DateTime.Now;

            return emailAlert;
        }
    }
}
