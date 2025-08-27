using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using BrainMonitor.Services;

namespace BrainMonitor.Views
{
    public partial class InstitutionLoginPage : UserControl
    {
        public InstitutionLoginPage()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取当前显示的密码
                string password = PasswordBox.Visibility == Visibility.Visible ? PasswordBox.Password : PasswordTextBox.Text;
                string institutionId = InstitutionIdTextBox.Text.Trim();
                
                // 验证必填字段
                if (string.IsNullOrWhiteSpace(institutionId))
                {
                    ModernMessageBoxWindow.Show("请输入机构ID", "登录失败", ModernMessageBoxWindow.MessageBoxType.Warning);
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(password))
                {
                    ModernMessageBoxWindow.Show("请输入密码", "登录失败", ModernMessageBoxWindow.MessageBoxType.Warning);
                    return;
                }
                
                // 检查隐私协议
                if (!PrivacyCheckBox.IsChecked == true)
                {
                    ModernMessageBoxWindow.Show("请同意隐私协议", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                    return;
                }
                
                // 禁用登录按钮，显示加载状态
                LoginButton.IsEnabled = false;
                LoginButton.Content = "登录中...";
                
                // 调用后端API进行登录验证
                var loginRequest = new InstitutionLoginRequest
                {
                    InstitutionId = institutionId,
                    Password = password
                };
                
                var response = await HttpService.PostAsync<ApiResponse<InstitutionLoginResponse>>("/auth/institution/login", loginRequest);
                
                if (response.Success)
                {
                    // 登录成功
                    GlobalInstitutionManager.SetCurrentInstitution(
                        response.Data.InstitutionId, 
                        response.Data.InstitutionName, 
                        response.Data.InstitutionDbId
                    );
                    
                    // 不自动登录医护人员，保持未登录状态
                    // GlobalMedicalStaffManager.Login("1", "1");
                    
                    // 导航到医护人员操作界面
                    NavigationManager.NavigateTo(new MedicalStaffPage());
                }
                else
                {
                    ModernMessageBoxWindow.Show(response.Message ?? "登录失败", "登录失败", ModernMessageBoxWindow.MessageBoxType.Error);
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                ModernMessageBoxWindow.Show($"登录失败: {ex.Message}", "登录失败", ModernMessageBoxWindow.MessageBoxType.Error);
            }
            catch (Exception ex)
            {
                ModernMessageBoxWindow.Show($"系统错误: {ex.Message}", "登录失败", ModernMessageBoxWindow.MessageBoxType.Error);
            }
            finally
            {
                // 恢复登录按钮状态
                LoginButton.IsEnabled = true;
                LoginButton.Content = "登录";
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