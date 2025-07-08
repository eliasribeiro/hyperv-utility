using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HyperVUtilities.Pages;

namespace HyperVUtilities
{
    /// <summary>
    /// Janela principal do aplicativo HyperV Utilities
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "HyperV Utilities";
            
            // Selecionar o primeiro item por padrão
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
            NavigateToPage("CopyFile");
        }

        /// <summary>
        /// Manipula a mudança de seleção no NavigationView
        /// </summary>
        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                NavigateToPage(tag);
                
                // Atualizar o título da página
                PageTitle.Text = item.Content?.ToString() ?? "HyperV Utilities";
            }
        }

        /// <summary>
        /// Navega para a página especificada
        /// </summary>
        private void NavigateToPage(string? pageTag)
        {
            Type pageType = pageTag switch
            {
                "CopyFile" => typeof(CopyFilePage),
                "CreateVmGpu" => typeof(CreateVmGpuPage),
                "About" => typeof(AboutPage),
                _ => typeof(CopyFilePage)
            };

            ContentFrame.Navigate(pageType);
        }
    }
}
