using Domain.MainBoundedContext.ValueObjects;
using Domain.Seedwork;

namespace Domain.MainBoundedContext.MessagingModule.Aggregates.TextAlertAgg
{
    public class TextAlert : Entity
    {
        public virtual TextMessage TextMessage { get; set; }
    }
}
