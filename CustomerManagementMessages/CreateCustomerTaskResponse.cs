namespace CustomerManagementMessages
{
    using System;
    using NServiceBus;

    public class CreateCustomerTaskResponse : IMessage
    {
        public Guid ContactId { get; set; }
        public Guid TaskId { get; set; }

    }
}