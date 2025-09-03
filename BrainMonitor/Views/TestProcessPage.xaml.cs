using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BrainMonitor.Models;
using BrainMonitor.Services;

namespace BrainMonitor.Views
{
    public partial class TestProcessPage : UserControl
    {
        private DispatcherTimer instructionTimer;
        private DispatcherTimer countdownTimer;
        private DispatcherTimer audioWaitTimer; // 音频播放完成后的等待定时器
        private DispatcherTimer dataRecordTimer; // 数据记录定时器
        private int currentStep = 0;
        private int countdownSeconds = 0;
        private bool isCountingDown = false;
        private bool isAudioPlaying = false; // 标记是否正在播放音频
        private bool isRecordingData = false; // 标记是否正在记录数据
        
        // 音频播放器
        private MediaPlayer audioPlayer;
        
        // 取消令牌，用于取消后台任务
        private CancellationTokenSource cancellationTokenSource;
        
        // 数据记录相关
        private List<string> currentTestData = new List<string>(); // 当前测试的数据
        private string currentTestType = ""; // 当前测试类型（睁眼/闭眼）
        private DateTime testStartTime; // 测试开始时间
        
        // 当前测试者信息
        private Tester currentTester;
        
        // 存储睁眼和闭眼的测试结果ID
        private int openEyesResultId = 0;
        private int closedEyesResultId = 0;
        
        // 存储当前测试记录ID
        private int currentTestRecordId = 0;
        
        // 存储闭眼测试的原始数据
        private List<double> closedEyesRawData = new List<double>();
        
        // 脑电数据处理器
        private BrainwaveDataProcessor brainwaveProcessor;
        
        // 测试流程步骤 - 现在有8个步骤
        private readonly string[] instructions = new string[]
        {
            "下面将进行睁眼测试，测试时间为3分钟",
            "测试过程中请盯着界面中的点，保持头不动，尽量不眨眼",
            "现在开始睁眼测试，剩余时间：{0}",
            "睁眼测试结束",
            "下面将进行闭眼测试，测试时间为3分钟",
            "测试过程中请闭上双眼，保持头和眼球不动",
            "现在开始闭眼测试，剩余时间：{0}",
            "现在测试结束，请返回测试界面生成报告"
        };
        
        // 每个步骤的持续时间（秒）
        private readonly int[] stepDurations = new int[]
        {
            3,  // 第一个指令显示3秒
            3,  // 第二个指令显示3秒
            10,  // 第一次倒计时10秒（临时调试用）
            3,  // 第四个指令显示3秒
            3,  // 第五个指令显示3秒
            10,  // 第二次倒计时10秒（临时调试用）
            3,  // 第七个指令显示3秒
            -1   // 最后一个指令一直显示
        };
        
        // 音频文件路径（相对于应用程序执行目录）
        private readonly string[] audioFiles = new string[]
        {
            "audio/audio1.mp3",
            "audio/audio2.mp3",
            "audio/audio3.mp3",
            "audio/audio4.mp3",
            "audio/audio5.mp3",
            "audio/audio6.mp3",
            "audio/audio7.mp3",
            "audio/audio8.mp3"
        };
        
        public event EventHandler ReturnToTestPage;
        public event EventHandler<TestResultsEventArgs> TestCompletedWithResults; // 带测试结果的测试完成事件

        public TestProcessPage(Tester tester = null)
        {
            InitializeComponent();
            
            // 设置当前测试者
            currentTester = tester ?? new Tester { Name = "测试者" };
            
            // 初始化音频播放器
            audioPlayer = new MediaPlayer();
            
            // 初始化取消令牌
            cancellationTokenSource = new CancellationTokenSource();
            
            // 初始化脑电数据处理器
            brainwaveProcessor = new BrainwaveDataProcessor();
            
            InitializeTimers();
            StartTestProcess();
            
            // 订阅页面卸载事件
            this.Unloaded += TestProcessPage_Unloaded;
        }
        
        private void TestProcessPage_Unloaded(object sender, RoutedEventArgs e)
        {
            // 页面卸载时清理资源
            Cleanup();
        }

        private void InitializeTimers()
        {
            // 指令切换定时器
            instructionTimer = new DispatcherTimer();
            instructionTimer.Interval = TimeSpan.FromSeconds(1);
            instructionTimer.Tick += InstructionTimer_Tick;
            
            // 倒计时定时器
            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = TimeSpan.FromSeconds(1);
            countdownTimer.Tick += CountdownTimer_Tick;

            // 音频播放完成后的等待定时器
            audioWaitTimer = new DispatcherTimer();
            audioWaitTimer.Interval = TimeSpan.FromSeconds(1); // 等待1秒
            audioWaitTimer.Tick += AudioWaitTimer_Tick;
            
            // 数据记录定时器
            dataRecordTimer = new DispatcherTimer();
            dataRecordTimer.Interval = TimeSpan.FromMilliseconds(100); // 每100ms记录一次数据
            dataRecordTimer.Tick += DataRecordTimer_Tick;
        }

        private void StartTestProcess()
        {
            currentStep = 0;
            ShowCurrentInstruction();
            
            // 不再启动instructionTimer，因为现在由音频播放控制流程
            // 所有的流程切换都由音频播放完成后的等待定时器控制
        }

        private void ShowCurrentInstruction()
        {
            if (currentStep < instructions.Length)
            {
                string instruction = instructions[currentStep];
                
                // 播放对应的音频
                PlayAudio(currentStep);
                
                // 如果是倒计时步骤，显示倒计时
                if (currentStep == 2 || currentStep == 6)
                {
                    countdownSeconds = 10; // 10秒（临时调试用）
                    isCountingDown = true;
                    
                    // 设置测试类型
                    if (currentStep == 2)
                    {
                        currentTestType = "睁眼";
                    }
                    else
                    {
                        currentTestType = "闭眼";
                    }
                    
                    // 开始记录数据
                    StartDataRecording();
                    
                    instruction = string.Format(instruction, FormatTime(countdownSeconds));
                    countdownTimer.Start();
                }
                
                InstructionText.Text = instruction;
            }
        }

        private void InstructionTimer_Tick(object sender, EventArgs e)
        {
            // 已禁用 - 现在流程完全由音频播放控制
            instructionTimer.Stop();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            if (isCountingDown && countdownSeconds > 0)
            {
                countdownSeconds--;
                
                // 更新倒计时显示
                string instruction = instructions[currentStep];
                instruction = string.Format(instruction, FormatTime(countdownSeconds));
                InstructionText.Text = instruction;
                
                                    // 倒计时结束
                    if (countdownSeconds <= 0)
                    {
                        countdownTimer.Stop();
                        isCountingDown = false;
                        
                        // 停止记录数据并保存
                        if (currentStep == 6)
                        {
                            // 闭眼测试完成，等待文件上传完成后再触发事件
                            _ = Task.Run(async () => {
                                await StopDataRecordingAndSave();
                                
                                // 等待一小段时间确保文件上传完成
                                await Task.Delay(1000);
                                
                                // 处理闭眼脑电数据
                                var brainwaveResult = ProcessClosedEyesBrainwaveData();
                                
                                // 在UI线程中触发事件
                                Dispatcher.Invoke(() => {
                                    var testResultsArgs = new TestResultsEventArgs
                                    {
                                        OpenEyesResultId = openEyesResultId,
                                        ClosedEyesResultId = closedEyesResultId,
                                        ThetaValue = brainwaveResult.ThetaValue,
                                        AlphaValue = brainwaveResult.AlphaValue,
                                        BetaValue = brainwaveResult.BetaValue,
                                        BrainwaveFinalIndex = brainwaveResult.BrainwaveFinalIndex,
                                        ClosedEyesData = closedEyesRawData
                                    };
                                    TestCompletedWithResults?.Invoke(this, testResultsArgs);
                                });
                            });
                        }
                        else
                        {
                            // 睁眼测试，异步保存但不触发事件
                            _ = Task.Run(async () => await StopDataRecordingAndSave());
                        }
                        
                        audioWaitTimer.Start();
                    }
            }
        }
        
        // 开始记录数据
        private void StartDataRecording()
        {
            currentTestData.Clear();
            testStartTime = DateTime.Now;
            isRecordingData = true;
            dataRecordTimer.Start();
            
            // 如果是闭眼测试，清空闭眼数据存储
            if (currentStep == 6) // 闭眼测试步骤
            {
                closedEyesRawData.Clear();
            }
        }
        
        // 停止记录数据并保存
        private async Task StopDataRecordingAndSave()
        {
            if (!isRecordingData) return;
            
            isRecordingData = false;
            dataRecordTimer.Stop();
            
            // 保存数据到文件
            await SaveDataToFile();
        }
        
        // 数据记录定时器事件
        private void DataRecordTimer_Tick(object sender, EventArgs e)
        {
            if (isRecordingData)
            {
                // 从实际设备获取脑电波数据
                double brainwaveData = GetActualDeviceData();
                if (brainwaveData != double.MinValue) // 检查是否获取到有效数据
                {
                    currentTestData.Add(brainwaveData.ToString("F2"));
                    
                    // 如果是闭眼测试，同时存储到闭眼数据中
                    if (currentStep == 6) // 闭眼测试步骤
                    {
                        closedEyesRawData.Add(brainwaveData);
                    }
                }
                // 如果没有获取到有效数据，不记录任何内容，等待下一次数据
            }
        }
        
        // 从实际设备获取脑电波数据
        private double GetActualDeviceData()
        {
            try
            {
                // 从全局数据管理器获取最新的脑电波数据
                if (GlobalBrainwaveDataManager.HasData())
                {
                    double latestData = GlobalBrainwaveDataManager.GetLatestBrainwaveData();
                    if (latestData != double.MinValue)
                    {
                        return latestData;
                    }
                }
                
                // 如果没有可用数据，返回特殊值表示无数据
                return double.MinValue;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                return double.MinValue;
            }
        }
        
        // 保存数据到文件
        private async Task SaveDataToFile()
        {
            try
            {
                // 获取机构ID、医护人员姓名、测试者姓名
                string institutionId = GetCurrentInstitutionId();
                string staffName = GetCurrentStaffName();
                string testerName = GetCurrentTesterName();
                
                // 创建目录
                string baseDir = "data";
                string institutionDir = Path.Combine(baseDir, institutionId);
                string staffDir = Path.Combine(institutionDir, staffName);
                string testerDir = Path.Combine(staffDir, testerName);
                
                Directory.CreateDirectory(testerDir);
                
                // 生成文件名 - 使用北京时间
                TimeZoneInfo beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                DateTime beijingTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, beijingTimeZone);
                string timestamp = beijingTime.ToString("yyyyMMdd_HHmmss");
                string fileName = $"{timestamp}_{currentTestType}.csv";
                string filePath = Path.Combine(testerDir, fileName);
                
                // 写入CSV文件
                File.WriteAllLines(filePath, currentTestData, Encoding.UTF8);
                
                // 上传文件到后端服务器
                await UploadFileToServer(filePath, fileName, institutionId, staffName, testerName);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ModernMessageBoxWindow.Show($"保存数据失败: {ex.Message}", "错误", ModernMessageBoxWindow.MessageBoxType.Error);
                });
            }
        }
        
        // 上传文件到后端服务器
        private async Task UploadFileToServer(string filePath, string fileName, string institutionId, string staffName, string testerName)
        {
            try
            {
                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    return;
                }
                
                // 文件上传不需要测试记录ID
                
                // 获取当前医护人员ID
                int medicalStaffId = GetCurrentMedicalStaffId();
                if (medicalStaffId <= 0)
                {
                    return;
                }
                
                // 获取JWT令牌
                string token = GetCurrentAuthToken();
                if (string.IsNullOrEmpty(token))
                {
                    return;
                }
                
                // 创建HTTP客户端
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    
                    // 创建multipart form data
                    using (var formData = new System.Net.Http.MultipartFormDataContent())
                    {
                        // 添加文件
                        // 读取文件内容到字节数组，避免流关闭问题
                        byte[] fileBytes = File.ReadAllBytes(filePath);
                        var fileContent = new System.Net.Http.ByteArrayContent(fileBytes);
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
                        formData.Add(fileContent, "csvFile", fileName);
                        
                        // 添加其他参数
                        formData.Add(new System.Net.Http.StringContent(currentTestType), "dataType");
                        formData.Add(new System.Net.Http.StringContent(institutionId), "institutionId");
                        formData.Add(new System.Net.Http.StringContent(staffName), "staffName");
                        formData.Add(new System.Net.Http.StringContent(testerName), "testerName");
                        
                        // 发送请求
                        var response = await httpClient.PostAsync("http://localhost:3000/api/brainwave-data/upload", formData);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            
                            // 解析响应，获取测试结果ID
                            try
                            {
                                var responseData = System.Text.Json.JsonSerializer.Deserialize<UploadResponse>(responseContent);
                                if (responseData.data != null)
                                {
                                    // 根据数据类型存储测试结果ID
                                    if (currentTestType == "睁眼")
                                    {
                                        openEyesResultId = responseData.data.testResultId;
                                    }
                                    else if (currentTestType == "闭眼")
                                    {
                                        closedEyesResultId = responseData.data.testResultId;
                                    }
                                    
                                    // 更新test_results表中的Theta、Alpha、Beta值（睁眼和闭眼都需要）
                                    if (responseData.data.testResultId > 0)
                                    {
                                        await UpdateTestResultWithBrainwaveData(responseData.data.testResultId);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                // 忽略解析错误
                            }
                        }
                    }
                }
                
            }
            catch (Exception)
            {
                // 忽略上传异常
            }
        }
        
        // 文件上传不需要测试记录ID
        private async Task<int> GetCurrentTestRecordIdAsync()
        {
            try
            {
                return 0; // 不需要测试记录ID
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        // 创建测试记录
        private async Task<int> CreateTestRecordAsync()
        {
            try
            {
                // 获取必要信息
                string institutionId = GetCurrentInstitutionId();
                string staffName = GetCurrentStaffName();
                string testerName = GetCurrentTesterName();
                int medicalStaffId = GetCurrentMedicalStaffId();
                string token = GetCurrentAuthToken();
                
                if (string.IsNullOrEmpty(token))
                {
                    return 0;
                }
                
                // 创建测试记录数据
                var testRecordData = new
                {
                    testerId = currentTester.ID,
                    testerName = testerName,
                    medicalStaffId = medicalStaffId,
                    medicalStaffName = staffName,
                    institutionId = institutionId,
                    testDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    testStatus = "进行中"
                };
                
                // 调用后端API创建测试记录
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(testRecordData);
                    var content = new System.Net.Http.StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync("http://localhost:3000/api/test-records", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
        
                        
                        // 解析响应获取测试记录ID
                        try
                        {
                            var responseData = System.Text.Json.JsonSerializer.Deserialize<TestRecordResponse>(responseContent);
                            if (responseData.success && responseData.data != null)
                            {
                                return responseData.data.testRecordId;
                            }
                        }
                        catch (Exception parseEx)
                        {
                            // 解析异常
                        }
                    }
                    else
                    {
                        // 创建测试记录失败
                    }
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        
        // 获取当前医护人员ID
        private int GetCurrentMedicalStaffId()
        {
            try
            {
                // 从全局医护人员管理获取当前登录的医护人员ID
                if (GlobalMedicalStaffManager.CurrentLoggedInStaff != null)
                {
                    // 尝试从JWT token中解析用户ID
                    var token = GlobalMedicalStaffManager.CurrentToken;
                    if (!string.IsNullOrEmpty(token))
                    {
                        try
                        {
                            // 解析JWT token获取用户ID
                            var tokenParts = token.Split('.');
                            if (tokenParts.Length == 3)
                            {
                                var payload = tokenParts[1];
                                var paddedPayload = payload.PadRight(4 * ((payload.Length + 3) / 4), '=');
                                var decodedPayload = Convert.FromBase64String(paddedPayload.Replace('-', '+').Replace('_', '/'));
                                var jsonPayload = System.Text.Encoding.UTF8.GetString(decodedPayload);
                                
                                // 解析JSON获取userId
                                var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonPayload);
                                if (tokenData.ContainsKey("userId") && tokenData["userId"].ValueKind != JsonValueKind.Null)
                                {
                                    var userId = tokenData["userId"].GetInt32();
                    
                                    return userId;
                                }
                            }
                        }
                        catch (Exception tokenEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"解析JWT token失败: {tokenEx.Message}");
                        }
                    }
                    
                    // 如果无法从token解析，尝试从全局状态获取
                    if (GlobalMedicalStaffManager.CurrentLoggedInStaff.Id > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"从全局状态获取医护人员ID: {GlobalMedicalStaffManager.CurrentLoggedInStaff.Id}");
                        return GlobalMedicalStaffManager.CurrentLoggedInStaff.Id;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("无法获取医护人员ID，返回0");
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取医护人员ID异常: {ex.Message}");
                return 0;
            }
        }
        
        // 获取当前认证令牌
        private string GetCurrentAuthToken()
        {
            try
            {
                // 从全局医护人员管理获取当前用户的JWT令牌
                if (GlobalMedicalStaffManager.CurrentToken != null)
                {
                    return GlobalMedicalStaffManager.CurrentToken;
                }
                
                return "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        
        // 获取当前机构ID
        private string GetCurrentInstitutionId()
        {
            // 从全局机构管理获取当前机构ID
            return GlobalInstitutionManager.CurrentInstitutionId;
        }
        
        // 获取当前医护人员姓名
        private string GetCurrentStaffName()
        {
            if (GlobalMedicalStaffManager.CurrentLoggedInStaff != null)
            {
                return GlobalMedicalStaffManager.CurrentLoggedInStaff.Name ?? "未知医护人员";
            }
            return "未知医护人员";
        }
        
        // 获取当前测试者姓名
        private string GetCurrentTesterName()
        {
            // 这里需要从父窗口或全局状态获取当前测试者信息
            // 暂时使用默认值
            return currentTester.Name;
        }
        
        /// <summary>
        /// 处理闭眼脑电数据并计算相关指标
        /// </summary>
        /// <returns>脑电处理结果</returns>
        private BrainwaveProcessResult ProcessClosedEyesBrainwaveData()
        {
            try
            {
                if (closedEyesRawData == null || closedEyesRawData.Count == 0)
                {
                    return new BrainwaveProcessResult
                    {
                        Success = false,
                        ErrorMessage = "闭眼测试数据为空"
                    };
                }
                
                // 使用脑电数据处理器处理数据
                var result = brainwaveProcessor.ProcessClosedEyesData(closedEyesRawData);
                
                return result;
            }
            catch (Exception ex)
            {
                return new BrainwaveProcessResult
                {
                    Success = false,
                    ErrorMessage = $"处理异常: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// 处理睁眼脑电数据并计算相关指标
        /// </summary>
        /// <returns>脑电处理结果</returns>
        private BrainwaveProcessResult ProcessOpenEyesBrainwaveData()
        {
            try
            {
                if (currentTestData == null || currentTestData.Count == 0)
                {
                    return new BrainwaveProcessResult
                    {
                        Success = false,
                        ErrorMessage = "睁眼测试数据为空"
                    };
                }
                
                // 将字符串数据转换为double列表
                var rawData = new List<double>();
                foreach (var dataPoint in currentTestData)
                {
                    if (double.TryParse(dataPoint, out double value))
                    {
                        rawData.Add(value);
                    }
                }
                
                if (rawData.Count == 0)
                {
                    return new BrainwaveProcessResult
                    {
                        Success = false,
                        ErrorMessage = "睁眼测试数据格式错误"
                    };
                }
                
                // 使用脑电数据处理器处理数据
                var result = brainwaveProcessor.ProcessClosedEyesData(rawData);
                
                return result;
            }
            catch (Exception ex)
            {
                return new BrainwaveProcessResult
                {
                    Success = false,
                    ErrorMessage = $"处理异常: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// 更新test_results表中的Theta、Alpha、Beta值
        /// </summary>
        /// <param name="testResultId">测试结果ID</param>
        /// <returns>是否更新成功</returns>
        private async Task<bool> UpdateTestResultWithBrainwaveData(int testResultId)
        {
            try
            {
                // 处理脑电数据（睁眼或闭眼）
                BrainwaveProcessResult brainwaveResult;
                if (currentTestType == "闭眼")
                {
                    brainwaveResult = ProcessClosedEyesBrainwaveData();
                }
                else
                {
                    // 睁眼测试也使用相同的数据处理逻辑
                    brainwaveResult = ProcessOpenEyesBrainwaveData();
                }
                
                if (!brainwaveResult.Success)
                {
                    return false;
                }
                
                // 获取JWT令牌
                string token = GetCurrentAuthToken();
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }
                
                // 创建HTTP客户端
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    
                    // 准备更新数据
                    var updateData = new
                    {
                        testResultId = testResultId,
                        thetaValue = brainwaveResult.ThetaValue,
                        alphaValue = brainwaveResult.AlphaValue,
                        betaValue = brainwaveResult.BetaValue
                    };
                    
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(updateData);
                    var content = new System.Net.Http.StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                    
                    // 发送更新请求
                    var response = await httpClient.PutAsync("http://localhost:3000/api/brainwave-data/update-result", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void NextStep()
        {
            currentStep++;
            
            if (currentStep < instructions.Length)
            {
                ShowCurrentInstruction();
            }
            // 注意：测试完成现在在CountdownTimer_Tick中处理，不需要在这里处理
            // 第八步会一直显示，直到用户点击返回
        }

        private void ResetStepDuration(int step)
        {
            // 重置步骤持续时间到原始值
            int[] originalDurations = { 3, 3, 180, 3, 3, 180, 3, -1 };
            if (step < originalDurations.Length)
            {
                stepDurations[step] = originalDurations[step];
            }
        }

        private string FormatTime(int seconds)
        {
            // 格式化时间为 MM:SS 格式
            int minutes = seconds / 60;
            int remainingSeconds = seconds % 60;
            return $"{minutes:D2}:{remainingSeconds:D2}";
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 如果正在记录数据，先停止并保存（异步处理，不等待完成）
            if (isRecordingData)
            {
                _ = Task.Run(async () => await StopDataRecordingAndSave());
            }
            
            // 取消所有后台任务
            cancellationTokenSource?.Cancel();
            
            // 停止所有定时器
            instructionTimer?.Stop();
            countdownTimer?.Stop();
            audioWaitTimer?.Stop();
            dataRecordTimer?.Stop();
            
            // 停止音频播放
            if (audioPlayer != null)
            {
                audioPlayer.Stop();
            }
            
            // 重置状态
            isAudioPlaying = false;
            isCountingDown = false;
            isRecordingData = false;
            currentStep = 0;
            
            // 注意：测试完成现在在CountdownTimer_Tick中处理，不需要在这里检查
            
            // 触发返回事件
            ReturnToTestPage?.Invoke(this, EventArgs.Empty);
        }

        // 清理资源
        public void Cleanup()
        {
            // 如果正在记录数据，先停止并保存（异步处理，不等待完成）
            if (isRecordingData)
            {
                _ = Task.Run(async () => await StopDataRecordingAndSave());
            }
            
            // 取消所有后台任务
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            
            // 停止所有定时器
            instructionTimer?.Stop();
            countdownTimer?.Stop();
            audioWaitTimer?.Stop();
            dataRecordTimer?.Stop();
            instructionTimer = null;
            countdownTimer = null;
            audioWaitTimer = null;
            dataRecordTimer = null;
            
            // 清理音频播放器
            if (audioPlayer != null)
            {
                audioPlayer.MediaEnded -= AudioPlayer_MediaEnded;
                audioPlayer.MediaFailed -= AudioPlayer_MediaFailed;
                audioPlayer.Stop();
                audioPlayer.Close();
                audioPlayer = null;
            }
            
            // 重置状态
            isAudioPlaying = false;
            isCountingDown = false;
            isRecordingData = false;
            currentStep = 0;
        }

        // 音频播放完成后的等待逻辑
        private void AudioWaitTimer_Tick(object sender, EventArgs e)
        {
            audioWaitTimer.Stop();
            isAudioPlaying = false;
            NextStep();
        }
        
        // 播放音频文件
        private void PlayAudio(int stepIndex)
        {
            try
            {
                if (stepIndex < audioFiles.Length)
                {
                    string audioPath = audioFiles[stepIndex];
                    // 从应用程序执行目录获取音频文件路径
                    string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string fullAudioPath = System.IO.Path.Combine(appDirectory, audioPath);
                    
                    if (File.Exists(fullAudioPath))
                    {
                        if (audioPlayer != null)
                        {
                            audioPlayer.Stop();
                            audioPlayer.Close();
                        }
                        
                        audioPlayer = new MediaPlayer();
                        audioPlayer.MediaEnded += AudioPlayer_MediaEnded;
                        audioPlayer.MediaFailed += AudioPlayer_MediaFailed;
                        
                        audioPlayer.Open(new Uri(fullAudioPath));
                        
                        audioPlayer.MediaOpened += (s, e) => {
                            try
                            {
                                if (audioPlayer.NaturalDuration.HasTimeSpan)
                                {
                                    TimeSpan duration = audioPlayer.NaturalDuration.TimeSpan;
                                    double audioLengthSeconds = duration.TotalSeconds;
                                    
                                    audioPlayer.Play();
                                    isAudioPlaying = true;
                                    
                                    double waitTimeSeconds = Math.Max(audioLengthSeconds + 0.5, 3.0);
                                    
                                    System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(waitTimeSeconds), cancellationTokenSource.Token).ContinueWith(t =>
                                    {
                                        if (!t.IsCanceled && Dispatcher != null)
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                if (isAudioPlaying)
                                                {
                                                    AudioPlayer_MediaEnded(null, EventArgs.Empty);
                                                }
                                            });
                                        }
                                    }, cancellationTokenSource.Token);
                                }
                                else
                                {
                                    audioPlayer.Play();
                                    isAudioPlaying = true;
                                    
                                    System.Threading.Tasks.Task.Delay(10000, cancellationTokenSource.Token).ContinueWith(t =>
                                    {
                                        if (!t.IsCanceled && Dispatcher != null)
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                if (isAudioPlaying)
                                                {
                                                    AudioPlayer_MediaEnded(null, EventArgs.Empty);
                                                }
                                            });
                                        }
                                    }, cancellationTokenSource.Token);
                                }
                            }
                            catch (Exception playEx)
                            {
                                isAudioPlaying = false;
                                if (stepIndex != 2 && stepIndex != 6)
                                {
                                    audioWaitTimer.Start();
                                }
                            }
                        };
                        
                        try
                        {
                            audioPlayer.Play();
                            isAudioPlaying = true;
                        }
                        catch (Exception playEx)
                        {
                            // 等待MediaOpened事件
                        }
                    }
                    else
                    {
                        isAudioPlaying = false;
                        
                        if (stepIndex != 2 && stepIndex != 6)
                        {
                            audioWaitTimer.Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isAudioPlaying = false;
                
                if (stepIndex != 2 && stepIndex != 6)
                {
                    audioWaitTimer.Start();
                }
            }
        }
        

        
        // 音频播放完成事件处理
        private void AudioPlayer_MediaEnded(object sender, EventArgs e)
        {
            if (!isAudioPlaying)
            {
                return;
            }
            
            isAudioPlaying = false;
            
            Dispatcher.Invoke(() =>
            {
                if (currentStep != 2 && currentStep != 6)
                {
                    audioWaitTimer.Start();
                }
            });
        }

        // 音频播放失败事件处理
        private void AudioPlayer_MediaFailed(object sender, ExceptionEventArgs e)
        {
            isAudioPlaying = false;
            
            if (currentStep != 2 && currentStep != 6)
            {
                audioWaitTimer.Start();
            }
        }
    }
    
    // 测试记录响应数据类
    public class TestRecordResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public TestRecordData? data { get; set; }
    }
    
    public class TestRecordData
    {
        public int testRecordId { get; set; }
        public string? testerId { get; set; }
        public string? testerName { get; set; }
        public int medicalStaffId { get; set; }
        public string? medicalStaffName { get; set; }
        public string? institutionId { get; set; }
        public string? testDate { get; set; }
        public string? testStatus { get; set; }
    }
}