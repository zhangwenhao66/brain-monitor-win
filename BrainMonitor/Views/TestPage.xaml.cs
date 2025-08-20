using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using BrainMonitor.SDK;
using System.Runtime.InteropServices;
using System.IO.Ports;

namespace BrainMonitor.Views
{
    public partial class TestPage : UserControl, INavigationAware
    {
        public Tester CurrentTester { get; set; }
        private double currentX = 0;
        private bool sdkInitialized = false;
        private bool isScanning = false; // 防止重复扫描的标志
        private int scanFailureCount = 0; // 扫描失败计数器
        private const int MAX_SCAN_FAILURES = 1; // 最大失败次数，超过后进行完全重置
        
        // 测试状态管理
        private bool isTestStarted = false; // 是否已开始测试
        private bool isTestCompleted = false; // 是否已完成测试流程
        
        // 数据缓冲相关变量
        private Queue<int[]> dataBuffer = new Queue<int[]>();
        private DispatcherTimer displayTimer;
        private readonly object bufferLock = new object();
        private List<DeviceInfo> scannedDevices = new List<DeviceInfo>();
        private List<DeviceInfo> connectedDevices = new List<DeviceInfo>();
        
        // 时间坐标相关变量
        private DateTime startTime = DateTime.Now;
        private bool isDeviceConnected = false;
        private const double DISPLAY_WINDOW_SECONDS = 8.0; // 显示8秒窗口
        private DispatcherTimer? timeUpdateTimer; // 时间标签更新定时器
        
        // 回调函数引用，防止被垃圾回收
        private RawDataCallback? rawDataCallback;
        private PostDataCallback? postDataCallback;
        private BattInfoCallback? battInfoCallback;
        private EventCallback? eventCallback;
        
        // 数据接收统计变量
        private DateTime lastDataReceiveTime = DateTime.MinValue;
        private int totalDataPointsReceived = 0;
        private int dataReceiveCount = 0;
        private DateTime firstDataReceiveTime = DateTime.MinValue;
        
        // 显示更新频率控制
        private const int DISPLAY_UPDATE_INTERVAL_MS = 20; // 50Hz显示更新频率，减少显示延迟
        
        // 数据波动监控相关变量
        private Queue<double> recentDataPoints = new Queue<double>(); // 存储最近5秒的数据点
        private DateTime lastDataPointTime = DateTime.MinValue;
        private bool isWaveformRed = false; // 当前曲线是否为红色
        private const double FLUCTUATION_THRESHOLD = 200.0; // 波动阈值（微伏）
        private const double DATA_WINDOW_SECONDS = 5.0; // 数据窗口时间（秒）
        private DispatcherTimer zeroDataTimer; // 显示0值数据的定时器
        private bool isShowingZeroData = false; // 是否正在显示0值数据
        
        private void InitializeDisplayTimer()
        {
            displayTimer = new DispatcherTimer();
            displayTimer.Interval = TimeSpan.FromMilliseconds(DISPLAY_UPDATE_INTERVAL_MS);
            displayTimer.Tick += DisplayTimer_Tick;
            
            // 初始化0值数据显示定时器
            zeroDataTimer = new DispatcherTimer();
            zeroDataTimer.Interval = TimeSpan.FromMilliseconds(100); // 每100ms显示一个0值数据点
            zeroDataTimer.Tick += ZeroDataTimer_Tick;
        }
        
        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            // 处理缓冲队列中的数据
            List<int[]> dataToProcess = new List<int[]>();
            
            lock (bufferLock)
            {
                // 限制每次处理的数据包数量，避免UI阻塞
                int maxPacketsPerTick = 5;
                int processedCount = 0;
                
                while (dataBuffer.Count > 0 && processedCount < maxPacketsPerTick)
                {
                    dataToProcess.Add(dataBuffer.Dequeue());
                    processedCount++;
                }
            }
            
            // 更新显示
            foreach (var data in dataToProcess)
            {
                UpdateBrainwaveDisplay(data);
            }
            
            // 如果还有数据待处理，继续在下一个周期处理
            // (移除了调试输出以减少日志噪音)
        }
        
        private void ZeroDataTimer_Tick(object sender, EventArgs e)
        {
            // 显示0值数据点
            if (isShowingZeroData)
            {
                // 计算从上次tick到现在应该产生的数据点数量
                // 假设采样率为520Hz，每100ms应该产生约52个数据点
                // 为了保持平滑移动，我们生成足够的数据点
                int dataPointsPerTick = 52; // 每100ms约52个数据点
                int[] zeroData = new int[dataPointsPerTick];
                for (int i = 0; i < dataPointsPerTick; i++)
                {
                    zeroData[i] = 0;
                }
                UpdateBrainwaveDisplay(zeroData);
            }
        }
        
        private void CalculateDataFluctuation(double microVoltValue)
        {
            DateTime currentTime = DateTime.Now;
            
            // 添加新数据点
            recentDataPoints.Enqueue(microVoltValue);
            
            // 移除超过5秒的旧数据点
            while (recentDataPoints.Count > 0)
            {
                // 估算数据点时间（假设采样率为520Hz）
                double estimatedAge = recentDataPoints.Count / 520.0;
                if (estimatedAge > DATA_WINDOW_SECONDS)
                {
                    recentDataPoints.Dequeue();
                }
                else
                {
                    break;
                }
            }
            
            // 计算波动值（最大值 - 最小值）
            if (recentDataPoints.Count > 10) // 至少需要一些数据点才能计算波动
            {
                double maxValue = recentDataPoints.Max();
                double minValue = recentDataPoints.Min();
                double fluctuation = maxValue - minValue;
                
                // 更新曲线颜色状态
                bool shouldBeRed = fluctuation > FLUCTUATION_THRESHOLD;
                if (shouldBeRed != isWaveformRed)
                {
                    isWaveformRed = shouldBeRed;
                    // 更新现有曲线的颜色
                    UpdateWaveformColor();
                }
            }
        }
        
        private void UpdateWaveformColor()
        {
            // 更新画布中所有波形元素的颜色
            Color waveformColor = isWaveformRed ? Color.FromRgb(220, 50, 50) : Color.FromRgb(0, 120, 215);
            SolidColorBrush waveformBrush = new SolidColorBrush(waveformColor);
            
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                if (element is FrameworkElement fe && fe.Tag?.ToString() == "waveform")
                {
                    if (element is Line line)
                    {
                        line.Stroke = waveformBrush;
                    }
                    else if (element is Ellipse ellipse)
                    {
                        ellipse.Fill = waveformBrush;
                    }
                }
            }
        }

        public TestPage(Tester tester)
        {
            InitializeComponent();
            CurrentTester = tester;
            DataContext = this;
            
            // 初始化测试状态
            isTestStarted = false;
            isTestCompleted = false;
            
            // 初始化时禁用设备列表和连接按钮
            DeviceComboBox.IsEnabled = false;
            ConnectDeviceButton.IsEnabled = false;
            
            // 初始化时禁用开始测试按钮，只有连接设备后才启用
            StartTestButton.IsEnabled = false;
            
            // 初始化时禁用生成报告按钮，只有完成测试流程后才启用
            UpdateGenerateReportButtonState();
            
            // 初始化SDK
            InitializeSDK();
            
            // 初始化显示定时器
            InitializeDisplayTimer();
            
            // 页面加载完成后初始化画布
            this.Loaded += TestPage_Loaded;
        }
        
        private void StartTimeUpdateTimer()
        {
            // 停止现有的定时器
            StopTimeUpdateTimer();
            
            // 创建新的时间更新定时器
            timeUpdateTimer = new DispatcherTimer();
            timeUpdateTimer.Interval = TimeSpan.FromMilliseconds(100); // 每100毫秒更新一次
            timeUpdateTimer.Tick += (s, e) =>
            {
                DrawTimeLabels();
            };
            timeUpdateTimer.Start();
        }
        
        private void StopTimeUpdateTimer()
        {
            if (timeUpdateTimer != null)
            {
                timeUpdateTimer.Stop();
                timeUpdateTimer = null;
            }
        }
        
        private void DrawTimeLabels()
        {
            // 清除之前的时间标签
            XAxisLabelsCanvas.Children.Clear();
            
            double width = BrainwaveCanvas.ActualWidth;
            if (width <= 0) return;
            
            // 计算当前时间偏移
            double currentTimeOffset = (DateTime.Now - startTime).TotalSeconds;
            
            // 使用HashSet来避免重复的时间标签
            HashSet<string> addedTimeLabels = new HashSet<string>();
            
            // 绘制时间刻度（每秒一个标签）
            for (int i = 0; i <= (int)DISPLAY_WINDOW_SECONDS; i++)
            {
                double timeInSeconds = -DISPLAY_WINDOW_SECONDS + i + currentTimeOffset;
                double x = (i / DISPLAY_WINDOW_SECONDS) * width;
                
                // 将时间四舍五入到最近的整数秒，避免浮点数精度问题
                int roundedTimeInSeconds = (int)Math.Round(timeInSeconds);
                
                // 格式化时间显示
                string timeText;
                if (roundedTimeInSeconds < 0)
                {
                    TimeSpan negativeTime = TimeSpan.FromSeconds(-roundedTimeInSeconds);
                    timeText = $"-{negativeTime.Hours:00}:{negativeTime.Minutes:00}:{negativeTime.Seconds:00}";
                }
                else
                {
                    TimeSpan positiveTime = TimeSpan.FromSeconds(roundedTimeInSeconds);
                    timeText = $"{positiveTime.Hours:00}:{positiveTime.Minutes:00}:{positiveTime.Seconds:00}";
                }
                
                // 检查是否已经添加过这个时间标签
                if (addedTimeLabels.Contains(timeText))
                {
                    // 如果已经存在相同的时间标签，只绘制刻度线，不绘制文字标签
                    Line duplicateTickLine = new Line
                    {
                        X1 = x, Y1 = 0,
                        X2 = x, Y2 = 8,
                        Stroke = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                        StrokeThickness = 1,
                        Tag = "time_tick"
                    };
                    XAxisLabelsCanvas.Children.Add(duplicateTickLine);
                    continue;
                }
                
                // 添加到已使用的时间标签集合中
                addedTimeLabels.Add(timeText);
                
                // 创建时间标签
                TextBlock timeLabel = new TextBlock
                {
                    Text = timeText,
                    FontSize = 10,
                    FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Tag = "time_label"
                };
                
                // 计算标签位置，确保不超出画布边界
                double labelWidth = 40; // 估算标签宽度
                double labelLeft = Math.Max(0, Math.Min(width - labelWidth, x - 25));
                Canvas.SetLeft(timeLabel, labelLeft);
                Canvas.SetTop(timeLabel, 5);
                XAxisLabelsCanvas.Children.Add(timeLabel);
                
                // 绘制时间刻度线
                Line tickLine = new Line
                {
                    X1 = x, Y1 = 0,
                    X2 = x, Y2 = 8,
                    Stroke = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    StrokeThickness = 1,
                    Tag = "time_tick"
                };
                XAxisLabelsCanvas.Children.Add(tickLine);
            }
        }
        
        private void TestPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化画布显示栅格和绿色虚线
            InitializeCanvas();
            
            // 初始化端口列表
            RefreshPortList();
        }
        
        private void InitializeCanvas()
        {
            // 清空画布并设置背景
            BrainwaveCanvas.Children.Clear();
            BrainwaveCanvas.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)); // 浅灰色背景
            
            // 绘制网格和标准线
            DrawGridAndBaseline();
        }
        
        private void RefreshPortList()
        {
            try
            {
                // 获取当前选中的端口
                string selectedPort = PortComboBox.SelectedItem?.ToString();
                
                // 清空端口列表
                PortComboBox.Items.Clear();
                
                // 获取系统中所有可用的串口
                string[] availablePorts = SerialPort.GetPortNames();
                
                
                if (availablePorts.Length > 0)
                {
                    // 添加可用端口到列表
                    foreach (string port in availablePorts)
                    {
                        PortComboBox.Items.Add(port);
                    }
                    
                    // 尝试恢复之前选中的端口
                    if (!string.IsNullOrEmpty(selectedPort) && PortComboBox.Items.Contains(selectedPort))
                    {
                        PortComboBox.SelectedItem = selectedPort;
                    }
                    else if (PortComboBox.Items.Count > 0)
                    {
                        // 默认选择第一个端口
                        PortComboBox.SelectedIndex = 0;
                    }
                }
                // 如果没有找到串口，保持列表为空
            }
            catch (Exception ex)
            {
                // 如果获取端口失败，显示错误信息
                PortComboBox.Items.Clear();
                PortComboBox.Items.Add($"获取端口失败: {ex.Message}");
                PortComboBox.SelectedIndex = 0;
                
                // 显示详细错误信息给用户
                Dispatcher.Invoke(() => MessageBox.Show($"获取串口列表失败：\n{ex.Message}\n\n可能的原因：\n1. 系统权限不足\n2. 串口驱动程序问题\n3. 系统资源不足", 
                    "端口获取错误", MessageBoxButton.OK, MessageBoxImage.Warning));
            }
        }
        
        private void InitializeSDK()
        {
            try
            {
                
                // 检查DLL是否可用
                if (!BrainMonitorSDK.IsDllAvailable)
                {
                    Dispatcher.Invoke(() => MessageBox.Show("BrainMonitorSDK.dll不可用，无法使用设备功能", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }
                
                int result = BrainMonitorSDK.SDK_Init();
                
                if (result == 1)
                {
                    sdkInitialized = true;
                    
                    // 设置回调函数
                    rawDataCallback = OnRawDataReceived;
                    postDataCallback = OnPostDataReceived;
                    battInfoCallback = OnBatteryInfoReceived;
                    eventCallback = OnEventReceived;
                    
                    BrainMonitorSDK.SDK_SetRawDataCallback(rawDataCallback);
                    BrainMonitorSDK.SDK_SetPostDataCallback(postDataCallback);
                    BrainMonitorSDK.SDK_SetBattInfoCallback(battInfoCallback);
                    BrainMonitorSDK.SDK_SetEventCallback(eventCallback);
                    
                    // 检查端口并重试连接
                    string port = BrainMonitorSDK.CheckPortString();
                    
                    // 如果SDK检测不到端口，尝试使用UI中选中的端口
                    if (string.IsNullOrEmpty(port))
                    {
                        string selectedPort = null;
                        
                        // 在UI线程中获取选中的端口
                        Dispatcher.Invoke(() => {
                            selectedPort = PortComboBox.SelectedItem?.ToString();
                        });
                        
                        if (!string.IsNullOrEmpty(selectedPort))
                        {
                            port = selectedPort;
                        }
                        else
                        {
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(port))
                    {
                        // 重试端口连接，最多3次
                        int connectResult = 0;
                        for (int retry = 0; retry < 3; retry++)
                        {
                            try
                            {
                                connectResult = BrainMonitorSDK.SDK_ConnectPort(port);
                                
                                if (connectResult == 1)
                                {
                                    break; // 连接成功，退出重试循环
                                }
                                else
                                {
                                    System.Threading.Thread.Sleep(200); // 减少等待时间到200ms
                                }
                            }
                            catch (Exception portEx)
                            {
                                System.Threading.Thread.Sleep(200); // 减少等待时间到200ms
                            }
                        }
                        
                        if (connectResult != 1)
                        {
                        }
                    }
                }
                else
                {
                    MessageBox.Show($"SDK初始化失败，返回值: {result}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"SDK初始化异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OnRawDataReceived(int dev, int chan, IntPtr data, int len)
        {
            try
            {
                DateTime currentTime = DateTime.Now;
                
                // 统计数据接收情况
                dataReceiveCount++;
                totalDataPointsReceived += len;
                
                if (firstDataReceiveTime == DateTime.MinValue)
                {
                    firstDataReceiveTime = currentTime;
                    lastDataReceiveTime = currentTime;
                }
                
                // 计算接收间隔
                double intervalMs = 0;
                if (lastDataReceiveTime != DateTime.MinValue)
                {
                    intervalMs = (currentTime - lastDataReceiveTime).TotalMilliseconds;
                }
                
                // 计算总体频率
                double totalElapsedSeconds = (currentTime - firstDataReceiveTime).TotalSeconds;
                double overallFrequency = totalElapsedSeconds > 0 ? totalDataPointsReceived / totalElapsedSeconds : 0;
                
                // 计算当前批次的等效频率（假设这批数据代表的时间间隔）
                double currentBatchFrequency = intervalMs > 0 ? (len * 1000.0 / intervalMs) : 0;
                
                // 将原始数据转换为可用的数据
                int[] rawData = new int[len];
                Marshal.Copy(data, rawData, 0, len);
                
                lastDataReceiveTime = currentTime;
                
                // 将数据放入缓冲队列
                lock (bufferLock)
                {
                    dataBuffer.Enqueue(rawData);
                    
                    // 限制缓冲队列大小，避免内存溢出
                    while (dataBuffer.Count > 100)
                    {
                        dataBuffer.Dequeue();
                    }
                }
                
                // 将数据存储到全局数据管理器中，供TestProcessPage使用
                if (rawData.Length > 0)
                {
                    // 将int数组转换为double数组并存储
                    double[] doubleData = Array.ConvertAll(rawData, x => (double)x);
                    GlobalBrainwaveDataManager.AddBrainwaveDataRange(doubleData);
                }
            }
            catch (Exception ex)
            {
            }
        }
        
        private void OnPostDataReceived(int dev, byte ele, byte att, byte med, byte res, IntPtr psd)
        {
            // 处理后的数据回调
        }
        
        private void OnBatteryInfoReceived(int dev, uint level, uint vol)
        {
            // 电池信息回调
            Dispatcher.BeginInvoke(() =>
            {
                // 可以在UI上显示电池信息
            });
        }
        
        private void OnEventReceived(uint eventType, uint param)
        {
            // 事件回调
            Dispatcher.BeginInvoke(() =>
            {
                // 处理设备事件
            });
        }
        
        private void UpdateBrainwaveDisplay(int[] rawData)
        {
            if (rawData == null || rawData.Length == 0) return;
            
            // 如果是第一次接收数据，设置设备连接状态和开始时间
            if (!isDeviceConnected)
            {
                isDeviceConnected = true;
                startTime = DateTime.Now;
            }
            
            // 使用真实数据更新显示
            double baselineY = BrainwaveCanvas.ActualHeight / 2;
            double canvasWidth = BrainwaveCanvas.ActualWidth;
            
            // 调整缩放因子以适应-400到400微伏的范围
            double microVoltRange = 800.0; // 总范围800微伏
            double scale = BrainwaveCanvas.ActualHeight / microVoltRange;
            
            // 使用实际的数据接收频率计算像素移动距离
            // 对于0值数据，使用固定的520Hz采样率
            // 对于真实数据，使用实际接收频率
            double actualSamplingRate;
            if (isShowingZeroData)
            {
                // 0值数据使用固定采样率
                actualSamplingRate = 520.0;
            }
            else
            {
                // 真实数据使用实际接收频率
                actualSamplingRate = totalDataPointsReceived > 0 && dataReceiveCount > 0 ? 
                    (totalDataPointsReceived / ((DateTime.Now - firstDataReceiveTime).TotalSeconds)) : 520.0;
            }
            double pixelsPerDataPoint = canvasWidth / (DISPLAY_WINDOW_SECONDS * actualSamplingRate);
            double moveDistance = rawData.Length * pixelsPerDataPoint;
            MoveWaveformLeftByPixels(moveDistance);
            
            // 获取最后一个数据点的位置（用于连线）
            double lastX = canvasWidth;
            double lastY = baselineY;
            bool hasLastPoint = false;
            
            // 查找最右边的波形点
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                if (element is FrameworkElement fe && fe.Tag?.ToString() == "waveform")
                {
                    if (element is Line line && Canvas.GetLeft(line) + line.X2 > lastX - moveDistance)
                    {
                        lastX = line.X2;
                        lastY = line.Y2;
                        hasLastPoint = true;
                    }
                    else if (element is Ellipse ellipse)
                    {
                        double ellipseX = Canvas.GetLeft(ellipse) + ellipse.Width / 2;
                        if (ellipseX > lastX - moveDistance)
                        {
                            lastX = ellipseX;
                            lastY = Canvas.GetTop(ellipse) + ellipse.Height / 2;
                            hasLastPoint = true;
                        }
                    }
                }
            }
            
            // 逐点绘制新数据
            for (int i = 0; i < rawData.Length; i++)
            {
                // 将原始ADC数据转换为微伏值（ADC值 * 0.2 = 微伏值）
                double microVoltValue = rawData[i] * 0.2;
                
                // 如果收到真实数据，停止显示0值数据
                // 注意：这里需要检查原始数据而不是转换后的微伏值
                if (isShowingZeroData && rawData[i] != 0)
                {
                    isShowingZeroData = false;
                    zeroDataTimer.Stop();
                }
                
                // 计算数据波动
                CalculateDataFluctuation(microVoltValue);
                
                // 计算Y坐标（向下为正，所以需要反转）
                double y = baselineY - (microVoltValue * scale);
                
                // 确保Y坐标在画布范围内
                y = Math.Max(10, Math.Min(BrainwaveCanvas.ActualHeight - 10, y));
                
                // 计算X坐标（新数据从右边开始）
                double x = canvasWidth - (rawData.Length - i) * pixelsPerDataPoint;
                
                // 根据波动状态选择颜色
                Color waveformColor = isWaveformRed ? Color.FromRgb(220, 50, 50) : Color.FromRgb(0, 120, 215);
                SolidColorBrush waveformBrush = new SolidColorBrush(waveformColor);
                
                // 绘制连线或点
                if (hasLastPoint || i > 0)
                {
                    double prevX = i == 0 ? lastX : canvasWidth - (rawData.Length - i + 1) * pixelsPerDataPoint;
                    double prevY = i == 0 ? lastY : baselineY - (rawData[i-1] * 0.2 * scale);
                    prevY = Math.Max(10, Math.Min(BrainwaveCanvas.ActualHeight - 10, prevY));
                    
                    Line line = new Line
                    {
                        X1 = prevX,
                        Y1 = prevY,
                        X2 = x,
                        Y2 = y,
                        Stroke = waveformBrush,
                        StrokeThickness = 1.5,
                        Tag = "waveform"
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
                        Fill = waveformBrush,
                        Tag = "waveform"
                    };
                    Canvas.SetLeft(point, x - 1);
                    Canvas.SetTop(point, y - 1);
                    BrainwaveCanvas.Children.Add(point);
                }
                
                hasLastPoint = true;
            }
            
            // 清理超出左边界的线条
            CleanupLeftBoundary();
            
            // 更新时间标签
            DrawTimeLabels();
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
        
        private void ConfigureDevice()
        {
            try
            {
                
                // 发送设备配置命令（参考demo中的device_config函数）
                BrainMonitorSDK.SDK_SendCommand(0, 0xEC);
                BrainMonitorSDK.SDK_SendCommand(0, 0xEF);
                BrainMonitorSDK.SDK_SendCommand(0, 0x04);
                
                // 发送带payload的命令
                byte[] payload = { 0x00, 0x00, 0x01 };
                IntPtr payloadPtr = Marshal.AllocHGlobal(payload.Length);
                Marshal.Copy(payload, 0, payloadPtr, payload.Length);
                BrainMonitorSDK.SDK_SendCommandWithPayload(0, 0x2B, payloadPtr, (byte)payload.Length);
                Marshal.FreeHGlobal(payloadPtr);
                
                BrainMonitorSDK.SDK_SendCommand(0, 0xFF);
                
                // 等待设备配置生效（添加短暂延迟）
                System.Threading.Thread.Sleep(100);
                
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"设备配置异常: {ex.Message}");
            }
        }

        public void OnNavigatedTo()
        {
            // 页面导航到时的处理
            // 确保按钮状态正确
            UpdateGenerateReportButtonState();
        }

        public void OnNavigatedFrom()
        {
            // 页面离开时的处理
            StopTimeUpdateTimer();
            
            // 停止显示定时器
            displayTimer?.Stop();
            
            // 清空缓冲队列
            lock (bufferLock)
            {
                dataBuffer.Clear();
            }
            
            // 页面离开时只停止数据采集和断开设备，不清理SDK
            // 这样可以保持SDK初始化状态，允许重新扫描设备
            StopDataCollectionOnly();
        }
        
        private void StopDataCollectionOnly()
        {
            try
            {
                if (sdkInitialized)
                {
                    // 停止数据采集
                    BrainMonitorSDK.SDK_StopDataCollection();
                    
                    // 断开所有设备
                    foreach (var device in connectedDevices)
                    {
                        BrainMonitorSDK.SDK_DisconnectDevice(device.Mac);
                    }
                    connectedDevices.Clear();
                    
                    // 重置设备连接状态
                    isDeviceConnected = false;
                    
                    // 停止0值数据显示
                    isShowingZeroData = false;
                    zeroDataTimer?.Stop();
                    
                    // 重置波动监控状态
                    recentDataPoints.Clear();
                    isWaveformRed = false;
                    
                    // 注意：不调用SDK_DisconnectPort()和SDK_Cleanup()
                    // 保持SDK和端口连接状态，以便重新扫描设备
                }
            }
            catch (Exception ex)
            {
                // 忽略清理过程中的异常
                // System.Diagnostics.Debug.WriteLine($"停止数据采集异常: {ex.Message}");
            }
        }
        
        private void CleanupSDK()
        {
            try
            {
                if (sdkInitialized)
                {
                    // 停止数据采集
                    BrainMonitorSDK.SDK_StopDataCollection();
                    
                    // 断开所有设备
                    foreach (var device in connectedDevices)
                    {
                        BrainMonitorSDK.SDK_DisconnectDevice(device.Mac);
                    }
                    connectedDevices.Clear();
                    
                    // 断开端口
                    BrainMonitorSDK.SDK_DisconnectPort();
                    
                    // 清理SDK
                    BrainMonitorSDK.SDK_Cleanup();
                    sdkInitialized = false;
                }
            }
            catch (Exception ex)
            {
                // 忽略清理过程中的异常
                // System.Diagnostics.Debug.WriteLine($"SDK清理异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 完全重置所有状态到软件刚启动时的状态
        /// 这个方法模拟软件重新打开的完整初始化过程
        /// </summary>
        private void ResetToInitialState()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("=== 开始完全重置到初始状态 ===");
                
                // 1. 停止所有定时器
                StopTimeUpdateTimer();
                if (displayTimer != null)
                {
                    displayTimer.Stop();
                }
                
                // 2. 重置所有状态变量到初始值
                 currentX = 0;
                 isScanning = false;
                 isDeviceConnected = false;
                 startTime = DateTime.Now;
                 lastDataReceiveTime = DateTime.MinValue;
                 totalDataPointsReceived = 0;
                 dataReceiveCount = 0;
                 firstDataReceiveTime = DateTime.MinValue;
                 scanFailureCount = 0; // 重置失败计数器
                
                // 重置测试状态
                isTestStarted = false;
                isTestCompleted = false;
                
                // 3. 清空所有集合
                scannedDevices.Clear();
                connectedDevices.Clear();
                
                // 4. 清空数据缓冲
                lock (bufferLock)
                {
                    dataBuffer.Clear();
                }
                
                // 5. 重置UI状态到初始状态
                DeviceComboBox.Items.Clear();
                DeviceComboBox.SelectedItem = null;
                DeviceComboBox.IsEnabled = false;
                ConnectDeviceButton.IsEnabled = false;
                DisconnectDeviceButton.IsEnabled = false;
                ScanDeviceButton.IsEnabled = true;
                
                // 重置DataContext（重要！）
                DataContext = this;
                
                // 6. 清空并重新初始化画布
                BrainwaveCanvas.Children.Clear();
                XAxisLabelsCanvas.Children.Clear();
                InitializeCanvas();
                
                // 7. 清空回调函数引用
                // System.Diagnostics.Debug.WriteLine("清空回调函数引用...");
                rawDataCallback = null;
                postDataCallback = null;
                battInfoCallback = null;
                eventCallback = null;
                
                // 8. 完全清理SDK（带超时保护和强制端口释放）
                // System.Diagnostics.Debug.WriteLine("开始完全清理SDK...");
                
                // 强制多次尝试断开端口，确保端口完全释放
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        // System.Diagnostics.Debug.WriteLine($"第{i+1}次尝试断开端口...");
                        BrainMonitorSDK.SDK_DisconnectPort();
                        System.Threading.Thread.Sleep(300); // 增加等待时间
                        
                        // 强制垃圾回收，释放可能的资源
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        System.Threading.Thread.Sleep(100);
                    }
                    catch (System.Runtime.InteropServices.SEHException sehEx)
                    {
                        // System.Diagnostics.Debug.WriteLine($"第{i+1}次断开端口SEH异常，SDK状态不稳定，跳过后续断开尝试: {sehEx.Message}");
                        break; // SEH异常表明SDK状态严重异常，停止重试
                    }
                    catch (Exception portEx)
                    {
                        // System.Diagnostics.Debug.WriteLine($"第{i+1}次断开端口异常: {portEx.Message}");
                    }
                }
                
                // 额外等待，确保端口完全释放
                // System.Diagnostics.Debug.WriteLine("等待端口完全释放...");
                System.Threading.Thread.Sleep(1000);
                
                var cleanupTask = Task.Run(() => {
                    try
                    {
                        if (sdkInitialized)
                        {
                            // 先清空SDK中的回调函数
                            try
                            {
                                BrainMonitorSDK.SDK_SetRawDataCallback(null);
                                BrainMonitorSDK.SDK_SetPostDataCallback(null);
                                BrainMonitorSDK.SDK_SetBattInfoCallback(null);
                                BrainMonitorSDK.SDK_SetEventCallback(null);
                            }
                            catch (System.Runtime.InteropServices.SEHException)
                            {
                                // System.Diagnostics.Debug.WriteLine("清空回调函数时发生SEH异常，跳过");
                            }
                            
                            try
                            {
                                BrainMonitorSDK.SDK_StopDataCollection();
                            }
                            catch (System.Runtime.InteropServices.SEHException)
                            {
                                // System.Diagnostics.Debug.WriteLine("停止数据收集时发生SEH异常，跳过");
                            }
                            
                            foreach (var device in connectedDevices.ToArray())
                            {
                                try
                                {
                                    BrainMonitorSDK.SDK_DisconnectDevice(device.Mac);
                                }
                                catch (System.Runtime.InteropServices.SEHException)
                                {
                                    // System.Diagnostics.Debug.WriteLine($"断开设备{device.Mac}时发生SEH异常，跳过");
                                }
                            }
                            
                            // 再次确保端口断开
                            try
                            {
                                BrainMonitorSDK.SDK_DisconnectPort();
                            }
                            catch (System.Runtime.InteropServices.SEHException)
                            {
                                // System.Diagnostics.Debug.WriteLine("最终断开端口时发生SEH异常，跳过");
                            }
                            
                            try
                            {
                                BrainMonitorSDK.SDK_Cleanup();
                            }
                            catch (System.Runtime.InteropServices.SEHException)
                            {
                                // System.Diagnostics.Debug.WriteLine("SDK_Cleanup时发生SEH异常，跳过清理直接重新初始化");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // System.Diagnostics.Debug.WriteLine($"SDK清理异常: {ex.Message}");
                    }
                });
                
                bool cleanupCompleted = cleanupTask.Wait(TimeSpan.FromSeconds(5));
                // System.Diagnostics.Debug.WriteLine($"SDK清理结果: {(cleanupCompleted ? "完成" : "超时")}");
                
                // 9. 重置SDK状态标志
                sdkInitialized = false;
                
                // 10. 等待更长时间确保端口资源完全释放
                // System.Diagnostics.Debug.WriteLine("等待端口资源完全释放...");
                System.Threading.Thread.Sleep(3000); // 增加到3秒
                
                // 10.5. 强制垃圾回收，确保所有资源释放
                // System.Diagnostics.Debug.WriteLine("执行强制垃圾回收...");
                for (int i = 0; i < 3; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.Threading.Thread.Sleep(200);
                }
                
                // 11. 重新初始化SDK（模拟软件启动）
                // System.Diagnostics.Debug.WriteLine("开始重新初始化SDK...");
                InitializeSDK();
                
                // 12. 重新初始化显示定时器
                InitializeDisplayTimer();
                
                // 13. 刷新端口列表
                RefreshPortList();
                
                // System.Diagnostics.Debug.WriteLine("=== 完全重置到初始状态完成 ===");
                
                // 显示重置完成消息
                Dispatcher.Invoke(() => MessageBox.Show("系统状态已完全重置，现在可以重新扫描设备", "重置完成", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"重置到初始状态异常: {ex.Message}");
                Dispatcher.Invoke(() => MessageBox.Show($"状态重置过程中发生异常: {ex.Message}", "重置异常", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private async void ScanDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            // System.Diagnostics.Debug.WriteLine("=== 开始扫描设备按钮点击事件 ===");
            // System.Diagnostics.Debug.WriteLine($"当前时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            
            // 防止重复扫描
            if (isScanning)
            {
                // System.Diagnostics.Debug.WriteLine("正在扫描中，忽略重复请求");
                return;
            }
            
            // 立即禁用按钮，避免用户感知到卡顿
            // System.Diagnostics.Debug.WriteLine("立即禁用扫描按钮");
            isScanning = true;
            ScanDeviceButton.IsEnabled = false;
            
            // 将耗时操作放到后台线程执行
            await Task.Run(async () => await PerformDeviceScan());
        }
        
        private async Task PerformDeviceScan()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("开始后台扫描设备流程");
            
                // 添加调试信息
                string debugInfo = $"DLL可用: {BrainMonitorSDK.IsDllAvailable}, SDK已初始化: {sdkInitialized}";
                // System.Diagnostics.Debug.WriteLine(debugInfo);
            
                // 检查DLL是否可用，如果不可用则使用模拟模式
                if (!BrainMonitorSDK.IsDllAvailable)
                {
                    // 在主线程更新UI
                    await Dispatcher.InvokeAsync(() =>
                    {
                        // 使用模拟设备扫描
                        DeviceComboBox.Items.Clear();
                        DeviceComboBox.Items.Add("模拟设备 1 (00:11:22:33:44:55)");
                        DeviceComboBox.Items.Add("模拟设备 2 (AA:BB:CC:DD:EE:FF)");
                        DeviceComboBox.IsEnabled = true;
                        
                        // 默认选中第一个设备
                        if (DeviceComboBox.Items.Count > 0)
                        {
                            DeviceComboBox.SelectedIndex = 0;
                        }
                    });
                    
                    // 不再弹窗，直接在设备列表显示
                    return;
                }
                
                // 如果SDK未初始化，尝试重新初始化
                if (!sdkInitialized)
                {
                    // System.Diagnostics.Debug.WriteLine("SDK未初始化，尝试重新初始化...");
                    InitializeSDK();
                    
                    if (!sdkInitialized)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show("SDK初始化失败，无法扫描设备", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        return;
                    }
                }
                
                // 智能端口连接管理 - 避免不必要的端口断开重连
                string selectedPort = null;
                await Dispatcher.InvokeAsync(() =>
                {
                    selectedPort = PortComboBox.SelectedItem?.ToString();
                });
                
                if (string.IsNullOrEmpty(selectedPort))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("请先选择一个端口", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                    return;
                }
                
                // System.Diagnostics.Debug.WriteLine($"尝试使用端口: {selectedPort}");
                
                // 检查当前端口是否已经连接，如果已连接则直接使用
                // 检查当前端口状态，确保端口真正可用
                int connectResult = 0; // 默认假设端口未连接
                
                // 获取SDK检测到的端口
                string sdkDetectedPort = BrainMonitorSDK.CheckPortString();
                // System.Diagnostics.Debug.WriteLine($"SDK检测到的端口: {sdkDetectedPort}");
                
                // 如果SDK没有检测到端口，尝试快速重连
                if (string.IsNullOrEmpty(sdkDetectedPort))
                {
                    // System.Diagnostics.Debug.WriteLine("SDK未检测到端口，尝试快速重连端口");
                    
                    // 快速重连端口（减少超时时间）
                    // System.Diagnostics.Debug.WriteLine($"正在快速重连端口: {selectedPort}（1秒超时）...");
                    var connectTask = Task.Run(() => {
                        try
                        {
                            return BrainMonitorSDK.SDK_ConnectPort(selectedPort);
                        }
                        catch (Exception ex)
                        {
                            // System.Diagnostics.Debug.WriteLine($"SDK_ConnectPort异常: {ex.Message}");
                            return 0;
                        }
                    });
                    
                    bool connectCompleted = connectTask.Wait(TimeSpan.FromSeconds(1)); // 减少到1秒
                    if (connectCompleted)
                    {
                        connectResult = connectTask.Result;
                        // System.Diagnostics.Debug.WriteLine($"快速端口重连完成，结果: {connectResult}");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine("快速端口重连超时（1秒），设置失败结果");
                        connectResult = 0;
                    }
                }
                else if (sdkDetectedPort != selectedPort)
                {
                    // System.Diagnostics.Debug.WriteLine("端口状态不一致，需要重新连接端口");
                
                // 先断开端口连接（使用超时保护）
                // System.Diagnostics.Debug.WriteLine("开始断开端口连接（3秒超时）...");
                var disconnectTask = Task.Run(() => {
                    try
                    {
                        BrainMonitorSDK.SDK_DisconnectPort();
                    }
                    catch (System.Runtime.InteropServices.SEHException sehEx)
                    {
                        // System.Diagnostics.Debug.WriteLine($"SDK_DisconnectPort SEH异常，SDK状态不稳定: {sehEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        // System.Diagnostics.Debug.WriteLine($"SDK_DisconnectPort异常: {ex.Message}");
                    }
                });
                
                bool disconnectCompleted = disconnectTask.Wait(TimeSpan.FromSeconds(3));
                if (!disconnectCompleted)
                {
                    // System.Diagnostics.Debug.WriteLine("SDK_DisconnectPort超时（3秒），继续执行...");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("SDK_DisconnectPort完成");
                }
                
                System.Threading.Thread.Sleep(200); // 等待断开完成
                
                // 重新连接端口（使用超时保护）
                // System.Diagnostics.Debug.WriteLine($"正在重新连接端口: {selectedPort}（3秒超时）...");
                var connectTask = Task.Run(() => {
                    try
                    {
                        return BrainMonitorSDK.SDK_ConnectPort(selectedPort);
                    }
                    catch (Exception ex)
                    {
                        // System.Diagnostics.Debug.WriteLine($"SDK_ConnectPort异常: {ex.Message}");
                        return 0;
                    }
                });
                
                bool connectCompleted = connectTask.Wait(TimeSpan.FromSeconds(3));
                if (connectCompleted)
                {
                    connectResult = connectTask.Result;
                    // System.Diagnostics.Debug.WriteLine($"端口连接完成，结果: {connectResult}");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("SDK_ConnectPort超时（3秒），设置失败结果");
                    connectResult = 0;
                    }
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("端口状态一致，无需重新连接");
                    connectResult = 1; // 端口已连接
                }
                
                try
                {
                    
                    if (connectResult != 1)
                    {
                        // System.Diagnostics.Debug.WriteLine($"端口连接失败: {selectedPort}, 返回值: {connectResult}");
                        
                        // 添加详细的诊断信息
                        // System.Diagnostics.Debug.WriteLine("=== 端口连接失败诊断开始 ===");
                        // System.Diagnostics.Debug.WriteLine($"当前SDK初始化状态: {sdkInitialized}");
                        
                        // 检查端口是否被其他程序占用
                        try
                        {
                            // System.Diagnostics.Debug.WriteLine("检查系统端口状态...");
                            var portNames = System.IO.Ports.SerialPort.GetPortNames();
                            // System.Diagnostics.Debug.WriteLine($"系统可用端口: {string.Join(", ", portNames)}");
                            // System.Diagnostics.Debug.WriteLine($"目标端口 {selectedPort} 在系统中: {(portNames.Contains(selectedPort) ? "存在" : "不存在")}");
                        }
                        catch (Exception portCheckEx)
                        {
                            // System.Diagnostics.Debug.WriteLine($"端口检查异常: {portCheckEx.Message}");
                        }
                        
                        // 尝试获取SDK状态信息
                        try
                        {
                            // System.Diagnostics.Debug.WriteLine("尝试获取SDK详细状态...");
                            // 这里可以添加更多SDK状态检查
                            // System.Diagnostics.Debug.WriteLine("SDK状态检查完成");
                        }
                        catch (Exception sdkCheckEx)
                        {
                            // System.Diagnostics.Debug.WriteLine($"SDK状态检查异常: {sdkCheckEx.Message}");
                        }
                        
                        // 尝试温和的SDK状态恢复，避免过度使用SDK_Cleanup() - 端口连接失败恢复逻辑
                        // System.Diagnostics.Debug.WriteLine("尝试温和的SDK状态恢复...");
                        try
                        {
                            // 先尝试简单的端口重连，而不是完全重置SDK
                            // System.Diagnostics.Debug.WriteLine("尝试简单的端口重连...");
                            var retryConnectTask = Task.Run(() => {
                                try
                                {
                                    System.Threading.Thread.Sleep(200); // 减少等待时间到200ms
                                    return BrainMonitorSDK.SDK_ConnectPort(selectedPort);
                                }
                                catch (Exception ex)
                                {
                                    // System.Diagnostics.Debug.WriteLine($"重试连接异常: {ex.Message}");
                                    return 0;
                                }
                            });
                            
                            bool retryConnectCompleted = retryConnectTask.Wait(TimeSpan.FromSeconds(2)); // 减少到2秒
                            if (retryConnectCompleted)
                            {
                                int retryConnectResult = retryConnectTask.Result;
                                // System.Diagnostics.Debug.WriteLine($"重试连接结果: {retryConnectResult}");
                                
                                if (retryConnectResult == 1)
                                {
                                    // System.Diagnostics.Debug.WriteLine("端口重连成功，继续扫描流程");
                                    connectResult = 1; // 更新连接结果，继续执行
                                }
                                else
                                {
                                    // System.Diagnostics.Debug.WriteLine("端口重连失败，尝试重置SDK");
                                    
                                    // 只有在端口重连失败时才尝试重置SDK
                            var resetCleanupTask = Task.Run(() => {
                                try
                                {
                                    // System.Diagnostics.Debug.WriteLine("执行重置清理...");
                                    BrainMonitorSDK.SDK_Cleanup();
                                }
                                catch (Exception ex)
                                {
                                    // System.Diagnostics.Debug.WriteLine($"重置清理异常: {ex.Message}");
                                }
                            });
                            
                                    bool resetCleanupCompleted = resetCleanupTask.Wait(TimeSpan.FromSeconds(3)); // 减少到3秒
                            // System.Diagnostics.Debug.WriteLine($"重置清理结果: {(resetCleanupCompleted ? "完成" : "超时")}");
                            
                                    System.Threading.Thread.Sleep(500); // 减少等待时间到500ms
                            
                            // 重新初始化
                            var resetInitTask = Task.Run(() => {
                                try
                                {
                                    // System.Diagnostics.Debug.WriteLine("执行重置初始化...");
                                    return BrainMonitorSDK.SDK_Init();
                                }
                                catch (Exception ex)
                                {
                                    // System.Diagnostics.Debug.WriteLine($"重置初始化异常: {ex.Message}");
                                    return 0;
                                }
                            });
                            
                                    bool resetInitCompleted = resetInitTask.Wait(TimeSpan.FromSeconds(3)); // 减少到3秒
                            if (resetInitCompleted)
                            {
                                int resetInitResult = resetInitTask.Result;
                                // System.Diagnostics.Debug.WriteLine($"重置初始化结果: {resetInitResult}");
                                sdkInitialized = (resetInitResult == 1);
                                
                                if (resetInitResult == 1)
                                {
                                    // 再次尝试连接端口
                                    // System.Diagnostics.Debug.WriteLine("重置后再次尝试连接端口...");
                                            var finalConnectTask = Task.Run(() => {
                                        try
                                        {
                                                    System.Threading.Thread.Sleep(200); // 减少等待时间到200ms
                                            return BrainMonitorSDK.SDK_ConnectPort(selectedPort);
                                        }
                                        catch (Exception ex)
                                        {
                                                    // System.Diagnostics.Debug.WriteLine($"最终连接异常: {ex.Message}");
                                            return 0;
                                        }
                                    });
                                    
                                            bool finalConnectCompleted = finalConnectTask.Wait(TimeSpan.FromSeconds(2)); // 减少到2秒
                                            if (finalConnectCompleted)
                                    {
                                                int finalConnectResult = finalConnectTask.Result;
                                                // System.Diagnostics.Debug.WriteLine($"最终连接结果: {finalConnectResult}");
                                        
                                                if (finalConnectResult == 1)
                                        {
                                            // System.Diagnostics.Debug.WriteLine("重置后端口连接成功，继续扫描流程");
                                            connectResult = 1; // 更新连接结果，继续执行
                                        }
                                        else
                                        {
                                            // System.Diagnostics.Debug.WriteLine("重置后端口连接仍然失败");
                                        }
                                    }
                                    else
                                    {
                                                // System.Diagnostics.Debug.WriteLine("最终连接超时");
                                    }
                                }
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine("重置初始化超时");
                                sdkInitialized = false;
                                    }
                                }
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine("重试连接超时");
                            }
                        }
                        catch (Exception resetEx)
                        {
                            // System.Diagnostics.Debug.WriteLine($"温和恢复异常: {resetEx.Message}");
                        }
                        
                        // System.Diagnostics.Debug.WriteLine("=== 端口连接失败诊断结束 ===");
                        
                        // 如果重置后仍然失败，显示错误信息
                        if (connectResult != 1)
                        {
                            await Dispatcher.InvokeAsync(() => MessageBox.Show($"连接端口 {selectedPort} 失败，无法扫描设备\n连接结果: {connectResult}\n\n建议：\n1. 检查设备是否正确连接\n2. 确认端口未被其他程序占用\n3. 尝试重启应用程序", "端口连接失败", MessageBoxButton.OK, MessageBoxImage.Error));
                            return;
                        }
                    }
                    
                    // System.Diagnostics.Debug.WriteLine($"端口连接成功: {selectedPort}");
                }
                catch (Exception portEx)
                {
                    await Dispatcher.InvokeAsync(() => MessageBox.Show($"端口连接异常: {portEx.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }
                
                // 验证端口连接状态，确保在端口连接失败时不进行扫描
                if (connectResult != 1)
                {
                    // System.Diagnostics.Debug.WriteLine("端口连接失败，无法进行扫描");
                    await Dispatcher.InvokeAsync(() => MessageBox.Show($"端口连接失败，无法扫描设备", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }
                
                // 开始扫描设备
                // System.Diagnostics.Debug.WriteLine("开始扫描设备");
                int result = 0;
                
                try
                {
                    // 使用CancellationToken来正确处理超时
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        var scanTask = Task.Run(() => BrainMonitorSDK.SDK_ScanDevices(), cts.Token);
                        result = await scanTask;
                    }
                }
                catch (OperationCanceledException)
                {
                    // System.Diagnostics.Debug.WriteLine("扫描设备超时");
                    await Dispatcher.InvokeAsync(() => MessageBox.Show("扫描设备超时（5秒），请检查设备连接和端口设置", "扫描超时", MessageBoxButton.OK, MessageBoxImage.Warning));
                    
                    // 超时后确保SDK状态一致性
                    // System.Diagnostics.Debug.WriteLine("超时后检查SDK状态...");
                    await Task.Run(() => {
                        try {
                            // 确保SDK处于可用状态
                            if (sdkInitialized) {
                                // System.Diagnostics.Debug.WriteLine("SDK状态正常，可以继续扫描");
                            } else {
                                // System.Diagnostics.Debug.WriteLine("SDK状态异常，尝试重新初始化");
                                BrainMonitorSDK.SDK_Init();
                                sdkInitialized = true;
                            }
                        } catch (Exception ex) {
                            // System.Diagnostics.Debug.WriteLine($"SDK状态检查异常: {ex.Message}");
                        }
                    });
                    
                    result = 0;
                }
                catch (Exception scanEx)
                {
                    // System.Diagnostics.Debug.WriteLine($"扫描设备异常: {scanEx.Message}");
                    result = 0;
                }
                
                // System.Diagnostics.Debug.WriteLine($"最终扫描设备结果: {result}");
                
                if (result == 1)
                {
                    // 扫描成功，重置失败计数器
                    scanFailureCount = 0;
                    // System.Diagnostics.Debug.WriteLine("扫描成功，失败计数器已重置");
                    
                    // 使用统一的扫描成功处理逻辑
                    await Dispatcher.InvokeAsync(() => HandleScanSuccess());
                    
                    // 检查是否真的扫描到设备
                    int deviceCount = BrainMonitorSDK.SDK_GetScanDevicesCount();
                    if (deviceCount == 0)
                    {
                        // System.Diagnostics.Debug.WriteLine("扫描成功但未找到设备，这是正常情况");
                        await Dispatcher.InvokeAsync(() => MessageBox.Show("未找到任何设备，请检查设备是否开启并靠近接收器", "提示", MessageBoxButton.OK, MessageBoxImage.Information));
                    }
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"设备扫描失败，返回值: {result}");
                    
                    // 尝试SDK状态恢复（在后台线程执行，避免阻塞UI）
                    // System.Diagnostics.Debug.WriteLine("尝试SDK状态恢复");
                    bool recoverySuccessful = false;
                    
                    // 为整个恢复过程设置10秒超时
                    using (var recoveryCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        try
                        {
                            // System.Diagnostics.Debug.WriteLine("开始SDK状态恢复流程（10秒超时）...");
                            // System.Diagnostics.Debug.WriteLine($"主线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                        
                        // 尝试温和的SDK状态恢复，避免过度使用SDK_Cleanup() - 扫描失败恢复逻辑
                        // System.Diagnostics.Debug.WriteLine("尝试温和的SDK状态恢复...");
                        try
                        {
                            // 先尝试简单的端口重连，而不是完全重置SDK
                            // System.Diagnostics.Debug.WriteLine("尝试简单的端口重连...");
                            var retryConnectTask = Task.Run(() => {
                                try
                                {
                                    System.Threading.Thread.Sleep(500); // 短暂等待
                                    return BrainMonitorSDK.SDK_ConnectPort(selectedPort);
                                }
                                catch (Exception ex)
                                {
                                    // System.Diagnostics.Debug.WriteLine($"重试连接异常: {ex.Message}");
                                    return 0;
                                }
                            });
                            
                            bool retryConnectCompleted = retryConnectTask.Wait(TimeSpan.FromSeconds(5));
                            if (retryConnectCompleted)
                            {
                                int retryConnectResult = retryConnectTask.Result;
                                // System.Diagnostics.Debug.WriteLine($"重试连接结果: {retryConnectResult}");
                                
                                if (retryConnectResult == 1)
                                {
                                    // System.Diagnostics.Debug.WriteLine("端口重连成功，继续扫描流程");
                                    connectResult = 1; // 更新连接结果，继续执行
                                    recoverySuccessful = true; // 标记恢复成功
                        }
                        else
                        {
                                    // System.Diagnostics.Debug.WriteLine("端口重连失败，尝试重置SDK");
                                    
                                    // 只有在端口重连失败时才尝试重置SDK
                                    var resetCleanupTask = Task.Run(() => {
                                        try
                                        {
                                            // System.Diagnostics.Debug.WriteLine("执行重置清理...");
                                            BrainMonitorSDK.SDK_Cleanup();
                                        }
                                        catch (Exception ex)
                                        {
                                            // System.Diagnostics.Debug.WriteLine($"重置清理异常: {ex.Message}");
                                        }
                                    });
                                    
                                    bool resetCleanupCompleted = resetCleanupTask.Wait(TimeSpan.FromSeconds(5));
                                    // System.Diagnostics.Debug.WriteLine($"重置清理结果: {(resetCleanupCompleted ? "完成" : "超时")}");
                                    
                                    System.Threading.Thread.Sleep(1000); // 等待更长时间
                                    
                                    // 重新初始化
                                    var resetInitTask = Task.Run(() => {
                                        try
                                        {
                                            // System.Diagnostics.Debug.WriteLine("执行重置初始化...");
                                            return BrainMonitorSDK.SDK_Init();
                                        }
                                        catch (Exception ex)
                                        {
                                            // System.Diagnostics.Debug.WriteLine($"重置初始化异常: {ex.Message}");
                                            return 0;
                                        }
                                    });
                                    
                                    bool resetInitCompleted = resetInitTask.Wait(TimeSpan.FromSeconds(5));
                                    if (resetInitCompleted)
                                    {
                                        int resetInitResult = resetInitTask.Result;
                                        // System.Diagnostics.Debug.WriteLine($"重置初始化结果: {resetInitResult}");
                                        sdkInitialized = (resetInitResult == 1);
                                        
                                        if (resetInitResult == 1)
                                        {
                                            // 再次尝试连接端口
                                            // System.Diagnostics.Debug.WriteLine("重置后再次尝试连接端口...");
                                            var finalConnectTask = Task.Run(() => {
                                                try
                                                {
                                                    System.Threading.Thread.Sleep(500); // 短暂等待
                                                    return BrainMonitorSDK.SDK_ConnectPort(selectedPort);
                                                }
                                                catch (Exception ex)
                                                {
                                                    // System.Diagnostics.Debug.WriteLine($"最终连接异常: {ex.Message}");
                                                    return 0;
                                                }
                                            });
                                            
                                            bool finalConnectCompleted = finalConnectTask.Wait(TimeSpan.FromSeconds(5));
                                            if (finalConnectCompleted)
                                            {
                                                int finalConnectResult = finalConnectTask.Result;
                                                // System.Diagnostics.Debug.WriteLine($"最终连接结果: {finalConnectResult}");
                                                
                                                if (finalConnectResult == 1)
                                                {
                                                    // System.Diagnostics.Debug.WriteLine("重置后端口连接成功，继续扫描流程");
                                                    connectResult = 1; // 更新连接结果，继续执行
                                                    recoverySuccessful = true; // 标记恢复成功
                                                }
                                                else
                                                {
                                                    // System.Diagnostics.Debug.WriteLine("重置后端口连接仍然失败");
                                                }
                                }
                                else
                                {
                                                // System.Diagnostics.Debug.WriteLine("最终连接超时");
                                            }
                                }
                            }
                            else
                            {
                                        // System.Diagnostics.Debug.WriteLine("重置初始化超时");
                                        sdkInitialized = false;
                                    }
                            }
                        }
                        else
                        {
                                // System.Diagnostics.Debug.WriteLine("重试连接超时");
                            }
                        }
                        catch (Exception resetEx)
                        {
                            // System.Diagnostics.Debug.WriteLine($"温和恢复异常: {resetEx.Message}");
                        }
                    }
                    catch (OperationCanceledException cancelEx)
                    {
                        // System.Diagnostics.Debug.WriteLine($"SDK状态恢复操作被取消: {cancelEx.Message}");
                        if (cancelEx.CancellationToken.IsCancellationRequested)
                        {
                            // System.Diagnostics.Debug.WriteLine("检测到超时取消，可能是SDK_Cleanup、SDK_Init或端口连接操作超时");
                        }
                        // System.Diagnostics.Debug.WriteLine("SDK状态恢复超时，停止恢复过程");
                        // System.Diagnostics.Debug.WriteLine("恢复超时，设置恢复失败标志");
                    }
                    catch (Exception recoveryEx)
                    {
                        // System.Diagnostics.Debug.WriteLine($"SDK状态恢复异常: {recoveryEx.Message}");
                        // System.Diagnostics.Debug.WriteLine($"异常类型: {recoveryEx.GetType().Name}");
                        // System.Diagnostics.Debug.WriteLine($"恢复异常堆栈: {recoveryEx.StackTrace}");
                        if (recoveryEx.InnerException != null)
                        {
                            // System.Diagnostics.Debug.WriteLine($"内部异常: {recoveryEx.InnerException.Message}");
                            // System.Diagnostics.Debug.WriteLine($"内部异常堆栈: {recoveryEx.InnerException.StackTrace}");
                        }
                        // System.Diagnostics.Debug.WriteLine("SDK状态恢复过程中发生异常，设置恢复失败标志");
                    }
                    } // 关闭CancellationTokenSource的using块
                    
                    // 只有当恢复失败时才显示错误弹窗
                    if (!recoverySuccessful)
                    {
                        // 增加失败计数器
                        scanFailureCount++;
                        // System.Diagnostics.Debug.WriteLine($"扫描失败，失败计数器: {scanFailureCount}/{MAX_SCAN_FAILURES}");
                        
                        // 检查是否达到最大失败次数
                        if (scanFailureCount >= MAX_SCAN_FAILURES)
                        {
                            // System.Diagnostics.Debug.WriteLine("达到最大失败次数，执行完全重置到初始状态...");
                            scanFailureCount = 0; // 重置计数器
                            
                            // 调用完全重置方法
                            ResetToInitialState();
                            return; // 重置后直接返回，不再显示错误消息
                        }
                        
                        // System.Diagnostics.Debug.WriteLine("SDK状态恢复失败，开始完全重新初始化...");
                        
                        try
                        {
                            // 先尝试温和的恢复，避免过度使用SDK_Cleanup()
                            // System.Diagnostics.Debug.WriteLine("尝试温和的SDK状态恢复...");
                            
                            // 尝试重新连接端口
                            var retryConnectTask = Task.Run(() => {
                                try
                                {
                                    System.Threading.Thread.Sleep(500); // 短暂等待
                                    return BrainMonitorSDK.SDK_ConnectPort(selectedPort);
                                }
                                catch (Exception ex)
                                {
                                    // System.Diagnostics.Debug.WriteLine($"温和恢复端口重连异常: {ex.Message}");
                                    return 0;
                                }
                            });
                            
                            bool retryConnectCompleted = retryConnectTask.Wait(TimeSpan.FromSeconds(3));
                            if (retryConnectCompleted)
                            {
                                int retryConnectResult = retryConnectTask.Result;
                                if (retryConnectResult == 1)
                                {
                                    // System.Diagnostics.Debug.WriteLine("温和恢复端口重连成功");
                                    sdkInitialized = true;
                                    return; // 温和恢复成功，直接返回
                                }
                            }
                            
                            // 温和恢复失败，才使用强制清理
                            // System.Diagnostics.Debug.WriteLine("温和恢复失败，使用强制清理...");
                            var forceCleanupTask = Task.Run(() => {
                                try
                                {
                                    BrainMonitorSDK.SDK_Cleanup();
                                }
                                catch (Exception ex)
                                {
                                    // System.Diagnostics.Debug.WriteLine($"强制SDK_Cleanup异常: {ex.Message}");
                                }
                            });
                            
                            bool cleanupCompleted = forceCleanupTask.Wait(TimeSpan.FromSeconds(2)); // 减少到2秒
                            if (!cleanupCompleted)
                            {
                                // System.Diagnostics.Debug.WriteLine("强制SDK_Cleanup超时（2秒），继续执行...");
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine("强制SDK_Cleanup完成");
                            }
                            
                            System.Threading.Thread.Sleep(300); // 减少等待时间到300ms
                            
                            // 重新初始化SDK（使用超时保护）
                            // System.Diagnostics.Debug.WriteLine("重新调用SDK_Init（2秒超时）...");
                            int reinitResult = 0;
                            var forceInitTask = Task.Run(() => {
                                try
                                {
                                    return BrainMonitorSDK.SDK_Init();
                                }
                                catch (Exception ex)
                                {
                                    // System.Diagnostics.Debug.WriteLine($"强制SDK_Init异常: {ex.Message}");
                                    return 0;
                                }
                            });
                            
                            bool initCompleted = forceInitTask.Wait(TimeSpan.FromSeconds(2)); // 减少到2秒
                            if (initCompleted)
                            {
                                reinitResult = forceInitTask.Result;
                                // System.Diagnostics.Debug.WriteLine($"强制重新初始化完成，结果: {reinitResult}");
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine("强制SDK_Init超时（2秒），设置失败结果");
                                reinitResult = 0;
                            }
                            
                            if (reinitResult == 1)
                            {
                                // System.Diagnostics.Debug.WriteLine("SDK强制重新初始化成功");
                                // 更新SDK状态标志
                                sdkInitialized = true;
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine("SDK强制重新初始化失败");
                                sdkInitialized = false;
                            }
                        }
                        catch (Exception reinitEx)
                        {
                            // System.Diagnostics.Debug.WriteLine($"强制重新初始化异常: {reinitEx.Message}");
                            sdkInitialized = false;
                        }
                        
                        // System.Diagnostics.Debug.WriteLine("显示设备扫描失败消息框...");
                        await Dispatcher.InvokeAsync(() => MessageBox.Show($"设备扫描失败（{scanFailureCount}/{MAX_SCAN_FAILURES}），SDK已重新初始化，请重试", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
                        // System.Diagnostics.Debug.WriteLine("设备扫描失败消息框已关闭");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine("SDK状态恢复成功，跳过错误提示");
                    }
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"扫描设备顶层异常: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"异常类型: {ex.GetType().Name}");
                // System.Diagnostics.Debug.WriteLine($"顶层异常堆栈: {ex.StackTrace}");
                
                // 如果是SEHException，说明SDK状态异常，需要重新初始化
                if (ex is System.Runtime.InteropServices.SEHException)
                {
                    // System.Diagnostics.Debug.WriteLine("检测到SEHException，开始强制重新初始化SDK...");
                    
                    try
                    {
                        // SEH异常表明SDK状态严重异常，需要强制重新初始化
                        // System.Diagnostics.Debug.WriteLine("检测到SEH异常，开始强制重新初始化SDK...");
                        
                        // 强制清理SDK状态（使用超时保护）
                        // System.Diagnostics.Debug.WriteLine("强制调用SDK_Cleanup（2秒超时）...");
                        var sehCleanupTask = Task.Run(() => {
                            try
                            {
                                BrainMonitorSDK.SDK_Cleanup();
                            }
                            catch (Exception ex)
                            {
                                // System.Diagnostics.Debug.WriteLine($"SEH强制SDK_Cleanup异常: {ex.Message}");
                            }
                        });
                        
                        bool sehCleanupCompleted = sehCleanupTask.Wait(TimeSpan.FromSeconds(2)); // 减少到2秒
                        if (!sehCleanupCompleted)
                        {
                            // System.Diagnostics.Debug.WriteLine("SEH强制SDK_Cleanup超时（2秒），继续执行...");
                        }
                        else
                        {
                            // System.Diagnostics.Debug.WriteLine("SEH强制SDK_Cleanup完成");
                        }
                        
                        System.Threading.Thread.Sleep(300); // 减少等待时间到300ms
                        
                        // 重新初始化SDK（使用超时保护）
                        // System.Diagnostics.Debug.WriteLine("重新调用SDK_Init（2秒超时）...");
                        int reinitResult = 0;
                        var sehInitTask = Task.Run(() => {
                            try
                            {
                                return BrainMonitorSDK.SDK_Init();
                            }
                            catch (Exception ex)
                            {
                                // System.Diagnostics.Debug.WriteLine($"SEH强制SDK_Init异常: {ex.Message}");
                                return 0;
                            }
                        });
                        
                        bool sehInitCompleted = sehInitTask.Wait(TimeSpan.FromSeconds(2)); // 减少到2秒
                        if (sehInitCompleted)
                        {
                            reinitResult = sehInitTask.Result;
                            // System.Diagnostics.Debug.WriteLine($"SEH强制重新初始化完成，结果: {reinitResult}");
                        }
                        else
                        {
                            // System.Diagnostics.Debug.WriteLine("SEH强制SDK_Init超时（2秒），设置失败结果");
                            reinitResult = 0;
                        }
                        
                        if (reinitResult == 1)
                        {
                            // System.Diagnostics.Debug.WriteLine("SEH SDK重新初始化成功");
                             sdkInitialized = true;
                            await Dispatcher.InvokeAsync(() => MessageBox.Show("检测到SDK异常，已重新初始化，请重试扫描", "提示", MessageBoxButton.OK, MessageBoxImage.Information));
                        }
                        else
                        {
                            // System.Diagnostics.Debug.WriteLine("SEH SDK重新初始化失败");
                             sdkInitialized = false;
                            await Dispatcher.InvokeAsync(() => MessageBox.Show("SDK异常且重新初始化失败，请重启应用程序", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error));
                        }
                    }
                    catch (Exception reinitEx)
                    {
                        // System.Diagnostics.Debug.WriteLine($"SEH重新初始化异常: {reinitEx.Message}");
                         sdkInitialized = false;
                        await Dispatcher.InvokeAsync(() => MessageBox.Show("SDK重新初始化失败，请重启应用程序", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                }
                else
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"扫描设备异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
            finally
            {
                // System.Diagnostics.Debug.WriteLine("进入PerformDeviceScan finally块，开始重置扫描状态...");
                // 在主线程重置扫描状态
                await Dispatcher.InvokeAsync(() =>
                {
                    isScanning = false;
                    ScanDeviceButton.IsEnabled = true;
                    // System.Diagnostics.Debug.WriteLine("ScanDeviceButton已重新启用");
                });
                // System.Diagnostics.Debug.WriteLine("扫描设备方法执行完成");
            }
        }

        private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 当选择设备时，根据连接状态更新按钮状态
            if (!isDeviceConnected)
            {
                ConnectDeviceButton.IsEnabled = DeviceComboBox.SelectedItem != null;
            }
        }
        
        private void PortComboBox_DropDownOpened(object sender, EventArgs e)
        {
            // 每次打开下拉列表时刷新端口列表
            RefreshPortList();
        }

        private async void ConnectDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceComboBox.SelectedItem == null)
            {
                MessageBox.Show("请先选择一个设备", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // 立即禁用连接按钮，避免用户感知到卡顿
            ConnectDeviceButton.IsEnabled = false;
            
            // 将耗时操作放到后台线程执行
            await Task.Run(async () => await PerformDeviceConnection());
        }
        
        private async Task PerformDeviceConnection()
        {
            try
            {
                // 检查SDK是否可用
                if (!BrainMonitorSDK.IsDllAvailable || !sdkInitialized)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("SDK未初始化或不可用，无法连接设备", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return;
                }
            
                int selectedIndex = -1;
                DeviceInfo? selectedDevice = null;
                
                await Dispatcher.InvokeAsync(() =>
                {
                    selectedIndex = DeviceComboBox.SelectedIndex;
                });
                
                if (selectedIndex >= 0 && selectedIndex < scannedDevices.Count)
                {
                    selectedDevice = scannedDevices[selectedIndex];
                    
                    // 使用SDK连接设备
                    int result = BrainMonitorSDK.SDK_ConnectDevice(selectedDevice.Value.Mac, selectedDevice.Value.Type);
                    if (result == 1)
                    {
                        connectedDevices.Add(selectedDevice.Value);
                        // 连接成功，不再弹窗提示
                        
                        // 在主线程更新UI状态
                        await Dispatcher.InvokeAsync(() =>
                        {
                            // 设置设备连接状态
                            isDeviceConnected = true;
                            
                            // 更新按钮状态
                            UpdateButtonStates(true);
                            
                            // 开始数据采集（图表移动和0值显示将在这里启动）
                            StartDataCollection();
                        });
                    }
                    else
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show("设备连接失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"连接设备异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                // 在主线程恢复按钮状态
                await Dispatcher.InvokeAsync(() =>
                {
                    if (!isDeviceConnected)
                    {
                        ConnectDeviceButton.IsEnabled = true;
                    }
                });
            }
        }
        
        private void DisconnectDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查SDK是否可用
                if (!BrainMonitorSDK.IsDllAvailable || !sdkInitialized)
                {
                    Dispatcher.Invoke(() => MessageBox.Show("SDK未初始化或不可用", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }
                
                // 先停止数据采集
                BrainMonitorSDK.SDK_StopDataCollection();
                
                // 断开所有已连接的设备
                foreach (var device in connectedDevices.ToList())
                {
                    // System.Diagnostics.Debug.WriteLine($"正在断开设备: {device.Mac}");
                    int result = BrainMonitorSDK.SDK_DisconnectDevice(device.Mac);
                    if (result == 1)
                    {
                        connectedDevices.Remove(device);
                        // System.Diagnostics.Debug.WriteLine($"设备 {device.Mac} 断开成功");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"设备 {device.Mac} 断开失败");
                    }
                }
                
                // 清空连接设备列表
                connectedDevices.Clear();
                
                // 停止显示定时器
                displayTimer?.Stop();
                
                // 清空缓冲队列
                lock (bufferLock)
                {
                    dataBuffer.Clear();
                }
                
                // 设置设备连接状态
                isDeviceConnected = false;
                
                // 重置测试状态
                isTestStarted = false;
                isTestCompleted = false;
                
                // 停止0值数据显示
                isShowingZeroData = false;
                zeroDataTimer?.Stop();
                
                // 重置波动监控状态
                recentDataPoints.Clear();
                isWaveformRed = false;
                
                // 重置开始时间，使横坐标恢复到原始状态（-00:00:08到00:00:00）
                startTime = DateTime.Now;
                
                // 清空脑电图显示
                BrainwaveCanvas.Children.Clear();
                XAxisLabelsCanvas.Children.Clear();
                YAxisLabelsCanvas.Children.Clear();
                
                // 重新绘制网格和坐标轴
                DrawGridAndBaseline();
                
                // 更新按钮状态
                UpdateButtonStates(false);
                
                // 更新生成报告按钮状态
                UpdateGenerateReportButtonState();
                
                // 停止时间更新定时器
                StopTimeUpdateTimer();
                
                // 清空设备列表，但保持SDK和端口连接状态以便重新扫描
                ClearDeviceList();
                
                // 重要：不要调用SDK_DisconnectPort()和SDK_Cleanup()
                // 保持SDK初始化状态和端口连接，以便重新扫描设备
                // 这样可以避免重复的端口断开重连操作
                
                // 额外：确保端口状态一致性，如果SDK检测不到端口，尝试重新连接
                string selectedPort = PortComboBox.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedPort))
                {
                    string sdkDetectedPort = BrainMonitorSDK.CheckPortString();
                    if (string.IsNullOrEmpty(sdkDetectedPort))
                    {
                        // System.Diagnostics.Debug.WriteLine("断开设备后检测到端口状态不一致，尝试重新连接端口");
                        try
                        {
                            int reconnectResult = BrainMonitorSDK.SDK_ConnectPort(selectedPort);
                            // System.Diagnostics.Debug.WriteLine($"断开设备后端口重连结果: {reconnectResult}");
                            
                            // 如果端口重连失败，标记SDK状态为未初始化，这样下次扫描时会重新初始化
                            if (reconnectResult != 1)
                            {
                                // System.Diagnostics.Debug.WriteLine("断开设备后端口重连失败，标记SDK为未初始化状态");
                                sdkInitialized = false;
                            }
                        }
                        catch (Exception portEx)
                        {
                            // System.Diagnostics.Debug.WriteLine($"断开设备后端口重连异常: {portEx.Message}");
                            // 异常也标记为未初始化
                            sdkInitialized = false;
                        }
                    }
                }
                
                // System.Diagnostics.Debug.WriteLine("设备断开连接完成，SDK和端口状态已保持");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"断开连接异常: {ex.Message}");
                Dispatcher.Invoke(() => MessageBox.Show($"断开连接异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
        
        private void UpdateButtonStates(bool isConnected)
        {
            // 连接状态下：禁用连接按钮，启用断开连接按钮
            // 断开状态下：启用连接按钮（如果有选中设备），禁用断开连接按钮
            ConnectDeviceButton.IsEnabled = !isConnected && DeviceComboBox.SelectedItem != null;
            DisconnectDeviceButton.IsEnabled = isConnected;
            
            // 开始测试按钮：只有在设备连接成功后才启用
            StartTestButton.IsEnabled = isConnected;
            
            // 连接状态下禁用设备列表，断开状态下也禁用设备列表（需要重新扫描才能启用）
            if (isConnected)
            {
                DeviceComboBox.IsEnabled = false;
            }
            else
            {
                // 断开连接后，设备列表应该被禁用，只有扫描后才能启用
                DeviceComboBox.IsEnabled = false;
            }
            
            // 更新生成报告按钮状态
            UpdateGenerateReportButtonState();
        }
        
        private void ClearDeviceList()
        {
            // 清空设备列表
            DeviceComboBox.Items.Clear();
            DeviceComboBox.SelectedItem = null;
            scannedDevices.Clear();
            // 不在这里设置IsEnabled，让UpdateButtonStates统一管理
        }
        
        private void HandleScanSuccess()
        {
            // 统一的扫描成功处理逻辑
            int deviceCount = BrainMonitorSDK.SDK_GetScanDevicesCount();
            // System.Diagnostics.Debug.WriteLine($"扫描到 {deviceCount} 个设备");
            
            // 清空并重新填充设备列表
            DeviceComboBox.Items.Clear();
            scannedDevices.Clear();
            
            for (int i = 0; i < deviceCount; i++)
            {
                DeviceInfo device = new DeviceInfo();
                if (BrainMonitorSDK.SDK_GetScanDevice(i, ref device) == 1)
                {
                    scannedDevices.Add(device);
                    string deviceName = device.Name?.Trim() ?? "未知设备";
                    string displayName = $"{deviceName} ({device.Mac})";
                    DeviceComboBox.Items.Add(displayName);
                    // System.Diagnostics.Debug.WriteLine($"添加设备: {deviceName} ({device.Mac})");
                }
            }
            
            // 默认选中第一个设备
            if (deviceCount > 0)
            {
                DeviceComboBox.SelectedIndex = 0;
            }
            
            // 更新按钮状态
            UpdateButtonStates(false);
            
            // 更新生成报告按钮状态
            UpdateGenerateReportButtonState();
            
            // 扫描成功后启用设备列表（覆盖UpdateButtonStates的设置）
            DeviceComboBox.IsEnabled = true;
            
            // System.Diagnostics.Debug.WriteLine("设备列表更新完成");
        }

        private void StartDataCollection()
        {
            try
            {
                // 清除之前的波形数据，但保留栅格和基线
                ClearWaveformData();
                
                // 重置数据接收统计变量，确保每次连接都有正确的频率计算
                totalDataPointsReceived = 0;
                dataReceiveCount = 0;
                firstDataReceiveTime = DateTime.MinValue;
                lastDataReceiveTime = DateTime.MinValue;
                
                currentX = BrainwaveCanvas.ActualWidth; // 从右侧开始
                
                // 设置开始时间并启动图表移动
                startTime = DateTime.Now;
                StartTimeUpdateTimer();
                
                // 开始显示0值数据（在收到真实数据之前）
                isShowingZeroData = true;
                zeroDataTimer.Start();
                
                // 先进行设备配置（这是关键步骤！）
                ConfigureDevice();
                
                // 使用SDK开始数据采集
                int result = BrainMonitorSDK.SDK_StartDataCollection();
                if (result == 1)
                {
                    // 数据采集成功启动，数据将通过回调函数接收
                    // 启动显示定时器
                    displayTimer?.Start();
                }
                else
                {
                    Dispatcher.Invoke(() => MessageBox.Show("启动数据采集失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"启动数据采集异常: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
        

        
        private void DrawGridAndBaseline()
        {
            double width = BrainwaveCanvas.ActualWidth;
            double height = BrainwaveCanvas.ActualHeight;
            
            // 清除之前的标签
            YAxisLabelsCanvas.Children.Clear();
            
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
            
            // 水平网格线和刻度标签
            double microVoltRange = 800.0; // -400到+400微伏
            double scale = height / microVoltRange;
            double centerY = height / 2;
            
            // 绘制主要刻度线（每100微伏一条）
            for (int voltage = -400; voltage <= 400; voltage += 100)
            {
                double y = centerY - (voltage * scale);
                
                if (y >= 0 && y <= height)
                {
                    // 主刻度线
                    Line gridLine = new Line
                    {
                        X1 = 0, Y1 = y,
                        X2 = width, Y2 = y,
                        Stroke = voltage == 0 ? new SolidColorBrush(Color.FromRgb(0, 150, 0)) : gridBrush,
                        StrokeThickness = voltage == 0 ? 2 : (voltage % 200 == 0 ? 1 : 0.5),
                        StrokeDashArray = voltage == 0 ? new DoubleCollection { 5, 3 } : null,
                        Tag = voltage == 0 ? "baseline" : "grid"
                    };
                    BrainwaveCanvas.Children.Add(gridLine);
                    
                    // 在左侧Canvas添加刻度标签
                    if (voltage % 200 == 0) // 每200微伏显示一个标签
                    {
                        TextBlock label = new TextBlock
                        {
                            Text = $"{voltage}μV",
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Tag = "scale_label"
                        };
                        Canvas.SetRight(label, 5); // 右对齐到左侧Canvas
                        
                        // 确保标签不会被遮挡，特别是顶部和底部的标签
                        double labelTop = Math.Max(0, Math.Min(YAxisLabelsCanvas.ActualHeight - 16, y - 8));
                        Canvas.SetTop(label, labelTop);
                        YAxisLabelsCanvas.Children.Add(label);
                    }
                }
            }
            
            // 绘制次要刻度线（每50微伏一条）
            for (int voltage = -350; voltage <= 350; voltage += 100)
            {
                double y = centerY - (voltage * scale);
                
                if (y >= 0 && y <= height)
                {
                    Line minorGridLine = new Line
                    {
                        X1 = 0, Y1 = y,
                        X2 = width, Y2 = y,
                        Stroke = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                        StrokeThickness = 0.3,
                        Tag = "grid"
                    };
                    BrainwaveCanvas.Children.Add(minorGridLine);
                }
            }
            
            // 绘制横坐标时间标签
            DrawTimeLabels();
        }
        
        private void MoveWaveformLeftByPixels(double moveDistance)
        {
            var elementsToRemove = new List<UIElement>();
            
            foreach (UIElement element in BrainwaveCanvas.Children)
            {
                if (element is FrameworkElement fe && fe.Tag?.ToString() == "waveform")
                {
                    if (element is Line line)
                    {
                        line.X1 -= moveDistance;
                        line.X2 -= moveDistance;
                        
                        // 标记完全移出左边界的线条
                        if (line.X2 < 0)
                        {
                            elementsToRemove.Add(element);
                        }
                    }
                    else if (element is Ellipse ellipse)
                    {
                        double currentLeft = Canvas.GetLeft(ellipse);
                        Canvas.SetLeft(ellipse, currentLeft - moveDistance);
                        
                        // 标记完全移出左边界的点
                        if (currentLeft - moveDistance + ellipse.Width < 0)
                        {
                            elementsToRemove.Add(element);
                        }
                    }
                }
            }
            
            // 删除移出边界的元素
            foreach (var element in elementsToRemove)
            {
                BrainwaveCanvas.Children.Remove(element);
            }
        }
        
        private void MoveWaveformLeft(int dataPointCount = 1)
        {
            if (!isDeviceConnected) return;
            
            double canvasWidth = BrainwaveCanvas.ActualWidth;
            if (canvasWidth <= 0) return;
            
            // 使用实际的数据接收频率计算移动距离
            // 对于0值数据，使用固定的520Hz采样率
            double actualSamplingRate;
            if (isShowingZeroData)
            {
                // 0值数据使用固定采样率
                actualSamplingRate = 520.0;
            }
            else
            {
                // 真实数据使用实际接收频率
                actualSamplingRate = totalDataPointsReceived > 0 && dataReceiveCount > 0 ? 
                    (totalDataPointsReceived / ((DateTime.Now - firstDataReceiveTime).TotalSeconds)) : 520.0;
            }
            double pixelsPerDataPoint = canvasWidth / (DISPLAY_WINDOW_SECONDS * actualSamplingRate);
            double moveDistance = dataPointCount * pixelsPerDataPoint;
            
            MoveWaveformLeftByPixels(moveDistance);
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
            // 检查曲线颜色状态
            if (isWaveformRed)
            {
                MessageBox.Show("目前脑电波波动太大，无法开始测试，请保持稳定后，等待曲线变成蓝色再点击", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // 设置测试开始状态
            isTestStarted = true;
            isTestCompleted = false;
            
            // 禁用生成报告按钮，直到测试完成
            UpdateGenerateReportButtonState();
            
            // 跳转到测试流程界面
            var testProcessPage = new TestProcessPage(CurrentTester);
            testProcessPage.ReturnToTestPage += TestProcessPage_ReturnToTestPage;
            testProcessPage.TestCompleted += TestProcessPage_TestCompleted;
            NavigationManager.NavigateTo(testProcessPage);
        }
        
        private void TestProcessPage_ReturnToTestPage(object sender, EventArgs e)
        {
            // 从测试流程界面返回到测试界面
            NavigationManager.NavigateTo(this);
            
            // 如果测试已完成，启用生成报告按钮
            if (isTestCompleted)
            {
                UpdateGenerateReportButtonState();
            }
        }
        
        private void TestProcessPage_TestCompleted(object sender, EventArgs e)
        {
            // 测试流程完成，设置测试完成状态
            isTestCompleted = true;
            
            // 更新生成报告按钮状态
            UpdateGenerateReportButtonState();
        }
        
        private void UpdateGenerateReportButtonState()
        {
            // 只有在测试已开始且测试流程已完成的情况下，才启用生成报告按钮
            bool shouldEnable = isTestStarted && isTestCompleted;
            
            // 在UI线程中更新按钮状态
            Dispatcher.Invoke(() =>
            {
                // 直接使用按钮引用更新其启用状态
                GenerateReportButton.IsEnabled = shouldEnable;
            });
        }

        private void GetReportButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取输入值，允许为空
            double? macaScore = null;
            double? mmseScore = null;
            double? gripStrength = null;

            // 验证MACA评分（如果有输入）
            if (!string.IsNullOrWhiteSpace(MacaScoreTextBox.Text))
            {
                if (!double.TryParse(MacaScoreTextBox.Text, out double maca) || maca < 0 || maca > 30)
                {
                    Dispatcher.Invoke(() => MessageBox.Show("MACA评分必须是0-30之间的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }
                macaScore = maca;
            }

            // 验证MMSE评分（如果有输入）
            if (!string.IsNullOrWhiteSpace(MmseScoreTextBox.Text))
            {
                if (!double.TryParse(MmseScoreTextBox.Text, out double mmse) || mmse < 0 || mmse > 30)
                {
                    Dispatcher.Invoke(() => MessageBox.Show("MMSE评分必须是0-30之间的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }
                mmseScore = mmse;
            }

            // 验证握力值（如果有输入）
            if (!string.IsNullOrWhiteSpace(GripStrengthTextBox.Text))
            {
                if (!double.TryParse(GripStrengthTextBox.Text, out double grip) || grip < 0)
                {
                    Dispatcher.Invoke(() => MessageBox.Show("握力值必须是大于等于0的数字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }
                gripStrength = grip;
            }

            // 停止数据采集和脑电波模拟
            StopDataCollection();

            // 导航到报告页面
            NavigationManager.NavigateTo(new ReportPage(CurrentTester, macaScore, mmseScore, gripStrength));
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 停止数据采集和脑电波模拟
            StopDataCollection();
            
            // 返回到医护人员操作页面
            NavigationManager.NavigateTo(new MedicalStaffPage());
        }
        
        private void StopDataCollection()
        {
            try
            {
                // 停止显示定时器
                displayTimer?.Stop();
                
                // 清空缓冲队列
                lock (bufferLock)
                {
                    dataBuffer.Clear();
                }
                
                if (sdkInitialized)
                {
                    // 停止SDK数据采集
                    BrainMonitorSDK.SDK_StopDataCollection();
                    
                    // 断开已连接的设备
                    foreach (var device in connectedDevices)
                    {
                        BrainMonitorSDK.SDK_DisconnectDevice(device.Mac);
                    }
                    connectedDevices.Clear();
                    
                    // 注意：这里不调用SDK_DisconnectPort()和SDK_Cleanup()
                    // 保持SDK初始化状态，以便重新扫描设备时能使用真实SDK
                }
            }
            catch (Exception ex)
            {
                // 忽略清理过程中的异常
            }
        }
    }
}