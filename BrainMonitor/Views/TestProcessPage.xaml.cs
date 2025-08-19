using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BrainMonitor.Views
{
    public partial class TestProcessPage : UserControl
    {
        private DispatcherTimer instructionTimer;
        private DispatcherTimer countdownTimer;
        private int currentStep = 0;
        private int countdownSeconds = 0;
        private bool isCountingDown = false;
        
        // 测试流程步骤
        private readonly string[] instructions = new string[]
        {
            "下面将进行睁眼测试，测试时间为1分钟",
            "测试过程中请盯着界面中的点，保持头不动，尽量不眨眼，维持一分钟",
            "现在开始睁眼测试，剩余时间：{0}",
            "下面将进行闭眼测试，测试时间为1分钟",
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
            -1   // 最后一个指令一直显示
        };
        
        public event EventHandler ReturnToTestPage;
        public event EventHandler TestCompleted; // 测试完成事件

        public TestProcessPage()
        {
            InitializeComponent();
            InitializeTimers();
            StartTestProcess();
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
        }

        private void StartTestProcess()
        {
            currentStep = 0;
            ShowCurrentInstruction();
            
            // 开始第一个步骤的计时
            if (stepDurations[currentStep] > 0)
            {
                instructionTimer.Start();
            }
        }

        private void ShowCurrentInstruction()
        {
            if (currentStep < instructions.Length)
            {
                string instruction = instructions[currentStep];
                
                // 如果是倒计时步骤，显示倒计时
                if (currentStep == 2 || currentStep == 5)
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
            // 如果当前步骤有固定持续时间，递减计时
            if (currentStep < stepDurations.Length && stepDurations[currentStep] > 0)
            {
                stepDurations[currentStep]--;
                
                // 时间到了，切换到下一个指令
                if (stepDurations[currentStep] <= 0)
                {
                    instructionTimer.Stop();
                    NextStep();
                }
            }
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
                    NextStep();
                }
            }
        }

        private void NextStep()
        {
            currentStep++;
            
            if (currentStep < instructions.Length)
            {
                ShowCurrentInstruction();
                
                // 如果不是倒计时步骤且有持续时间，启动指令定时器
                if (currentStep < stepDurations.Length && 
                    stepDurations[currentStep] > 0 && 
                    currentStep != 2 && currentStep != 5)
                {
                    // 重置步骤持续时间（因为之前可能被修改）
                    ResetStepDuration(currentStep);
                    instructionTimer.Start();
                }
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
            int[] originalDurations = { 3, 3, 5, 3, 3, 5, -1 };
            if (step < originalDurations.Length)
            {
                stepDurations[step] = originalDurations[step];
            }
        }

        private string FormatTime(int seconds)
        {
            int minutes = seconds / 5;
            int remainingSeconds = seconds % 5;
            return $"{minutes:D2}:{remainingSeconds:D2}";
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 停止所有定时器
            instructionTimer?.Stop();
            countdownTimer?.Stop();
            
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
            instructionTimer?.Stop();
            countdownTimer?.Stop();
            instructionTimer = null;
            countdownTimer = null;
        }
    }
}