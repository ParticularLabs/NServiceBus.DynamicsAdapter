namespace FraudManagementEndpoint
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using CustomerManagementMessages;
    using FraudManagementMessages;

    public class NewCustomerHandler : IHandleMessages<NewCustomerReceived>
    {
        public async Task Handle(NewCustomerReceived message, IMessageHandlerContext context)
        {
            //In a full solution this endpoint Would create a command to send to a Fraud API Gateway.  
            //In this simulation we'll simply decide pass or failure based on the state the contact lives in. 
            FraudReviewResult fraudResult;
            Console.WriteLine($"New customer received is {message.LastName} created by {message.CreatedById}");

            if (message.FirstName.Contains("y"))
            {
                fraudResult = new FraudReviewResult { Success=false, ContactId=message.ContactId,ResponseDescription = "New contact has failed fraud review", ReferenceId = new Guid().ToString() };
                Console.WriteLine($"Failed Fraud Review of {message.FirstName} {message.LastName}.");
            }
            else
            {
                fraudResult = new FraudReviewResult { Success=true, ContactId = message.ContactId, ResponseDescription = "New customer has passed fraud review", ReferenceId = new Guid().ToString() };
                Console.WriteLine($"Successful Fraud Review of {message.FirstName} {message.LastName}.");
            }

            //Communicate to the other services the outcome of our work... 
            await context.Publish(fraudResult);
        }
    }
}
