using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace BrainMonitor.Views
{
    public partial class StaffLoginPage : UserControl, INavigationAware
    {
        public StaffLoginPage()
        {
            InitializeComponent();
        }

        public void OnNavigatedTo()
        {
            // 页面导航到时的处理
        }

        public void OnNavigatedFrom()
        {
            // 页面离开时的处理
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取登录信息
            string account = LoginAccountTextBox.Text.Trim();
            string password = LoginPasswordBox.Password;

            // 验证输入
            if (string.IsNullOrEmpty(account))
            {
                ModernMessageBoxWindow.Show("请输入账号", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ModernMessageBoxWindow.Show("请输入密码", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            // 禁用登录按钮，显示加载状态
            var loginButton = sender as Button;
            if (loginButton != null)
            {
                loginButton.IsEnabled = false;
                loginButton.Content = "登录中...";
            }

            try
            {
                // 尝试登录
                bool loginSuccess = await GlobalMedicalStaffManager.LoginAsync(account, password);
                
                if (loginSuccess)
                {
                    // 登录成功，导航到医护人员操作页面，并传递需要刷新的标志
                    var medicalStaffPage = new MedicalStaffPage();
                    medicalStaffPage.SetRefreshFlag(true);
                    NavigationManager.NavigateTo(medicalStaffPage);
                }
                else
                {
                    ModernMessageBoxWindow.Show("账号或密码错误，请重试", "登录失败", ModernMessageBoxWindow.MessageBoxType.Error);
                    // 清空密码框
                    LoginPasswordBox.Password = "";
                }
            }
            catch (Exception ex)
            {
                ModernMessageBoxWindow.Show($"登录失败: {ex.Message}", "登录失败", ModernMessageBoxWindow.MessageBoxType.Error);
            }
            finally
            {
                // 恢复登录按钮状态
                if (loginButton != null)
                {
                    loginButton.IsEnabled = true;
                    loginButton.Content = "登录";
                }
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取注册信息
            string name = RegisterNameTextBox.Text.Trim();
            string staffId = RegisterStaffIdTextBox.Text.Trim();
            string account = RegisterAccountTextBox.Text.Trim();
            string password = RegisterPasswordBox.Password;

            // 验证输入
            if (string.IsNullOrEmpty(name))
            {
                ModernMessageBoxWindow.Show("请输入姓名", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(staffId))
            {
                ModernMessageBoxWindow.Show("请输入工号", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(account))
            {
                ModernMessageBoxWindow.Show("请输入账号", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ModernMessageBoxWindow.Show("请输入密码", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            // 检查是否已登录机构
            if (GlobalInstitutionManager.CurrentInstitutionDbId <= 0)
            {
                ModernMessageBoxWindow.Show("请先登录机构", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                return;
            }

            // 创建新的医护人员对象
            var newStaff = new MedicalStaff
            {
                Name = name,
                StaffId = staffId,
                Account = account,
                Password = password,
                Phone = "" // 不再使用Phone字段存储科室信息
            };
            
            // 禁用注册按钮，显示加载状态
            var registerButton = sender as Button;
            if (registerButton != null)
            {
                registerButton.IsEnabled = false;
                registerButton.Content = "注册中...";
            }

            try
            {
                // 尝试注册
                bool registerSuccess = await GlobalMedicalStaffManager.RegisterAsync(newStaff, GlobalInstitutionManager.CurrentInstitutionDbId);
                
                if (registerSuccess)
                {
                    // 注册成功后直接登录
                    bool loginSuccess = await GlobalMedicalStaffManager.LoginAsync(account, password);
                    
                    if (loginSuccess)
                    {
                        // 导航到医护人员操作页面
                        NavigationManager.NavigateTo(new MedicalStaffPage());
                    }
                    else
                    {
                        ModernMessageBoxWindow.Show("注册成功，但自动登录失败，请手动登录", "提示", ModernMessageBoxWindow.MessageBoxType.Warning);
                    }
                }
                else
                {
                    ModernMessageBoxWindow.Show("该账号已存在，请使用其他账号", "注册失败", ModernMessageBoxWindow.MessageBoxType.Error);
                }
            }
            catch (Exception ex)
            {
                ModernMessageBoxWindow.Show($"注册失败: {ex.Message}", "注册失败", ModernMessageBoxWindow.MessageBoxType.Error);
            }
            finally
            {
                // 恢复注册按钮状态
                if (registerButton != null)
                {
                    registerButton.IsEnabled = true;
                    registerButton.Content = "注册";
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // 返回到医护人员操作页面
            NavigationManager.NavigateTo(new MedicalStaffPage());
        }

        private void ClearRegisterInputs()
        {
            RegisterNameTextBox.Text = "";
            RegisterStaffIdTextBox.Text = "";
            RegisterAccountTextBox.Text = "";
            RegisterPasswordBox.Password = "";
        }

        private void ClearLoginInputs()
        {
            LoginAccountTextBox.Text = "";
            LoginPasswordBox.Password = "";
        }
    }
}