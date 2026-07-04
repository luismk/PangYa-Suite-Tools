# PangYa-Suite-Tools
Advanced PangYa File Suite Editor written in C#

[![Build Status](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://microsoft.com/windows)
[![Project Stage](https://img.shields.io/badge/Stage-In--Development-orange.svg)]()

---

## 🇺🇸 English

**Pangya Suite Tools** (or *Pangya Studio Tools*) is an integrated ecosystem for advanced reverse engineering and modification, developed entirely in **C# (.NET 10)**. This solution centralizes reading, editing, converting, and compiling multiple native file formats used by both the client and server of the game **PangYa** (such as `.PAK` structures, `.IFF` tables, and patch lists).

The project is built on top of a high-performance API (`PangyaAPI`) and a rich **Windows Forms** graphical interface, utilizing modern asynchronous Task-based operations to ensure heavy disk I/O and cryptographic tasks run smoothly in the background, keeping the UI fully responsive.

### 🗺️ Module Overview
- [x] **PangyaAPI.PAK (`FrmPakMaker.cs`)**: Surgical data package manipulation. Individual or batch extraction, dynamic file injection/merging, and full multi-region XTEA algorithm support.
- [ ] **PangyaAPI.PAK Sync (`FrmPakDiff.cs`)**: Cross-client Multi-PAK structural synchronization tool to compare and isolate missing, modified, or identical files between different clients.
- [ ] **PangyaAPI.IFF**: Structured parser and editor for game data tables (`Character.iff`, `Item.iff`, etc.), enabling complete customization of server attributes and item mechanics.
- [x] **PangyaAPI.UpdateList**: Utility for generating and signing encrypted XML patch lists for the game Launcher/Updater.

### 🚀 Advanced Features
- **Auto-Elevated Execution:** Built-in dynamic check to request administrative UAC privileges automatically, preserving file path arguments.
- **Windows File Association:** Integrated option to register the `.pak` extension into the Windows Registry, enabling direct execution via double-click or the "Open with PakMaker" context menu.
- **Application Log Viewer:** A shared logging interface retains tool activity for the current session and exposes it from the main menu; PAK audit activity is also written to `activity_log.txt`.
- **Multi-Region XTEA Cryptography:** Full support for official and custom header encryptions: Global (GB), Thailand (TH), Japan (JP), Korea (KR), Indonesia (ID), Europe (EU), and Super SS Dev (Custom).
- **Advanced Tree View Interaction:** Full keyboard mapping supporting the **Delete** key for instant folder removals, along with right-click context menus for targeted extraction or deletion.

### 🛠️ Technical Snippet (PAK Compilation Example)
To code with this API to compile a folder using Japanese specification (V3):

```csharp
using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;

var writer = new PakWriter
{
    EntryVersion = PakFileEntryVersion.V3,
    EntryType = PakFileEntryType.LZ772,
    CompressLevel = 5,
    LocationKeys = PakKeys.JP,// xtea_key
    Author = "SuiteTools"
};

// Compiles recursively while preserving offsets.
writer.CreateFromDirectory(@"C:\Modding\data", @"C:\Games\PangYa\ProjectG.pak");
```

### JSON IFF schemas

IFF editor layouts are defined by versioned JSON files in
`%LocalAppData%\PangYa-Suite-Tools\schemas`. Default TH and JP schemas are copied there on first use without overwriting existing files. Schema files are matched by IFF filename and region (for example, `Item.TH.json`), with `.default.json` as the optional fallback. The editor's **Schema columns** dialog saves column changes back to the matching JSON file.
The region selector can be set before opening an IFF; recognized TH or JP headers automatically update the selector, while the manual choice is used for files whose region cannot be detected.
The editor reports bytes whose bits are not fully represented by schema fields. Its catch-all `Raw record` column is hidden by default and can be shown with a persisted display option without affecting this coverage count.

---

## 🇧🇷 Português

**Pangya Suite Tools** (ou *Pangya Studio Tools*) é uma solução integrada de engenharia reversa e modificação avançada, desenvolvida inteiramente em **C# (.NET 10)**. O objetivo deste ecossistema é centralizar a leitura, edição, conversão e compilação de múltiplos formatos de arquivos nativos utilizados pelo cliente e servidor do jogo **PangYa** (como estruturas `.PAK`, tabelas `.IFF` e listas de atualização).

O projeto é estruturado sobre uma API de alto desempenho (`PangyaAPI`) e uma interface gráfica rica em **Windows Forms**, utilizando operações assíncronas modernas baseadas em Tasks para garantir que tarefas pesadas de I/O de disco e criptografia ocorram em background, mantendo a UI totalmente responsiva.

### 🗺️ Visão Geral dos Módulos
- [x] **PangyaAPI.PAK (`FrmPakMaker.cs`)**: Manipulação cirúrgica de pacotes de dados. Extração individual ou em lote, injeção/mesclagem dinâmica de arquivos e suporte total ao algoritmo XTEA multiregião.
- [ ] **PangyaAPI.PAK Sync (`FrmPakDiff.cs`)**: Ferramenta de sincronização estrutural Multi-PAK entre clientes para comparar e isolar arquivos ausentes, modificados ou idênticos.
- [ ] **PangyaAPI.IFF**: Parser e editor estruturado para tabelas de dados do jogo (`Character.iff`, `Item.iff`, etc.), permitindo a customização completa de atributos, itens e mecânicas internas do servidor.
- [x] **PangyaAPI.UpdateList**: Utilitário para geração e assinatura de listas criptografadas em XML para o Launcher/Updater do jogo.

### 🚀 Recursos Avançados
- **Execução Auto-Elevada:** Verificação dinâmica integrada para solicitar privilégios administrativos (UAC) automaticamente, preservando os argumentos de arquivos originais.
- **Associação de Arquivos do Windows:** Opção de registrar a extensão `.pak` no Registro do Windows, permitindo abertura direta por duplo clique ou pelo menu "Abrir com PakMaker".
- **Visualizador de Log do Aplicativo:** Uma interface de log compartilhada mantém a atividade das ferramentas durante a sessão e pode ser aberta pelo menu principal; a auditoria de PAK também é gravada em `activity_log.txt`.
- **Criptografia por Região (XTEA):** Suporte completo ao algoritmo XTEA para criptografia de cabeçalhos utilizando chaves oficiais e customizadas: Global (GB), Tailândia (TH), Japão (JP), Coreia (KR), Indonésia (ID), Europa (EU) e Super SS Dev (Custom).
- **Interação Avançada em Árvore:** Mapeamento completo do teclado com suporte à tecla **Delete** para remoção instantânea de diretórios, além de menus de contexto via botão direito para extração ou exclusão direcionada.

### 🛠️ Trecho Técnico (Exemplo de Compilação PAK)
Código base para compilar uma pasta de modificações usando a especificação do cliente Japonês (V3):

 ```csharp

using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;

var writer = new PakWriter
{
    EntryVersion = PakFileEntryVersion.V3,
    EntryType = PakFileEntryType.LZ772,
    CompressLevel = 5,
    LocationKeys = PakKeys.JP, // xtea_key
    Author = "SuiteTools"

};
// Compila recursivamente mantendo a integridade dos offsets
writer.CreateFromDirectory(@"C:\Modding\data", @"C:\Games\PangYa\ProjectG.pak"); ```

```
