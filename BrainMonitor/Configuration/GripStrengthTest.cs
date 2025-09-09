using System;
using BrainMirror.Services;

namespace BrainMirror.Configuration
{
    /// <summary>
    /// 握力计算测试类
    /// </summary>
    public class GripStrengthTest
    {
        public static void RunTests()
        {
            Console.WriteLine("=== 握力计算测试 ===");
            
            // 测试女性握力计算
            TestFemaleGripStrength();
            
            // 测试男性握力计算
            TestMaleGripStrength();
            
            // 测试边界情况
            TestEdgeCases();
        }
        
        private static void TestFemaleGripStrength()
        {
            Console.WriteLine("\n--- 女性握力测试 ---");
            
            // 测试30岁女性，握力值25kg
            double percentage = GripStrengthService.CalculateGripStrengthPercentage(25.0, "女", 30);
            double score = GripStrengthService.CalculateGripStrengthScore(25.0, "女", 30);
            Console.WriteLine($"30岁女性，握力25kg: 百分比={percentage:F1}%, 分数={score:F1}");
            
            // 测试50岁女性，握力值20kg
            percentage = GripStrengthService.CalculateGripStrengthPercentage(20.0, "女", 50);
            score = GripStrengthService.CalculateGripStrengthScore(20.0, "女", 50);
            Console.WriteLine($"50岁女性，握力20kg: 百分比={percentage:F1}%, 分数={score:F1}");
            
            // 测试70岁女性，握力值15kg
            percentage = GripStrengthService.CalculateGripStrengthPercentage(15.0, "女", 70);
            score = GripStrengthService.CalculateGripStrengthScore(15.0, "女", 70);
            Console.WriteLine($"70岁女性，握力15kg: 百分比={percentage:F1}%, 分数={score:F1}");
        }
        
        private static void TestMaleGripStrength()
        {
            Console.WriteLine("\n--- 男性握力测试 ---");
            
            // 测试30岁男性，握力值40kg
            double percentage = GripStrengthService.CalculateGripStrengthPercentage(40.0, "男", 30);
            double score = GripStrengthService.CalculateGripStrengthScore(40.0, "男", 30);
            Console.WriteLine($"30岁男性，握力40kg: 百分比={percentage:F1}%, 分数={score:F1}");
            
            // 测试50岁男性，握力值35kg
            percentage = GripStrengthService.CalculateGripStrengthPercentage(35.0, "男", 50);
            score = GripStrengthService.CalculateGripStrengthScore(35.0, "男", 50);
            Console.WriteLine($"50岁男性，握力35kg: 百分比={percentage:F1}%, 分数={score:F1}");
            
            // 测试70岁男性，握力值25kg
            percentage = GripStrengthService.CalculateGripStrengthPercentage(25.0, "男", 70);
            score = GripStrengthService.CalculateGripStrengthScore(25.0, "男", 70);
            Console.WriteLine($"70岁男性，握力25kg: 百分比={percentage:F1}%, 分数={score:F1}");
        }
        
        private static void TestEdgeCases()
        {
            Console.WriteLine("\n--- 边界情况测试 ---");
            
            // 测试极小值
            double percentage = GripStrengthService.CalculateGripStrengthPercentage(5.0, "女", 30);
            double score = GripStrengthService.CalculateGripStrengthScore(5.0, "女", 30);
            Console.WriteLine($"30岁女性，握力5kg: 百分比={percentage:F1}%, 分数={score:F1}");
            
            // 测试极大值
            percentage = GripStrengthService.CalculateGripStrengthPercentage(100.0, "男", 30);
            score = GripStrengthService.CalculateGripStrengthScore(100.0, "男", 30);
            Console.WriteLine($"30岁男性，握力100kg: 百分比={percentage:F1}%, 分数={score:F1}");
            
            // 测试边界年龄
            percentage = GripStrengthService.CalculateGripStrengthPercentage(20.0, "女", 75);
            score = GripStrengthService.CalculateGripStrengthScore(20.0, "女", 75);
            Console.WriteLine($"75岁女性，握力20kg: 百分比={percentage:F1}%, 分数={score:F1}");
        }
    }
}
