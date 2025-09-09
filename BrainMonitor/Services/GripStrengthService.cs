using System;
using System.Collections.Generic;

namespace BrainMirror.Services
{
    /// <summary>
    /// 握力服务类，用于处理握力值到握力百分比的转换
    /// </summary>
    public class GripStrengthService
    {
        /// <summary>
        /// 握力对照表数据结构
        /// </summary>
        public class GripStrengthReference
        {
            public int MinAge { get; set; }
            public int MaxAge { get; set; }
            public string Gender { get; set; }
            public Dictionary<string, double> PercentileRanges { get; set; }

            public GripStrengthReference()
            {
                PercentileRanges = new Dictionary<string, double>();
            }
        }

        private static readonly List<GripStrengthReference> FemaleGripStrengthTable = new List<GripStrengthReference>
        {
            new GripStrengthReference { MinAge = 20, MaxAge = 24, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 17.3}, {"30%", 18.3}, {"50%", 21.1}, {"55%", 22.9}, {"60%", 24.3}, {"65%", 25.6}, {"70%", 26.9}, {"75%", 28.3}, {"80%", 29.9}, {"85%", 32.0}, {"90%", 33.4}, {"95%", 35.7}, {"100%", 35.8} }},
            new GripStrengthReference { MinAge = 25, MaxAge = 29, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 17.3}, {"30%", 18.3}, {"50%", 21.2}, {"55%", 22.9}, {"60%", 24.3}, {"65%", 25.6}, {"70%", 26.9}, {"75%", 28.2}, {"80%", 29.8}, {"85%", 31.9}, {"90%", 33.3}, {"95%", 35.5}, {"100%", 35.6} }},
            new GripStrengthReference { MinAge = 30, MaxAge = 34, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 17.5}, {"30%", 18.6}, {"50%", 21.5}, {"55%", 23.3}, {"60%", 24.7}, {"65%", 26.0}, {"70%", 27.3}, {"75%", 28.6}, {"80%", 30.2}, {"85%", 32.2}, {"90%", 33.7}, {"95%", 35.9}, {"100%", 36.0} }},
            new GripStrengthReference { MinAge = 35, MaxAge = 39, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 17.6}, {"30%", 18.6}, {"50%", 21.7}, {"55%", 23.5}, {"60%", 24.9}, {"65%", 26.2}, {"70%", 27.5}, {"75%", 28.8}, {"80%", 30.4}, {"85%", 32.4}, {"90%", 33.8}, {"95%", 35.9}, {"100%", 36.0} }},
            new GripStrengthReference { MinAge = 40, MaxAge = 44, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 17.6}, {"30%", 18.7}, {"50%", 21.8}, {"55%", 23.7}, {"60%", 25.1}, {"65%", 26.4}, {"70%", 27.7}, {"75%", 29.0}, {"80%", 30.5}, {"85%", 32.5}, {"90%", 33.9}, {"95%", 36.1}, {"100%", 36.2} }},
            new GripStrengthReference { MinAge = 45, MaxAge = 49, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 17.4}, {"30%", 18.5}, {"50%", 21.5}, {"55%", 23.3}, {"60%", 24.7}, {"65%", 25.6}, {"70%", 26.9}, {"75%", 28.6}, {"80%", 30.1}, {"85%", 32.1}, {"90%", 33.5}, {"95%", 35.7}, {"100%", 35.8} }},
            new GripStrengthReference { MinAge = 50, MaxAge = 54, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 16.8}, {"30%", 17.8}, {"50%", 20.7}, {"55%", 22.4}, {"60%", 23.8}, {"65%", 25.1}, {"70%", 26.3}, {"75%", 27.6}, {"80%", 29.1}, {"85%", 31.1}, {"90%", 32.5}, {"95%", 34.8}, {"100%", 34.9} }},
            new GripStrengthReference { MinAge = 55, MaxAge = 59, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 16.0}, {"30%", 17.1}, {"50%", 20.0}, {"55%", 21.8}, {"60%", 23.2}, {"65%", 24.4}, {"70%", 25.6}, {"75%", 26.9}, {"80%", 28.4}, {"85%", 30.5}, {"90%", 31.9}, {"95%", 34.1}, {"100%", 34.2} }},
            new GripStrengthReference { MinAge = 60, MaxAge = 64, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 14.5}, {"30%", 15.5}, {"50%", 18.5}, {"55%", 20.3}, {"60%", 21.7}, {"65%", 22.9}, {"70%", 24.0}, {"75%", 25.3}, {"80%", 26.7}, {"85%", 28.6}, {"90%", 30.0}, {"95%", 32.1}, {"100%", 32.2} }},
            new GripStrengthReference { MinAge = 65, MaxAge = 69, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 13.4}, {"30%", 14.5}, {"50%", 17.6}, {"55%", 19.4}, {"60%", 20.8}, {"65%", 22.0}, {"70%", 23.2}, {"75%", 24.4}, {"80%", 25.9}, {"85%", 27.8}, {"90%", 29.2}, {"95%", 31.3}, {"100%", 31.4} }},
            new GripStrengthReference { MinAge = 70, MaxAge = 74, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 12.2}, {"30%", 13.3}, {"50%", 16.3}, {"55%", 18.1}, {"60%", 19.5}, {"65%", 20.7}, {"70%", 21.9}, {"75%", 23.2}, {"80%", 24.6}, {"85%", 26.6}, {"90%", 28.0}, {"95%", 30.3}, {"100%", 30.4} }},
            new GripStrengthReference { MinAge = 75, MaxAge = 999, Gender = "女", PercentileRanges = new Dictionary<string, double> { {"10%", 11.5}, {"30%", 12.5}, {"50%", 15.6}, {"55%", 17.4}, {"60%", 18.8}, {"65%", 20.0}, {"70%", 21.2}, {"75%", 22.5}, {"80%", 24.1}, {"85%", 26.2}, {"90%", 27.7}, {"95%", 30.2}, {"100%", 30.3} }}
        };

        private static readonly List<GripStrengthReference> MaleGripStrengthTable = new List<GripStrengthReference>
        {
            new GripStrengthReference { MinAge = 20, MaxAge = 24, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 29.0}, {"30%", 30.7}, {"50%", 35.5}, {"55%", 38.3}, {"60%", 40.4}, {"65%", 42.4}, {"70%", 44.2}, {"75%", 46.2}, {"80%", 48.4}, {"85%", 51.4}, {"90%", 53.5}, {"95%", 56.6}, {"100%", 56.7} }},
            new GripStrengthReference { MinAge = 25, MaxAge = 29, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 29.6}, {"30%", 31.4}, {"50%", 36.2}, {"55%", 39.1}, {"60%", 41.3}, {"65%", 43.2}, {"70%", 45.1}, {"75%", 47.1}, {"80%", 49.4}, {"85%", 52.4}, {"90%", 54.4}, {"95%", 57.6}, {"100%", 57.7} }},
            new GripStrengthReference { MinAge = 30, MaxAge = 34, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 29.9}, {"30%", 31.7}, {"50%", 36.5}, {"55%", 39.3}, {"60%", 41.5}, {"65%", 43.5}, {"70%", 45.4}, {"75%", 47.3}, {"80%", 49.6}, {"85%", 52.5}, {"90%", 54.6}, {"95%", 57.7}, {"100%", 57.8} }},
            new GripStrengthReference { MinAge = 35, MaxAge = 39, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 29.6}, {"30%", 31.4}, {"50%", 36.2}, {"55%", 38.9}, {"60%", 41.1}, {"65%", 43.1}, {"70%", 44.9}, {"75%", 46.9}, {"80%", 49.1}, {"85%", 51.9}, {"90%", 53.9}, {"95%", 56.9}, {"100%", 57.0} }},
            new GripStrengthReference { MinAge = 40, MaxAge = 44, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 29.3}, {"30%", 31.1}, {"50%", 35.8}, {"55%", 38.6}, {"60%", 40.8}, {"65%", 42.7}, {"70%", 44.5}, {"75%", 46.5}, {"80%", 48.6}, {"85%", 51.5}, {"90%", 53.4}, {"95%", 56.3}, {"100%", 56.4} }},
            new GripStrengthReference { MinAge = 45, MaxAge = 49, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 28.9}, {"30%", 30.6}, {"50%", 35.3}, {"55%", 38.0}, {"60%", 40.1}, {"65%", 42.0}, {"70%", 43.8}, {"75%", 45.8}, {"80%", 47.9}, {"85%", 50.7}, {"90%", 52.6}, {"95%", 55.5}, {"100%", 55.6} }},
            new GripStrengthReference { MinAge = 50, MaxAge = 54, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 28.1}, {"30%", 29.7}, {"50%", 34.2}, {"55%", 36.9}, {"60%", 39.0}, {"65%", 40.8}, {"70%", 42.6}, {"75%", 44.5}, {"80%", 46.7}, {"85%", 49.5}, {"90%", 51.4}, {"95%", 54.4}, {"100%", 54.5} }},
            new GripStrengthReference { MinAge = 55, MaxAge = 59, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 26.2}, {"30%", 27.8}, {"50%", 32.3}, {"55%", 35.0}, {"60%", 37.1}, {"65%", 39.0}, {"70%", 40.8}, {"75%", 42.7}, {"80%", 44.9}, {"85%", 47.7}, {"90%", 49.6}, {"95%", 52.6}, {"100%", 52.7} }},
            new GripStrengthReference { MinAge = 60, MaxAge = 64, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 22.8}, {"30%", 24.5}, {"50%", 29.1}, {"55%", 31.8}, {"60%", 33.9}, {"65%", 35.8}, {"70%", 37.6}, {"75%", 39.5}, {"80%", 41.6}, {"85%", 44.3}, {"90%", 46.1}, {"95%", 48.9}, {"100%", 49.0} }},
            new GripStrengthReference { MinAge = 65, MaxAge = 69, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 20.8}, {"30%", 22.5}, {"50%", 27.2}, {"55%", 30.0}, {"60%", 32.1}, {"65%", 34.0}, {"70%", 35.9}, {"75%", 37.8}, {"80%", 39.9}, {"85%", 42.7}, {"90%", 44.5}, {"95%", 47.3}, {"100%", 47.4} }},
            new GripStrengthReference { MinAge = 70, MaxAge = 74, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 18.3}, {"30%", 20.0}, {"50%", 24.5}, {"55%", 27.2}, {"60%", 29.3}, {"65%", 31.2}, {"70%", 33.0}, {"75%", 35.0}, {"80%", 37.1}, {"85%", 39.9}, {"90%", 41.8}, {"95%", 44.6}, {"100%", 44.7} }},
            new GripStrengthReference { MinAge = 75, MaxAge = 999, Gender = "男", PercentileRanges = new Dictionary<string, double> { {"10%", 16.0}, {"30%", 17.5}, {"50%", 21.9}, {"55%", 24.6}, {"60%", 26.7}, {"65%", 28.6}, {"70%", 30.5}, {"75%", 32.4}, {"80%", 34.6}, {"85%", 37.5}, {"90%", 39.4}, {"95%", 42.3}, {"100%", 42.4} }}
        };

        /// <summary>
        /// 根据握力值、性别和年龄计算握力百分比
        /// </summary>
        /// <param name="gripStrength">握力值（kg）</param>
        /// <param name="gender">性别（"男"或"女"）</param>
        /// <param name="age">年龄</param>
        /// <returns>握力百分比（0-100）</returns>
        public static double CalculateGripStrengthPercentage(double gripStrength, string gender, int age)
        {
            try
            {
                var referenceTable = gender == "男" ? MaleGripStrengthTable : FemaleGripStrengthTable;
                
                // 查找对应年龄段的参考数据
                var reference = referenceTable.FirstOrDefault(r => age >= r.MinAge && age <= r.MaxAge);
                if (reference == null)
                {
                    // 如果找不到对应年龄段，返回默认值
                    return 50.0; // 默认50%
                }

                var ranges = reference.PercentileRanges;
                
                // 如果握力值小于10%的值，返回10%
                if (gripStrength < ranges["10%"])
                {
                    return 10.0;
                }
                
                // 如果握力值大于等于100%的值，返回100%
                if (gripStrength >= ranges["100%"])
                {
                    return 100.0;
                }

                // 线性插值计算百分比
                var percentiles = new[] { "10%", "30%", "50%", "55%", "60%", "65%", "70%", "75%", "80%", "85%", "90%", "95%", "100%" };
                
                for (int i = 0; i < percentiles.Length - 1; i++)
                {
                    var currentPercentile = percentiles[i];
                    var nextPercentile = percentiles[i + 1];
                    
                    var currentValue = ranges[currentPercentile];
                    var nextValue = ranges[nextPercentile];
                    
                    if (gripStrength >= currentValue && gripStrength < nextValue)
                    {
                        // 线性插值
                        var currentPercent = double.Parse(currentPercentile.Replace("%", ""));
                        var nextPercent = double.Parse(nextPercentile.Replace("%", ""));
                        
                        var ratio = (gripStrength - currentValue) / (nextValue - currentValue);
                        return currentPercent + ratio * (nextPercent - currentPercent);
                    }
                }

                // 如果握力值正好等于某个百分位值
                foreach (var kvp in ranges)
                {
                    if (Math.Abs(gripStrength - kvp.Value) < 0.01) // 考虑浮点数精度
                    {
                        return double.Parse(kvp.Key.Replace("%", ""));
                    }
                }

                return 50.0; // 默认值
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"计算握力百分比时发生异常: {ex.Message}");
                return 50.0; // 异常时返回默认值
            }
        }

        /// <summary>
        /// 计算握力分数（100% - 握力百分比）
        /// </summary>
        /// <param name="gripStrength">握力值（kg）</param>
        /// <param name="gender">性别（"男"或"女"）</param>
        /// <param name="age">年龄</param>
        /// <returns>握力分数（0-100，数值越高风险越大）</returns>
        public static double CalculateGripStrengthScore(double gripStrength, string gender, int age)
        {
            var percentage = CalculateGripStrengthPercentage(gripStrength, gender, age);
            return 100.0 - percentage; // 握力分数 = 100% - 握力百分比
        }
    }
}
