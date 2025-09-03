using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using BrainMirror.Services;
using System.Text.Json;

namespace BrainMirror.Views
{
    public partial class InstitutionLoginPage : UserControl
    {
        public InstitutionLoginPage()
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
            try
            {
                // 获取当前显示的密码
                string password = LoginPasswordBox.Visibility == Visibility.Visible ? LoginPasswordBox.Password : LoginPasswordTextBox.Text;
                string institutionId = LoginInstitutionIdTextBox.Text.Trim();
                
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
                if (!LoginPrivacyCheckBox.IsChecked == true)
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

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
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

                // 禁用注册按钮，显示加载状态
                RegisterButton.IsEnabled = false;
                RegisterButton.Content = "注册中...";

                // 准备注册数据
                var registerRequest = new InstitutionRegisterRequest
                {
                    InstitutionName = RegisterInstitutionNameTextBox.Text.Trim(),
                    InstitutionId = RegisterInstitutionIdTextBox.Text.Trim(),
                    Password = RegisterPasswordBox.Password,
                    ContactPerson = "", // 不再需要联系人信息
                    ContactPhone = ""   // 不再需要联系电话信息
                };

                // 发送注册请求
                var response = await HttpService.PostAsync<ApiResponse<InstitutionRegisterResponse>>("/auth/institution/register", registerRequest);
                
                if (response.Success)
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
                    ModernMessageBoxWindow.Show(response.Message ?? "注册失败", "注册失败", ModernMessageBoxWindow.MessageBoxType.Error);
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                ModernMessageBoxWindow.Show($"注册失败: {ex.Message}", "注册失败", ModernMessageBoxWindow.MessageBoxType.Error);
            }
            catch (Exception ex)
            {
                ModernMessageBoxWindow.Show($"系统错误: {ex.Message}", "注册失败", ModernMessageBoxWindow.MessageBoxType.Error);
            }
            finally
            {
                // 恢复注册按钮状态
                RegisterButton.IsEnabled = true;
                RegisterButton.Content = "注册";
            }
        }

        private void ClearRegisterForm()
        {
            RegisterInstitutionNameTextBox.Text = "";
            RegisterInstitutionIdTextBox.Text = "";
            RegisterPasswordBox.Password = "";
            RegisterConfirmPasswordBox.Password = "";
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
    }
}