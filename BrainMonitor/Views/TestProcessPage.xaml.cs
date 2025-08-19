using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.IO;

namespace BrainMonitor.Views
{
    public partial class TestProcessPage : UserControl
    {
        private DispatcherTimer instructionTimer;
        private DispatcherTimer countdownTimer;
        private DispatcherTimer audioWaitTimer; // 音频播放完成后的等待定时器
        private int currentStep = 0;
        private int countdownSeconds = 0;
        private bool isCountingDown = false;
        private bool isAudioPlaying = false; // 标记是否正在播放音频
        
        // 音频播放器
        private MediaPlayer audioPlayer;
        
        // 取消令牌，用于取消后台任务
        private CancellationTokenSource cancellationTokenSource;
        
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
            5,  // 第一次倒计时5秒
            3,  // 第四个指令显示3秒
            3,  // 第五个指令显示3秒
            5,  // 第二次倒计时5秒
            3,  // 第七个指令显示3秒
            -1   // 最后一个指令一直显示
        };
        
        // 音频文件路径
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
        public event EventHandler TestCompleted; // 测试完成事件

        public TestProcessPage()
        {
            InitializeComponent();
            
            // 初始化音频播放器
            audioPlayer = new MediaPlayer();
            
            // 初始化取消令牌
            cancellationTokenSource = new CancellationTokenSource();
            
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
                    countdownSeconds = 5;
                    isCountingDown = true;
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
                    audioWaitTimer.Start();
                }
            }
        }

        private void NextStep()
        {
            currentStep++;
            
            if (currentStep < instructions.Length)
            {
                ShowCurrentInstruction();
            }
            else
            {
                // 测试流程完成，触发TestCompleted事件
                TestCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ResetStepDuration(int step)
        {
            // 重置步骤持续时间到原始值
            int[] originalDurations = { 3, 3, 5, 3, 3, 5, 3, -1 };
            if (step < originalDurations.Length)
            {
                stepDurations[step] = originalDurations[step];
            }
        }

        private string FormatTime(int seconds)
        {
            // 由于倒计时只有5秒，直接显示秒数即可
            return $"{seconds:D2}";
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 取消所有后台任务
            cancellationTokenSource?.Cancel();
            
            // 停止所有定时器
            instructionTimer?.Stop();
            countdownTimer?.Stop();
            audioWaitTimer?.Stop();
            
            // 停止音频播放
            if (audioPlayer != null)
            {
                audioPlayer.Stop();
            }
            
            // 重置状态
            isAudioPlaying = false;
            isCountingDown = false;
            currentStep = 0;
            
            // 如果测试流程已经完成，触发TestCompleted事件
            if (currentStep >= instructions.Length - 1)
            {
                TestCompleted?.Invoke(this, EventArgs.Empty);
            }
            
            // 触发返回事件
            ReturnToTestPage?.Invoke(this, EventArgs.Empty);
        }

        // 清理资源
        public void Cleanup()
        {
            // 取消所有后台任务
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            
            // 停止所有定时器
            instructionTimer?.Stop();
            countdownTimer?.Stop();
            audioWaitTimer?.Stop();
            instructionTimer = null;
            countdownTimer = null;
            audioWaitTimer = null;
            
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
                    string projectRoot = GetProjectRootDirectory();
                    string fullAudioPath = System.IO.Path.Combine(projectRoot, audioPath);
                    
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
        
        // 获取项目根目录
        private string GetProjectRootDirectory()
        {
            try
            {
                string currentDir = System.IO.Directory.GetCurrentDirectory();
                
                while (!string.IsNullOrEmpty(currentDir))
                {
                    string audioDir = System.IO.Path.Combine(currentDir, "audio");
                    if (Directory.Exists(audioDir))
                    {
                        return currentDir;
                    }
                    
                    string parentDir = System.IO.Directory.GetParent(currentDir)?.FullName;
                    if (parentDir == currentDir || string.IsNullOrEmpty(parentDir))
                    {
                        break;
                    }
                    currentDir = parentDir;
                }
                
                return System.IO.Directory.GetCurrentDirectory();
            }
            catch (Exception ex)
            {
                return System.IO.Directory.GetCurrentDirectory();
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
}