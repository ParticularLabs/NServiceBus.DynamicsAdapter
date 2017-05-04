namespace CRMMapping.Messages
{
    using Microsoft.Xrm.Sdk;

    public static class CrMMessageExtension
    {
        /// <summary>Return string value for a CRM entity attribute.</summary>
        /// <param name="entity"></param>
        /// <param name="key">CRM entity attribute</param>
        public static string GetCrmValue(this Entity entity, string key)
        {
            if (entity.Contains(key))
            {
                return entity[key].ToString();
            }

            return string.Empty;
        }
    }
}
