using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media.Imaging;
using BrainMonitor.Views;

namespace BrainMonitor.Views
{
    public partial class InstitutionLoginWindow : Window
    {
        public InstitutionLoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前显示的密码
            string password = PasswordBox.Visibility == Visibility.Visible ? PasswordBox.Password : PasswordTextBox.Text;
            
            // 修改登录验证逻辑 - 允许空输入直接登录
            // 如果机构ID和密码都为空，直接登录
            if (string.IsNullOrWhiteSpace(InstitutionIdTextBox.Text) && string.IsNullOrWhiteSpace(password))
            {
                // 空输入直接登录，跳过隐私协议检查
            }
            else
            {
                // 如果有输入内容，则需要检查隐私协议
                if (!PrivacyCheckBox.IsChecked == true)
                {
                    ModernMessageBoxWindow.Show("请同意隐私协议", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                    return;
                }
            }

            try
            {
                // 登录成功，自动登录默认的测试医护人员账号
                bool loginSuccess = await GlobalMedicalStaffManager.LoginAsync("1", "1");
                
                if (loginSuccess)
                {
                    // 打开医护人员操作界面
                    var medicalStaffWindow = new MedicalStaffWindow();
                    medicalStaffWindow.Show();
                    this.Close();
                }
                else
                {
                    ModernMessageBoxWindow.Show("医护人员登录失败", "登录失败", ModernMessageBoxWindow.MessageBoxType.Error);
                }
            }
            catch (System.Exception ex)
            {
                ModernMessageBoxWindow.Show($"登录失败: {ex.Message}", "登录失败", ModernMessageBoxWindow.MessageBoxType.Error);
            }
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换密码显示/隐藏
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                // 显示密码 - 切换到TextBox
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                TogglePasswordIcon.Glyph = "\uE7B2"; // 显示密码图标
            }
            else
            {
                // 隐藏密码 - 切换到PasswordBox
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                TogglePasswordIcon.Glyph = "\uE7B3"; // 隐藏密码图标
            }
        }
    }
}