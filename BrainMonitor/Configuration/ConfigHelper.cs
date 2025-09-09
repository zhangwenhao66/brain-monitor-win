using System;
using System.IO;
using System.Reflection;

namespace BrainMirror.Configuration
{
    /// <summary>
    /// 配置辅助类，支持多种配置方式
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// 获取配置值，优先级：环境变量 > 配置文件 > 默认值
        /// </summary>
        public static string GetConfigValue(string key, string defaultValue = "")
        {
            // 1. 首先检查环境变量
            var envValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(envValue))
            {
                return envValue;
            }

            // 2. 检查配置文件
            try
            {
                var config = AppConfig.Instance;
                switch (key.ToUpper())
                {
                    case "ISDEVELOPMENT":
                        return config.IsDevelopment().ToString().ToLower();
                    case "APIBASEURL":
                        return config.AppSettings.ApiBaseUrl;
                    case "DEVELOPMENTAPIBASEURL":
                        return config.AppSettings.DevelopmentApiBaseUrl;
                }
            }
            catch
            {
                // 配置文件读取失败，使用默认值
            }

            // 3. 返回默认值
            return defaultValue;
        }

        /// <summary>
        /// 检查是否为开发模式
        /// 默认返回false（生产模式），只有在明确配置为开发模式时才返回true
        /// 支持以下方式设置：
        /// 1. 环境变量：ISDEVELOPMENT=true/false
        /// 2. 配置文件：appsettings.json中的IsDevelopment
        /// 3. 编译时定义：DEBUG模式默认为开发模式（仅在开发环境中）
        /// </summary>
        public static bool IsDevelopmentMode()
        {
            // 1. 首先检查环境变量
            var envValue = Environment.GetEnvironmentVariable("ISDEVELOPMENT");
            if (!string.IsNullOrEmpty(envValue))
            {
                return envValue.ToLower() == "true";
            }

            // 2. 检查配置文件
            try
            {
                return AppConfig.Instance.IsDevelopment();
            }
            catch
            {
                // 配置文件读取失败，使用编译时定义（仅在开发环境中）
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 获取API基础URL
        /// 默认使用生产环境地址，只有在明确配置为开发模式时才使用开发环境地址
        /// </summary>
        public static string GetApiBaseUrl()
        {
            // 检查是否有明确的环境变量配置
            var envIsDev = Environment.GetEnvironmentVariable("ISDEVELOPMENT");
            if (!string.IsNullOrEmpty(envIsDev))
            {
                // 有环境变量配置，按环境变量决定
                if (envIsDev.ToLower() == "true")
                {
                    return GetConfigValue("DEVELOPMENTAPIBASEURL", "http://localhost:3000/api");
                }
                else
                {
                    return GetConfigValue("APIBASEURL", "https://bm.miyinbot.com/api");
                }
            }

            // 没有环境变量，检查配置文件
            try
            {
                var config = AppConfig.Instance;
                if (config.IsDevelopment())
                {
                    return config.AppSettings.DevelopmentApiBaseUrl;
                }
                else
                {
                    return config.AppSettings.ApiBaseUrl;
                }
            }
            catch
            {
                // 配置文件读取失败，默认使用生产环境
                return "https://bm.miyinbot.com/api";
            }
        }
    }
}
