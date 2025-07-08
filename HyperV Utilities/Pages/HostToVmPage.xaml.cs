using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace HyperVUtilities.Pages
{
    public partial class HostToVmPage : Page
    {
        public HostToVmPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Manipula o clique do botão "Arquivo" para selecionar um arquivo
        /// </summary>
        private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Selecione um arquivo",
                    Filter = "Todos os arquivos (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    LocalPathTextBox.Text = openFileDialog.FileName;
                    UpdateStatus($"Arquivo selecionado: {openFileDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Erro ao selecionar arquivo", ex.Message);
                UpdateStatus($"Erro ao selecionar arquivo: {ex.Message}");
            }
        }

        /// <summary>
        /// Manipula o clique do botão "Pasta" para selecionar uma pasta
        /// </summary>
        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Selecione uma pasta",
                    ShowNewFolderButton = false
                };

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LocalPathTextBox.Text = folderDialog.SelectedPath;
                    UpdateStatus($"Pasta selecionada: {folderDialog.SelectedPath}");
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Erro ao selecionar pasta", ex.Message);
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
                ShowErrorDialog("Campo obrigatório", "Por favor, informe o nome da máquina virtual.");
                return;
            }

            if (string.IsNullOrWhiteSpace(VmDestinationPathTextBox.Text))
            {
                ShowErrorDialog("Campo obrigatório", "Por favor, informe o caminho de destino na VM.");
                return;
            }

            if (string.IsNullOrWhiteSpace(LocalPathTextBox.Text))
            {
                ShowErrorDialog("Campo obrigatório", "Por favor, selecione o arquivo ou pasta a ser copiado.");
                return;
            }

            // Verificar se o caminho local existe
            if (!Directory.Exists(LocalPathTextBox.Text) && !File.Exists(LocalPathTextBox.Text))
            {
                ShowErrorDialog("Caminho inválido", "O arquivo ou pasta selecionado não existe.");
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
                ShowErrorDialog("Erro na transferência", ex.Message);
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

            // Executar PowerShell
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            var process = Process.Start(processStartInfo);
            
            if (process != null)
            {
                await process.WaitForExitAsync();
                cancellationTokenSource.Cancel();
                
                try
                {
                    await logMonitorTask;
                }
                catch (TaskCanceledException)
                {
                    // Expected when cancellation is requested
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
                    // Ignorar erros de limpeza
                }

                if (process.ExitCode == 0)
                {
                    ShowInfoDialog("Transferência concluída", $"O {itemType} foi transferido com sucesso para a VM!");
                }
                else
                {
                    UpdateStatus($"A transferência falhou com código de saída: {process.ExitCode}");
                }
            }
            else
            {
                cancellationTokenSource.Cancel();
                throw new InvalidOperationException("Não foi possível iniciar o processo PowerShell.");
            }
        }

        /// <summary>
        /// Monitora o arquivo de log e atualiza o status em tempo real
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
                            using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            stream.Seek(lastPosition, SeekOrigin.Begin);
                            
                            using var reader = new StreamReader(stream);
                            string? line;
                            while ((line = await reader.ReadLineAsync()) != null)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    Dispatcher.Invoke(() => UpdateStatus(line));
                                }
                            }
                            
                            lastPosition = fileInfo.Length;
                        }
                    }
                    
                    await Task.Delay(500, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => UpdateStatus($"Erro no monitoramento: {ex.Message}"));
                    break;
                }
            }
        }

        /// <summary>
        /// Configura o estado de loading do botão de transferência
        /// </summary>
        private void SetTransferButtonLoading(bool isLoading)
        {
            TransferButtonText.Text = isLoading ? "Transferindo..." : "Transferir";
            TransferButton.IsEnabled = !isLoading;
        }

        /// <summary>
        /// Atualiza o texto de status
        /// </summary>
        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text += Environment.NewLine + message;
        }

        /// <summary>
        /// Mostra um diálogo de erro
        /// </summary>
        private void ShowErrorDialog(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Mostra um diálogo informativo
        /// </summary>
        private void ShowInfoDialog(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
} 