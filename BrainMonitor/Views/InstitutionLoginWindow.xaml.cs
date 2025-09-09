using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media.Imaging;
using BrainMirror.Views;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using BrainMirror.Configuration;

namespace BrainMirror.Views
{
    public partial class InstitutionLoginWindow : Window
    {
        public InstitutionLoginWindow()
        {
            InitializeComponent();
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 更新标题文本
            if (MainTabControl.SelectedItem == LoginTab)
            {
                TitleText.Text = "机构登录";
            }
            else if (MainTabControl.SelectedItem == RegisterTab)
            {
                TitleText.Text = "机构注册";
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前显示的密码
            string password = LoginPasswordBox.Visibility == Visibility.Visible ? LoginPasswordBox.Password : LoginPasswordTextBox.Text;
            
            // 修改登录验证逻辑 - 允许空输入直接登录
            // 如果机构ID和密码都为空，直接登录
            if (string.IsNullOrWhiteSpace(LoginInstitutionIdTextBox.Text) && string.IsNullOrWhiteSpace(password))
            {
                // 空输入直接登录，跳过隐私协议检查
            }
            else
            {
                // 如果有输入内容，则需要检查隐私协议
                if (!LoginPrivacyCheckBox.IsChecked == true)
                {
                    ModernMessageBoxWindow.Show("请同意隐私协议", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                    return;
                }
            }

            try
            {
                // 登录成功，自动登录默认的测试工作人员账号
                bool loginSuccess = await GlobalMedicalStaffManager.LoginAsync("1", "1");
                
                if (loginSuccess)
                {
                    // 打开工作人员操作界面
                    var medicalStaffWindow = new MedicalStaffWindow();
                    medicalStaffWindow.Show();
                    this.Close();
                }
                else
                {
                    ModernMessageBoxWindow.Show("工作人员登录失败", "登录失败", ModernMessageBoxWindow.MessageBoxType.Error);
                }
            }
            catch (System.Exception ex)
            {
                ModernMessageBoxWindow.Show($"登录失败: {ex.Message}", "登录失败", ModernMessageBoxWindow.MessageBoxType.Error);
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证必填字段
            if (string.IsNullOrWhiteSpace(RegisterInstitutionNameTextBox.Text))
            {
                ModernMessageBoxWindow.Show("请输入机构名称", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RegisterInstitutionIdTextBox.Text))
            {
                ModernMessageBoxWindow.Show("请输入机构ID", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RegisterPasswordBox.Password))
            {
                ModernMessageBoxWindow.Show("请输入密码", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RegisterConfirmPasswordBox.Password))
            {
                ModernMessageBoxWindow.Show("请输入确认密码", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (RegisterPasswordBox.Password != RegisterConfirmPasswordBox.Password)
            {
                ModernMessageBoxWindow.Show("两次输入的密码不一致", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RegisterContactPersonTextBox.Text))
            {
                ModernMessageBoxWindow.Show("请输入联系人", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RegisterContactPhoneTextBox.Text))
            {
                ModernMessageBoxWindow.Show("请输入联系电话", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            try
            {
                // 准备注册数据
                var registerData = new
                {
                    institutionName = RegisterInstitutionNameTextBox.Text.Trim(),
                    institutionId = RegisterInstitutionIdTextBox.Text.Trim(),
                    password = RegisterPasswordBox.Password,
                    contactPerson = RegisterContactPersonTextBox.Text.Trim(),
                    contactPhone = RegisterContactPhoneTextBox.Text.Trim()
                };

                // 发送注册请求
                using (var httpClient = new HttpClient())
                {
                    var jsonContent = JsonSerializer.Serialize(registerData);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync($"{ConfigHelper.GetApiBaseUrl()}/auth/institution/register", content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (result.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                    {
                        ModernMessageBoxWindow.Show("机构注册成功！", "注册成功", ModernMessageBoxWindow.MessageBoxType.Success);
                        
                        // 清空注册表单
                        ClearRegisterForm();
                        
                        // 切换到登录Tab
                        MainTabControl.SelectedItem = LoginTab;
                        
                        // 自动填充机构ID
                        LoginInstitutionIdTextBox.Text = RegisterInstitutionIdTextBox.Text;
                    }
                    else
                    {
                        string errorMessage = "注册失败";
                        if (result.TryGetProperty("message", out var messageElement))
                        {
                            errorMessage = messageElement.GetString() ?? errorMessage;
                        }
                        ModernMessageBoxWindow.Show(errorMessage, "注册失败", ModernMessageBoxWindow.MessageBoxType.Error);
                    }
                }
            }
            catch (System.Exception ex)
            {
                ModernMessageBoxWindow.Show($"注册失败: {ex.Message}", "注册失败", ModernMessageBoxWindow.MessageBoxType.Error);
            }
        }

        private void ClearRegisterForm()
        {
            RegisterInstitutionNameTextBox.Text = "";
            RegisterInstitutionIdTextBox.Text = "";
            RegisterPasswordBox.Password = "";
            RegisterConfirmPasswordBox.Password = "";
            RegisterContactPersonTextBox.Text = "";
            RegisterContactPhoneTextBox.Text = "";
        }

        private void LoginTogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            // 切换密码显示/隐藏
            if (LoginPasswordBox.Visibility == Visibility.Visible)
            {
                // 显示密码 - 切换到TextBox
                LoginPasswordTextBox.Text = LoginPasswordBox.Password;
                LoginPasswordBox.Visibility = Visibility.Collapsed;
                LoginPasswordTextBox.Visibility = Visibility.Visible;
                LoginTogglePasswordIcon.Glyph = "\uE7B2"; // 显示密码图标
            }
            else
            {
                // 隐藏密码 - 切换到PasswordBox
                LoginPasswordBox.Password = LoginPasswordTextBox.Text;
                LoginPasswordTextBox.Visibility = Visibility.Collapsed;
                LoginPasswordBox.Visibility = Visibility.Visible;
                LoginTogglePasswordIcon.Glyph = "\uE7B3"; // 隐藏密码图标
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // 关闭当前窗口，返回到主窗口
            this.Close();
        }
    }
}