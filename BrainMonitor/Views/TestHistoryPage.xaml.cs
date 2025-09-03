using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using BrainMirror.Services;

namespace BrainMirror.Views
{
    public partial class TestHistoryPage : UserControl, INavigationAware
    {
        private Tester? currentTester;

        public TestHistoryPage(Tester tester)
        {
            InitializeComponent();
            currentTester = tester;
            LoadHistoryData();
        }

        public void OnNavigatedTo()
        {
            // 页面导航到时的处理
        }

        public void OnNavigatedFrom()
        {
            // 页面离开时的处理
        }

        private async void LoadHistoryData()
        {
            if (currentTester == null)
            {
                HistoryDataGrid.ItemsSource = new List<TestHistoryRecord>();
                return;
            }

            try
            {
                // 显示加载状态
                // 这里可以添加一个加载指示器

                // 从后端获取测试历史数据
                var historyRecords = await TestHistoryService.GetAllTestHistoryAsync(currentTester.ID);

                if (historyRecords != null && historyRecords.Count > 0)
                {
                    HistoryDataGrid.ItemsSource = historyRecords;
                    
                    // 设置数据网格的选择变化事件
                    HistoryDataGrid.SelectionChanged += HistoryDataGrid_SelectionChanged;
                }
                else
                {
                    // 如果没有历史记录，显示空列表
                    HistoryDataGrid.ItemsSource = new List<TestHistoryRecord>();
                }
            }
            catch (System.Exception ex)
            {
                // 记录错误日志
                System.Diagnostics.Debug.WriteLine($"加载测试历史失败: {ex.Message}");
                
                // 显示错误提示
                ModernMessageBoxWindow.Show($"加载测试历史失败: {ex.Message}", "错误", ModernMessageBoxWindow.MessageBoxType.Error);
                
                // 显示空列表
                HistoryDataGrid.ItemsSource = new List<TestHistoryRecord>();
            }
        }

        private void EnterTestButton_Click(object sender, RoutedEventArgs e)
        {
            // 导航到测试页面
            NavigationManager.NavigateTo(new TestPage(currentTester ?? new Tester()));
        }

        private void TesterGroupButton_Click(object sender, RoutedEventArgs e)
        {
            ModernMessageBoxWindow.Show("测试者分组功能", "功能", ModernMessageBoxWindow.MessageBoxType.Info);
        }

        private void RiskLevelButton_Click(object sender, RoutedEventArgs e)
        {
            ModernMessageBoxWindow.Show("风险等级功能", "功能", ModernMessageBoxWindow.MessageBoxType.Info);
        }

        private void MyProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ModernMessageBoxWindow.Show("我的功能", "功能", ModernMessageBoxWindow.MessageBoxType.Info);
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            // 返回到医护人员操作页面
            NavigationManager.NavigateTo(new MedicalStaffPage());
        }

        // 数据网格选择变化事件
        private void HistoryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 根据是否有选中的行来启用/禁用打开报告按钮
            OpenReportButton.IsEnabled = HistoryDataGrid.SelectedItem != null;
        }

        // 打开报告按钮点击事件
        private async void OpenReportButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRecord = HistoryDataGrid.SelectedItem as TestHistoryRecord;
            if (selectedRecord != null)
            {
                try
                {
                    // 显示加载状态
                    OpenReportButton.IsEnabled = false;
                    var originalContent = OpenReportButton.Content;
                    var stackPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                    stackPanel.Children.Add(new TextBlock { Text = "加载中..." });
                    OpenReportButton.Content = stackPanel;
                    
                    // 调用服务器接口获取完整的测试记录信息
                    string token = GetCurrentAuthToken();
                    if (!string.IsNullOrEmpty(token))
                    {
                        using (var httpClient = new System.Net.Http.HttpClient())
                        {
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                            
                            var reportResponse = await httpClient.GetAsync($"http://localhost:3000/api/test-records/{selectedRecord.Id}/report");
                            
                            if (reportResponse.IsSuccessStatusCode)
                            {
                                var responseContent = await reportResponse.Content.ReadAsStringAsync();
                                var reportData = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<ReportData>>(responseContent);
                                
                                if (reportData?.Success == true && reportData.Data != null)
                                {
                                    var testRecord = reportData.Data.TestRecord;
                                    var closedEyesResult = reportData.Data.ClosedEyesResult;
                                    var openEyesResult = reportData.Data.OpenEyesResult;
                                    
                                    // 计算脑电指标
                                    double thetaValue = 0, alphaValue = 0, betaValue = 0;
                                    if (closedEyesResult != null)
                                    {
                                        thetaValue = closedEyesResult.ThetaValue ?? 0;
                                        alphaValue = closedEyesResult.AlphaValue ?? 0;
                                        betaValue = closedEyesResult.BetaValue ?? 0;
                                    }
                                    else if (openEyesResult != null)
                                    {
                                        thetaValue = openEyesResult.ThetaValue ?? 0;
                                        alphaValue = openEyesResult.AlphaValue ?? 0;
                                        betaValue = openEyesResult.BetaValue ?? 0;
                                    }
                                    
                                    double brainwaveIndex = (thetaValue + alphaValue + betaValue) / 3.0;
                                    double adRiskValue = testRecord.AdRiskValue ?? 0;
                                    
                                    // 导航到报告页面，传递完整的测试数据
                                    NavigationManager.NavigateTo(new ReportPage(currentTester, 
                                        testRecord.MocaScore, 
                                        testRecord.MmseScore, 
                                        testRecord.GripStrength,
                                        thetaValue, alphaValue, betaValue, brainwaveIndex, adRiskValue, 
                                        testRecord.CreatedAt, "TestHistoryPage"));
                                    return;
                                }
                            }
                        }
                    }
                    
                    // 如果获取服务器数据失败，使用本地数据创建报告页面
                    NavigationManager.NavigateTo(new ReportPage(currentTester, selectedRecord));
                }
                catch (System.Exception ex)
                {
                    // 如果出现异常，使用本地数据创建报告页面
                    NavigationManager.NavigateTo(new ReportPage(currentTester, selectedRecord));
                }
                finally
                {
                    // 恢复按钮状态
                    OpenReportButton.IsEnabled = true;
                    var stackPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                    stackPanel.Children.Add(new TextBlock { Text = "打开报告" });
                    OpenReportButton.Content = stackPanel;
                }
            }
        }
        
        // 获取当前认证令牌
        private string GetCurrentAuthToken()
        {
            return GlobalMedicalStaffManager.CurrentToken ?? string.Empty;
        }
    }
}