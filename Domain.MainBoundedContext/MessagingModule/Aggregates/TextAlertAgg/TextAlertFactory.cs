using Domain.MainBoundedContext.ValueObjects;
using Infrastructure.Crosscutting.Framework.Utils;
using System;

namespace Domain.MainBoundedContext.MessagingModule.Aggregates.TextAlertAgg
{
    public static class TextAlertFactory
    {
        public static TextAlert CreateTextAlert(TextMessage textMessage, ServiceHeader serviceHeader)
        {
            var textAlert = new TextAlert();

            textAlert.GenerateNewIdentity();

            textAlert.TextMessage = textMessage;

            textAlert.CreatedBy = serviceHeader.ApplicationUserName;

            textAlert.CreatedDate = DateTime.Now;
            
            return textAlert;
        }
    }
}
