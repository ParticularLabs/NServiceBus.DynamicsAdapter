namespace CustomerManagementMessages
{
    using System;
    using NServiceBus;

    public class UpdateTaskResponse : IMessage
    {
        public Guid TaskId { get; set; }
        public bool Success { get; set; }
    }
}


