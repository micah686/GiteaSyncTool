using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoSmart.SecureStore;
using Newtonsoft.Json;

namespace GiteaSyncTool.Encryption
{
    public static class JsonConvertExtensions
    {        
        public static string SerializeObjectEncrypt(object value)
        {           
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new EncryptedContractResolver(),
                Formatting = Formatting.Indented
            };
            return JsonConvert.SerializeObject(value, settings);
        }

        public static T DeserializeObjectDecrypt<T>(string json)
        {            
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new EncryptedContractResolver(),
                Formatting = Formatting.Indented
            };

            return JsonConvert.DeserializeObject<T>(json, settings);
        }
    }
}
