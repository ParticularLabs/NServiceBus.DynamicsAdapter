namespace CustomerManagermentEndpoint
{
    using System;
    using CustomerManagementMessages;
    using FraudManagementMessages;
    using NServiceBus;

    public class CustomerManagementSagaData : ContainSagaData
    {
        //The customer ID is the correlation between the new customer event and the fraud event.
        public Guid ContactId { get; set; }
        public NewCustomerReceived NewCustomerEvent { get; set; }
        public FraudReviewResult ReviewResult { get; set; }
        public CreateCustomerTaskResponse TaskCreatedResponse { get; set; }
        public UpdateTaskResponse TaskUpdatedResponse { get; set; }
    }
}