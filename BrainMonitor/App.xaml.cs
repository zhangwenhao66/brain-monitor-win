using System.Windows;
using ModernWpf;
using BrainMirror.Views;

namespace BrainMirror
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 设置ModernWpf主题为Light模式，符合Win11风格
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            
            // 设置主题色为系统默认的蓝色
            ThemeManager.Current.AccentColor = System.Windows.Media.Color.FromRgb(0, 120, 215);
            
            // 初始化示例数据
            GlobalTesterList.InitializeSampleData();
            
            // 创建并显示主窗口
            var mainWindow = new MainWindow();
            NavigationManager.Initialize(mainWindow);
            mainWindow.Show();
        }
    }
}