namespace CustomerManagementMessages
{
    using System;
    using NServiceBus;

    /// <summary>
    /// Communicates the new customer from CRM inside our NServiceBus 
    /// </summary>
    public class NewCustomerReceived : IEvent
    {
        public Guid ContactId { get; set; }
        public Guid CreatedById { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public DateTime CreateDate { get; set; }
    }
}