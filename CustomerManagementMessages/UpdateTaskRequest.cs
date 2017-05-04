namespace CustomerManagementMessages
{
    using System;
    using NServiceBus;

    public class UpdateTaskRequest : ICommand
    {
        public Guid TaskId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public bool MarkComplete { get; set; }
        public Guid AssignedToUserId { get; set; }

    }
}


