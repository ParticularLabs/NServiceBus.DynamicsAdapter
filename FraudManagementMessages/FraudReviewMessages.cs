namespace FraudManagementMessages
{
    using System;
    using NServiceBus;

    public class FraudReviewResult:IEvent
    {
        public Guid ContactId { get; set; }
        public Boolean Success { get; set; }
        public string ResponseDescription { get; set; }
        public string ReferenceId { get; set; }
    }
    
}
