using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HyperVUtilities.Pages
{
    public partial class VmToHostPage : Page
    {
        public VmToHostPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Manipula o clique do botão "Procurar" para selecionar pasta de destino
        /// </summary>
        private void BrowseDestinationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Selecione a pasta de destino local",
                    ShowNewFolderButton = true
                };

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LocalDestinationPathTextBox.Text = folderDialog.SelectedPath;
                    UpdateStatus($"Pasta de destino selecionada: {folderDialog.SelectedPath}");
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Erro ao selecionar pasta", ex.Message);
                UpdateStatus($"Erro ao selecionar pasta de destino: {ex.Message}");
            }
        }

        /// <summary>
        /// Manipula o clique do botão "Transferir" para executar a transferência reversa
        /// </summary>
        private async void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            // Validar os campos
            if (string.IsNullOrWhiteSpace(VmNameTextBox.Text))
            {
                ShowErrorDialog("Campo obrigatório", "Por favor, informe o nome da máquina virtual.");
                return;
            }

            if (string.IsNullOrWhiteSpace(VmSourcePathTextBox.Text))
            {
                ShowErrorDialog("Campo obrigatório", "Por favor, informe o caminho do arquivo/pasta na VM.");
                return;
            }

            if (string.IsNullOrWhiteSpace(LocalDestinationPathTextBox.Text))
            {
                ShowErrorDialog("Campo obrigatório", "Por favor, selecione a pasta de destino local.");
                return;
            }

            // Verificar se a pasta de destino existe
            if (!Directory.Exists(LocalDestinationPathTextBox.Text))
            {
                ShowErrorDialog("Caminho inválido", "A pasta de destino selecionada não existe.");
                return;
            }

            // Mostrar loading e desabilitar o botão durante a transferência
            SetTransferButtonLoading(true);
            UpdateStatus("Iniciando transferência da VM para o host...");

            try
            {
                await ExecutePowerShellReverseTransfer();
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
        /// Executa o script PowerShell para transferir arquivos da VM para o host
        /// </summary>
        private async Task ExecutePowerShellReverseTransfer()
        {
            var vmName = VmNameTextBox.Text.Trim();
            var vmSourcePath = VmSourcePathTextBox.Text.Trim();
            var localDestination = LocalDestinationPathTextBox.Text.Trim();

            // Criar arquivos temporários
            var tempScriptPath = Path.Combine(Path.GetTempPath(), $"HyperVReverseTransfer_{Guid.NewGuid()}.ps1");
            var tempLogPath = Path.Combine(Path.GetTempPath(), $"HyperVReverseTransferLog_{Guid.NewGuid()}.txt");

            // Criar o script PowerShell com logging
            var scriptBuilder = new StringBuilder();
            scriptBuilder.AppendLine("# Script para transferir arquivos de VM Hyper-V para Host");
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
            scriptBuilder.AppendLine("    Write-Log \"Iniciando transferência de: $vmSourcePath\"");
            scriptBuilder.AppendLine("    Write-Log \"Destino: $localDestination\"");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("    Copy-Item -Path $vmSourcePath -Destination $localDestination -FromSession $session -Recurse -Force");
            scriptBuilder.AppendLine();
            scriptBuilder.AppendLine("    Write-Log \"Arquivo/pasta transferido com sucesso da VM para o host!\"");
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
                    ShowInfoDialog("Transferência concluída", "O arquivo/pasta foi transferido com sucesso da VM para o host!");
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