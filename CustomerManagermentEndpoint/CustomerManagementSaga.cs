namespace CustomerManagermentEndpoint
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using CustomerManagementMessages;
    using FraudManagementMessages;

    public class CustomerManagementSaga : Saga<CustomerManagementSagaData>, IAmStartedByMessages<NewCustomerReceived>, IAmStartedByMessages<FraudReviewResult>, IHandleMessages<CreateCustomerTaskResponse>, IHandleMessages<UpdateTaskResponse>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CustomerManagementSagaData> mapper)
        {
            mapper.ConfigureMapping<NewCustomerReceived>(m => m.ContactId).ToSaga(s => s.ContactId);
            mapper.ConfigureMapping<FraudReviewResult>(m => m.ContactId).ToSaga(s => s.ContactId);
            mapper.ConfigureMapping<CreateCustomerTaskResponse>(m => m.ContactId).ToSaga(s => s.ContactId);
        }

        public Task Handle(FraudReviewResult message, IMessageHandlerContext context)
        {
            Data.ReviewResult = message;
            return CheckForTaskUpdate(context);
        }

        public Task Handle(NewCustomerReceived message, IMessageHandlerContext context)
        {
            //Save the entire message.  We'll need it.
            Data.NewCustomerEvent = message;
            Console.WriteLine($"New Customer {message.ContactId} Received. Lastname is {message.LastName}.  Creating a Task due in an hour");
            var newTaskRequest = new CreateCustomerTaskRequest
            {
                ContactId = Data.ContactId,
                Description = "Customer Fraud Review",
                Subject = "Fraud Review Task",
                Deadline = DateTimeOffset.Now.AddHours(8),
                CreatedById =Data.NewCustomerEvent.CreatedById
            };

            return context.Send(newTaskRequest);
        }

        public Task Handle(CreateCustomerTaskResponse message, IMessageHandlerContext context)
        {
            Data.TaskCreatedResponse = message;
            Console.WriteLine($"Task response received for {message.ContactId}");
            return CheckForTaskUpdate(context);
        }

        public Task Handle(UpdateTaskResponse message, IMessageHandlerContext context)
        {
            Data.TaskUpdatedResponse = message;
            MarkAsComplete();
            return Task.CompletedTask;
        }

        private Task CheckForTaskUpdate(IMessageHandlerContext context)
        {
            //If we have a response from the Task, we can update it. Can't be certain that the response is back before the fraud event arrives.
            if (Data.TaskCreatedResponse != null && Data.ReviewResult != null)
            {
                UpdateTaskRequest updateTask = new UpdateTaskRequest();
                updateTask.TaskId = Data.TaskCreatedResponse.TaskId;
                updateTask.Subject = "Fraud Review";
                
                Console.WriteLine($"Updating the task {updateTask.TaskId}. Mark Complete will be {Data.ReviewResult.Success}");

                var fraudDetail = Data.ReviewResult.ResponseDescription;

                if (Data.ReviewResult.Success)
                {
                    updateTask.Description = $"Automated Fraud Review Successful. Detail:{fraudDetail}";
                    updateTask.MarkComplete = true;
                }
                else
                {
                    updateTask.Description = $"Automated Fraud Review Failure.  It's up to you now. Detail:{fraudDetail}";
                    updateTask.MarkComplete = false;
                    updateTask.AssignedToUserId = Data.NewCustomerEvent.CreatedById;
                }

                return context.Send(updateTask);
            }

            return Task.CompletedTask;
        }
    }
}
