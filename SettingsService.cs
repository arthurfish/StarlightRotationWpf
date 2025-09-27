// SettingsService.cs
using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace StarlightRotationWpf
{
    public class SettingsService
    {
        private const string SettingsFilePath = "settings.json";

        public AppSettings LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch (System.Exception ex)
                {
                    Trace.WriteLine($"Error loading settings: {ex.Message}");
                    // 如果加载失败，返回默认设置
                    return new AppSettings();
                }
            }
            // 如果文件不存在，返回默认设置
            return new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFilePath, json);
                Trace.WriteLine("Settings saved successfully.");
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}