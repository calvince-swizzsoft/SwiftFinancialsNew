using Domain.MainBoundedContext.AdministrationModule.Aggregates.CompanyAgg;
using Domain.MainBoundedContext.ValueObjects;
using Domain.Seedwork;
using System;

namespace Domain.MainBoundedContext.MessagingModule.Aggregates.TextAlertAgg
{
    public class TextAlert : Entity
    {
        public Guid? CompanyId { get; set; }

        public virtual Company Company { get; private set; }

        public virtual TextMessage TextMessage { get; set; }
    }
}
