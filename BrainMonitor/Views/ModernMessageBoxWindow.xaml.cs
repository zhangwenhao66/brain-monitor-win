using System;
using System.Windows;
using System.Windows.Media;
using System.Media;

namespace BrainMirror.Views
{
    public partial class ModernMessageBoxWindow : Window
    {
        public enum MessageBoxType
        {
            Info,
            Warning,
            Error,
            Success
        }

        public enum MessageBoxButtons
        {
            Ok,
            YesNo
        }

        public enum MessageBoxResult
        {
            None,
            Ok,
            Yes,
            No
        }

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public ModernMessageBoxWindow()
        {
            InitializeComponent();
        }

        public void SetMessage(string message, string title = "提示", MessageBoxType type = MessageBoxType.Info, MessageBoxButtons buttons = MessageBoxButtons.Ok)
        {
            MessageText.Text = message;
            TitleText.Text = title;
            Title = title;
            
            // 根据按钮类型显示/隐藏按钮
            if (buttons == MessageBoxButtons.YesNo)
            {
                OkButton.Visibility = Visibility.Collapsed;
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
            }
            else
            {
                OkButton.Visibility = Visibility.Visible;
                YesButton.Visibility = Visibility.Collapsed;
                NoButton.Visibility = Visibility.Collapsed;
            }
            
            // 根据类型设置图标和颜色，并播放相应声音
            switch (type)
            {
                case MessageBoxType.Info:
                    MessageIcon.Glyph = "\uE946"; // 信息图标
                    MessageIcon.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // 蓝色
                    SystemSounds.Asterisk.Play(); // 信息声音
                    break;
                case MessageBoxType.Warning:
                    MessageIcon.Glyph = "\uE7BA"; // 警告图标
                    MessageIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 140, 0)); // 橙色
                    SystemSounds.Exclamation.Play(); // 警告声音
                    break;
                case MessageBoxType.Error:
                    MessageIcon.Glyph = "\uE783"; // 错误图标
                    MessageIcon.Foreground = new SolidColorBrush(Color.FromRgb(232, 17, 35)); // 红色
                    SystemSounds.Hand.Play(); // 错误声音
                    break;
                case MessageBoxType.Success:
                    MessageIcon.Glyph = "\uE930"; // 成功图标
                    MessageIcon.Foreground = new SolidColorBrush(Color.FromRgb(16, 124, 16)); // 绿色
                    SystemSounds.Asterisk.Play(); // 成功声音
                    break;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Ok;
            this.Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            this.Close();
        }

        // 静态方法，方便调用
        public static void Show(string message, string title = "提示", MessageBoxType type = MessageBoxType.Info)
        {
            var messageBox = new ModernMessageBoxWindow();
            messageBox.SetMessage(message, title, type, MessageBoxButtons.Ok);
            
            // 设置父窗口
            if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                messageBox.Owner = Application.Current.MainWindow;
            }
            
            messageBox.ShowDialog();
        }

        // 静态方法，支持返回结果
        public static MessageBoxResult ShowDialog(string message, string title = "提示", MessageBoxType type = MessageBoxType.Info, MessageBoxButtons buttons = MessageBoxButtons.Ok)
        {
            var messageBox = new ModernMessageBoxWindow();
            messageBox.SetMessage(message, title, type, buttons);
            
            // 设置父窗口
            if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                messageBox.Owner = Application.Current.MainWindow;
            }
            
            messageBox.ShowDialog();
            return messageBox.Result;
        }
    }
}
