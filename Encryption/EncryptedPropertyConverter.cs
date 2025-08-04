using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GiteaSyncTool.Encryption
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EncryptAttribute : Attribute { }

    // Custom JSON converter for encrypting/decrypting properties
    public class EncryptedPropertyConverter : JsonConverter
    {
        private readonly string _propertyKeyPrefix;

        public EncryptedPropertyConverter(string propertyKeyPrefix)
        {
            _propertyKeyPrefix = propertyKeyPrefix ?? throw new ArgumentNullException(nameof(propertyKeyPrefix));
            Secrets.CreateStore();
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(string);

        //regenerates each secret each time. See about cleaning it up
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var stringValue = value.ToString();
            if (string.IsNullOrEmpty(stringValue))
            {
                writer.WriteValue(stringValue);
                return;
            }

            //don't re-encrypt encrypted values
            var prefixForEncryptedValue = $"{_propertyKeyPrefix}_ENC";
            if(stringValue.StartsWith(prefixForEncryptedValue))
            {
                writer.WriteValue(stringValue);
                return;
            }

            var previousKey = Secrets.FindFirstKey(_propertyKeyPrefix);
            if (previousKey != null)
            {
                Secrets.DeleteSecret(previousKey);
            }
            
            // Generate a unique key for this property instance
            var secretKey = $"{prefixForEncryptedValue}_{Guid.NewGuid():N}";
            // Store the encrypted value in the SecretsManager
            Secrets.SetSecret(secretKey, stringValue);
            // Write the secret key to JSON (not the encrypted value)
            writer.WriteValue(secretKey);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var secretKey = reader.Value as string;
            if (string.IsNullOrEmpty(secretKey))
            {
                return secretKey;
            }

            try
            {
                return Secrets.GetSecret(secretKey);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    // Custom contract resolver to apply encryption converter to [Encrypt] properties
    public class EncryptedContractResolver : DefaultContractResolver
    {

        public EncryptedContractResolver()
        {
            Secrets.CreateStore();
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);

            if (prop.PropertyType == typeof(string) && member.GetCustomAttribute<EncryptAttribute>() != null)
            {
                // Use property name as prefix to ensure unique secret keys
                prop.Converter = new EncryptedPropertyConverter(member.Name);
            }

            return prop;
        }
    }


}
