using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BrainMonitor.Views
{
    public partial class TestPage : UserControl, INavigationAware
    {
        public Tester CurrentTester { get; set; }
        private DispatcherTimer? brainwaveTimer;
        private Random random = new Random();
        private double currentX = 0;

        public TestPage(Tester tester)
        {
            InitializeComponent();
            CurrentTester = tester;
            DataContext = this;
            
            // 初始化时禁用设备列表和连接按钮
            DeviceComboBox.IsEnabled = false;
            ConnectDeviceButton.IsEnabled = false;
            
            // 页面加载完成后初始化画布
            this.Loaded += TestPage_Loaded;
        }
        
        private void TestPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化画布显示栅格和绿色虚线
            InitializeCanvas();
        }
        
        private void InitializeCanvas()
        {
            // 清空画布并设置背景
            BrainwaveCanvas.Children.Clear();
            BrainwaveCanvas.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // 浅灰色背景
            
            // 绘制网格和标准线
            DrawGridAndBaseline();
        }
        
        private void ClearWaveformData()
        {
            // 只清除波形数据，保留栅格和基线
            var itemsToRemove = new List<UIElement>();
            
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                string tag = element.GetValue(FrameworkElement.TagProperty)?.ToString() ?? "";
                if (tag == "waveform")
                {
                    itemsToRemove.Add(element);
                }
            }
            
            foreach (var item in itemsToRemove)
            {
                BrainwaveCanvas.Children.Remove(item);
            }
        }

        public void OnNavigatedTo()
        {
            // 页面导航到时的处理
        }

        public void OnNavigatedFrom()
        {
            // 页面离开时的处理
            brainwaveTimer?.Stop();
        }

        private void ScanDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            // 模拟扫描设备
            DeviceComboBox.Items.Clear();
            DeviceComboBox.Items.Add("脑电设备 A");
            DeviceComboBox.Items.Add("脑电设备 B");
            DeviceComboBox.Items.Add("脑电设备 C");
            
            // 启用设备列表
            DeviceComboBox.IsEnabled = true;
            
            MessageBox.Show("设备扫描完成！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 当选择设备时，启用连接按钮
            ConnectDeviceButton.IsEnabled = DeviceComboBox.SelectedItem != null;
        }

        private void ConnectDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceComboBox.SelectedItem == null)
            {
                MessageBox.Show("请先选择一个设备", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"已连接到 {DeviceComboBox.SelectedItem}", "连接成功", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // 开始模拟脑电波显示
            StartBrainwaveSimulation();
        }

        private void StartBrainwaveSimulation()
        {
            // 清除之前的波形数据，但保留栅格和基线
            ClearWaveformData();
            
            currentX = BrainwaveCanvas.ActualWidth; // 从右侧开始

            // 创建定时器模拟实时脑电波
            brainwaveTimer = new DispatcherTimer();
            brainwaveTimer.Interval = TimeSpan.FromMilliseconds(50);
            brainwaveTimer.Tick += (s, e) =>
            {
                // 移动所有现有的脑电波线条向左
                MoveWaveformLeft();
                
                // 生成新的脑电波数据点
                double amplitude = random.NextDouble() * 80 + 40; // 40-120的振幅
                double baselineY = BrainwaveCanvas.ActualHeight / 2;
                double y = baselineY + Math.Sin(DateTime.Now.Millisecond * 0.01) * amplitude * 0.4;
                
                // 在最右侧添加新的数据点
                if (BrainwaveCanvas.Children.Count > 0)
                {
                    // 找到最后一个脑电波点的Y坐标
                    double lastY = GetLastWaveformY();
                    
                    Line line = new Line
                    {
                        X1 = currentX - 5,
                        Y1 = lastY,
                        X2 = currentX,
                        Y2 = y,
                        Stroke = new SolidColorBrush(Color.FromRgb(0, 120, 215)), // 蓝色
                        StrokeThickness = 2,
                        Tag = "waveform" // 标记为脑电波线条
                    };
                    BrainwaveCanvas.Children.Add(line);
                }
                else
                {
                    // 第一个点
                    Ellipse point = new Ellipse
                    {
                        Width = 2,
                        Height = 2,
                        Fill = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                        Tag = "waveform"
                    };
                    Canvas.SetLeft(point, currentX - 1);
                    Canvas.SetTop(point, y - 1);
                    BrainwaveCanvas.Children.Add(point);
                }
                
                // 清理超出左边界的线条
                CleanupLeftBoundary();
            };
            brainwaveTimer.Start();
        }
        
        private void DrawGridAndBaseline()
        {
            double width = BrainwaveCanvas.ActualWidth;
            double height = BrainwaveCanvas.ActualHeight;
            
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
                    StrokeThickness = 0.5,
                    Tag = "grid"
                };
                BrainwaveCanvas.Children.Add(gridLine);
            }
            
            // 水平网格线
            for (double y = 0; y <= height; y += 30)
            {
                Line gridLine = new Line
                {
                    X1 = 0, Y1 = y,
                    X2 = width, Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 0.5,
                    Tag = "grid"
                };
                BrainwaveCanvas.Children.Add(gridLine);
            }
            
            // 绘制绿色虚线标准值（中心线）
            Line baseline = new Line
            {
                X1 = 0, Y1 = height / 2,
                X2 = width, Y2 = height / 2,
                Stroke = new SolidColorBrush(Color.FromRgb(0, 150, 0)), // 绿色
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 }, // 虚线样式
                Tag = "baseline"
            };
            BrainwaveCanvas.Children.Add(baseline);
        }
        
        private void MoveWaveformLeft()
        {
            var itemsToRemove = new List<UIElement>();
            
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                if (element.GetValue(FrameworkElement.TagProperty)?.ToString() == "waveform")
                {
                    if (element is Line line)
                    {
                        line.X1 -= 5;
                        line.X2 -= 5;
                        
                        // 如果线条完全移出左边界，标记为删除
                        if (line.X2 < 0)
                        {
                            itemsToRemove.Add(element);
                        }
                    }
                    else if (element is Ellipse ellipse)
                    {
                        double left = Canvas.GetLeft(ellipse) - 5;
                        Canvas.SetLeft(ellipse, left);
                        
                        if (left < -ellipse.Width)
                        {
                            itemsToRemove.Add(element);
                        }
                    }
                }
            }
            
            // 删除超出边界的元素
            foreach (var item in itemsToRemove)
            {
                BrainwaveCanvas.Children.Remove(item);
            }
        }
        
        private double GetLastWaveformY()
        {
            double lastY = BrainwaveCanvas.ActualHeight / 2; // 默认中心线
            
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                if (element.GetValue(FrameworkElement.TagProperty)?.ToString() == "waveform" && element is Line line)
                {
                    if (line.X2 >= currentX - 10) // 找最近的线条
                    {
                        lastY = line.Y2;
                    }
                }
            }
            
            return lastY;
        }
        
        private void CleanupLeftBoundary()
        {
            var itemsToRemove = new List<UIElement>();
            
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                if (element.GetValue(FrameworkElement.TagProperty)?.ToString() == "waveform")
                {
                    if (element is Line line && line.X2 < -10)
                    {
                        itemsToRemove.Add(element);
                    }
                    else if (element is Ellipse ellipse && Canvas.GetLeft(ellipse) < -ellipse.Width - 10)
                    {
                        itemsToRemove.Add(element);
                    }
                }
            }
            
            foreach (var item in itemsToRemove)
            {
                BrainwaveCanvas.Children.Remove(item);
            }
        }

        private void StartTestButton_Click(object sender, RoutedEventArgs e)
        {
            // 开始测试的逻辑
            MessageBox.Show("开始脑电测试", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // 这里可以添加具体的测试开始逻辑
            // 例如：开始数据采集、启动测试流程等
        }

        private void GetReportButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(MocaScoreTextBox.Text) || string.IsNullOrWhiteSpace(MmseScoreTextBox.Text))
            {
                MessageBox.Show("请输入MOCA和MMSE评分", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(MocaScoreTextBox.Text, out double mocaScore) || mocaScore < 0 || mocaScore > 30)
            {
                MessageBox.Show("MOCA评分必须是0-30之间的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(MmseScoreTextBox.Text, out double mmseScore) || mmseScore < 0 || mmseScore > 30)
            {
                MessageBox.Show("MMSE评分必须是0-30之间的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 停止脑电波模拟
            brainwaveTimer?.Stop();

            // 导航到报告页面
            NavigationManager.NavigateTo(new ReportPage(CurrentTester, mocaScore, mmseScore));
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 停止脑电波模拟
            brainwaveTimer?.Stop();
            
            // 返回到医护人员操作页面
            NavigationManager.NavigateTo(new MedicalStaffPage());
        }
    }
}