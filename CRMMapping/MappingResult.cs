namespace CRMMapping
{
    public class MappingResult
    {
        public MappingResult(byte[] serializedMessageBody, string typeHeaderValue)
        {
            SerializedMessageBody = serializedMessageBody;
            TypeHeaderValue = typeHeaderValue;
        }

        public byte[] SerializedMessageBody { get; }
        public string TypeHeaderValue { get; }
    }
}