using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HyperVUtilities.Pages
{
    public partial class GpuPassthroughPage : Page
    {
        public GpuPassthroughPage()
        {
            InitializeComponent();
        }

        private async void CheckCompatibilityButton_Click(object sender, RoutedEventArgs e)
        {
            CheckCompatibilityButton.IsEnabled = false;
            CheckCompatibilityButton.Content = "🔄 Verificando...";
            
            // Limpar resultados anteriores
            WarningPanel.Visibility = Visibility.Collapsed;
            WarningList.Children.Clear();
            SystemStatusPanel.Children.Clear();

            try
            {
                var systemCheck = await CheckSystemCompatibilityAsync();
                DisplaySystemStatus(systemCheck);
            }
            catch (Exception ex)
            {
                SystemStatusText.Text = $"Erro ao verificar compatibilidade: {ex.Message}";
                MessageBox.Show($"Erro: {ex.Message}", "Erro na Verificação", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CheckCompatibilityButton.IsEnabled = true;
                CheckCompatibilityButton.Content = "🔍 Verificar Compatibilidade";
            }
        }

        private async void DetectGpuButton_Click(object sender, RoutedEventArgs e)
        {
            DetectGpuButton.IsEnabled = false;
            DetectGpuButton.Content = "🔄 Detectando...";
            GpuStatusText.Text = "Verificando compatibilidade do sistema...";
            
            // Limpar resultados anteriores
            GpuListBox.Items.Clear();
            GpuListBox.Visibility = Visibility.Collapsed;
            WarningPanel.Visibility = Visibility.Collapsed;
            WarningList.Children.Clear();
            SystemStatusPanel.Children.Clear();

            try
            {
                var systemCheck = await CheckSystemCompatibilityAsync();
                DisplaySystemStatus(systemCheck);

                if (systemCheck.IsCompatible)
                {
                    var gpus = await DetectGpusAsync();
                    DisplayGpuResults(gpus, systemCheck.Warnings);
                }
                else
                {
                    GpuStatusText.Text = "Sistema não compatível. Resolva os problemas acima para continuar.";
                }
            }
            catch (Exception ex)
            {
                GpuStatusText.Text = $"Erro ao detectar GPUs: {ex.Message}";
                MessageBox.Show($"Erro: {ex.Message}", "Erro na Detecção", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DetectGpuButton.IsEnabled = true;
                DetectGpuButton.Content = "🔍 Detectar GPUs";
            }
        }

        private async Task<SystemCompatibilityResult> CheckSystemCompatibilityAsync()
        {
            var result = new SystemCompatibilityResult();
            
            // Script PowerShell para verificação do sistema
            var script = @"
                Function Get-DesktopPC {
                    $isDesktop = $true
                    if(Get-WmiObject -Class win32_systemenclosure | Where-Object { $_.chassistypes -eq 9 -or $_.chassistypes -eq 10 -or $_.chassistypes -eq 14}) {
                        Write-Output 'LAPTOP_CHASSIS'
                        $isDesktop = $false 
                    }
                    if (Get-WmiObject -Class win32_battery) { 
                        Write-Output 'BATTERY_DETECTED'
                        $isDesktop = $false 
                    }
                    $isDesktop
                }

                Function Get-WindowsCompatibleOS {
                    $build = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
                    if ($build.CurrentBuild -ge 19041 -and ($($build.editionid -like 'Professional*') -or $($build.editionid -like 'Enterprise*') -or $($build.editionid -like 'Education*'))) {
                        Return $true
                    } Else {
                        Write-Output 'WINDOWS_INCOMPATIBLE'
                        Return $false
                    }
                }

                Function Get-HyperVEnabled {
                    if (Get-WindowsOptionalFeature -Online | Where-Object FeatureName -Like 'Microsoft-Hyper-V-All') {
                        Return $true
                    } Else {
                        Write-Output 'HYPERV_DISABLED'
                        Return $false
                    }
                }

                Function Get-WSLEnabled {
                    try {
                        if ((wsl -l -v 2>$null)[2].length -gt 1 ) {
                            Write-Output 'WSL_ENABLED'
                            Return $true
                        } Else {
                            Return $false
                        }
                    } catch {
                        Return $false
                    }
                }

                $isDesktop = Get-DesktopPC
                $isWindowsOK = Get-WindowsCompatibleOS
                $isHyperVOK = Get-HyperVEnabled
                $isWSLEnabled = Get-WSLEnabled

                Write-Output ""DESKTOP:$isDesktop""
                Write-Output ""WINDOWS:$isWindowsOK""
                Write-Output ""HYPERV:$isHyperVOK""
                Write-Output ""WSL:$isWSLEnabled""
            ";

            var output = await ExecutePowerShellAsync(script);
            
            foreach (var line in output)
            {
                if (line.Contains("LAPTOP_CHASSIS"))
                    result.Warnings.Add("Computador é um laptop. GPUs dedicadas particionadas podem não funcionar com Parsec.");
                if (line.Contains("BATTERY_DETECTED"))
                    result.Warnings.Add("Bateria detectada (laptop). GPUs via Thunderbolt 3/4 podem funcionar.");
                if (line.Contains("WINDOWS_INCOMPATIBLE"))
                    result.Warnings.Add("Apenas Windows 10 20H1+ ou Windows 11 (Pro/Enterprise/Education) é suportado.");
                if (line.Contains("HYPERV_DISABLED"))
                    result.Warnings.Add("Você precisa habilitar Virtualização na BIOS e adicionar o recurso Hyper-V do Windows.");
                if (line.Contains("WSL_ENABLED"))
                    result.Warnings.Add("WSL está habilitado. Isso pode interferir com GPU-P e produzir erro 43 na VM.");

                if (line.StartsWith("DESKTOP:"))
                    result.IsDesktop = line.Split(':')[1] == "True";
                if (line.StartsWith("WINDOWS:"))
                    result.IsWindowsCompatible = line.Split(':')[1] == "True";
                if (line.StartsWith("HYPERV:"))
                    result.IsHyperVEnabled = line.Split(':')[1] == "True";
            }

            result.IsCompatible = result.IsDesktop && result.IsWindowsCompatible && result.IsHyperVEnabled;
            
            return result;
        }

        private async Task<List<string>> DetectGpusAsync()
        {
            var script = @"
                Function Get-VMGpuPartitionAdapterFriendlyName {
                    try {
                        $Devices = (Get-WmiObject -Class 'Msvm_PartitionableGpu' -ComputerName $env:COMPUTERNAME -Namespace 'ROOT\virtualization\v2').name
                        if ($Devices) {
                            Foreach ($GPU in $Devices) {
                                $GPUParse = $GPU.Split('#')[1]
                                $DeviceName = Get-WmiObject Win32_PNPSignedDriver | where {($_.HardwareID -eq ""PCI\$GPUParse"")} | select DeviceName -ExpandProperty DeviceName
                                if ($DeviceName) {
                                    Write-Output $DeviceName
                                }
                            }
                        } else {
                            Write-Output 'NO_GPUS_FOUND'
                        }
                    } catch {
                        Write-Output 'ERROR_DETECTING_GPUS'
                    }
                }
                Get-VMGpuPartitionAdapterFriendlyName
            ";

            var output = await ExecutePowerShellAsync(script);
            var gpus = new List<string>();

            foreach (var line in output.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                if (line == "NO_GPUS_FOUND" || line == "ERROR_DETECTING_GPUS")
                    continue;
                
                gpus.Add(line.Trim());
            }

            return gpus;
        }

        private async Task<List<string>> ExecutePowerShellAsync(string script)
        {
            return await Task.Run(() =>
            {
                var output = new List<string>();
                
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            var result = process.StandardOutput.ReadToEnd();
                            var error = process.StandardError.ReadToEnd();
                            
                            process.WaitForExit();

                            if (!string.IsNullOrEmpty(result))
                            {
                                output.AddRange(result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                            }

                            if (!string.IsNullOrEmpty(error))
                            {
                                output.Add($"ERROR: {error}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    output.Add($"EXCEPTION: {ex.Message}");
                }

                return output;
            });
        }

        private void DisplaySystemStatus(SystemCompatibilityResult result)
        {
            SystemStatusPanel.Children.Clear();

            // Status geral
            var statusIcon = result.IsCompatible ? "✅" : "❌";
            var statusText = result.IsCompatible ? "Sistema Compatível" : "Sistema Incompatível";
            var statusColor = result.IsCompatible ? Brushes.Green : Brushes.Red;

            var statusBlock = new TextBlock
            {
                Text = $"{statusIcon} {statusText}",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = statusColor,
                Margin = new Thickness(0, 0, 0, 10)
            };
            SystemStatusPanel.Children.Add(statusBlock);

            // Detalhes
            AddStatusItem("Desktop PC", result.IsDesktop);
            AddStatusItem("Windows Compatível", result.IsWindowsCompatible);
            AddStatusItem("Hyper-V Habilitado", result.IsHyperVEnabled);

            // Mostrar avisos se houver
            if (result.Warnings.Any())
            {
                DisplayWarnings(result.Warnings);
            }
        }

        private void AddStatusItem(string label, bool status)
        {
            var icon = status ? "✅" : "❌";
            var color = status ? Brushes.Green : Brushes.Red;
            
            var statusBlock = new TextBlock
            {
                Text = $"{icon} {label}",
                FontSize = 14,
                Foreground = color,
                Margin = new Thickness(20, 0, 0, 5)
            };
            
            SystemStatusPanel.Children.Add(statusBlock);
        }

        private void DisplayWarnings(List<string> warnings)
        {
            WarningList.Children.Clear();
            
            foreach (var warning in warnings)
            {
                var warningBlock = new TextBlock
                {
                    Text = $"• {warning}",
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                
                WarningList.Children.Add(warningBlock);
            }
            
            WarningPanel.Visibility = Visibility.Visible;
        }

        private void DisplayGpuResults(List<string> gpus, List<string> warnings)
        {
            if (gpus.Any())
            {
                GpuListBox.Items.Clear();
                foreach (var gpu in gpus)
                {
                    GpuListBox.Items.Add(gpu);
                }
                
                GpuListBox.Visibility = Visibility.Visible;
                GpuStatusText.Text = $"✅ {gpus.Count} GPU(s) compatível(is) encontrada(s)";
                GpuStatusText.Foreground = Brushes.Green;
            }
            else
            {
                GpuStatusText.Text = "❌ Nenhuma GPU compatível encontrada";
                GpuStatusText.Foreground = Brushes.Red;
            }
        }
    }

    public class SystemCompatibilityResult
    {
        public bool IsCompatible { get; set; }
        public bool IsDesktop { get; set; }
        public bool IsWindowsCompatible { get; set; }
        public bool IsHyperVEnabled { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }
} 