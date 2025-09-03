using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Newtonsoft.Json;
using BrainMirror.Services;

namespace BrainMirror.Views
{
    public partial class ReportPage : UserControl, INavigationAware
    {
        private Tester currentTester;
        private double? mocaScore;
        private double? mmseScore;
        private double? gripStrength;
        private string sourcePage; // 记录来源页面
        
        // 脑电处理结果
        private double brainwaveThetaValue;
        private double brainwaveAlphaValue;
        private double brainwaveBetaValue;
        private double brainwaveFinalIndex;
        private double adRiskIndex;

        public ReportPage(Tester tester, double? moca, double? mmse, double? grip)
        {
            InitializeComponent();
            currentTester = tester;
            mocaScore = moca;
            mmseScore = mmse;
            gripStrength = grip;
            sourcePage = "TestPage"; // 从测试页面跳转过来
            
            // 初始化脑电处理结果为默认值
            brainwaveThetaValue = 0.0;
            brainwaveAlphaValue = 0.0;
            brainwaveBetaValue = 0.0;
            brainwaveFinalIndex = 0.0;
            adRiskIndex = 0.0;
            
            LoadReportData(null);
        }
        
        // 新的构造函数，包含脑电处理结果和AD风险指数
        public ReportPage(Tester tester, double? moca, double? mmse, double? grip, 
            double theta, double alpha, double beta, double brainwaveIndex, double adRisk)
        {
            InitializeComponent();
            currentTester = tester;
            mocaScore = moca;
            mmseScore = mmse;
            gripStrength = grip;
            sourcePage = "TestPage"; // 从测试页面跳转过来
            
            // 设置脑电处理结果
            brainwaveThetaValue = theta;
            brainwaveAlphaValue = alpha;
            brainwaveBetaValue = beta;
            brainwaveFinalIndex = brainwaveIndex;
            adRiskIndex = adRisk;
            
            LoadReportData(null);
        }
        
        // 从服务器数据创建报告页面的构造函数
        public ReportPage(Tester tester, double? moca, double? mmse, double? grip, 
            double theta, double alpha, double beta, double brainwaveIndex, double adRisk, bool fromServer = false)
        {
            InitializeComponent();
            currentTester = tester;
            mocaScore = moca;
            mmseScore = mmse;
            gripStrength = grip;
            sourcePage = fromServer ? "Server" : "TestPage"; // 标记数据来源
            
            // 设置脑电处理结果
            brainwaveThetaValue = theta;
            brainwaveAlphaValue = alpha;
            brainwaveBetaValue = beta;
            brainwaveFinalIndex = brainwaveIndex;
            adRiskIndex = adRisk;
            
            LoadReportData(null);
        }
        
        // 从服务器数据创建报告页面的构造函数（指定来源页面）- 已删除，使用构造函数5替代

        // 从服务器数据创建报告页面的构造函数（包含测试记录创建时间，可替代构造函数4）
        public ReportPage(Tester tester, double? moca, double? mmse, double? grip, 
            double theta, double alpha, double beta, double brainwaveIndex, double adRisk, 
            DateTime? testRecordCreatedAt, string sourcePage)
        {
            InitializeComponent();
            currentTester = tester;
            mocaScore = moca;
            mmseScore = mmse;
            gripStrength = grip;
            this.sourcePage = sourcePage; // 使用指定的来源页面
            
            // 设置脑电处理结果
            brainwaveThetaValue = theta;
            brainwaveAlphaValue = alpha;
            brainwaveBetaValue = beta;
            brainwaveFinalIndex = brainwaveIndex;
            adRiskIndex = adRisk;
            
            // 创建临时的TestHistoryRecord对象来传递创建时间
            TestHistoryRecord? tempRecord = null;
            if (testRecordCreatedAt.HasValue)
            {
                tempRecord = new TestHistoryRecord
                {
                    CreatedAt = testRecordCreatedAt.Value,
                    MocaScore = moca,
                    MmseScore = mmse,
                    GripStrength = grip,
                    AdRiskValue = adRisk
                };
            }
            
            LoadReportData(tempRecord);
        }

        // 从测试历史记录创建报告页面的构造函数
        public ReportPage(Tester tester, TestHistoryRecord historyRecord)
        {
            InitializeComponent();
            currentTester = tester;
            
            // 从历史记录中获取评分数据
            mocaScore = historyRecord.MocaScore;
            mmseScore = historyRecord.MmseScore;
            gripStrength = historyRecord.GripStrength;
            
            // 从历史记录中获取AD风险值
            adRiskIndex = historyRecord.AdRiskValue ?? 0.0;
            
            sourcePage = "TestHistoryPage"; // 从测试历史页面跳转过来
            
            LoadReportData(historyRecord);
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
            LoadReportData(null);
        }

        private void LoadReportData(TestHistoryRecord? historyRecord)
        {
            
            // 设置测试者信息
            TesterNameText.Text = currentTester.Name;
            TesterPhoneText.Text = currentTester.Phone;
            TesterGenderText.Text = currentTester.Gender;
            TesterAgeText.Text = currentTester.Age;

            // 使用新的AD风险指数（如果可用）
            double riskPercentage;
            if (adRiskIndex > 0)
            {
                // 使用从服务器获取的AD风险指数
                riskPercentage = adRiskIndex;
            }
            else if (brainwaveFinalIndex > 0)
            {
                // 如果没有AD风险值，但有脑电数据，使用脑电数据计算风险
                riskPercentage = brainwaveFinalIndex;
            }
            else
            {
                // 使用传统的MoCA和MMSE评分计算
                riskPercentage = CalculateADRisk(mocaScore, mmseScore);
            }
            
            // 注意：这里只是设置初始值，如果后面有AD风险值，会被覆盖
            RiskPercentageText.Text = $"{riskPercentage:F1}";
            
            // 设置风险等级
            string riskLevel;
            if (riskPercentage < 30)
            {
                riskLevel = "健康";
                RiskLevelText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else if (riskPercentage < 60)
            {
                riskLevel = "轻风险";
                RiskLevelText.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                riskLevel = "中、高风险";
                RiskLevelText.Foreground = System.Windows.Media.Brushes.Red;
            }
            RiskLevelText.Text = riskLevel;

            // 设置进度条宽度
            RiskProgressBar.Width = (riskPercentage / 100) * 200; // 假设最大宽度为200

            // 计算大脑年龄 - 直接使用测试者的实际年龄
            int actualAge = int.TryParse(currentTester.Age, out int parsedAge) ? parsedAge : 30;
            int brainAge = actualAge; // 直接使用实际年龄
            BrainAgeText.Text = $"{brainAge}岁";
            
            // 年龄差异为0，显示与实际年龄相符
            BrainAgeComparisonText.Text = "与实际年龄相符";
            BrainAgeComparisonText.Foreground = System.Windows.Media.Brushes.Gray;
            

            
            // 显示AD风险值（从服务器获取）
            if (adRiskIndex > 0)
            {
                // 使用从服务器获取的AD风险值
                RiskPercentageText.Text = $"{adRiskIndex:F1}";
                
                // 设置风险等级
                string serverRiskLevel;
                if (adRiskIndex < 30)
                {
                    serverRiskLevel = "健康";
                    RiskLevelText.Foreground = System.Windows.Media.Brushes.Green;
                }
                else if (adRiskIndex < 60)
                {
                    serverRiskLevel = "轻风险";
                    RiskLevelText.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    serverRiskLevel = "中、高风险";
                    RiskLevelText.Foreground = System.Windows.Media.Brushes.Red;
                }
                RiskLevelText.Text = serverRiskLevel;
                
                // 设置进度条宽度
                RiskProgressBar.Width = (adRiskIndex / 100) * 200; // 假设最大宽度为200
            }
            
            // 由于大脑年龄直接使用实际年龄，所以总是显示"与实际年龄相符"
            // 这部分逻辑已经在上面处理了，这里不需要重复设置

            // 生成报告分析文本
            GenerateReportAnalysis();

            // 设置报告时间
            if (historyRecord != null)
            {
                // 使用测试记录的时间，将UTC时间转换为本地时间
                DateTime localTime = historyRecord.CreatedAt.ToLocalTime();
                ReportTimeText.Text = $"报告时间: {localTime:yyyy年M月d日 HH:mm}";
            }
            else
            {
                // 使用当前时间
                ReportTimeText.Text = $"报告时间: {DateTime.Now:yyyy年M月d日 HH:mm}";
            }
            
            // 绘制脑电波图表
            DrawBrainwaveCharts(historyRecord);
        }

        private double CalculateADRisk(double? moca, double? mmse)
        {
            // 简化的AD风险计算算法
            // 正常MoCA评分：26-30，正常MMSE评分：24-30
            double mocaRisk = moca.HasValue ? Math.Max(0, (26 - moca.Value) / 26 * 50) : 0; // 如果没有数据，风险为0
            double mmseRisk = mmse.HasValue ? Math.Max(0, (24 - mmse.Value) / 24 * 50) : 0; // 如果没有数据，风险为0
            
            // 如果两个量表都没有数据，返回0
            if (!moca.HasValue && !mmse.HasValue)
            {
                return 0;
            }
            
            // 如果只有一个量表有数据，直接返回该量表的风险值
            if (!moca.HasValue)
            {
                return mmseRisk;
            }
            if (!mmse.HasValue)
            {
                return mocaRisk;
            }
            
            // 两个量表都有数据，取平均值
            return Math.Min(100, (mocaRisk + mmseRisk) / 2);
        }



        private void GenerateReportAnalysis()
        {
            string analysis = "根据脑电波分析结果显示，";
            
            // 根据AD风险值生成不同的结果解读
            if (adRiskIndex > 0)
            {
                // 使用AD风险值生成分析
                if (adRiskIndex < 30)
                {
                    // 健康版本 (0-30)
                    analysis += "受试者的认知功能处于健康状态，脑电波活动正常，各项指标表现良好。";
                }
                else if (adRiskIndex < 60)
                {
                    // 轻风险版本 (30-60)
                    analysis += "受试者的认知功能存在轻度风险，脑电波活动显示轻微的异常模式，建议加强认知功能监测。";
                }
                else
                {
                    // 中、高风险版本 (60-100)
                    analysis += "受试者的认知功能存在中高风险，脑电波活动显示明显的异常模式，建议及时进行专业医疗咨询和进一步检查。";
                }
            }
            else
            {
                // 如果没有AD风险值，使用传统的MoCA和MMSE评分分析
                analysis += "受试者的认知功能";
                
                // 计算平均分，如果有值则使用，否则使用默认值
                double mocaValue = mocaScore ?? 25;
                double mmseValue = mmseScore ?? 25;
                double averageScore = (mocaValue + mmseValue) / 2;
                
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
            }
            
            // 添加评分信息（保持不变）
            analysis += " ";
            if (mocaScore.HasValue)
            {
                analysis += $"MoCA量表得分{mocaScore.Value}分";
            }
            else
            {
                analysis += "MoCA量表未测试";
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
            
            // 添加握力值信息（保持不变）
            if (gripStrength.HasValue)
            {
                analysis += $"，握力值{gripStrength.Value}";
            }
            else
            {
                analysis += "，握力值未测试";
            }
            
            // 评估结论（根据AD风险值调整）
            if (adRiskIndex > 0)
            {
                if (adRiskIndex < 30)
                {
                    analysis += "，各项指标均处于正常范围，建议保持健康的生活方式。";
                }
                else if (adRiskIndex < 60)
                {
                    analysis += "，需要关注认知功能变化，建议定期复查。";
                }
                else
                {
                    analysis += "，需要重点关注认知功能变化，建议及时就医。";
                }
            }
            else
            {
                // 原有的评估结论逻辑
                if ((mocaScore ?? 25) >= 26 && (mmseScore ?? 24) >= 24)
                {
                    analysis += "，各项指标均处于正常范围。";
                }
                else
                {
                    analysis += "，需要关注认知功能变化。";
                }
            }

            ReportAnalysisText.Text = analysis;
        }

        private async void DrawBrainwaveCharts(TestHistoryRecord? historyRecord)
        {
            // 清空画布
            CombinedChartCanvas.Children.Clear();

            // 设置画布背景
            CombinedChartCanvas.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));

            // 获取闭眼测试结果数据
            double thetaValue = 0.0;
            double alphaValue = 0.0;
            double betaValue = 0.0;

            if (historyRecord != null && historyRecord.ClosedEyesResultId.HasValue)
            {
                try
                {
                    // 从服务器获取闭眼测试结果数据
                    var testResult = await GetTestResultData(historyRecord.ClosedEyesResultId.Value);
                    if (testResult != null)
                    {
                        thetaValue = testResult.ThetaValue ?? 0.0;
                        alphaValue = testResult.AlphaValue ?? 0.0;
                        betaValue = testResult.BetaValue ?? 0.0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取测试结果数据失败: {ex.Message}");
                }
            }
            else
            {
                // 使用当前测试的数据
                thetaValue = brainwaveThetaValue;
                alphaValue = brainwaveAlphaValue;
                betaValue = brainwaveBetaValue;
            }

            // 绘制合并的图表
            DrawCombinedChart(CombinedChartCanvas, thetaValue, alphaValue, betaValue);
        }

        private async Task<TestResultData?> GetTestResultData(int resultId)
        {
            try
            {
                // 调用后端API获取测试结果数据
                var response = await HttpService.GetAsync<ApiResponse<TestResultData>>($"/test-records/result/{resultId}", GlobalMedicalStaffManager.CurrentToken);
                
                if (response.Success && response.Data != null)
                {
                    return response.Data;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取测试结果数据失败: {ex.Message}");
                return null;
            }
        }

        private void DrawCombinedChart(Canvas canvas, double thetaValue, double alphaValue, double betaValue)
        {
            // 等待Canvas渲染完成后绘制
            canvas.Loaded += (s, e) => {
                if (canvas.ActualWidth > 0 && canvas.ActualHeight > 0)
                {
                    DrawCombinedChartContent(canvas, thetaValue, alphaValue, betaValue);
                }
            };
            
            // 如果Canvas已经加载，直接绘制
            if (canvas.ActualWidth > 0 && canvas.ActualHeight > 0)
            {
                DrawCombinedChartContent(canvas, thetaValue, alphaValue, betaValue);
            }
        }

        private void DrawCombinedChartContent(Canvas canvas, double thetaValue, double alphaValue, double betaValue)
        {
            double width = canvas.ActualWidth;
            double height = canvas.ActualHeight;
            double chartHeight = height * 0.8; // 图表区域高度
            double chartTop = height * 0.1; // 图表顶部位置

            // 清空画布
            canvas.Children.Clear();

            // 绘制网格线
            var gridBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            
            // 水平网格线（0-100刻度，每20一个值）
            for (int i = 0; i <= 5; i++)
            {
                double y = chartTop + (i * chartHeight / 5);
                Line gridLine = new Line
                {
                    X1 = 0, Y1 = y,
                    X2 = width, Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 0.5
                };
                canvas.Children.Add(gridLine);

                // 添加刻度标签
                TextBlock scaleLabel = new TextBlock
                {
                    Text = (100 - i * 20).ToString(),
                    FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                };
                Canvas.SetLeft(scaleLabel, 5);
                Canvas.SetTop(scaleLabel, y - 8);
                canvas.Children.Add(scaleLabel);
            }

            // 计算柱状图参数
            double barWidth = width * 0.08; // 柱子宽度（减小）
            double barSpacing = width * 0.15; // 柱子间距（增加）
            double totalWidth = 3 * barWidth + 2 * barSpacing;
            double startX = (width - totalWidth) / 2; // 起始X位置

            // 绘制三个柱子
            var values = new[] { thetaValue, alphaValue, betaValue };
            var labels = new[] { "Theta", "Alpha", "Beta" };

            for (int i = 0; i < 3; i++)
            {
                double value = values[i];
                string label = labels[i];
                double x = startX + i * (barWidth + barSpacing);
                
                // 限制值在0-100范围内
                value = Math.Max(0, Math.Min(100, value));
                
                // 计算柱子高度
                double barHeight = (value / 100.0) * chartHeight;
                double barY = chartTop + chartHeight - barHeight;

                // 生成热力图颜色
                Color barColor = GetHeatmapColor(value);

                // 绘制柱子（圆角矩形）
                Border bar = new Border
                {
                    Width = barWidth,
                    Height = barHeight,
                    Background = new SolidColorBrush(barColor),
                    CornerRadius = new CornerRadius(barWidth * 0.1), // 稍微圆角
                    Opacity = 0.6 // 降低饱和度
                };
                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, barY);
                canvas.Children.Add(bar);

                // 在柱子顶部添加数值标签
                Border valueContainer = new Border
                {
                    Width = barWidth,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                TextBlock valueLabel = new TextBlock
                {
                    Text = $"{value:F1}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                valueContainer.Child = valueLabel;
                Canvas.SetLeft(valueContainer, x);
                Canvas.SetTop(valueContainer, barY - 20);
                canvas.Children.Add(valueContainer);

                // 在柱子底部添加标签
                Border labelContainer = new Border
                {
                    Width = barWidth,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                TextBlock labelText = new TextBlock
                {
                    Text = label,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                labelContainer.Child = labelText;
                Canvas.SetLeft(labelContainer, x);
                Canvas.SetTop(labelContainer, chartTop + chartHeight + 5);
                canvas.Children.Add(labelContainer);
            }
        }

        private Color GetHeatmapColor(double value)
        {
            // 热力图颜色映射：0=绿色，50=黄色，100=红色（降低饱和度）
            if (value <= 50)
            {
                // 绿色到黄色的过渡（降低饱和度）
                double ratio = value / 50.0;
                byte r = (byte)Math.Min(255, 180 * ratio + 100); // 降低红色分量
                byte g = (byte)Math.Min(255, 180 + 100); // 降低绿色分量
                byte b = (byte)(100); // 添加灰色分量降低饱和度
                return Color.FromRgb(r, g, b);
            }
            else
            {
                // 黄色到红色的过渡（降低饱和度）
                double ratio = (value - 50) / 50.0;
                byte r = (byte)Math.Min(255, 180 + 100); // 降低红色分量
                byte g = (byte)Math.Min(255, 180 * (1 - ratio) + 100); // 降低绿色分量
                byte b = (byte)(100); // 添加灰色分量降低饱和度
                return Color.FromRgb(r, g, b);
            }
        }

        // 测试结果数据模型
        private class TestResultData
        {
            [JsonProperty("theta_value")]
            public double? ThetaValue { get; set; }
            
            [JsonProperty("alpha_value")]
            public double? AlphaValue { get; set; }
            
            [JsonProperty("beta_value")]
            public double? BetaValue { get; set; }
            
            [JsonProperty("result")]
            public string Result { get; set; } = string.Empty;
            
            [JsonProperty("created_at")]
            public DateTime CreatedAt { get; set; }
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 根据来源页面决定返回到哪里
            switch (sourcePage)
            {
                case "TestHistoryPage":
                    // 从测试历史页面来的，返回到测试历史页面
                    NavigationManager.NavigateTo(new TestHistoryPage(currentTester));
                    break;
                case "TestPage":
                default:
                    // 从测试页面来的，返回到测试页面
                    NavigationManager.NavigateTo(new TestPage(currentTester));
                    break;
            }
        }
    }
}