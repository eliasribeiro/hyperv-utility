using System;
using System.Windows;
using System.Windows.Controls;
using HyperVUtilities.Pages;

namespace HyperVUtilities
{
    /// <summary>
    /// Janela principal do aplicativo HyperV Utilities
    /// </summary>
    public partial class MainWindow : Window
    {
        private Button? _activeButton;

        public MainWindow()
        {
            InitializeComponent();
            
            // Navegar para a primeira página por padrão
            ContentFrame.Navigate(new HostToVmPage());
            _activeButton = HostToVmButton;
        }

        private void NavigateToHostToVm(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new HostToVmPage());
            UpdateActiveButton(HostToVmButton);
        }

        private void NavigateToVmToHost(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new VmToHostPage());
            UpdateActiveButton(VmToHostButton);
        }

        private void NavigateToSettings(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new SettingsPage());
            UpdateActiveButton(SettingsButton);
        }

        private void NavigateToAbout(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new AboutPage());
            UpdateActiveButton(AboutButton);
        }

        private void UpdateActiveButton(Button newActiveButton)
        {
            // Remover estilo ativo do botão anterior
            if (_activeButton != null)
            {
                _activeButton.Style = (Style)FindResource("SidebarButtonStyle");
            }

            // Aplicar estilo ativo ao novo botão
            newActiveButton.Style = (Style)FindResource("ActiveSidebarButtonStyle");
            _activeButton = newActiveButton;
        }
    }
}
