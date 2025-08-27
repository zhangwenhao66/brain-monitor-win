using System.Windows;
using System.Windows.Controls;

namespace BrainMonitor.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // 初始化导航管理器
            NavigationManager.Initialize(this);
            // 默认显示机构登录页面
            NavigateToPage(new InstitutionLoginPage());
        }

        /// <summary>
        /// 导航到指定页面
        /// </summary>
        /// <param name="page">要显示的页面</param>
        public void NavigateToPage(UserControl page)
        {
            ContentContainer.Content = page;
            
            // 如果页面实现了INavigationAware接口，调用OnNavigatedTo方法
            if (page is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo();
            }
        }

        /// <summary>
        /// 获取当前显示的页面
        /// </summary>
        /// <returns>当前页面</returns>
        public UserControl? GetCurrentPage()
        {
            return ContentContainer.Content as UserControl;
        }
    }

    /// <summary>
    /// 页面导航接口
    /// </summary>
    public interface INavigationAware
    {
        void OnNavigatedTo();
    }

    /// <summary>
    /// 全局导航管理器
    /// </summary>
    public static class NavigationManager
    {
        private static MainWindow _mainWindow;

        public static void Initialize(MainWindow mainWindow)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }

        public static void NavigateTo(UserControl page)
        {
            _mainWindow?.NavigateToPage(page);
        }

        public static UserControl? GetCurrentPage()
        {
            return _mainWindow?.GetCurrentPage();
        }

        public static MainWindow? GetMainWindow()
        {
            return _mainWindow;
        }
    }
}