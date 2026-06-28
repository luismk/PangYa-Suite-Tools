# PangYa-Suite-Tools
Editor Files PangYa using Csharp

[![Build Status](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://microsoft.com/windows)
[![Project Stage](https://img.shields.io/badge/Stage-In--Development-orange.svg)]()

**Pangya Suite Tools** (ou *Pangya Studio Tools*) é uma solução integrada de engenharia reversa e modificação avançada, desenvolvida inteiramente em **C# (.NET 10)**. O objetivo deste ecossistema é centralizar a leitura, edição, conversão e compilação de múltiplos formatos de arquivos nativos utilizados pelo cliente e servidor do jogo **PangYa** (como estruturas `.PAK`, tabelas `.IFF` e listas de atualização).

O projeto é estruturado sobre uma API de alto desempenho (`PangyaAPI`) e uma interface gráfica rica em **Windows Forms**, utilizando operações assíncronas modernas baseadas em Tasks para garantir que tarefas pesadas de I/O de disco e criptografia ocorram em background, mantendo a UI totalmente responsiva.

---

## 🗺️ Visão Geral dos Módulos

O ecossistema foi projetado para ser modular, expandindo-se pelas seguintes frentes de desenvolvimento:

- [x] **PangyaAPI.PAK (`FrmPakMaker.cs`)**: Manipulação cirúrgica de pacotes de dados. Extração individual ou em lote, injeção/mesclagem dinâmica de arquivos e suporte total ao algoritmo XTEA multiregião.
- [ ] **PangyaAPI.IFF**: Parser e editor estruturado para tabelas de dados do jogo (`Character.iff`, `Item.iff`, etc.), permitindo a customização completa de atributos, itens e mecânicas internas do servidor.
- [x] **PangyaAPI.UpdateList**: Utilitário para geração e assinatura de listas criptografadas em XML para o Launcher/Updater do jogo.

---

## 🚀 Recursos Atuais do Módulo PAK

- **Suporte Multiversão:** Manipulação nativa de estruturas clássicas (`Raw`, versões inferiores com XOR) e estruturas modernas baseadas na especificação `V3`.
- **Compressão Nativa:** Implementação de alta performance dos compressores utilizados pelo jogo: **LZ77**, **LZ772** e armazenamento direto (**Raw**) para formatos de áudio como `.wav` e `.mp3`.
- **Criptografia por Região (XTEA):** Suporte completo ao algoritmo XTEA para criptografia de cabeçalhos utilizando chaves oficiais e customizadas:
  - 🌍 Global (GB) | 🇹🇭 Tailandês (TH) | 🇯🇵 Japonês (JP) | 🇰🇷 Coreano (KR)
  - 🇮🇩 Indonesiano (ID) | 🇪🇺 Europeu (EU) | 🛠️ Super SS Dev (Custom)
- **Interface Gráfica Avançada:**
  - Carregamento rápido via **Drag-and-Drop** (Arrastar e Soltar) de arquivos ou pastas diretamente na UI.
  - Menu de contexto na `ListView` para extração isolada e cirúrgica de um único arquivo de dentro do pacote.
  - Sistema de mesclagem inteligente para atualizar arquivos dentro de um `.pak` existente gerando backups automáticos de segurança.
  - Processador em lote (*Batch Extraction*) para extrair múltiplos arquivos de uma só vez.

---

## 🛠️ Arquitetura Técnica (`PangyaAPI`)

- **Isolamento de Estado:** A classe `PakWriter` separa completamente a gravação física dos bytes em disco da cifragem do cabeçalho de metadados. Isso previne bugs clássicos de desalinhamento de blocos do XTEA e evita a corrupção de offsets lidos por ferramentas de terceiros (como o WinPak).
- **Tratamento de Strings Binárias:** O módulo `PakFileEntry` realiza a higienização automatizada a nível de bytes (`SanitizeNameRaw`), eliminando lixo de alinhamento em memória e terminadores nulos (`\0`), prevenindo erros de I/O no sistema de arquivos do Windows.

---

## 💻 Como Iniciar (Módulo PAK)

Exemplo rápido de uso da API para compilar uma pasta de modificações para o formato do cliente japonês (`V3`):

```csharp
using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;

var writer = new PakWriter
{
    EntryVersion = PakFileEntryVersion.V3,
    EntryType = PakFileEntryType.LZ772,
    CompressLevel = 5,
    LocationKeys = PakKeys.JP, // Aplica os 4 uints da região japonesa
    Author = "SuiteTools"
};

// Compila recursivamente mantendo a integridade dos offsets
writer.CreateFromDirectory(@"C:\Modding\data", @"C:\Games\PangYa\ProjectG.pak");