namespace CustomerManagementMessages
{
    using System;
    using NServiceBus;

    public class CreateCustomerTaskRequest : ICommand
    {
        public Guid ContactId { get; set; }
        public Guid CreatedById { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public DateTimeOffset Deadline { get; set; }
    }
}