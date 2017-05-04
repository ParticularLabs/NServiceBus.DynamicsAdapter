using System;
using NServiceBus;
using Microsoft.Xrm.Sdk;

namespace CRMMapping.Messages
{
    public class ContactCreate : IMessage
    {
        public Guid ContactId { get; set; }

        public Guid CreatedById { get; set; }

        public DateTime CreateDate { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName { get; set; }

        public string Address { get; set; }

        public string Email { get; set; }


        public ContactCreate(RemoteExecutionContext context)
        {

            ContactId = context.PrimaryEntityId;
            CreatedById = context.InitiatingUserId;
            CreateDate = context.OperationCreatedOn;

            //We can cast the 'Target' to a late bound CRM Entity type to parse it a bit easier.
            Entity entity = (Entity) context.InputParameters["Target"];

            FullName = entity.GetCrmValue("fullname");
            FirstName = entity.GetCrmValue("firstname");
            LastName = entity.GetCrmValue("lastname");
            Address = entity.GetCrmValue("address1_composite");
            Email = entity.GetCrmValue("emailaddress1");
        }
    }
}
