using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace BrainMirror.Views
{
    public partial class StaffLoginWindow : Window
    {
        public StaffLoginWindow()
        {
            InitializeComponent();
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            // 根据当前选中的Tab更新标题
            if (MainTabControl.SelectedIndex == 0)
            {
                TitleText.Text = "工作人员登录";
            }
            else
            {
                TitleText.Text = "工作人员注册";
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTitle();
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

            try
            {
                // 尝试登录
                bool loginSuccess = await GlobalMedicalStaffManager.LoginAsync(account, password);
                
                if (loginSuccess)
                {
                    // 登录成功，直接跳转
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ModernMessageBoxWindow.Show("账号或密码错误，请重试", "登录失败", ModernMessageBoxWindow.MessageBoxType.Error);
                    // 清空密码框
                    LoginPasswordBox.Password = "";
                }
            }
            catch (System.Exception ex)
            {
                ModernMessageBoxWindow.Show($"登录失败: {ex.Message}", "登录失败", ModernMessageBoxWindow.MessageBoxType.Error);
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

            // 创建新的工作人员对象
            var newStaff = new MedicalStaff
            {
                Name = name,
                StaffId = staffId,
                Account = account,
                Password = password,
                Phone = "" // 不再使用Phone字段存储科室信息
            };
            
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
                        // 直接关闭窗口并返回成功结果
                        this.DialogResult = true;
                        this.Close();
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
            catch (System.Exception ex)
            {
                ModernMessageBoxWindow.Show($"注册失败: {ex.Message}", "注册失败", ModernMessageBoxWindow.MessageBoxType.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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