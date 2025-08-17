using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BrainMonitor.Views
{
    public partial class ReportPage : UserControl, INavigationAware
    {
        private Tester currentTester;
        private double? macaScore;
        private double? mmseScore;
        private double? gripStrength;

        public ReportPage(Tester tester, double? maca, double? mmse, double? grip)
        {
            InitializeComponent();
            currentTester = tester;
            macaScore = maca;
            mmseScore = mmse;
            gripStrength = grip;
            LoadReportData();
        }

        public void OnNavigatedTo()
        {
            // 页面导航到时的处理
        }

        public void OnNavigatedFrom()
        {
            // 页面离开时的处理
        }

        private void LoadReportData()
        {
            // 设置测试者信息
            TesterNameText.Text = currentTester.Name;
            TesterPhoneText.Text = currentTester.Phone;
            TesterGenderText.Text = currentTester.Gender;
            TesterAgeText.Text = currentTester.Age;

            // 计算AD风险评估（基于MACA和MMSE评分）
            double riskPercentage = CalculateADRisk(macaScore, mmseScore);
            RiskPercentageText.Text = $"{riskPercentage:F0}%";
            
            // 设置风险等级
            string riskLevel;
            if (riskPercentage < 20)
            {
                riskLevel = "低风险";
                RiskLevelText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else if (riskPercentage < 50)
            {
                riskLevel = "中等风险";
                RiskLevelText.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                riskLevel = "高风险";
                RiskLevelText.Foreground = System.Windows.Media.Brushes.Red;
            }
            RiskLevelText.Text = riskLevel;

            // 设置进度条宽度
            RiskProgressBar.Width = (riskPercentage / 100) * 200; // 假设最大宽度为200

            // 计算大脑年龄
            int actualAge = int.TryParse(currentTester.Age, out int parsedAge) ? parsedAge : 30;
            int brainAge = CalculateBrainAge(macaScore, mmseScore, actualAge);
            BrainAgeText.Text = $"{brainAge}岁";
            
            int ageDifference = brainAge - actualAge;
            if (ageDifference > 0)
            {
                BrainAgeComparisonText.Text = $"高于实际年龄{ageDifference}岁";
                BrainAgeComparisonText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (ageDifference < 0)
            {
                BrainAgeComparisonText.Text = $"低于实际年龄{Math.Abs(ageDifference)}岁";
                BrainAgeComparisonText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                BrainAgeComparisonText.Text = "与实际年龄相符";
                BrainAgeComparisonText.Foreground = System.Windows.Media.Brushes.Gray;
            }

            // 生成报告分析文本
            GenerateReportAnalysis();

            // 设置报告时间
            ReportTimeText.Text = $"报告时间: {DateTime.Now:yyyy年M月d日 HH:mm}";
            
            // 绘制静态脑电波图
            DrawStaticBrainwaveChart();
        }

        private double CalculateADRisk(double? maca, double? mmse)
        {
            // 简化的AD风险计算算法
            // 正常MACA评分：26-30，正常MMSE评分：24-30
            double macaRisk = maca.HasValue ? Math.Max(0, (26 - maca.Value) / 26 * 50) : 25; // 默认中等风险
            double mmseRisk = mmse.HasValue ? Math.Max(0, (24 - mmse.Value) / 24 * 50) : 25; // 默认中等风险
            
            return Math.Min(100, (macaRisk + mmseRisk) / 2);
        }

        private int CalculateBrainAge(double? maca, double? mmse, int actualAge)
        {
            // 简化的大脑年龄计算算法
            double macaValue = maca ?? 25; // 如果为空，使用默认值25
            double mmseValue = mmse ?? 25; // 如果为空，使用默认值25
            double averageScore = (macaValue + mmseValue) / 2;
            double normalScore = 27; // 正常平均分
            
            double ageFactor = (normalScore - averageScore) * 2;
            return Math.Max(20, actualAge + (int)ageFactor);
        }

        private void GenerateReportAnalysis()
        {
            string analysis = $"根据脑电波分析结果显示，受试者的认知功能";
            
            // 计算平均分，如果有值则使用，否则使用默认值
            double macaValue = macaScore ?? 25;
            double mmseValue = mmseScore ?? 25;
            double averageScore = (macaValue + mmseValue) / 2;
            
            if (averageScore >= 26)
            {
                analysis += "处于正常范围，认知功能良好。";
            }
            else if (averageScore >= 22)
            {
                analysis += "处于正常范围，但存在轻微的认知功能下降趋势。建议定期进行认知功能监测，并采取相应的预防措施。";
            }
            else
            {
                analysis += "存在明显的认知功能下降，建议进一步检查和专业医疗咨询。";
            }
            
            // 添加评分信息
            analysis += " ";
            if (macaScore.HasValue)
            {
                analysis += $"MACA量表得分{macaScore.Value}分";
            }
            else
            {
                analysis += "MACA量表未测试";
            }
            
            analysis += "，";
            if (mmseScore.HasValue)
            {
                analysis += $"MMSE量表得分{mmseScore.Value}分";
            }
            else
            {
                analysis += "MMSE量表未测试";
            }
            
            // 添加握力值信息
            if (gripStrength.HasValue)
            {
                analysis += $"，握力值{gripStrength.Value}";
            }
            else
            {
                analysis += "，握力值未测试";
            }
            
            // 评估结论
            if ((macaScore ?? 25) >= 26 && (mmseScore ?? 24) >= 24)
            {
                analysis += "，各项指标均处于正常范围。";
            }
            else
            {
                analysis += "，需要关注认知功能变化。";
            }

            ReportAnalysisText.Text = analysis;
        }

        private void DrawStaticBrainwaveChart()
        {
            // 清空画布并设置背景
            BrainwaveReportCanvas.Children.Clear();
            BrainwaveReportCanvas.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // 浅灰色背景
            
            // 等待Canvas渲染完成后绘制
            BrainwaveReportCanvas.Loaded += (s, e) => {
                if (BrainwaveReportCanvas.ActualWidth > 0 && BrainwaveReportCanvas.ActualHeight > 0)
                {
                    DrawGridAndBaseline();
                    DrawStaticWaveforms();
                }
            };
            
            // 如果Canvas已经加载，直接绘制
            if (BrainwaveReportCanvas.ActualWidth > 0 && BrainwaveReportCanvas.ActualHeight > 0)
            {
                DrawGridAndBaseline();
                DrawStaticWaveforms();
            }
        }
        
        private void DrawGridAndBaseline()
        {
            double width = BrainwaveReportCanvas.ActualWidth;
            double height = BrainwaveReportCanvas.ActualHeight;
            
            // 绘制网格线
            var gridBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            
            // 垂直网格线
            for (double x = 0; x <= width; x += 40)
            {
                Line gridLine = new Line
                {
                    X1 = x, Y1 = 0,
                    X2 = x, Y2 = height,
                    Stroke = gridBrush,
                    StrokeThickness = 0.5
                };
                BrainwaveReportCanvas.Children.Add(gridLine);
            }
            
            // 水平网格线
            for (double y = 0; y <= height; y += 30)
            {
                Line gridLine = new Line
                {
                    X1 = 0, Y1 = y,
                    X2 = width, Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 0.5
                };
                BrainwaveReportCanvas.Children.Add(gridLine);
            }
            
            // 绘制绿色虚线标准值（中心线）
            Line baseline = new Line
            {
                X1 = 0, Y1 = height / 2,
                X2 = width, Y2 = height / 2,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 150, 0)), // 绿色
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 } // 虚线样式
            };
            BrainwaveReportCanvas.Children.Add(baseline);
        }
        
        private void DrawStaticWaveforms()
        {
            double width = BrainwaveReportCanvas.ActualWidth;
            double height = BrainwaveReportCanvas.ActualHeight;
            double centerY = height / 2;
            
            // 定义三种颜色的波形
            var waveformColors = new List<Color>
            {
                Color.FromRgb(0, 150, 0),   // 绿色
                Color.FromRgb(0, 120, 215), // 蓝色
                Color.FromRgb(128, 0, 128)  // 紫色
            };
            
            // 为每种颜色绘制一条波形
            for (int waveIndex = 0; waveIndex < waveformColors.Count; waveIndex++)
            {
                var color = waveformColors[waveIndex];
                var brush = new SolidColorBrush(color);
                
                // 生成波形数据点
                var points = GenerateWaveformData(width, centerY, waveIndex);
                
                // 绘制波形线条
                for (int i = 1; i < points.Count; i++)
                {
                    Line line = new Line
                    {
                        X1 = points[i - 1].X,
                        Y1 = points[i - 1].Y,
                        X2 = points[i].X,
                        Y2 = points[i].Y,
                        Stroke = brush,
                        StrokeThickness = 2
                    };
                    BrainwaveReportCanvas.Children.Add(line);
                }
            }
        }
        
        private List<Point> GenerateWaveformData(double width, double centerY, int waveIndex)
        {
            var points = new List<Point>();
            double stepSize = 3; // 每3像素一个点
            
            for (double x = 0; x <= width; x += stepSize)
            {
                // 为不同的波形创建不同的模式
                double y = centerY;
                
                switch (waveIndex)
                {
                    case 0: // 绿色波形 - 较平缓的正弦波
                        y += Math.Sin(x * 0.02) * 30 + Math.Sin(x * 0.05) * 15;
                        break;
                    case 1: // 蓝色波形 - 中等频率的波形
                        y += Math.Sin(x * 0.03) * 25 + Math.Cos(x * 0.08) * 20;
                        break;
                    case 2: // 紫色波形 - 较高频率的波形
                        y += Math.Sin(x * 0.04) * 35 + Math.Sin(x * 0.1) * 10;
                        break;
                }
                
                // 添加一些随机变化使波形更自然
                Random rand = new Random(waveIndex * 1000 + (int)x);
                y += (rand.NextDouble() - 0.5) * 8;
                
                // 确保Y坐标在合理范围内
                y = Math.Max(10, Math.Min(BrainwaveReportCanvas.ActualHeight - 10, y));
                
                points.Add(new Point(x, y));
            }
            
            return points;
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 返回测试页面
            NavigationManager.NavigateTo(new TestPage(currentTester));
        }
    }
}