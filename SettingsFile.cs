
using GiteaSyncTool.Encryption;
using NeoSmart.SecureStore;
using Newtonsoft.Json;
using Spectre.Console;

namespace GiteaSyncTool
{
    internal static class SettingsFile
    {

        private const string SETTINGS_FILE = "settings.json";
        public static void GenerateSettings()
        {
            if (!File.Exists("settings.json"))
            {
                AnsiConsole.MarkupLine($"[red]{SETTINGS_FILE} not found. Generating a new one...[/]");
                var settings = new BaseSettings()
                {
                    GiteaSyncSettings = new(),
                    GithubExportSettings = new()
                };
                File.WriteAllText(SETTINGS_FILE, JsonConvert.SerializeObject(settings, Formatting.Indented));
                AnsiConsole.MarkupLine($"Created [deepskyblue1]{SETTINGS_FILE}[/]");
                AnsiConsole.MarkupLine($"Plese edit [deepskyblue1]{SETTINGS_FILE}[/] and press any key when finished");
                Console.ReadKey(true);
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]{SETTINGS_FILE} file found.[/]");
            }            
            EncryptSettings();
        }

        public static BaseSettings? LoadSettings()
        {
            if (File.Exists(SETTINGS_FILE))
            {
                AnsiConsole.MarkupLine($"[green]{SETTINGS_FILE} file found. Encrypting...[/]");
                var json = File.ReadAllText(SETTINGS_FILE);
                var settings = JsonConvertExtensions.DeserializeObjectDecrypt<BaseSettings>(json);
                return settings;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]{SETTINGS_FILE} not found. Please generate it first.[/]");
                return null;
            }
        }



        private static void EncryptSettings()
        {
            if (File.Exists(SETTINGS_FILE))
            {
                var settingsJsonInsecure = File.ReadAllText(SETTINGS_FILE);
                var settings = JsonConvert.DeserializeObject<BaseSettings>(settingsJsonInsecure);
                //now have to "swap" it back
                var encryptedJson = JsonConvertExtensions.SerializeObjectEncrypt(settings);
                var encryptedSettings = JsonConvert.DeserializeObject<BaseSettings>(encryptedJson);
                //write back the encrypted file
                File.WriteAllText(SETTINGS_FILE, JsonConvert.SerializeObject(encryptedSettings, Formatting.Indented));
                AnsiConsole.MarkupLine($"Encrypted [green]{SETTINGS_FILE} file.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]{SETTINGS_FILE} not found. Please generate it first.[/]");
                return;
            }
            
        }
        
    }
}
