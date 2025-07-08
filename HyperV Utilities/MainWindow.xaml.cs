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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
        }

        /// <summary>
        /// Manipula o clique do botão "Arquivo" para selecionar um arquivo
        /// </summary>
        private async void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Criar o FileOpenPicker
                var fileOpenPicker = new FileOpenPicker();
                fileOpenPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                fileOpenPicker.FileTypeFilter.Add("*");

                // Configurar a janela proprietária para o picker
                var hwnd = WindowNative.GetWindowHandle(this);
                InitializeWithWindow.Initialize(fileOpenPicker, hwnd);

                // Mostrar o picker e aguardar a seleção
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

        /// <summary>
        /// Manipula o clique do botão "Pasta" para selecionar uma pasta
        /// </summary>
        private async void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Criar o FolderPicker
                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                folderPicker.FileTypeFilter.Add("*");

                // Configurar a janela proprietária para o picker
                var hwnd = WindowNative.GetWindowHandle(this);
                InitializeWithWindow.Initialize(folderPicker, hwnd);

                // Mostrar o picker e aguardar a seleção
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

        /// <summary>
        /// Manipula o clique do botão "Transferir" para executar a cópia
        /// </summary>
        private async void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            // Validar os campos
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

            // Verificar se o caminho local existe
            if (!Directory.Exists(LocalPathTextBox.Text) && !File.Exists(LocalPathTextBox.Text))
            {
                await ShowErrorDialog("Caminho inválido", "O arquivo ou pasta selecionado não existe.");
                return;
            }

            // Mostrar loading e desabilitar o botão durante a transferência
            SetTransferButtonLoading(true);
            UpdateStatus("Iniciando transferência...");

            try
            {
                await ExecutePowerShellCopy();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Erro na transferência", ex.Message);
                UpdateStatus($"Erro na transferência: {ex.Message}");
            }
            finally
            {
                SetTransferButtonLoading(false);
            }
        }

        /// <summary>
        /// Executa o script PowerShell para copiar os arquivos
        /// </summary>
        private async Task ExecutePowerShellCopy()
        {
            var vmName = VmNameTextBox.Text.Trim();
            var vmDestination = VmDestinationPathTextBox.Text.Trim();
            var localPath = LocalPathTextBox.Text.Trim();

            // Determinar se é arquivo ou pasta
            var isFile = File.Exists(localPath);
            var itemType = isFile ? "arquivo" : "pasta";

            // Criar arquivos temporários
            var tempScriptPath = Path.Combine(Path.GetTempPath(), $"HyperVCopy_{Guid.NewGuid()}.ps1");
            var tempLogPath = Path.Combine(Path.GetTempPath(), $"HyperVCopyLog_{Guid.NewGuid()}.txt");

            // Criar o script PowerShell com logging
            var scriptBuilder = new StringBuilder();
            scriptBuilder.AppendLine("# Script para copiar arquivos para VM Hyper-V");
            scriptBuilder.AppendLine($"$logFile = '{tempLogPath}'");
            scriptBuilder.AppendLine("function Write-Log { param($Message) Add-Content -Path $logFile -Value \"$(Get-Date -Format 'HH:mm:ss') - $Message\" }");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("try {");
            scriptBuilder.AppendLine($"    $vmName = '{vmName}'");
            scriptBuilder.AppendLine($"    $localPath = '{localPath}'");
            scriptBuilder.AppendLine($"    $vmDestination = '{vmDestination}'");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("    Write-Log \"Conectando à VM: $vmName\"");
            scriptBuilder.AppendLine("    $session = New-PSSession -VMName $vmName");
            scriptBuilder.AppendLine("    Write-Log \"Conexão estabelecida com sucesso\"");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine($"    Write-Log \"Iniciando cópia do {itemType}: $localPath\"");
            scriptBuilder.AppendLine($"    Write-Log \"Destino: $vmDestination\"");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("    Copy-Item -Path $localPath -Destination $vmDestination -ToSession $session -Recurse -Force");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine($"    Write-Log \"{char.ToUpper(itemType[0])}{itemType.Substring(1)} transferido com sucesso!\"");
            scriptBuilder.AppendLine("    Remove-PSSession $session");
            scriptBuilder.AppendLine("    Write-Log \"Sessão PowerShell fechada\"");
            scriptBuilder.AppendLine("    Write-Log \"TRANSFER_COMPLETED\"");
            scriptBuilder.AppendLine("}");
            scriptBuilder.AppendLine("catch {");
            scriptBuilder.AppendLine("    Write-Log \"Erro durante a transferência: $($_.Exception.Message)\"");
            scriptBuilder.AppendLine("    if ($session) { Remove-PSSession $session }");
            scriptBuilder.AppendLine("    Write-Log \"TRANSFER_FAILED\"");
            scriptBuilder.AppendLine("    exit 1");
            scriptBuilder.AppendLine("}");

            // Salvar o script
            await File.WriteAllTextAsync(tempScriptPath, scriptBuilder.ToString());

            // Inicializar arquivo de log
            await File.WriteAllTextAsync(tempLogPath, "");

            // Configurar monitoramento do arquivo de log
            var cancellationTokenSource = new CancellationTokenSource();
            var logMonitorTask = MonitorLogFile(tempLogPath, cancellationTokenSource.Token);

            try
            {
                UpdateStatus($"Executando PowerShell com privilégios de administrador para transferir {itemType}...");

                // Configurar o processo para executar PowerShell como administrador
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas", // Executar como administrador
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                // Executar o processo
                using (var process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        
                        // Aguardar um pouco para capturar logs finais
                        await Task.Delay(1000);
                        cancellationTokenSource.Cancel();
                        
                        if (process.ExitCode == 0)
                        {
                            UpdateStatus($"Transferência do {itemType} concluída com sucesso!");
                            await ShowInfoDialog("Sucesso", $"A transferência do {itemType} foi concluída com sucesso!");
                        }
                        else
                        {
                            UpdateStatus($"Transferência falhou. Código de saída: {process.ExitCode}");
                            await ShowErrorDialog("Erro na transferência", 
                                "A transferência falhou. Verifique se:\n" +
                                "• Você tem privilégios de administrador\n" +
                                "• A VM está em execução\n" +
                                "• O nome da VM está correto\n" +
                                "• O caminho de destino existe na VM");
                        }
                    }
                    else
                    {
                        cancellationTokenSource.Cancel();
                        throw new InvalidOperationException("Não foi possível iniciar o processo PowerShell.");
                    }
                }
            }
            finally
            {
                // Parar monitoramento
                cancellationTokenSource.Cancel();
                
                // Aguardar o task de monitoramento terminar
                try
                {
                    await logMonitorTask;
                }
                catch (OperationCanceledException)
                {
                    // Esperado quando cancelamos
                }

                // Limpar arquivos temporários
                try
                {
                    if (File.Exists(tempScriptPath))
                        File.Delete(tempScriptPath);
                    if (File.Exists(tempLogPath))
                        File.Delete(tempLogPath);
                }
                catch
                {
                    // Ignorar erros ao deletar arquivos temporários
                }
            }
        }

        /// <summary>
        /// Monitora o arquivo de log em tempo real
        /// </summary>
        private async Task MonitorLogFile(string logPath, CancellationToken cancellationToken)
        {
            var lastPosition = 0L;
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (File.Exists(logPath))
                    {
                        var fileInfo = new FileInfo(logPath);
                        if (fileInfo.Length > lastPosition)
                        {
                            using (var fileStream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                fileStream.Seek(lastPosition, SeekOrigin.Begin);
                                using (var reader = new StreamReader(fileStream))
                                {
                                    string line;
                                    while ((line = await reader.ReadLineAsync()) != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(line))
                                        {
                                            // Atualizar UI no thread principal
                                            DispatcherQueue.TryEnqueue(() =>
                                            {
                                                if (line.Contains("TRANSFER_COMPLETED") || line.Contains("TRANSFER_FAILED"))
                                                {
                                                    // Logs de controle, não mostrar na UI
                                                    return;
                                                }
                                                UpdateStatus(line);
                                            });
                                        }
                                    }
                                }
                                lastPosition = fileInfo.Length;
                            }
                        }
                    }
                    
                    await Task.Delay(200, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Ignorar erros de I/O temporários
                    await Task.Delay(500, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Controla o estado de loading do botão de transferir
        /// </summary>
        private void SetTransferButtonLoading(bool isLoading)
        {
            TransferButton.IsEnabled = !isLoading;
            LoadingProgressRing.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            LoadingProgressRing.IsActive = isLoading;
            TransferButtonText.Text = isLoading ? "Transferindo..." : "Transferir";
        }

        /// <summary>
        /// Atualiza o texto de status na interface
        /// </summary>
        private void UpdateStatus(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            StatusTextBlock.Text = $"[{timestamp}] {message}";
        }

        /// <summary>
        /// Mostra um diálogo de erro
        /// </summary>
        private async Task ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// Mostra um diálogo de informação
        /// </summary>
        private async Task ShowInfoDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// Manipula o clique do botão "Procurar" para selecionar pasta de destino (VM → Host)
        /// </summary>
        private async void BrowseDestinationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Criar o FolderPicker
                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                folderPicker.FileTypeFilter.Add("*");

                // Configurar a janela proprietária para o picker
                var hwnd = WindowNative.GetWindowHandle(this);
                InitializeWithWindow.Initialize(folderPicker, hwnd);

                // Mostrar o picker e aguardar a seleção
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

        /// <summary>
        /// Manipula o clique do botão "Transferir" para executar a cópia reversa (VM → Host)
        /// </summary>
        private async void TransferReverseButton_Click(object sender, RoutedEventArgs e)
        {
            // Validar os campos
            if (string.IsNullOrWhiteSpace(VmNameTextBox2.Text))
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
                await ShowErrorDialog("Campo obrigatório", "Por favor, selecione a pasta de destino local.");
                return;
            }

            // Verificar se o caminho de destino local existe
            if (!Directory.Exists(LocalDestinationPathTextBox.Text))
            {
                await ShowErrorDialog("Caminho inválido", "A pasta de destino selecionada não existe.");
                return;
            }

            // Mostrar loading e desabilitar o botão durante a transferência
            SetTransferReverseButtonLoading(true);
            UpdateStatusReverse("Iniciando transferência da VM para o host...");

            try
            {
                await ExecutePowerShellReverseTransfer();
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Erro na transferência", ex.Message);
                UpdateStatusReverse($"Erro na transferência: {ex.Message}");
            }
            finally
            {
                SetTransferReverseButtonLoading(false);
            }
        }

        /// <summary>
        /// Executa o script PowerShell para copiar arquivos da VM para o Host
        /// </summary>
        private async Task ExecutePowerShellReverseTransfer()
        {
            var vmName = VmNameTextBox2.Text.Trim();
            var vmSourcePath = VmSourcePathTextBox.Text.Trim();
            var localDestination = LocalDestinationPathTextBox.Text.Trim();

            // Criar arquivos temporários
            var tempScriptPath = Path.Combine(Path.GetTempPath(), $"HyperVReverseCopy_{Guid.NewGuid()}.ps1");
            var tempLogPath = Path.Combine(Path.GetTempPath(), $"HyperVReverseCopyLog_{Guid.NewGuid()}.txt");

            // Criar o script PowerShell com logging para transferência reversa
            var scriptBuilder = new StringBuilder();
            scriptBuilder.AppendLine("# Script para copiar arquivos da VM Hyper-V para o Host");
            scriptBuilder.AppendLine($"$logFile = '{tempLogPath}'");
            scriptBuilder.AppendLine("function Write-Log { param($Message) Add-Content -Path $logFile -Value \"$(Get-Date -Format 'HH:mm:ss') - $Message\" }");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("try {");
            scriptBuilder.AppendLine($"    $vmName = '{vmName}'");
            scriptBuilder.AppendLine($"    $vmSourcePath = '{vmSourcePath}'");
            scriptBuilder.AppendLine($"    $localDestination = '{localDestination}'");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("    Write-Log \"Conectando à VM: $vmName\"");
            scriptBuilder.AppendLine("    $session = New-PSSession -VMName $vmName");
            scriptBuilder.AppendLine("    Write-Log \"Conexão estabelecida com sucesso\"");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("    Write-Log \"Verificando se o arquivo/pasta existe na VM: $vmSourcePath\"");
            scriptBuilder.AppendLine("    $itemExists = Invoke-Command -Session $session -ScriptBlock { param($path) Test-Path $path } -ArgumentList $vmSourcePath");
            scriptBuilder.AppendLine("    if (-not $itemExists) {");
            scriptBuilder.AppendLine("        throw \"O arquivo ou pasta especificado não existe na VM: $vmSourcePath\"");
            scriptBuilder.AppendLine("    }");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("    Write-Log \"Iniciando cópia da VM para o host\"");
            scriptBuilder.AppendLine("    Write-Log \"Origem na VM: $vmSourcePath\"");
            scriptBuilder.AppendLine("    Write-Log \"Destino local: $localDestination\"");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("    Copy-Item -Path $vmSourcePath -Destination $localDestination -FromSession $session -Recurse -Force");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("    Write-Log \"Transferência concluída com sucesso!\"");
            scriptBuilder.AppendLine("    Remove-PSSession $session");
            scriptBuilder.AppendLine("    Write-Log \"Sessão PowerShell fechada\"");
            scriptBuilder.AppendLine("    Write-Log \"TRANSFER_COMPLETED\"");
            scriptBuilder.AppendLine("}");
            scriptBuilder.AppendLine("catch {");
            scriptBuilder.AppendLine("    Write-Log \"Erro durante a transferência: $($_.Exception.Message)\"");
            scriptBuilder.AppendLine("    if ($session) { Remove-PSSession $session }");
            scriptBuilder.AppendLine("    Write-Log \"TRANSFER_FAILED\"");
            scriptBuilder.AppendLine("    exit 1");
            scriptBuilder.AppendLine("}");

            // Salvar o script
            await File.WriteAllTextAsync(tempScriptPath, scriptBuilder.ToString());

            // Inicializar arquivo de log
            await File.WriteAllTextAsync(tempLogPath, "");

            // Configurar monitoramento do arquivo de log
            var cancellationTokenSource = new CancellationTokenSource();
            var logMonitorTask = MonitorLogFileReverse(tempLogPath, cancellationTokenSource.Token);

            try
            {
                UpdateStatusReverse("Executando PowerShell com privilégios de administrador...");

                // Configurar o processo para executar PowerShell como administrador
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas", // Executar como administrador
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                // Executar o processo
                using (var process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        
                        // Aguardar um pouco para capturar logs finais
                        await Task.Delay(1000);
                        cancellationTokenSource.Cancel();
                        
                        if (process.ExitCode == 0)
                        {
                            UpdateStatusReverse("Transferência da VM para o host concluída com sucesso!");
                            await ShowInfoDialog("Sucesso", "A transferência da VM para o host foi concluída com sucesso!");
                        }
                        else
                        {
                            UpdateStatusReverse($"Transferência falhou. Código de saída: {process.ExitCode}");
                            await ShowErrorDialog("Erro na transferência", 
                                "A transferência falhou. Verifique se:\n" +
                                "• Você tem privilégios de administrador\n" +
                                "• A VM está em execução\n" +
                                "• O nome da VM está correto\n" +
                                "• O caminho do arquivo/pasta existe na VM\n" +
                                "• A pasta de destino tem permissões de escrita");
                        }
                    }
                    else
                    {
                        cancellationTokenSource.Cancel();
                        throw new InvalidOperationException("Não foi possível iniciar o processo PowerShell.");
                    }
                }
            }
            finally
            {
                // Parar monitoramento
                cancellationTokenSource.Cancel();
                
                // Aguardar o task de monitoramento terminar
                try
                {
                    await logMonitorTask;
                }
                catch (OperationCanceledException)
                {
                    // Esperado quando cancelamos
                }

                // Limpar arquivos temporários
                try
                {
                    if (File.Exists(tempScriptPath))
                        File.Delete(tempScriptPath);
                    if (File.Exists(tempLogPath))
                        File.Delete(tempLogPath);
                }
                catch
                {
                    // Ignorar erros ao deletar arquivos temporários
                }
            }
        }

        /// <summary>
        /// Monitora o arquivo de log em tempo real para transferência reversa
        /// </summary>
        private async Task MonitorLogFileReverse(string logPath, CancellationToken cancellationToken)
        {
            var lastPosition = 0L;
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (File.Exists(logPath))
                    {
                        var fileInfo = new FileInfo(logPath);
                        if (fileInfo.Length > lastPosition)
                        {
                            using (var fileStream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                fileStream.Seek(lastPosition, SeekOrigin.Begin);
                                using (var reader = new StreamReader(fileStream))
                                {
                                    string line;
                                    while ((line = await reader.ReadLineAsync()) != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(line))
                                        {
                                            // Atualizar UI no thread principal
                                            DispatcherQueue.TryEnqueue(() =>
                                            {
                                                if (line.Contains("TRANSFER_COMPLETED") || line.Contains("TRANSFER_FAILED"))
                                                {
                                                    // Logs de controle, não mostrar na UI
                                                    return;
                                                }
                                                UpdateStatusReverse(line);
                                            });
                                        }
                                    }
                                }
                                lastPosition = fileInfo.Length;
                            }
                        }
                    }
                    
                    await Task.Delay(200, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Ignorar erros de I/O temporários
                    await Task.Delay(500, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Controla o estado de loading do botão de transferir reverso
        /// </summary>
        private void SetTransferReverseButtonLoading(bool isLoading)
        {
            TransferReverseButton.IsEnabled = !isLoading;
            LoadingProgressRing2.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            LoadingProgressRing2.IsActive = isLoading;
            TransferReverseButtonText.Text = isLoading ? "Transferindo..." : "Transferir";
        }

        /// <summary>
        /// Atualiza o texto de status na interface da segunda aba
        /// </summary>
        private void UpdateStatusReverse(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            StatusTextBlock2.Text = $"[{timestamp}] {message}";
        }
    }
}
