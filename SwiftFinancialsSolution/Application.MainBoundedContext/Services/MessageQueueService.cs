using Infrastructure.Crosscutting.Framework.Utils;
using System;
using System.ComponentModel.Composition;
using System.Messaging;

namespace Application.MainBoundedContext.Services
{
    [Export(typeof(IMessageQueueService))]
    public class MessageQueueService : IMessageQueueService
    {
        public void Send(string queuePath, object data, MessageCategory messageCategory, MessagePriority priority, int timeToBeReceived)
        {
            if (string.IsNullOrWhiteSpace(queuePath))
                throw new ArgumentNullException(nameof(queuePath));

            if (!MessageQueue.Exists(queuePath))
                MessageQueue.Create(queuePath, true);

            using (MessageQueue messageQueue = new MessageQueue(queuePath, QueueAccessMode.Send))
            {
                messageQueue.Formatter = new BinaryMessageFormatter();

                messageQueue.MessageReadPropertyFilter.SetAll();

                using (MessageQueueTransaction mqt = new MessageQueueTransaction())
                {
                    mqt.Begin();

                    using (Message message = new Message(data, new BinaryMessageFormatter()))
                    {
                        message.Label = string.Format("{0}|{1}", EnumHelper.GetDescription(messageCategory), EnumHelper.GetDescription(priority));
                        message.AppSpecific = (int)messageCategory;
                        message.Priority = priority;
                        message.TimeToBeReceived = TimeSpan.FromMinutes(timeToBeReceived);

                        messageQueue.Send(message, mqt);
                    }

                    mqt.Commit();
                }
            }
        }
    }
}
