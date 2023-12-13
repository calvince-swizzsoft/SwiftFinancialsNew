using Domain.MainBoundedContext.AdministrationModule.Aggregates.CompanyAgg;
using Domain.MainBoundedContext.ValueObjects;
using Domain.Seedwork;
using System;

namespace Domain.MainBoundedContext.MessagingModule.Aggregates.EmailAlertAgg
{
    public class EmailAlert : Entity
    {
        public Guid? CompanyId { get; set; }

        public virtual Company Company { get; private set; }

        public virtual MailMessage MailMessage { get; set; }
    }
}
