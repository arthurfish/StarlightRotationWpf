using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StarlightRotationWpf
{
    public class AppSettings
    {
        public double HorizentalBias { get; set; } = 0;
        public double VerticalBias { get; set; } = 0;


        public void SaveSettings(string filePath)
        {
            try
            {
                // 将对象序列化为 JSON 字符串
                string jsonString = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

                // 将 JSON 字符串写入文件
                File.WriteAllText(filePath, jsonString);
                Console.WriteLine("设置已成功保存。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存设置时出错: {ex.Message}");
            }
        }
        public void LoadSettings(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // 从文件中读取所有文本
                    string jsonString = File.ReadAllText(filePath);

                    // 将 JSON 字符串反序列化为对象
                    AppSettings settings = JsonSerializer.Deserialize<AppSettings>(jsonString);
                    Trace.WriteLine("设置已成功加载。");
                    this.HorizentalBias = settings.HorizentalBias;
                    this.VerticalBias = settings.VerticalBias;
                }
                else
                {
                    Trace.WriteLine("设置文件不存在，将返回默认设置。");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"加载设置时出错: {ex.Message}");
            }
        }
    }
}
