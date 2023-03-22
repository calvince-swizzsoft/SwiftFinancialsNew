using Domain.MainBoundedContext.ValueObjects;
using Domain.Seedwork;

namespace Domain.MainBoundedContext.MessagingModule.Aggregates.EmailAlertAgg
{
    public class EmailAlert : Entity
    {
        public virtual MailMessage MailMessage { get; set; }
    }
}
