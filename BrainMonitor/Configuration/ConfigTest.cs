using System;
using System.Diagnostics;

namespace BrainMirror.Configuration
{
    /// <summary>
    /// 配置测试类，用于验证配置逻辑
    /// </summary>
    public static class ConfigTest
    {
        /// <summary>
        /// 测试配置逻辑
        /// </summary>
        public static void TestConfig()
        {
            Debug.WriteLine("=== 配置测试开始 ===");
            
            // 测试1：默认配置（应该是生产模式）
            Debug.WriteLine($"1. 默认模式: {ConfigHelper.IsDevelopmentMode()}");
            Debug.WriteLine($"   默认API地址: {ConfigHelper.GetApiBaseUrl()}");
            
            // 测试2：环境变量测试
            var originalEnv = Environment.GetEnvironmentVariable("ISDEVELOPMENT");
            
            // 设置为开发模式
            Environment.SetEnvironmentVariable("ISDEVELOPMENT", "true");
            Debug.WriteLine($"2. 环境变量设置为开发模式:");
            Debug.WriteLine($"   开发模式: {ConfigHelper.IsDevelopmentMode()}");
            Debug.WriteLine($"   API地址: {ConfigHelper.GetApiBaseUrl()}");
            
            // 设置为生产模式
            Environment.SetEnvironmentVariable("ISDEVELOPMENT", "false");
            Debug.WriteLine($"3. 环境变量设置为生产模式:");
            Debug.WriteLine($"   开发模式: {ConfigHelper.IsDevelopmentMode()}");
            Debug.WriteLine($"   API地址: {ConfigHelper.GetApiBaseUrl()}");
            
            // 恢复原始环境变量
            if (originalEnv != null)
            {
                Environment.SetEnvironmentVariable("ISDEVELOPMENT", originalEnv);
            }
            else
            {
                Environment.SetEnvironmentVariable("ISDEVELOPMENT", null);
            }
            
            Debug.WriteLine("=== 配置测试结束 ===");
        }
    }
}
