using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Security.Principal;
using System.Windows.Media; // Added for SolidColorBrush

namespace HyperVUtilities.Pages
{
    public partial class GpuPassthroughPage : Page
    {
        private List<string> _detectedGpus;

        public GpuPassthroughPage()
        {
            InitializeComponent();
            _detectedGpus = new List<string>();
            InitializeDefaultValues();
        }

        private void InitializeDefaultValues()
        {
            // Definir pasta padrão para VHDs
            string defaultVhdPath = @"C:\Users\Public\Documents\Hyper-V\Virtual Hard Disks\";
            if (Directory.Exists(defaultVhdPath))
            {
                VhdPathTextBox.Text = defaultVhdPath;
            }
            
            // Definir senha padrão se não especificada
            PasswordTextBox.Text = "CoolestPassword!";
        }

        private async void DetectGpuButton_Click(object sender, RoutedEventArgs e)
        {
            DetectGpuButton.IsEnabled = false;
            DetectGpuButton.Content = "🔄 Detectando...";

            try
            {
                _detectedGpus.Clear();
                GpuItemsControl.ItemsSource = null;

                string script = @"
                    Function Get-VMGpuPartitionAdapterFriendlyName {
                        try {
                            $Devices = (Get-WmiObject -Class 'Msvm_PartitionableGpu' -ComputerName $env:COMPUTERNAME -Namespace 'ROOT\virtualization\v2').name
                            if ($Devices) {
                                Foreach ($GPU in $Devices) {
                                    $GPUParse = $GPU.Split('#')[1]
                                    $DeviceName = Get-WmiObject Win32_PNPSignedDriver | where {$_.HardwareID -eq 'PCI\' + $GPUParse} | select DeviceName -ExpandProperty DeviceName
                                    if ($DeviceName) {
                                        # Limpar o nome da GPU de forma mais robusta
                                        $CleanName = $DeviceName
                                        # Remover caracteres especiais comuns
                                        $CleanName = $CleanName -replace '\(TM\)', '™'
                                        $CleanName = $CleanName -replace '\(R\)', '®'
                                        $CleanName = $CleanName -replace '\(C\)', '©'
                                        $CleanName = $CleanName -replace '\(tm\)', '™'
                                        $CleanName = $CleanName -replace '\(r\)', '®'
                                        $CleanName = $CleanName -replace '\(c\)', '©'
                                        # Corrigir espaçamentos
                                        $CleanName = $CleanName -replace '\s+', ' '
                                        $CleanName = $CleanName.Trim()
                                        # Garantir que nomes comuns apareçam corretamente
                                        if ($CleanName -match 'Radeon') {
                                            $CleanName = $CleanName -replace 'MD\s*Radeon', 'AMD Radeon'
                                            $CleanName = $CleanName -replace 'RadeaonT', 'Radeon™'
                                            $CleanName = $CleanName -replace 'RadeonT', 'Radeon™'
                                        }
                                        Write-Output $CleanName
                                    }
                                }
                            } else {
                                Write-Output 'ERRO: Nenhuma GPU compatível encontrada'
                            }
                        } catch {
                            Write-Output 'ERRO: Erro ao detectar GPUs compatíveis'
                        }
                    }
                    
                    Get-VMGpuPartitionAdapterFriendlyName
                ";

                var result = await ExecutePowerShellScript(script);

                if (result.StartsWith("ERRO:") || result.Contains("ERRO:"))
                {
                    System.Windows.MessageBox.Show(result, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    var gpuLines = result.Split('\n')
                        .Where(line => !string.IsNullOrWhiteSpace(line) && 
                                       !line.StartsWith("ERRO:") && 
                                       !line.Contains("Erro ao detectar GPUs"))
                        .Select(line => line.Trim())
                        .ToList();

                    if (gpuLines.Any())
                    {
                        GpuListPanel.Children.Clear();
                        _detectedGpus = gpuLines;

                        foreach (var gpu in gpuLines)
                        {
                            var gpuCard = new Border
                            {
                                Background = new SolidColorBrush(Color.FromArgb(255, 240, 248, 255)),
                                CornerRadius = new CornerRadius(4),
                                Padding = new Thickness(10),
                                Margin = new Thickness(0, 5, 0, 5)
                            };

                            var gpuText = new TextBlock
                            {
                                Text = gpu,
                                FontSize = 14,
                                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 100, 0))
                            };

                            gpuCard.Child = gpuText;
                            GpuListPanel.Children.Add(gpuCard);
                        }

                        // Tornar o painel visível
                        GpuListPanel.Visibility = Visibility.Visible;

                        // Atualizar ComboBox
                        GpuComboBox.Items.Clear();
                        foreach (var gpu in _detectedGpus)
                        {
                            GpuComboBox.Items.Add(gpu);
                        }
                        if (_detectedGpus.Count > 0)
                        {
                            GpuComboBox.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Nenhuma GPU compatível foi encontrada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao detectar GPUs: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DetectGpuButton.IsEnabled = true;
                DetectGpuButton.Content = "🔍 Detectar GPUs";
            }
        }

        private void BrowseIsoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Selecionar ISO do Windows 11",
                Filter = "Arquivos ISO (*.iso)|*.iso",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IsoPathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void BrowseVhdPathButton_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderDialog.Description = "Selecionar pasta para o HD Virtual";
                folderDialog.ShowNewFolderButton = true;

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    VhdPathTextBox.Text = folderDialog.SelectedPath;
                }
            }
        }

        private async void CreateVmButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            // Verificar se está executando como administrador
            if (!IsRunningAsAdministrator())
            {
                var result = System.Windows.MessageBox.Show(
                    "Esta operação requer privilégios de administrador.\n\nDeseja executar como administrador?",
                    "Permissões Necessárias",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        RestartAsAdministrator();
                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Erro ao solicitar privilégios de administrador: {ex.Message}", 
                            "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            CreateVmButton.IsEnabled = false;
            StatusCard.Visibility = Visibility.Visible;
            UpdateStatus("Validando configurações...", 10);

            try
            {
                // Coletar dados do formulário
                var vmParams = CollectVmParameters();
                
                UpdateStatus("Preparando script de criação...", 30);
                
                // Criar o script PowerShell principal
                string script = CreateVmScript(vmParams);
                
                UpdateStatus("Executando criação da VM...", 50);
                
                // Executar o script
                var result = await ExecutePowerShellScript(script);
                
                if (result.Contains("ERRO") || result.Contains("ERROR"))
                {
                    UpdateStatus("Erro durante a criação da VM", 100);
                    System.Windows.MessageBox.Show($"Erro na criação da VM:\n{result}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    UpdateStatus("VM criada com sucesso!", 100);
                    System.Windows.MessageBox.Show("VM com GPU Passthrough criada com sucesso!\nA VM será iniciada automaticamente.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Erro durante a criação", 100);
                System.Windows.MessageBox.Show($"Erro ao criar VM: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CreateVmButton.IsEnabled = true;
            }
        }

        private bool ValidateFields()
        {
            var errors = new List<string>();

            // Validar Nome da VM
            if (string.IsNullOrWhiteSpace(VmNameTextBox.Text))
            {
                errors.Add("Nome da VM é obrigatório");
            }
            else if (VmNameTextBox.Text.Length < 3)
            {
                errors.Add("Nome da VM deve ter pelo menos 3 caracteres");
            }

            // Validar GPU selecionada
            if (GpuComboBox.SelectedItem == null)
            {
                errors.Add("Selecione uma GPU da lista");
            }

            // Validar ISO
            if (string.IsNullOrWhiteSpace(IsoPathTextBox.Text))
            {
                errors.Add("Selecione um arquivo ISO do Windows 11");
            }
            else if (!File.Exists(IsoPathTextBox.Text))
            {
                errors.Add("Arquivo ISO não encontrado");
            }

            // Validar tamanho do HD
            if (!int.TryParse(HdSizeTextBox.Text, out int hdSize) || hdSize < 20 || hdSize > 2000)
            {
                errors.Add("Tamanho do HD deve estar entre 20 e 2000 GB");
            }

            // Validar tamanho da RAM
            if (!int.TryParse(RamSizeTextBox.Text, out int ramSize) || ramSize < 2 || ramSize > 128)
            {
                errors.Add("Tamanho da RAM deve estar entre 2 e 128 GB");
            }

            // Validar alocação da GPU
            if (!int.TryParse(GpuAllocationTextBox.Text, out int gpuAllocation) || gpuAllocation < 10 || gpuAllocation > 100)
            {
                errors.Add("Alocação da GPU deve estar entre 10 e 100%");
            }

            // Validar local do HD virtual
            if (string.IsNullOrWhiteSpace(VhdPathTextBox.Text))
            {
                errors.Add("Selecione um local para o HD virtual");
            }
            else if (!Directory.Exists(VhdPathTextBox.Text))
            {
                errors.Add("Pasta do HD virtual não encontrada");
            }

            // Validar usuário
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                errors.Add("Nome do usuário é obrigatório");
            }

            if (errors.Any())
            {
                string errorMessage = "Corrija os seguintes erros:\n\n" + string.Join("\n", errors);
                System.Windows.MessageBox.Show(errorMessage, "Erro de Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private Dictionary<string, object> CollectVmParameters()
        {
            return new Dictionary<string, object>
            {
                ["VMName"] = VmNameTextBox.Text.Trim(),
                ["SourcePath"] = IsoPathTextBox.Text.Trim(),
                ["VhdFormat"] = "VHDX",
                ["DiskLayout"] = "UEFI",
                ["SizeBytes"] = $"{HdSizeTextBox.Text}GB",
                ["MemoryAmount"] = $"{RamSizeTextBox.Text}GB",
                ["CPUCores"] = "4",
                ["NetworkSwitch"] = "Default Switch",
                ["VHDPath"] = VhdPathTextBox.Text.Trim(),
                ["GPUName"] = GpuComboBox.SelectedItem?.ToString() ?? "AUTO",
                ["GPUResourceAllocationPercentage"] = GpuAllocationTextBox.Text,
                ["Username"] = UsernameTextBox.Text.Trim(),
                ["Password"] = string.IsNullOrWhiteSpace(PasswordTextBox.Text) ? "Windows123!" : PasswordTextBox.Text,
                ["Autologon"] = AutoLogonCheckBox.IsChecked == true ? "true" : "false"
            };
        }

        private string CreateVmScript(Dictionary<string, object> vmParams)
        {
            var scriptBuilder = new StringBuilder();
            
            // Adicionar parâmetros
            scriptBuilder.AppendLine("$params = @{");
            foreach (var param in vmParams)
            {
                if (param.Value is string)
                {
                    scriptBuilder.AppendLine($"    {param.Key} = \"{param.Value}\"");
                }
                else
                {
                    scriptBuilder.AppendLine($"    {param.Key} = {param.Value}");
                }
            }
            scriptBuilder.AppendLine("}");
            
            // Script simplificado sem dependências externas
            scriptBuilder.AppendLine(@"
function Is-Administrator {  
    $CurrentUser = [Security.Principal.WindowsIdentity]::GetCurrent();
    (New-Object Security.Principal.WindowsPrincipal $CurrentUser).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)  
}

function Test-HyperVEnabled {
    try {
        $hyperv = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All
        return $hyperv.State -eq 'Enabled'
    } catch {
        return $false
    }
}

function New-GPUEnabledVM {
    param(
        [string]$VMName,
        [string]$SourcePath,
        [string]$VHDPath,
        [int64]$SizeBytes,
        [int64]$MemoryAmount,
        [int]$CPUCores,
        [string]$NetworkSwitch,
        [string]$Username,
        [string]$Password
    )
    
    try {
        Write-Host 'Verificando pré-requisitos...'
        
        # Verificar se está executando como administrador
        if (!(Is-Administrator)) {
            throw 'Este script deve ser executado como Administrador'
        }
        
        # Verificar se o Hyper-V está habilitado
        if (!(Test-HyperVEnabled)) {
            throw 'Hyper-V não está habilitado. Habilite o Hyper-V nas Funcionalidades do Windows'
        }
        
        # Verificar se o ISO existe
        if (!(Test-Path $SourcePath)) {
            throw 'Arquivo ISO não encontrado: ' + $SourcePath
        }
        
        # Verificar se a pasta VHD existe
        if (!(Test-Path $VHDPath)) {
            throw 'Pasta do VHD não encontrada: ' + $VHDPath
        }
        
        # Verificar se VM já existe
        if (Get-VM -Name $VMName -ErrorAction SilentlyContinue) {
            throw 'Uma VM com o nome ' + $VMName + ' já existe'
        }
        
        # Criar caminho completo do VHD
        $VHDFullPath = Join-Path $VHDPath ($VMName + '.vhdx')
        
        if (Test-Path $VHDFullPath) {
            throw 'Arquivo VHD já existe: ' + $VHDFullPath
        }
        
        Write-Host 'Criando VM básica...'
        
        # Criar VM Generation 2 (UEFI)
        $VM = New-VM -Name $VMName -Generation 2 -MemoryStartupBytes $MemoryAmount -NewVHDPath $VHDFullPath -NewVHDSizeBytes $SizeBytes
        
        # Configurar processador
        Set-VMProcessor -VMName $VMName -Count $CPUCores
        
        # Configurar memória
        Set-VMMemory -VMName $VMName -DynamicMemoryEnabled $false
        
        # Adicionar DVD drive e anexar ISO
        Add-VMDvdDrive -VMName $VMName -Path $SourcePath
        
        # Configurar boot order (DVD primeiro)
        $DVDDrive = Get-VMDvdDrive -VMName $VMName
        $HardDrive = Get-VMHardDiskDrive -VMName $VMName
        Set-VMFirmware -VMName $VMName -BootOrder $DVDDrive, $HardDrive
        
        # Configurar switch de rede se especificado
        if ($NetworkSwitch -and $NetworkSwitch -ne '') {
            if (Get-VMSwitch -Name $NetworkSwitch -ErrorAction SilentlyContinue) {
                Get-VMNetworkAdapter -VMName $VMName | Connect-VMNetworkAdapter -SwitchName $NetworkSwitch
            }
        }
        
        # Configurações específicas para GPU Passthrough
        Write-Host 'Configurando VM para GPU Passthrough...'
        
        # Desabilitar checkpoints
        Set-VM -Name $VMName -CheckpointType Disabled
        
        # Configurar memória para GPU
        Set-VM -Name $VMName -LowMemoryMappedIoSpace 1GB -HighMemoryMappedIoSpace 32GB
        
        # Habilitar Guest Control Cache Types
        Set-VM -Name $VMName -GuestControlledCacheTypes $true
        
        # Configurar parada automática
        Set-VM -Name $VMName -AutomaticStopAction ShutDown
        
        # Habilitar TPM e Secure Boot
        Set-VMKeyProtector -VMName $VMName -NewLocalKeyProtector
        Enable-VMTPM -VMName $VMName
        
        # Detectar e adicionar GPU se disponível
        try {
            Write-Host 'Tentando adicionar GPU Passthrough...'
            $PartitionableGPUs = Get-WmiObject -Class 'Msvm_PartitionableGpu' -Namespace 'ROOT\virtualization\v2' -ErrorAction SilentlyContinue
            
            if ($PartitionableGPUs) {
                # Adicionar primeira GPU disponível
                $FirstGPU = $PartitionableGPUs[0]
                Add-VMGpuPartitionAdapter -VMName $VMName -InstancePath $FirstGPU.Name
                Write-Host 'GPU Passthrough adicionado com sucesso!'
            } else {
                Write-Host 'AVISO: Nenhuma GPU compatível encontrada para passthrough'
            }
        } catch {
            Write-Host 'AVISO: Não foi possível adicionar GPU Passthrough: ' + $_.Exception.Message
        }
        
        Write-Host 'VM criada com sucesso: ' + $VMName
        Write-Host 'Arquivo VHD: ' + $VHDFullPath
        Write-Host ''
        Write-Host 'Para iniciar a VM, execute: Start-VM -Name ' + $VMName
        Write-Host 'Para conectar, execute: vmconnect localhost ' + $VMName
        
        return $true
        
    } catch {
        Write-Host 'ERRO: ' + $_.Exception.Message
        return $false
    }
}

try {
    Write-Host 'Iniciando criação da VM com GPU Passthrough...'
    Write-Host ('VMName: ' + $params.VMName)
    Write-Host ('SourcePath: ' + $params.SourcePath)
    Write-Host ('VHDPath: ' + $params.VHDPath)
    Write-Host ('SizeBytes: ' + $params.SizeBytes)
    Write-Host ('MemoryAmount: ' + $params.MemoryAmount)
    Write-Host ''
    
    $success = New-GPUEnabledVM -VMName $params.VMName -SourcePath $params.SourcePath -VHDPath $params.VHDPath -SizeBytes ([int64]($params.SizeBytes.Replace('GB','')) * 1GB) -MemoryAmount ([int64]($params.MemoryAmount.Replace('GB','')) * 1GB) -CPUCores $params.CPUCores -NetworkSwitch $params.NetworkSwitch -Username $params.Username -Password $params.Password
    
    if ($success) {
        Write-Host 'VM criada com sucesso!'
    } else {
        Write-Host 'Falha na criação da VM'
    }
    
} catch {
    Write-Host 'ERRO GERAL: ' + $_.Exception.Message
}
");
            
            return scriptBuilder.ToString();
        }

        private void UpdateStatus(string message, int progress)
        {
            StatusTextBlock.Text = message;
            ProgressBar.Value = progress;
        }

        private async Task<string> ExecutePowerShellScript(string script)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Criar arquivo temporário para o script
                    string tempFile = Path.GetTempFileName() + ".ps1";
                    
                    // Salvar com encoding UTF-8 com BOM para suporte completo a caracteres
                    File.WriteAllText(tempFile, script, new UTF8Encoding(true));

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-ExecutionPolicy Bypass -NoProfile -InputFormat None -OutputFormat Text -File \"{tempFile}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    // Configurar variáveis de ambiente para UTF-8
                    startInfo.Environment["POWERSHELL_TELEMETRY_OPTOUT"] = "1";
                    startInfo.Environment["PSModulePath"] = "";

                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            
                            process.WaitForExit();

                            // Limpar arquivo temporário
                            try { File.Delete(tempFile); } catch { }

                            if (!string.IsNullOrEmpty(error))
                            {
                                // Limpar caracteres de controle que podem causar problemas de codificação
                                error = CleanOutputString(error);
                                return $"ERRO: {error}";
                            }

                            // Limpar caracteres de controle da saída
                            output = CleanOutputString(output);
                            return output;
                        }
                    }
                }
                catch (Exception ex)
                {
                    return $"ERRO: {ex.Message}";
                }

                return "ERRO: Não foi possível executar o script";
            });
        }

        private string CleanOutputString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remover caracteres de controle e BOM
            var cleanChars = input.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray();
            string cleaned = new string(cleanChars);
            
            // Remover BOM UTF-8 se presente
            if (cleaned.StartsWith("\uFEFF"))
                cleaned = cleaned.Substring(1);
                
            return cleaned.Trim();
        }

        private bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private void RestartAsAdministrator()
        {
            var currentProcess = Process.GetCurrentProcess();
            var mainModule = currentProcess.MainModule;
            
            if (mainModule?.FileName == null)
            {
                throw new InvalidOperationException("Não foi possível obter o caminho do executável atual");
            }

            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = mainModule.FileName,
                Verb = "runas"
            };

            try
            {
                Process.Start(processInfo);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Não foi possível solicitar privilégios de administrador: {ex.Message}");
            }
        }
    }
}