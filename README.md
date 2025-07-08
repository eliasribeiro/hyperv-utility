# 🖥️ HyperV Utilities

<div align="center">

![HyperV Logo](https://img.shields.io/badge/HyperV-Utilities-blue?style=for-the-badge&logo=microsoft&logoColor=white)
![.NET](https://img.shields.io/badge/.NET_9-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**Um conjunto completo de utilitários para maximizar o uso do Hyper-V no Windows**

[📥 Download](#-download) •
[🚀 Instalação](#-instalação) •
[📖 Documentação](#-funcionalidades) •
[❓ Suporte](#-suporte)

</div>

---

## 📋 Sobre o Projeto

O **HyperV Utilities** é uma coleção de ferramentas poderosas para usuários avançados do Hyper-V no Windows. O projeto oferece duas funcionalidades principais que simplificam tarefas complexas de virtualização:

### 🎯 Funcionalidades Principais

| 🔧 Funcionalidade      | 📝 Descrição                                                                  |
| ---------------------- | ----------------------------------------------------------------------------- |
| **🎮 GPU Passthrough** | Criação automatizada de VMs com GPU habilitada para máxima performance        |
| **📁 File Copier**     | Interface moderna para transferência bidirecional de arquivos entre Host ↔ VM |

---

## ⚡ Funcionalidades

### 🎮 1. GPU Passthrough (HyperV Utilities)

Automatiza a criação de máquinas virtuais com acesso direto à GPU física do sistema, permitindo:

- **Performance Nativa**: Acesso direto ao hardware gráfico
- **Gaming em VM**: Execute jogos com performance próxima ao nativo
- **Workloads GPU**: Ideal para machine learning, renderização e mineração
- **Configuração Automática**: Script que configura todos os parâmetros necessários

#### 📋 Pré-requisitos

- Windows 10/11 Pro ou Enterprise
- CPU com suporte à virtualização (Intel VT-x ou AMD-V)
- GPU compatível com passthrough
- Hyper-V habilitado
- Privilégios de administrador

### 📁 2. File Copier (HyperVFileCopier)

Aplicação moderna com interface WinUI 3 para transferência eficiente de arquivos:

- **Interface Intuitiva**: Design moderno com Fluent Design
- **Transferência Bidirecional**: Host → VM e VM → Host
- **Suporte Completo**: Arquivos individuais e pastas inteiras
- **Monitoramento**: Acompanhe o progresso em tempo real
- **Versão Portátil**: Executável único sem necessidade de instalação

---

## 🚀 Instalação

### 📥 Download

Escolha a versão adequada para seu sistema:

| Versão           | Descrição                       | Tamanho   | Link          |
| ---------------- | ------------------------------- | --------- | ------------- |
| **Portátil x64** | Recomendada para Windows 64-bit | ~50-80 MB | [Download](#) |
| **Portátil x86** | Compatível com Windows 32-bit   | ~45-75 MB | [Download](#) |

### 🛠️ Compilação Manual

Se preferir compilar o projeto:

```powershell
# Clone o repositório
git clone https://github.com/seu-usuario/hyperv-utility.git
cd hyperv-utility

# Para HyperVFileCopier - Execute o script de build
cd HyperVFileCopier
.\build-portable.cmd

# Ou compile manualmente
cd HyperVFileCopier\HyperVFileCopier
dotnet publish HyperVFileCopier.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -o "dist\win-x64"
```

---

## 📖 Como Usar

### 🎮 GPU Passthrough

```powershell
# 1. Abra PowerShell como Administrador
# 2. Navegue até a pasta HyperV Utilities
cd "HyperV Utilities"

# 3. Execute o script de configuração
.\setup-gpu-passthrough.ps1

# 4. Siga as instruções na tela
```

**⚠️ Importante**: Certifique-se de que:

- A GPU não está sendo usada pelo host
- O secure boot está desabilitado na VM
- Os drivers de GPU estão disponíveis para a VM

### 📁 File Copier

#### 🖱️ Interface Gráfica

1. **Execute** o `HyperVFileCopier.exe`
2. **Configure** na aba apropriada:
   - **Host → VM**: Para enviar arquivos para a VM
   - **VM → Host**: Para receber arquivos da VM
3. **Preencha** os campos:
   - Nome da máquina virtual
   - Caminho de origem e destino
4. **Clique** em "Transferir"

#### 💻 Exemplo de Uso

```
┌─────────────────────────────────────────┐
│           Hyper-V File Copier           │
├─────────────────────────────────────────┤
│ [Host → VM] [VM → Host]                 │
│                                         │
│ 🖥️ Máquina Virtual                      │
│ Nome da VM: MinhaVM                     │
│ Destino: C:\Temp\                       │
│                                         │
│ 📁 Host                                 │
│ Arquivo: C:\Users\João\documento.pdf    │
│                                         │
│ [🔄 Transferir]                         │
└─────────────────────────────────────────┘
```

---

## ⚙️ Requisitos do Sistema

### 🖥️ Sistema Operacional

- **Windows 10** versão 1809 (build 17763) ou superior
- **Windows 11** (qualquer versão)
- **Windows Server 2019/2022** (para ambientes corporativos)

### 🔧 Componentes Necessários

- **Hyper-V** instalado e configurado
- **PowerShell** 5.1 ou superior
- **Privilégios administrativos**
- **.NET 9 Runtime** (incluído na versão portátil)

### 💾 Hardware Recomendado

- **CPU**: Intel VT-x ou AMD-V habilitado
- **RAM**: 8GB mínimo, 16GB recomendado
- **Armazenamento**: 10GB livres
- **GPU**: DirectX 11 compatível (para GPU passthrough)

---

## 🔧 Configuração Avançada

### 🎮 GPU Passthrough

Para configuração manual do GPU passthrough:

```powershell
# Verificar GPUs disponíveis
Get-VMHostAssignableDevice

# Desmontar GPU do host
Disable-PnpDevice -InstanceId "PCI\VEN_10DE&DEV_..."

# Atribuir GPU à VM
Add-VMAssignableDevice -VMName "MinhaVM" -LocationPath "PCIROOT(0)#PCI(..."

# Configurar CPU e memória
Set-VM -VMName "MinhaVM" -ProcessorCount 4 -DynamicMemoryEnabled $false -MemoryStartupBytes 8GB
```

### 📁 File Copier

Configurações avançadas no registro:

```powershell
# Aumentar timeout de transferência
Set-ItemProperty -Path "HKLM:\SOFTWARE\HyperVFileCopier" -Name "TimeoutSeconds" -Value 300

# Configurar buffer de transferência
Set-ItemProperty -Path "HKLM:\SOFTWARE\HyperVFileCopier" -Name "BufferSize" -Value 1048576
```

---

## 🐛 Solução de Problemas

### ❌ Problemas Comuns

<details>
<summary><strong>🔧 GPU Passthrough não funciona</strong></summary>

**Verificações**:

- [ ] IOMMU/VT-d habilitado no BIOS
- [ ] Secure boot desabilitado na VM
- [ ] GPU não está sendo usada pelo host
- [ ] Drivers instalados na VM

**Solução**:

```powershell
# Verificar suporte IOMMU
Get-VMHostSupportedVersion

# Verificar dispositivos atribuíveis
Get-VMHostAssignableDevice
```

</details>

<details>
<summary><strong>📁 File Copier não conecta à VM</strong></summary>

**Verificações**:

- [ ] VM está ligada e funcionando
- [ ] PowerShell Remoting habilitado
- [ ] Credenciais corretas
- [ ] Firewall não está bloqueando

**Solução**:

```powershell
# Habilitar PowerShell Remoting na VM
Enable-PSRemoting -Force

# Verificar conectividade
Test-NetConnection -ComputerName "IP_DA_VM" -Port 5985
```

</details>

<details>
<summary><strong>⚠️ Aplicação não abre</strong></summary>

**Verificações**:

- [ ] Windows 10 build 17763 ou superior
- [ ] Visual C++ Redistributable instalado
- [ ] Executar como administrador
- [ ] Windows Defender não está bloqueando

**Solução**:

```powershell
# Instalar Visual C++ Redistributable
# Download: https://aka.ms/vs/17/release/vc_redist.x64.exe

# Verificar versão do Windows
Get-ComputerInfo | Select WindowsProductName, WindowsVersion
```

</details>

---

## 📊 Performance e Otimização

### 🎮 GPU Passthrough

| Configuração    | Performance   | Uso Recomendado     |
| --------------- | ------------- | ------------------- |
| **Gaming**      | 95-98% nativo | Jogos AAA, VR       |
| **Workstation** | 90-95% nativo | CAD, Renderização   |
| **ML/AI**       | 85-95% nativo | Training, Inference |

### 📁 File Transfer

| Tamanho do Arquivo | Velocidade Esperada | Tempo Estimado |
| ------------------ | ------------------- | -------------- |
| **< 100 MB**       | 50-100 MB/s         | < 5 segundos   |
| **100 MB - 1 GB**  | 80-150 MB/s         | 10-60 segundos |
| **> 1 GB**         | 100-200 MB/s        | 1-10 minutos   |

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Por favor:

1. **Fork** o projeto
2. **Crie** uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. **Commit** suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. **Push** para a branch (`git push origin feature/AmazingFeature`)
5. **Abra** um Pull Request

---

## 📄 Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

---

## 🆘 Suporte

### 💬 Comunidade

- **Issues**: [GitHub Issues](https://github.com/seu-usuario/hyperv-utility/issues)
- **Discussões**: [GitHub Discussions](https://github.com/seu-usuario/hyperv-utility/discussions)
- **Wiki**: [Documentação Completa](https://github.com/seu-usuario/hyperv-utility/wiki)

### 📧 Contato

- **Email**: seu-email@exemplo.com
- **LinkedIn**: [Seu Perfil](https://linkedin.com/in/seu-perfil)

---

## ⭐ Agradecimentos

- Microsoft pelo Hyper-V e WinUI 3
- Comunidade .NET
- Todos os contribuidores do projeto

---

<div align="center">

**⭐ Se este projeto foi útil, considere dar uma estrela!**

![Visitors](https://visitor-badge.laobi.icu/badge?page_id=seu-usuario.hyperv-utility)
![Stars](https://img.shields.io/github/stars/seu-usuario/hyperv-utility?style=social)
![Forks](https://img.shields.io/github/forks/seu-usuario/hyperv-utility?style=social)

</div>
