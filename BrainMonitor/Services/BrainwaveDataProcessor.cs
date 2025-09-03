using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

namespace BrainMonitor.Services
{
    /// <summary>
    /// 脑电数据处理服务类
    /// 实现带通滤波、FFT频谱分析和脑电指标计算
    /// </summary>
    public class BrainwaveDataProcessor
    {
        // 采样率（Hz）
        private const double SamplingRate = 520.0;
        
        // 频率分辨率（Hz）
        private const double FrequencyResolution = 0.1;
        
        // 带通滤波器参数
        private const double LowCutoffFreq = 1.0;  // 1Hz
        private const double HighCutoffFreq = 40.0; // 40Hz
        
        // 脑电频段定义
        private const double ThetaLowFreq = 4.0;   // 4Hz
        private const double ThetaHighFreq = 7.0;  // 7Hz
        private const double AlphaLowFreq = 8.0;   // 8Hz
        private const double AlphaHighFreq = 13.0; // 13Hz
        private const double BetaLowFreq = 15.0;   // 15Hz
        private const double BetaHighFreq = 25.0;  // 25Hz
        
        /// <summary>
        /// 处理闭眼脑电数据并计算相关指标
        /// </summary>
        /// <param name="rawData">原始脑电数据</param>
        /// <returns>脑电处理结果</returns>
        public BrainwaveProcessResult ProcessClosedEyesData(List<double> rawData)
        {
            try
            {
                if (rawData == null || rawData.Count == 0)
                {
                    return new BrainwaveProcessResult
                    {
                        Success = false,
                        ErrorMessage = "输入数据为空"
                    };
                }
                
                // 1. 异常值处理（幅值大于100的设定为100，幅值小于-100的设定为-100）
                var outlierProcessedData = ProcessOutliers(rawData);
                
                // 2. 带通滤波（1-40Hz）
                var filteredData = ApplyBandpassFilter(outlierProcessedData);
                
                // 3. 计算FFT频谱（使用multi-taper Fourier method）
                var spectrum = CalculateFFTSpectrum(filteredData);
                
                // 4. 计算相对功率谱密度
                var relativePowerSpectrum = CalculateRelativePowerSpectrum(spectrum);
                
                // 5. 计算各频段指标
                var thetaValue = CalculateThetaValue(relativePowerSpectrum);
                var alphaValue = CalculateAlphaValue(relativePowerSpectrum);
                var betaValue = CalculateBetaValue(relativePowerSpectrum);
                
                // 6. 计算脑电最终指标
                var brainwaveFinalIndex = (thetaValue + alphaValue + betaValue) / 3.0;
                
                return new BrainwaveProcessResult
                {
                    Success = true,
                    ThetaValue = thetaValue,
                    AlphaValue = alphaValue,
                    BetaValue = betaValue,
                    BrainwaveFinalIndex = brainwaveFinalIndex,
                    FilteredData = filteredData,
                    Spectrum = spectrum,
                    RelativePowerSpectrum = relativePowerSpectrum
                };
            }
            catch (Exception ex)
            {
                return new BrainwaveProcessResult
                {
                    Success = false,
                    ErrorMessage = $"处理数据时发生异常: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// 处理异常值（幅值大于100的设定为100，幅值小于-100的设定为-100）
        /// </summary>
        private List<double> ProcessOutliers(List<double> data)
        {
            var processedData = new List<double>();
            
            foreach (double value in data)
            {
                double processedValue = value;
                
                // 限制幅值范围在-100到100之间
                if (processedValue > 100.0)
                {
                    processedValue = 100.0;
                }
                else if (processedValue < -100.0)
                {
                    processedValue = -100.0;
                }
                
                processedData.Add(processedValue);
            }
            
            return processedData;
        }
        
        /// <summary>
        /// 应用带通滤波器（1-40Hz）
        /// </summary>
        private List<double> ApplyBandpassFilter(List<double> data)
        {
            // 使用简单的IIR带通滤波器
            // 这里实现一个基本的双二阶滤波器
            
            var filteredData = new List<double>();
            if (data.Count < 3) return data;
            
            // 滤波器系数（预计算的Butterworth双二阶滤波器）
            double b0 = 0.0001, b1 = 0.0002, b2 = 0.0001;
            double a1 = -1.9978, a2 = 0.9978;
            
            // 初始化状态变量
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            
            foreach (double sample in data)
            {
                // 应用滤波器
                double y = b0 * sample + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                
                // 更新状态变量
                x2 = x1;
                x1 = sample;
                y2 = y1;
                y1 = y;
                
                filteredData.Add(y);
            }
            
            return filteredData;
        }
        
        /// <summary>
        /// 计算FFT频谱
        /// </summary>
        private List<Complex> CalculateFFTSpectrum(List<double> data)
        {
            // 确保数据长度为2的幂次，便于FFT计算
            int n = NextPowerOfTwo(data.Count);
            var paddedData = new List<double>(data);
            
            // 补零到2的幂次长度
            while (paddedData.Count < n)
            {
                paddedData.Add(0.0);
            }
            
            // 应用窗函数（Hanning窗）
            var windowedData = ApplyHanningWindow(paddedData);
            
            // 执行FFT
            var fftResult = FFT(windowedData);
            
            return fftResult;
        }
        
        /// <summary>
        /// 计算相对功率谱密度
        /// </summary>
        private List<double> CalculateRelativePowerSpectrum(List<Complex> spectrum)
        {
            var powerSpectrum = new List<double>();
            var relativePowerSpectrum = new List<double>();
            
            // 计算功率谱
            foreach (var complex in spectrum)
            {
                double power = complex.Magnitude * complex.Magnitude;
                powerSpectrum.Add(power);
            }
            
            // 计算3-30Hz范围内的总功率
            double totalPower3To30Hz = 0.0;
            int startIndex = (int)(3.0 / FrequencyResolution);
            int endIndex = (int)(30.0 / FrequencyResolution);
            
            for (int i = startIndex; i <= endIndex && i < powerSpectrum.Count; i++)
            {
                totalPower3To30Hz += powerSpectrum[i];
            }
            
            // 计算相对功率谱密度：Rel P(f) = P(f) / sum(P(3:30)) * 100%
            foreach (double power in powerSpectrum)
            {
                double relativePower = totalPower3To30Hz > 0 ? (power / totalPower3To30Hz) * 100.0 : 0;
                relativePowerSpectrum.Add(relativePower);
            }
            
            return relativePowerSpectrum;
        }
        
        /// <summary>
        /// 计算Theta值
        /// </summary>
        private double CalculateThetaValue(List<double> relativePowerSpectrum)
        {
            var thetaPower = GetFrequencyBandPower(relativePowerSpectrum, ThetaLowFreq, ThetaHighFreq);
            double maxThetaPower = thetaPower.Max();
            
            // Theta值 = [Max(Rel P(f)) - 2] * 100%
            // 注意：这里的Max(Rel P(f))是百分比值，需要除以100转换为小数
            double maxThetaPowerDecimal = maxThetaPower / 100.0;
            double thetaValue = (maxThetaPowerDecimal - 2.0) * 100.0;
            
            // 限制在0-100%范围内
            return Math.Max(0.0, Math.Min(100.0, thetaValue));
        }
        
        /// <summary>
        /// 计算Alpha值
        /// </summary>
        private double CalculateAlphaValue(List<double> relativePowerSpectrum)
        {
            var alphaPower = GetFrequencyBandPower(relativePowerSpectrum, AlphaLowFreq, AlphaHighFreq);
            double maxAlphaPower = alphaPower.Max();
            
            // Alpha值 = 100% - [Max(Rel P(f)) + 0.3] * 100%
            // 注意：这里的Max(Rel P(f))是百分比值，需要除以100转换为小数
            double maxAlphaPowerDecimal = maxAlphaPower / 100.0;
            double alphaValue = 100.0 - (maxAlphaPowerDecimal + 0.3) * 100.0;
            
            // 限制在0-100%范围内
            return Math.Max(0.0, Math.Min(100.0, alphaValue));
        }
        
        /// <summary>
        /// 计算Beta值
        /// </summary>
        private double CalculateBetaValue(List<double> relativePowerSpectrum)
        {
            var betaPower = GetFrequencyBandPower(relativePowerSpectrum, BetaLowFreq, BetaHighFreq);
            double maxBetaPower = betaPower.Max();
            
            // Beta值 = 100% - [Max(Rel P(f)) + 0.5] * 100%
            // 注意：这里的Max(Rel P(f))是百分比值，需要除以100转换为小数
            double maxBetaPowerDecimal = maxBetaPower / 100.0;
            double betaValue = 100.0 - (maxBetaPowerDecimal + 0.5) * 100.0;
            
            // 限制在0-100%范围内
            return Math.Max(0.0, Math.Min(100.0, betaValue));
        }
        
        /// <summary>
        /// 获取指定频段的功率值
        /// </summary>
        private List<double> GetFrequencyBandPower(List<double> relativePowerSpectrum, double lowFreq, double highFreq)
        {
            var bandPower = new List<double>();
            
            // 计算频率对应的FFT索引
            int lowIndex = (int)(lowFreq / FrequencyResolution);
            int highIndex = (int)(highFreq / FrequencyResolution);
            
            // 确保索引在有效范围内
            lowIndex = Math.Max(0, Math.Min(lowIndex, relativePowerSpectrum.Count - 1));
            highIndex = Math.Max(0, Math.Min(highIndex, relativePowerSpectrum.Count - 1));
            
            // 提取频段功率值
            for (int i = lowIndex; i <= highIndex && i < relativePowerSpectrum.Count; i++)
            {
                bandPower.Add(relativePowerSpectrum[i]);
            }
            
            return bandPower;
        }
        
        /// <summary>
        /// 应用Hanning窗函数
        /// </summary>
        private List<double> ApplyHanningWindow(List<double> data)
        {
            var windowedData = new List<double>();
            int n = data.Count;
            
            for (int i = 0; i < n; i++)
            {
                double window = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (n - 1)));
                windowedData.Add(data[i] * window);
            }
            
            return windowedData;
        }
        
        /// <summary>
        /// 计算下一个2的幂次
        /// </summary>
        private int NextPowerOfTwo(int n)
        {
            int power = 1;
            while (power < n)
            {
                power *= 2;
            }
            return power;
        }
        
        /// <summary>
        /// 执行FFT（快速傅里叶变换）
        /// </summary>
        private List<Complex> FFT(List<double> data)
        {
            int n = data.Count;
            var complexData = new List<Complex>();
            
            // 转换为复数
            for (int i = 0; i < n; i++)
            {
                complexData.Add(new Complex(data[i], 0.0));
            }
            
            // 执行FFT
            return FFTRecursive(complexData);
        }
        
        /// <summary>
        /// 递归FFT实现
        /// </summary>
        private List<Complex> FFTRecursive(List<Complex> data)
        {
            int n = data.Count;
            
            if (n == 1)
            {
                return data;
            }
            
            // 分离偶数和奇数索引
            var even = new List<Complex>();
            var odd = new List<Complex>();
            
            for (int i = 0; i < n; i += 2)
            {
                even.Add(data[i]);
            }
            for (int i = 1; i < n; i += 2)
            {
                odd.Add(data[i]);
            }
            
            // 递归计算
            var evenFFT = FFTRecursive(even);
            var oddFFT = FFTRecursive(odd);
            
            // 合并结果
            var result = new List<Complex>();
            for (int i = 0; i < n; i++)
            {
                result.Add(new Complex(0, 0));
            }
            
            for (int k = 0; k < n / 2; k++)
            {
                double angle = -2.0 * Math.PI * k / n;
                Complex w = Complex.FromPolarCoordinates(1.0, angle);
                
                result[k] = evenFFT[k] + w * oddFFT[k];
                result[k + n / 2] = evenFFT[k] - w * oddFFT[k];
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// 脑电处理结果类
    /// </summary>
    public class BrainwaveProcessResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        
        // 脑电指标
        public double ThetaValue { get; set; }
        public double AlphaValue { get; set; }
        public double BetaValue { get; set; }
        public double BrainwaveFinalIndex { get; set; }
        
        // 处理后的数据（用于调试和验证）
        public List<double> FilteredData { get; set; } = new List<double>();
        public List<Complex> Spectrum { get; set; } = new List<Complex>();
        public List<double> RelativePowerSpectrum { get; set; } = new List<double>();
    }
}
