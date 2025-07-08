using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace HyperVUtilities.Pages
{
    /// <summary>
    /// Página para transferência de arquivos entre host e VM
    /// </summary>
    public sealed partial class CopyFilePage : Page
    {
        public CopyFilePage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Método para obter a janela principal
        /// </summary>
        private Window GetMainWindow()
        {
            return (Application.Current as App)?.MainWindow;
        }

        // Todos os métodos da MainWindow serão implementados aqui
        // Por enquanto, métodos básicos para funcionar

        private async void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileOpenPicker = new FileOpenPicker();
                fileOpenPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                fileOpenPicker.FileTypeFilter.Add("*");

                var mainWindow = GetMainWindow();
                if (mainWindow != null)
                {
                    var hwnd = WindowNative.GetWindowHandle(mainWindow);
                    InitializeWithWindow.Initialize(fileOpenPicker, hwnd);
                }

                var file = await fileOpenPicker.PickSingleFileAsync();
                if (file != null)
                {
                    LocalPathTextBox.Text = file.Path;
                    UpdateStatus($"Arquivo selecionado: {file.Path}");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Erro ao selecionar arquivo", ex.Message);
                UpdateStatus($"Erro ao selecionar arquivo: {ex.Message}");
            }
        }

        private async void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                folderPicker.FileTypeFilter.Add("*");

                var mainWindow = GetMainWindow();
                if (mainWindow != null)
                {
                    var hwnd = WindowNative.GetWindowHandle(mainWindow);
                    InitializeWithWindow.Initialize(folderPicker, hwnd);
                }

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    LocalPathTextBox.Text = folder.Path;
                    UpdateStatus($"Pasta selecionada: {folder.Path}");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Erro ao selecionar pasta", ex.Message);
                UpdateStatus($"Erro ao selecionar pasta: {ex.Message}");
            }
        }

        private async void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(VmNameTextBox.Text))
            {
                await ShowErrorDialog("Campo obrigatório", "Por favor, informe o nome da máquina virtual.");
                return;
            }

            if (string.IsNullOrWhiteSpace(VmDestinationPathTextBox.Text))
            {
                await ShowErrorDialog("Campo obrigatório", "Por favor, informe o caminho de destino na VM.");
                return;
            }

            if (string.IsNullOrWhiteSpace(LocalPathTextBox.Text))
            {
                await ShowErrorDialog("Campo obrigatório", "Por favor, selecione o arquivo ou pasta a ser copiado.");
                return;
            }

            UpdateStatus("Iniciando transferência...");
            await ShowInfoDialog("Funcionalidade", "A transferência de arquivos será implementada em breve!");
        }

        private async void BrowseDestinationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                folderPicker.FileTypeFilter.Add("*");

                var mainWindow = GetMainWindow();
                if (mainWindow != null)
                {
                    var hwnd = WindowNative.GetWindowHandle(mainWindow);
                    InitializeWithWindow.Initialize(folderPicker, hwnd);
                }

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    LocalDestinationPathTextBox.Text = folder.Path;
                    UpdateStatusReverse($"Pasta de destino selecionada: {folder.Path}");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Erro ao selecionar pasta", ex.Message);
                UpdateStatusReverse($"Erro ao selecionar pasta: {ex.Message}");
            }
        }

        private async void TransferReverseButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(VmNameReverseTextBox.Text))
            {
                await ShowErrorDialog("Campo obrigatório", "Por favor, informe o nome da máquina virtual.");
                return;
            }

            if (string.IsNullOrWhiteSpace(VmSourcePathTextBox.Text))
            {
                await ShowErrorDialog("Campo obrigatório", "Por favor, informe o caminho do arquivo/pasta na VM.");
                return;
            }

            if (string.IsNullOrWhiteSpace(LocalDestinationPathTextBox.Text))
            {
                await ShowErrorDialog("Campo obrigatório", "Por favor, selecione a pasta de destino no host.");
                return;
            }

            UpdateStatusReverse("Iniciando transferência...");
            await ShowInfoDialog("Funcionalidade", "A transferência de arquivos será implementada em breve!");
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text += $"{DateTime.Now:HH:mm:ss} - {message}\n";
        }

        private void UpdateStatusReverse(string message)
        {
            StatusReverseTextBlock.Text += $"{DateTime.Now:HH:mm:ss} - {message}\n";
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            
            await dialog.ShowAsync();
        }

        private async Task ShowInfoDialog(string title, string message)
        {
            var dialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            
            await dialog.ShowAsync();
        }
    }
} 