using System;
using System.IO;
using Newtonsoft.Json;

namespace BrainMirror.Configuration
{
    public class AppConfig
    {
        public AppSettings AppSettings { get; set; } = new AppSettings();
        
        private static AppConfig? _instance;
        private static readonly object _lock = new object();
        
        public static AppConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = LoadConfig();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private static AppConfig LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<AppConfig>(json);
                    return config ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                // 如果配置文件读取失败，使用默认配置
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
            }
            
            return new AppConfig();
        }
        
        /// <summary>
        /// 获取当前API基础URL
        /// </summary>
        public string GetApiBaseUrl()
        {
            return AppSettings.IsDevelopment 
                ? AppSettings.DevelopmentApiBaseUrl 
                : AppSettings.ApiBaseUrl;
        }
        
        /// <summary>
        /// 检查是否为开发模式
        /// </summary>
        public bool IsDevelopment()
        {
            return AppSettings.IsDevelopment;
        }
    }
    
    public class AppSettings
    {
        public bool IsDevelopment { get; set; } = false;
        public string ApiBaseUrl { get; set; } = "https://bm.miyinbot.com/api";
        public string DevelopmentApiBaseUrl { get; set; } = "http://localhost:3000/api";
    }
}
