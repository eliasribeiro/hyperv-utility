using System.Windows;

namespace HyperVUtilities
{
    /// <summary>
    /// Lógica de interação para App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Criar e exibir a janela principal
            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }
}
