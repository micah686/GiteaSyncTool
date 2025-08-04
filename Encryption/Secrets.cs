using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoSmart.SecureStore;

namespace GiteaSyncTool.Encryption
{
    public static class Secrets
    {
        private const string STORE_FILE = "secrets.json";
        private const string KEY_FILE = "secrets.bin";
        public static void CreateStore()
        {
            if (!File.Exists(STORE_FILE) || !File.Exists(KEY_FILE))
            {
                using var sman = SecretsManager.CreateStore();
                sman.GenerateKey();
                sman.ExportKey(KEY_FILE);
                sman.SaveStore(STORE_FILE);
            }                
        }

        public static void SetSecret(string key, string value)
        {
            if (!File.Exists(STORE_FILE) || !File.Exists(KEY_FILE))
            {
                throw new FileNotFoundException("Store file or key file not found. Please ensure they exist before accessing secrets.");
            }
            using var sman = SecretsManager.LoadStore(STORE_FILE);
            sman.LoadKeyFromFile(KEY_FILE);
            sman.Set(key, value);
            sman.SaveStore(STORE_FILE);
        }

        public static string GetSecret(string Key)
        {
            if(!File.Exists(STORE_FILE) || !File.Exists(KEY_FILE))
            {
                throw new FileNotFoundException("Store file or key file not found. Please ensure they exist before accessing secrets.");
            }
            using var sman = SecretsManager.LoadStore(STORE_FILE);
            sman.LoadKeyFromFile(KEY_FILE);
            bool result = sman.TryGetValue<string>(Key, out var decrypted);
            if(result)
            {
                if (decrypted != null)
                    return decrypted;
                else
                    throw new InvalidOperationException($"The secret '{Key}' was found but its value is null.");
            }
            else
            {
                throw new KeyNotFoundException($"The key '{Key}' was not found in the secrets store.");
            }
        }

        public static void DeleteSecret(string key)
        {
            if (!File.Exists(STORE_FILE) || !File.Exists(KEY_FILE))
            {
                throw new FileNotFoundException("Store file or key file not found. Please ensure they exist before accessing secrets.");
            }
            using var sman = SecretsManager.LoadStore(STORE_FILE);
            sman.LoadKeyFromFile(KEY_FILE);
            bool result = sman.TryGetValue<string>(key, out var _);
            sman.Delete(key);
            sman.SaveStore(STORE_FILE);
        }

        public static string? FindFirstKey(string searchTerm)
        {
            if (!File.Exists(STORE_FILE))
            {
                throw new FileNotFoundException("Store file not found.");
            }
            using var sman = SecretsManager.LoadStore(STORE_FILE);
            try
            {
                if (!sman.Keys.Any())
                {
                    return null; // No keys found
                }
                var key = sman.Keys.FirstOrDefault(k => k.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase));
                return key;
            }
            catch (Exception)
            {
                return null; 
            }
            
        }
    }
}
