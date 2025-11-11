using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.IO;
using System.IO.Packaging;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Printing;
using Microsoft.Win32;
using Newtonsoft.Json;
using BrainMirror.Services;
using System.Threading.Tasks;

namespace BrainMirror.Views
{
    public partial class ReportPage : UserControl, INavigationAware
    {
        private Tester currentTester;
        private double? mocaScore;
        private double? mmseScore;
        private double? gripStrength;
        private string sourcePage;
        
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
            sourcePage = "TestPage";
            
            brainwaveThetaValue = 0.0;
            brainwaveAlphaValue = 0.0;
            brainwaveBetaValue = 0.0;
            brainwaveFinalIndex = 0.0;
            adRiskIndex = 0.0;
            
            LoadReportData(null);
        }
        
        public ReportPage(Tester tester, double? moca, double? mmse, double? grip, 
            double theta, double alpha, double beta, double brainwaveIndex, double adRisk)
        {
            InitializeComponent();
            currentTester = tester;
            mocaScore = moca;
            mmseScore = mmse;
            gripStrength = grip;
            sourcePage = "TestPage";
            
            brainwaveThetaValue = theta;
            brainwaveAlphaValue = alpha;
            brainwaveBetaValue = beta;
            brainwaveFinalIndex = brainwaveIndex;
            adRiskIndex = adRisk;
            
            LoadReportData(null);
        }
        
        public ReportPage(Tester tester, double? moca, double? mmse, double? grip, 
            double theta, double alpha, double beta, double brainwaveIndex, double adRisk, bool fromServer = false)
        {
            InitializeComponent();
            currentTester = tester;
            mocaScore = moca;
            mmseScore = mmse;
            gripStrength = grip;
            sourcePage = fromServer ? "Server" : "TestPage";
            
            brainwaveThetaValue = theta;
            brainwaveAlphaValue = alpha;
            brainwaveBetaValue = beta;
            brainwaveFinalIndex = brainwaveIndex;
            adRiskIndex = adRisk;
            
            LoadReportData(null);
        }
        
        public ReportPage(Tester tester, double? moca, double? mmse, double? grip, 
            double theta, double alpha, double beta, double brainwaveIndex, double adRisk, 
            DateTime? testRecordCreatedAt, string sourcePage)
        {
            InitializeComponent();
            currentTester = tester;
            mocaScore = moca;
            mmseScore = mmse;
            gripStrength = grip;
            this.sourcePage = sourcePage;
            
            brainwaveThetaValue = theta;
            brainwaveAlphaValue = alpha;
            brainwaveBetaValue = beta;
            brainwaveFinalIndex = brainwaveIndex;
            adRiskIndex = adRisk;
            
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

        public ReportPage(Tester tester, double? moca, double? mmse, double? grip, 
            double theta, double alpha, double beta, double brainwaveIndex, double adRisk, 
            DateTime? testRecordCreatedAt, string? reportNumber, string sourcePage)
        {
            InitializeComponent();
            currentTester = tester;
            mocaScore = moca;
            mmseScore = mmse;
            gripStrength = grip;
            this.sourcePage = sourcePage;
            
            brainwaveThetaValue = theta;
            brainwaveAlphaValue = alpha;
            brainwaveBetaValue = beta;
            brainwaveFinalIndex = brainwaveIndex;
            adRiskIndex = adRisk;
            
            TestHistoryRecord? tempRecord = null;
            if (testRecordCreatedAt.HasValue)
            {
                tempRecord = new TestHistoryRecord
                {
                    CreatedAt = testRecordCreatedAt.Value,
                    ReportNumber = reportNumber ?? string.Empty,
                    MocaScore = moca,
                    MmseScore = mmse,
                    GripStrength = grip,
                    AdRiskValue = adRisk
                };
            }
            
            LoadReportData(tempRecord);
        }

        public ReportPage(Tester tester, TestHistoryRecord historyRecord)
        {
            InitializeComponent();
            currentTester = tester;
            
            mocaScore = historyRecord.MocaScore;
            mmseScore = historyRecord.MmseScore;
            gripStrength = historyRecord.GripStrength;
            adRiskIndex = historyRecord.AdRiskValue ?? 0.0;
            
            sourcePage = "TestHistoryPage";
            
            LoadReportData(historyRecord);
        }

        public void OnNavigatedTo() { }
        public void OnNavigatedFrom() { }

        private void LoadReportData(TestHistoryRecord? historyRecord)
        {
            // 获取报告时间（用于显示生成时间）
            DateTime reportTime = historyRecord?.CreatedAt.ToLocalTime() ?? DateTime.Now;
            
            // 设置报告编号
            // 优先使用数据库中的报告编号，如果没有则生成临时编号
            if (historyRecord != null && !string.IsNullOrEmpty(historyRecord.ReportNumber))
            {
                ReportIdText.Text = historyRecord.ReportNumber;
            }
            else
            {
                // 生成临时报告编号（用于直接从测试页面生成报告时）
                string phoneSuffix = "";
                if (!string.IsNullOrEmpty(currentTester.Phone) && currentTester.Phone.Length >= 4)
                {
                    phoneSuffix = currentTester.Phone.Substring(currentTester.Phone.Length - 4);
                }
                else if (!string.IsNullOrEmpty(currentTester.Phone))
                {
                    phoneSuffix = currentTester.Phone;
                }
                else
                {
                    phoneSuffix = "0000";
                }
                ReportIdText.Text = $"{reportTime:yyyy-MMdd-HHmm}-{phoneSuffix}";
            }
            
            // 设置基本信息
            TesterNameText.Text = currentTester.Name;
            TesterPhoneText.Text = currentTester.Phone;
            TesterGenderText.Text = currentTester.Gender;
            TesterAgeText.Text = currentTester.Age;

            // 计算AD风险值
            double riskPercentage;
            if (adRiskIndex > 0)
            {
                riskPercentage = adRiskIndex;
            }
            else if (brainwaveFinalIndex > 0)
            {
                riskPercentage = brainwaveFinalIndex;
            }
            else
            {
                riskPercentage = CalculateADRisk(mocaScore, mmseScore);
            }
            
            // 绘制AD风险圆环图
            DrawRiskRing(riskPercentage);
            
            // 更新MoCA进度条
            UpdateMocaProgressBar();
            
            // 更新MMSE进度条
            UpdateMmseProgressBar();
            
            // 更新握力进度条
            UpdateGripProgressBar();
            
            // 生成综合解读
            GenerateComprehensiveInterpretation(riskPercentage);
            
            // 设置免责声明
            SetDisclaimerText();

            // 设置报告时间
            string timeText = $"生成时间：{reportTime:yyyy-MM-dd HH:mm}";
            HeaderReportTimeText.Text = timeText;
            
            // 绘制脑电波图表
            DrawBrainwaveChart(historyRecord);
        }

        private double CalculateADRisk(double? moca, double? mmse)
        {
            double brainwaveFinalIndex = (brainwaveThetaValue / 3.0) + (brainwaveAlphaValue / 3.0) + (brainwaveBetaValue / 3.0);
            
            double scaleScore = 0.0;
            int scaleCount = 0;
            
            if (mmse.HasValue)
            {
                double mmsePercentage = (mmse.Value / 30.0) * 100.0;
                scaleScore += mmsePercentage;
                scaleCount++;
            }
            
            if (moca.HasValue)
            {
                double mocaPercentage = (moca.Value / 30.0) * 100.0;
                scaleScore += mocaPercentage;
                scaleCount++;
            }
            
            double averageScaleScore = scaleCount > 0 ? scaleScore / scaleCount : 0.0;
            double finalScaleScore = 100.0 - averageScaleScore;
            
            double gripStrengthScore = 0.0;
            bool hasGripStrength = false;
            
            if (gripStrength.HasValue && currentTester != null)
            {
                try
                {
                    if (int.TryParse(currentTester.Age, out int age))
                    {
                        gripStrengthScore = Services.GripStrengthService.CalculateGripStrengthScore(
                            gripStrength.Value, currentTester.Gender, age);
                        hasGripStrength = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"计算握力分数时发生异常: {ex.Message}");
                }
            }
            
            double adRiskIndex;
            
            if (scaleCount > 0 && hasGripStrength)
            {
                adRiskIndex = (brainwaveFinalIndex / 3.0) + (finalScaleScore / 3.0) + (gripStrengthScore / 3.0);
            }
            else if (scaleCount > 0)
            {
                adRiskIndex = (brainwaveFinalIndex / 2.0) + (finalScaleScore / 2.0);
            }
            else if (hasGripStrength)
            {
                adRiskIndex = (brainwaveFinalIndex / 2.0) + (gripStrengthScore / 2.0);
            }
            else
            {
                adRiskIndex = brainwaveFinalIndex;
            }
            
            return adRiskIndex;
        }

        private void DrawRiskRing(double percentage)
        {
            RiskRingCanvas.Children.Clear();
            
            double centerX = 60;
            double centerY = 60;
            double outerRadius = 48;
            double innerRadius = 36;
            
            // 根据百分比确定颜色
            Color ringColor = GetRiskColor(percentage);
            
            // 绘制背景圆环（灰色）
            System.Windows.Shapes.Path backgroundRing = CreateRingSegment(centerX, centerY, outerRadius, innerRadius, 0, 360, 
                Color.FromRgb(230, 230, 230));
            RiskRingCanvas.Children.Add(backgroundRing);
            
            // 绘制彩色圆环（根据百分比）
            double angle = (percentage / 100.0) * 360.0;
            System.Windows.Shapes.Path coloredRing = CreateRingSegment(centerX, centerY, outerRadius, innerRadius, -90, angle, ringColor);
            RiskRingCanvas.Children.Add(coloredRing);
            
            // 设置百分比文本
            RiskPercentageText.Text = $"{percentage:F0}%";
            
            // 设置风险等级文本和颜色
            string riskLevel;
            SolidColorBrush textBrush;
            
            if (percentage <= 30)
            {
                riskLevel = "低风险";
                textBrush = new SolidColorBrush(Color.FromRgb(74, 158, 255)); // 蓝色
            }
            else if (percentage <= 50)
            {
                riskLevel = "轻度风险";
                textBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // 黄色
            }
            else if (percentage <= 70)
            {
                riskLevel = "中度风险";
                textBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // 橙色
                }
                else
                {
                riskLevel = "高风险";
                textBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 红色
            }
            
            RiskLevelText.Text = riskLevel;
            RiskLevelText.Foreground = textBrush;
        }

        private System.Windows.Shapes.Path CreateRingSegment(double centerX, double centerY, double outerRadius, double innerRadius, 
            double startAngle, double sweepAngle, Color color)
        {
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();
            
            double startAngleRad = startAngle * Math.PI / 180.0;
            double endAngleRad = (startAngle + sweepAngle) * Math.PI / 180.0;
            
            Point outerStart = new Point(
                centerX + outerRadius * Math.Cos(startAngleRad),
                centerY + outerRadius * Math.Sin(startAngleRad)
            );
            
            Point outerEnd = new Point(
                centerX + outerRadius * Math.Cos(endAngleRad),
                centerY + outerRadius * Math.Sin(endAngleRad)
            );
            
            Point innerStart = new Point(
                centerX + innerRadius * Math.Cos(startAngleRad),
                centerY + innerRadius * Math.Sin(startAngleRad)
            );
            
            Point innerEnd = new Point(
                centerX + innerRadius * Math.Cos(endAngleRad),
                centerY + innerRadius * Math.Sin(endAngleRad)
            );
            
            bool isLargeArc = sweepAngle > 180;
            
            figure.StartPoint = outerStart;
            figure.Segments.Add(new ArcSegment(outerEnd, new Size(outerRadius, outerRadius), 0, 
                isLargeArc, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(innerEnd, true));
            figure.Segments.Add(new ArcSegment(innerStart, new Size(innerRadius, innerRadius), 0, 
                isLargeArc, SweepDirection.Counterclockwise, true));
            figure.IsClosed = true;
            
            geometry.Figures.Add(figure);
            
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path
            {
                Data = geometry,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 0
            };
            
            return path;
        }

        private Color GetRiskColor(double percentage)
        {
            if (percentage <= 30)
            {
                return Color.FromRgb(74, 158, 255); // 蓝色
            }
            else if (percentage <= 50)
            {
                return Color.FromRgb(255, 193, 7); // 黄色
            }
            else if (percentage <= 70)
            {
                return Color.FromRgb(255, 152, 0); // 橙色
                }
                else
                {
                return Color.FromRgb(244, 67, 54); // 红色
            }
        }

        private void UpdateMocaProgressBar()
        {
            if (!mocaScore.HasValue)
            {
                MocaLabelText.Text = "MoCA";
                MocaValueText.Text = "未测";
                MocaProgressBar.Width = 0;
                MocaProgressBar.Background = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                return;
            }
            
            double score = mocaScore.Value;
            double percentage = Math.Round((score / 30.0) * 100);
            
            // 左边显示分数，右边显示百分比
            MocaLabelText.Text = $"MoCA ({score:F0}/30)";
            MocaValueText.Text = $"{percentage:F0}%";
            
            // 计算进度条宽度（相对于父容器）
            MocaProgressBar.Width = double.NaN; // 使用百分比
            MocaProgressBar.SetValue(Border.WidthProperty, DependencyProperty.UnsetValue);
            
            // 设置颜色：<26 橙色；≥26 蓝色
            if (score < 26)
            {
                MocaProgressBar.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // 橙色
            }
            else
            {
                MocaProgressBar.Background = new SolidColorBrush(Color.FromRgb(74, 158, 255)); // 蓝色
            }
            
            // 动态设置宽度百分比
            this.Loaded += (s, e) =>
            {
                var parent = MocaProgressBar.Parent as Border;
                if (parent != null && parent.ActualWidth > 0)
                {
                    MocaProgressBar.Width = parent.ActualWidth * (percentage / 100.0);
                }
            };
        }

        private void UpdateMmseProgressBar()
        {
            if (!mmseScore.HasValue)
            {
                MmseLabelText.Text = "MMSE";
                MmseValueText.Text = "未测";
                MmseProgressBar.Width = 0;
                MmseProgressBar.Background = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                return;
            }
            
            double score = mmseScore.Value;
            double percentage = Math.Round((score / 30.0) * 100);
            
            // 左边显示分数，右边显示百分比
            MmseLabelText.Text = $"MMSE ({score:F0}/30)";
            MmseValueText.Text = $"{percentage:F0}%";
            
            // 设置颜色
            Color barColor;
            if (score >= 27)
            {
                barColor = Color.FromRgb(74, 158, 255); // 蓝色：27-30
            }
            else if (score >= 21)
            {
                barColor = Color.FromRgb(255, 193, 7); // 黄色：21-26
            }
            else if (score >= 10)
            {
                barColor = Color.FromRgb(255, 152, 0); // 橙色：10-20
            }
            else
            {
                barColor = Color.FromRgb(244, 67, 54); // 红色：≤9
            }
            
            MmseProgressBar.Background = new SolidColorBrush(barColor);
            
            // 动态设置宽度百分比
            this.Loaded += (s, e) =>
            {
                var parent = MmseProgressBar.Parent as Border;
                if (parent != null && parent.ActualWidth > 0)
                {
                    MmseProgressBar.Width = parent.ActualWidth * (percentage / 100.0);
                }
            };
        }

        private void UpdateGripProgressBar()
        {
            if (!gripStrength.HasValue)
            {
                GripLabelText.Text = "握力";
                GripValueText.Text = "未测";
                GripProgressBar.Width = 0;
                GripProgressBar.Background = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                return;
            }
            
            double grip = gripStrength.Value;
            
            // 根据性别确定阈值
            double threshold;
            if (currentTester.Gender == "男")
            {
                threshold = 28.5;
                }
                else
                {
                threshold = 18.5;
            }
            
            // 计算进度条百分比（以阈值×1.3为满格）
            double maxValue = threshold * 1.3;
            double percentage = Math.Min(100, Math.Round((grip / maxValue) * 100));
            
            // 左边显示实际值，右边显示百分比
            GripLabelText.Text = $"握力 ({grip:F1}kg)";
            GripValueText.Text = $"{percentage:F0}%";
            
            // 设置颜色：≥阈值 蓝色；<阈值 橙色
            if (grip >= threshold)
            {
                GripProgressBar.Background = new SolidColorBrush(Color.FromRgb(74, 158, 255)); // 蓝色
            }
            else
            {
                GripProgressBar.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // 橙色
            }
            
            // 动态设置宽度百分比
            this.Loaded += (s, e) =>
            {
                var parent = GripProgressBar.Parent as Border;
                if (parent != null && parent.ActualWidth > 0)
                {
                    GripProgressBar.Width = parent.ActualWidth * (percentage / 100.0);
                }
            };
        }

        private void GenerateComprehensiveInterpretation(double riskPercentage)
        {
            // 根据风险等级生成不同的解读内容
            if (riskPercentage <= 30)
            {
                // 低风险
                OverallRiskText.Text = "综合风险水平：当前结果提示 AD 风险较低（偏健康）。建议保持良好生活习惯，定期复评，持续监测认知功能变化。";
                
                KeyFocusText.Text = "•建议 ≥ 150 分钟中等强度有氧运动与适度抗阻训练，保持认知灵活性。\n" +
                    "•每周 2–3 次阻抗训练，包含上肢握力练习；如单侧握力或弹力带训练。\n" +
                    "•可结合 10–15 分钟的协调/平衡训练，如八段锦、太极片段或瑜伽体式。";
                
                ExerciseAdviceText.Text = "保持健康生活方式，包括规律运动、均衡饮食、充足睡眠和社交活动。";
                
                FollowUpAdviceText.Text = "•建议 6–12 个月定期复评（如出现认知变化明显，可缩短至 3–6 个月）。\n" +
                    "•如出现近期记忆下降、多任务处理困难等变化，建议进行全面评估或咨询专科医生。";
            }
            else if (riskPercentage <= 50)
            {
                // 轻度风险
                OverallRiskText.Text = "总体风险：当前结果提示 AD 风险轻度升高。建议开始日常管理与阶段性复评，关注趋势变化。";
                
                KeyFocusText.Text = "•留意近事记忆、多任务处理与情境定向的细微变化。\n" +
                    "•优化睡眠、控制血压血脂血糖，减少久坐，限制酒精。";
                
                ExerciseAdviceText.Text = "•每周 ≥ 150–180 分钟中等强度有氧运动。\n" +
                    "•每周 2–3 次抗阻训练，重点包含上肢握力训练。\n" +
                    "•每天 10–15 分钟协调/平衡练习（如八段锦、太极动作拆分）。";
                
                FollowUpAdviceText.Text = "•建议 3–6 个月复评（MoCA/MMSE 与必要时脑电）。\n" +
                    "•如工作/生活效率下降影响日常功能或家属明显察觉变化，建议就医评估。";
            }
            else if (riskPercentage <= 70)
            {
                // 中度风险
                OverallRiskText.Text = "总体风险：当前结果提示 AD 风险中度升高。需加强生活方式干预并缩短随访间隔，警惕功能受限信号。";
                
                KeyFocusText.Text = "•出现近期学习新信息困难、重复发问增多、计划/执行家务困难等现象。\n" +
                    "•伴随情绪和睡眠问题时，更应尽快复测与评估。";
                
                ExerciseAdviceText.Text = "•每周 ≥ 150–180 分钟中等强度有氧运动。\n" +
                    "•每周 3 次抗阻训练（包含握力与上肢、躯干）。\n" +
                    "•建议加入有指导的认知+体力复合训练（如步行时口算/命名练习）。";
                
                FollowUpAdviceText.Text = "•建议 1–3 个月复评。\n" +
                    "•若出现明显日常功能下降、迷路或财务管理困难，建议尽快至神经内科/记忆门诊完善评估（必要时影像与实验室检查）。";
                }
                else
                {
                // 高风险
                OverallRiskText.Text = "总体风险：当前结果提示 AD 风险显著升高。建议尽快进行专科评估，明确原因并制定干预方案。";
                
                KeyFocusText.Text = "•出现明显近事记忆受损、定向力下降、言语流畅度明显降低。\n" +
                    "•出现行为/情绪改变（易激惹、冷淡）或睡眠严重紊乱。";
                
                ExerciseAdviceText.Text = "•在专业人员指导下进行安全可行的个体化运动。\n" +
                    "•每周 150–180 分钟分段有氧运动。\n" +
                    "•每周 2–3 次低到中等强度抗阻训练。\n" +
                    "•加入看护下的步态/平衡训练，防跌倒。";
                
                FollowUpAdviceText.Text = "•建议尽快就诊记忆门诊/神经内科，完善进一步检查与干预。\n" +
                    "•就医前保留近期评估记录，便于对比。\n" +
                    "•随访周期以专科医师建议为准，通常 ≤ 1–3 个月。";
            }
        }

        private void SetDisclaimerText()
        {
            DisclaimerText.Text = "本报告基于单次测试数据，测试结果受环境、状态等多因素影响，仅供参考，不作为临床诊断依据。" +
                "报告中提及的 MoCA、MMSE 与握力数据源于标准化筛查评估，脑电（若采集）为量化指标，可能受仪器精度、个体差异影响。" +
                "请在专业医师指导下结合影像学、实验室检查及临床综合判断进行诊疗决策。" +
                "如出现明显功能下降（如记忆、定向、语言、行为或情绪变化）或家属察觉显著异常，建议及时就医进行专科评估。";
        }

        private async void DrawBrainwaveChart(TestHistoryRecord? historyRecord)
        {
            BrainwaveChartCanvas.Children.Clear();
            BrainwaveChartCanvas.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));

            double thetaValue = 0.0;
            double alphaValue = 0.0;
            double betaValue = 0.0;

            if (historyRecord != null && historyRecord.ClosedEyesResultId.HasValue)
            {
                try
                {
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
                thetaValue = brainwaveThetaValue;
                alphaValue = brainwaveAlphaValue;
                betaValue = brainwaveBetaValue;
            }

            DrawBrainwaveChartContent(BrainwaveChartCanvas, thetaValue, alphaValue, betaValue);
        }

        private async Task<TestResultData?> GetTestResultData(int resultId)
        {
            try
            {
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

        private void DrawBrainwaveChartContent(Canvas canvas, double thetaValue, double alphaValue, double betaValue)
        {
            canvas.Loaded += (s, e) =>
            {
                if (canvas.ActualWidth > 0 && canvas.ActualHeight > 0)
                {
                    DrawBrainwaveBars(canvas, thetaValue, alphaValue, betaValue);
                }
            };
            
            if (canvas.ActualWidth > 0 && canvas.ActualHeight > 0)
            {
                DrawBrainwaveBars(canvas, thetaValue, alphaValue, betaValue);
            }
        }

        private void DrawBrainwaveBars(Canvas canvas, double thetaValue, double alphaValue, double betaValue)
        {
            double width = canvas.ActualWidth;
            double height = canvas.ActualHeight;
            double chartHeight = height * 0.72;
            double chartTop = height * 0.18;

            canvas.Children.Clear();

            // 绘制网格线
            var gridBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            
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
            double barWidth = width * 0.08;
            double barSpacing = width * 0.15;
            double totalWidth = 3 * barWidth + 2 * barSpacing;
            double startX = (width - totalWidth) / 2;

            // 绘制三个柱子
            var values = new[] { thetaValue, alphaValue, betaValue };

            for (int i = 0; i < 3; i++)
            {
                double value = values[i];
                double x = startX + i * (barWidth + barSpacing);
                
                value = Math.Max(0, Math.Min(100, value));
                
                double barHeight = (value / 100.0) * chartHeight;
                double barY = chartTop + chartHeight - barHeight;

                Color barColor = GetHeatmapColor(value);

                Border bar = new Border
                {
                    Width = barWidth,
                    Height = barHeight,
                    Background = new SolidColorBrush(barColor),
                    CornerRadius = new CornerRadius(barWidth * 0.1),
                    Opacity = 0.6
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

                // 不显示底部标签，根据用户要求删除
            }

        }

        private Color GetHeatmapColor(double value)
        {
            if (value <= 50)
            {
                double ratio = value / 50.0;
                byte r = (byte)Math.Min(255, 180 * ratio + 100);
                byte g = (byte)Math.Min(255, 180 + 100);
                byte b = (byte)(100);
                return Color.FromRgb(r, g, b);
            }
            else
            {
                double ratio = (value - 50) / 50.0;
                byte r = (byte)Math.Min(255, 180 + 100);
                byte g = (byte)Math.Min(255, 180 * (1 - ratio) + 100);
                byte b = (byte)(100);
                return Color.FromRgb(r, g, b);
            }
        }

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
            switch (sourcePage)
            {
                case "TestHistoryPage":
                    NavigationManager.NavigateTo(new TestHistoryPage(currentTester));
                    break;
                case "TestPage":
                default:
                    NavigationManager.NavigateTo(new TestPage(currentTester));
                    break;
            }
        }

        private async void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 选择保存位置
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PDF文件 (*.pdf)|*.pdf",
                    FileName = $"AD风险检测报告_{currentTester?.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                    Title = "导出报告为PDF"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // 显示进度提示
                    var progressWindow = new Window
                    {
                        Title = "导出PDF",
                        Width = 300,
                        Height = 100,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = Window.GetWindow(this),
                        WindowStyle = WindowStyle.None,
                        ResizeMode = ResizeMode.NoResize,
                        Content = new StackPanel
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = "正在生成PDF，请稍候...",
                                    FontSize = 16,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Margin = new Thickness(0, 0, 0, 10)
                                },
                                new System.Windows.Controls.ProgressBar
                                {
                                    IsIndeterminate = true,
                                    Width = 200,
                                    Height = 20
                                }
                            }
                        }
                    };

                    progressWindow.Show();

                    try
                    {
                        await Task.Run(() => ExportToPdf(saveDialog.FileName));
                        
                        progressWindow.Close();
                        
                        var result = ModernMessageBoxWindow.ShowDialog(
                            $"PDF导出成功！\n保存位置：{saveDialog.FileName}\n\n是否立即打开？",
                            "导出成功",
                            ModernMessageBoxWindow.MessageBoxType.Success,
                            ModernMessageBoxWindow.MessageBoxButtons.YesNo);

                        if (result == ModernMessageBoxWindow.MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = saveDialog.FileName,
                                UseShellExecute = true
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        progressWindow.Close();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出PDF错误: {ex.Message}");
                ModernMessageBoxWindow.Show(
                    $"导出PDF失败：{ex.Message}",
                    "导出失败",
                    ModernMessageBoxWindow.MessageBoxType.Error);
            }
        }

        private void ExportToPdf(string filePath)
        {
            // 在UI线程上执行
            Dispatcher.Invoke(() =>
            {
                try
                {
                    // 创建一个用于打印的副本
                    FrameworkElement? printElement = ReportContent;
                    if (printElement == null) return;

                    // 确保BrainwaveChartCanvas已经完成绘制
                    if (BrainwaveChartCanvas.Children.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("BrainwaveChartCanvas未绘制，强制更新...");
                        BrainwaveChartCanvas.UpdateLayout();
                        // 给一点时间让Canvas完成绘制
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                            System.Windows.Threading.DispatcherPriority.Render,
                            new Action(() => { }));
                    }

                    // 创建XPS文档（临时）
                    string tempXpsPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"temp_{Guid.NewGuid()}.xps");
                    
                    try
                    {
                        // A4纸张尺寸 (210mm x 297mm)，使用更高DPI提高清晰度
                        // 使用150 DPI: (210mm x 297mm) = (1240 x 1754) 像素
                        double dpi = 150;
                        double pageWidth = (210 / 25.4) * dpi;   // 1240
                        double pageHeight = (297 / 25.4) * dpi;  // 1754
                        double marginMM = 16;  // 16mm边距
                        double margin = (marginMM / 25.4) * dpi;  // 转换为像素

                        // 计算可打印区域
                        double printableWidth = pageWidth - (margin * 2);
                        double printableHeight = pageHeight - (margin * 2);

                        // 创建打印票据
                        PrintTicket printTicket = new PrintTicket
                        {
                            PageMediaSize = new PageMediaSize(pageWidth, pageHeight),
                            PageOrientation = PageOrientation.Portrait
                        };

                        // 创建XPS文档
                        using (Package package = Package.Open(tempXpsPath, FileMode.Create))
                        {
                            using (XpsDocument xpsDocument = new XpsDocument(package))
                            {
                                XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
                                
                                // 创建可打印内容
                                FixedDocument document = new FixedDocument();
                                document.DocumentPaginator.PageSize = new Size(pageWidth, pageHeight);

                                // 创建页面
                                FixedPage page = new FixedPage
                                {
                                    Width = pageWidth,
                                    Height = pageHeight,
                                    Background = Brushes.White
                                };

                                // 创建一个容器来放置内容
                                Border contentBorder = new Border
                                {
                                    Width = printableWidth,
                                    Height = printableHeight,
                                    Background = Brushes.White
                                };

                                // 在克隆之前，确保BrainwaveChartCanvas已经渲染
                                if (BrainwaveChartCanvas != null)
                                {
                                    // 如果Canvas没有子元素或尺寸为0，强制重新绘制
                                    if (BrainwaveChartCanvas.Children.Count == 0 || 
                                        BrainwaveChartCanvas.ActualWidth == 0 || 
                                        BrainwaveChartCanvas.ActualHeight == 0)
                                    {
                                        BrainwaveChartCanvas.UpdateLayout();
                                        if (BrainwaveChartCanvas.ActualWidth > 0 && BrainwaveChartCanvas.ActualHeight > 0)
                                        {
                                            DrawBrainwaveBars(BrainwaveChartCanvas, brainwaveThetaValue, brainwaveAlphaValue, brainwaveBetaValue);
                                        }
                                    }
                                }

                                // 克隆打印元素
                                Grid printContent = CloneElementForPrint(printElement as Grid);
                                if (printContent != null)
                                {
                                    // 关键：设置最大宽度约束，让内容在可打印宽度内重新布局
                                    // 这样高度会自然增加，更好地填充页面
                                    printContent.MaxWidth = printableWidth;
                                    
                                    // 在宽度约束下测量，让内容自适应布局
                                    printContent.Measure(new Size(printableWidth, double.PositiveInfinity));
                                    double actualWidth = printContent.DesiredSize.Width;
                                    double actualHeight = printContent.DesiredSize.Height;
                                    
                                    // 计算需要的缩放比例
                                    double scaleX = printableWidth / actualWidth;
                                    double scaleY = printableHeight / actualHeight;
                                    
                                    // 新策略：内容已经在宽度约束下重新布局了
                                    // 现在优先用高度来填充页面
                                    double scale;
                                    
                                    // 智能缩放策略：宽度已经约束好了，优先用高度填充
                                    if (scaleX >= 0.95 && scaleY > scaleX)
                                    {
                                        // 宽度已经接近满格（>=95%），高度还有空间，用高度填充
                                        // 限制最大缩放到1.4倍以保持清晰度
                                        scale = Math.Min(scaleY, 1.4);
                                    }
                                    else if (scaleX < 1.0 || scaleY < 1.0)
                                    {
                                        // 有任一维度需要缩小，使用较小的比例
                                        scale = Math.Min(scaleX, scaleY);
                                    }
                                    else
                                    {
                                        // 两个维度都可以放大，取较小的保证不超出
                                        scale = Math.Min(Math.Min(scaleX, scaleY), 1.4);
                                    }
                                    
                                    // 应用缩放
                                    printContent.LayoutTransform = new ScaleTransform(scale, scale);
                                    
                                    contentBorder.Child = printContent;
                                }

                                // 将内容放置在页面上（带边距）
                                FixedPage.SetLeft(contentBorder, margin);
                                FixedPage.SetTop(contentBorder, margin);
                                page.Children.Add(contentBorder);

                                // 强制布局更新
                                page.Measure(new Size(pageWidth, pageHeight));
                                page.Arrange(new Rect(0, 0, pageWidth, pageHeight));
                                page.UpdateLayout();

                                // 添加页面到文档
                                PageContent pageContent = new PageContent();
                                ((IAddChild)pageContent).AddChild(page);
                                document.Pages.Add(pageContent);

                                // 写入XPS
                                writer.Write(document, printTicket);
                            }
                        }

                        // 转换XPS为PDF
                        ConvertXpsToPdf(tempXpsPath, filePath);
                    }
                    finally
                    {
                        // 清理临时XPS文件
                        try
                        {
                            if (System.IO.File.Exists(tempXpsPath))
                                System.IO.File.Delete(tempXpsPath);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ExportToPdf错误: {ex.Message}\n{ex.StackTrace}");
                    throw;
                }
            });
        }

        private Grid? CloneElementForPrint(Grid? originalGrid)
        {
            if (originalGrid == null) return null;

            try
            {
                // 创建新的Grid用于打印
                Grid printGrid = new Grid
                {
                    Background = Brushes.White
                };

                // 复制列定义
                foreach (var colDef in originalGrid.ColumnDefinitions)
                {
                    printGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = colDef.Width });
                }

                // 复制行定义
                foreach (var rowDef in originalGrid.RowDefinitions)
                {
                    printGrid.RowDefinitions.Add(new RowDefinition { Height = rowDef.Height });
                }

                // 递归复制子元素（简化版，仅复制主要可视元素）
                CopyChildren(originalGrid, printGrid);

                return printGrid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CloneElementForPrint错误: {ex.Message}");
                return null;
            }
        }

        private void CopyChildren(Panel source, Panel target)
        {
            foreach (UIElement child in source.Children)
            {
                try
                {
                    UIElement? clonedChild = CloneUIElement(child);
                    if (clonedChild != null)
                    {
                        // 复制Grid附加属性
                        if (Grid.GetRow(child) > 0) Grid.SetRow(clonedChild, Grid.GetRow(child));
                        if (Grid.GetColumn(child) > 0) Grid.SetColumn(clonedChild, Grid.GetColumn(child));
                        if (Grid.GetRowSpan(child) > 1) Grid.SetRowSpan(clonedChild, Grid.GetRowSpan(child));
                        if (Grid.GetColumnSpan(child) > 1) Grid.SetColumnSpan(clonedChild, Grid.GetColumnSpan(child));
                        
                        target.Children.Add(clonedChild);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PDF] 克隆子元素错误: {ex.Message}");
                }
            }
        }

        private UIElement? CloneUIElement(UIElement element)
        {
            // 简化的克隆方法，仅处理常见控件
            if (element is TextBlock textBlock)
            {
                // PDF导出时保持原始字体大小，通过整体缩放来调整
                return new TextBlock
                {
                    Text = textBlock.Text,
                    FontSize = textBlock.FontSize,
                    FontWeight = textBlock.FontWeight,
                    Foreground = textBlock.Foreground,
                    HorizontalAlignment = textBlock.HorizontalAlignment,
                    VerticalAlignment = textBlock.VerticalAlignment,
                    Margin = textBlock.Margin,
                    TextWrapping = textBlock.TextWrapping
                };
            }
            else if (element is Border border)
            {
                // 调整负margin以避免PDF布局问题
                Thickness adjustedMargin = border.Margin;
                // 特殊处理：如果是蓝色分割线（Height=2且VerticalAlignment=Bottom），调整margin让它位于中间
                if (border.Height == 2 && border.VerticalAlignment == VerticalAlignment.Bottom && adjustedMargin.Bottom < 0)
                {
                    // 负的Bottom margin让横条往下移，正的让它往上移
                    adjustedMargin.Bottom = -10; // 从-10改为-5，让横条位于"生成时间"和区域1之间
                }
                else
                {
                    // 其他Border：将负margin设为0，避免布局错乱
                    if (adjustedMargin.Left < 0) adjustedMargin.Left = 0;
                    if (adjustedMargin.Top < 0) adjustedMargin.Top = 0;
                    if (adjustedMargin.Right < 0) adjustedMargin.Right = 0;
                    if (adjustedMargin.Bottom < 0) adjustedMargin.Bottom = 0;
                }
                
                Border newBorder = new Border
                {
                    BorderBrush = border.BorderBrush,
                    BorderThickness = border.BorderThickness,
                    Background = border.Background,
                    CornerRadius = border.CornerRadius,
                    Padding = border.Padding,
                    Margin = adjustedMargin,
                    HorizontalAlignment = border.HorizontalAlignment,
                    VerticalAlignment = border.VerticalAlignment
                };

                // 如果原始元素有明确的宽高，则保留
                if (!double.IsNaN(border.Width) && border.Width > 0)
                    newBorder.Width = border.Width;
                if (!double.IsNaN(border.Height) && border.Height > 0)
                    newBorder.Height = border.Height;

                if (border.Child is Panel panel)
                {
                    Panel? newPanel = ClonePanel(panel);
                    if (newPanel != null)
                        newBorder.Child = newPanel;
                }
                else if (border.Child != null)
                {
                    UIElement? clonedChild = CloneUIElement(border.Child);
                    if (clonedChild != null)
                        newBorder.Child = clonedChild;
                }

                return newBorder;
            }
            else if (element is Canvas canvas)
            {
                try
                {
                    // 确保Canvas有有效的尺寸
                    double width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 
                                   (!double.IsNaN(canvas.Width) && canvas.Width > 0 ? canvas.Width : 400);
                    double height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 
                                    (!double.IsNaN(canvas.Height) && canvas.Height > 0 ? canvas.Height : 200);
                    
                    if (width < 10 || height < 10)
                    {
                        return null;
                    }
                    
                    // 特殊处理：如果是BrainwaveChartCanvas，确保已绘制或重新绘制
                    if (canvas.Name == "BrainwaveChartCanvas" && canvas.Children.Count == 0)
                    {
                        // 创建一个新的Canvas用于绘制
                        Canvas newCanvas = new Canvas
                        {
                            Width = width,
                            Height = height,
                            Background = canvas.Background
                        };
                        
                        newCanvas.Measure(new Size(width, height));
                        newCanvas.Arrange(new Rect(0, 0, width, height));
                        newCanvas.UpdateLayout();
                        
                        DrawBrainwaveBars(newCanvas, brainwaveThetaValue, brainwaveAlphaValue, brainwaveBetaValue);
                        canvas = newCanvas;
                    }
                    
                    // 在UI线程确保Canvas已经渲染
                    if (!canvas.IsArrangeValid || !canvas.IsMeasureValid)
                    {
                        canvas.Measure(new Size(width, height));
                        canvas.Arrange(new Rect(0, 0, width, height));
                        canvas.UpdateLayout();
                    }
                    
                    // 确保渲染完成
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Render,
                        new Action(() => { }));
                    
                    // 使用更高DPI的RenderTargetBitmap捕获Canvas内容以提高清晰度
                    double dpi = 150;  // 使用150 DPI提高清晰度
                    RenderTargetBitmap rtb = new RenderTargetBitmap(
                        (int)Math.Ceiling(width * dpi / 96), 
                        (int)Math.Ceiling(height * dpi / 96),
                        dpi, dpi, PixelFormats.Pbgra32);
                    
                    rtb.Render(canvas);

                    Image image = new Image
                    {
                        Source = rtb,
                        Width = width,
                        Height = height,
                        Stretch = Stretch.Fill
                    };

                    return image;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Canvas渲染失败: {ex.Message}");
                    return null;
                }
            }
            else if (element is Panel panel)
            {
                return ClonePanel(panel);
            }

            return null;
        }

        private Panel? ClonePanel(Panel panel)
        {
            Panel? newPanel = null;

            if (panel is Grid grid)
            {
                Grid newGrid = new Grid
                {
                    Background = grid.Background,
                    Margin = grid.Margin,
                    HorizontalAlignment = grid.HorizontalAlignment,
                    VerticalAlignment = grid.VerticalAlignment
                };

                // 保留宽高设置
                if (!double.IsNaN(grid.Width) && grid.Width > 0)
                    newGrid.Width = grid.Width;
                if (!double.IsNaN(grid.Height) && grid.Height > 0)
                    newGrid.Height = grid.Height;

                foreach (var colDef in grid.ColumnDefinitions)
                    newGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = colDef.Width });

                foreach (var rowDef in grid.RowDefinitions)
                    newGrid.RowDefinitions.Add(new RowDefinition { Height = rowDef.Height });

                newPanel = newGrid;
            }
            else if (panel is StackPanel stackPanel)
            {
                newPanel = new StackPanel
                {
                    Orientation = stackPanel.Orientation,
                    Background = stackPanel.Background,
                    Margin = stackPanel.Margin,
                    HorizontalAlignment = stackPanel.HorizontalAlignment,
                    VerticalAlignment = stackPanel.VerticalAlignment
                };
            }

            if (newPanel != null)
            {
                CopyChildren(panel, newPanel);
            }

            return newPanel;
        }

        private void ConvertXpsToPdf(string xpsPath, string pdfPath)
        {
            // 使用PdfSharp将XPS转换为PDF
            try
            {
                using (XpsDocument xpsDoc = new XpsDocument(xpsPath, FileAccess.Read))
                {
                    FixedDocumentSequence? docSeq = xpsDoc.GetFixedDocumentSequence();
                    if (docSeq == null)
                    {
                        throw new Exception("无法读取XPS文档");
                    }

                    // 创建PDF文档
                    PdfSharp.Pdf.PdfDocument pdfDocument = new PdfSharp.Pdf.PdfDocument();
                    pdfDocument.Info.Title = "AD风险检测报告";
                    pdfDocument.Info.Author = "深圳神溯未来科技有限公司";
                    pdfDocument.Info.Creator = "脑镜BrainMirror";

                    foreach (DocumentReference docRef in docSeq.References)
                    {
                        FixedDocument? doc = docRef.GetDocument(false);
                        if (doc != null)
                        {
                            foreach (PageContent pageContent in doc.Pages)
                            {
                                FixedPage? fixedPage = pageContent.GetPageRoot(false);
                                if (fixedPage != null)
                                {
                                    // 添加PDF页面
                                    PdfSharp.Pdf.PdfPage pdfPage = pdfDocument.AddPage();
                                    pdfPage.Width = PdfSharp.Drawing.XUnit.FromPoint(fixedPage.Width * 0.75); // 转换为点
                                    pdfPage.Height = PdfSharp.Drawing.XUnit.FromPoint(fixedPage.Height * 0.75);

                                    // 使用更高DPI渲染页面为图像以提高清晰度
                                    double renderDpi = 150;  // 使用150 DPI
                                    RenderTargetBitmap rtb = new RenderTargetBitmap(
                                        (int)(fixedPage.Width * renderDpi / 96),
                                        (int)(fixedPage.Height * renderDpi / 96),
                                        renderDpi, renderDpi,
                                        PixelFormats.Pbgra32);
                                    
                                    rtb.Render(fixedPage);

                                    // 转换为PNG
                                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(rtb));

                                    using (MemoryStream ms = new MemoryStream())
                                    {
                                        encoder.Save(ms);
                                        ms.Position = 0;

                                        // 创建PdfSharp图像
                                        PdfSharp.Drawing.XImage img = PdfSharp.Drawing.XImage.FromStream(ms);

                                        // 绘制到PDF页面
                                        using (PdfSharp.Drawing.XGraphics gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(pdfPage))
                                        {
                                            gfx.DrawImage(img, 0, 0, pdfPage.Width, pdfPage.Height);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // 保存PDF
                    pdfDocument.Save(pdfPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ConvertXpsToPdf错误: {ex.Message}\n{ex.StackTrace}");
                throw new Exception($"PDF转换失败: {ex.Message}", ex);
            }
        }
    }
}
