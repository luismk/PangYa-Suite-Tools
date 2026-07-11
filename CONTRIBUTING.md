# Contributing to PangYa-Suite-Tools / Contribuindo para o PangYa-Suite-Tools

First off, thank you for considering contributing to PangYa-Suite-Tools! It's people like you that make this suite an incredible tool for the community.

Antes de mais nada, obrigado por considerares contribuir para o PangYa-Suite-Tools! São pessoas como tu que fazem desta suite uma ferramenta incrível para a comunidade.

---

## English Guide

### How to Report a Bug
If you encounter any crash, unhandled exception, or unexpected behavior while modifying .pak or .iff files:
1. Check the existing Issues to see if the bug has already been reported.
2. If not, open a new Issue and include:
   - A clear and descriptive title.
   - Steps to reproduce the bug.
   - The specific file or region you were handling (and a sample file if possible).
   - Error logs or stack traces from Visual Studio.

### Suggesting Enhancements or New Features
Have a great idea like a new multi-compare tool or a unique viewer? 
1. Open an Issue with the tag 'enhancement'.
2. Explain the core concept, how it benefits server developers or modders, and a rough idea of the layout/workflow.

### Code Contribution Process
1. Fork the repository and create your branch from 'main':
   Command: git checkout -b feature/my-amazing-tool

2. Keep the architecture clean:
   - The project runs on C# (.NET Core / Windows Forms).
   - Never run heavy operations (like extracting or compression loops) directly on the main UI thread. Always wrap them in Task.Run or asynchronous patterns to keep the UI fluid.
   - Ensure all new visual text or strings use the native RESX localization system for every supported culture.

3. Commit Guidelines: Use descriptive messages following the Conventional Commits pattern:
   - feat(diff): add multi-pak synchronization
   - fix(pakmaker): resolve file extraction pointer offset crash

4. Submit a Pull Request (PR) against the 'main' branch, explaining your changes in both English and Portuguese.

---

## Guia em Português

### Como Reportar um Erro (Bug)
Se encontrares algum travamento, exceção não tratada ou comportamento incorreto ao modificar arquivos .pak ou .iff:
1. Verifica as Issues existentes para ver se o erro já não foi reportado.
2. Caso contrário, abre uma nova Issue e inclui:
   - Um título claro e descritivo.
   - O passo a passo para reproduzir o erro.
   - O arquivo ou região específica que estavas a manipular (e um arquivo de exemplo, se possível).
   - Logs de erro ou a stack trace gerada pelo Visual Studio.

### Sugerindo Melhorias ou Novas Funcionalidades
Tens uma ideia fantástica como uma nova ferramenta de comparação ou um visualizador único?
1. Abre uma Issue com a tag 'enhancement'.
2. Explica o conceito principal, como isso ajuda os desenvolvedores de servidores ou modders, e uma ideia geral do layout/fluxo de trabalho.

### Processo de Contribuição de Código
1. Faz um Fork do repositório e cria a tua branch a partir da 'main':
   Comando: git checkout -b feature/minha-ferramenta-incrivel

2. Mantém a arquitetura limpa:
   - O projeto é executado em C# (.NET Core / Windows Forms).
   - Nunca executes operações pesadas (como loops de extração ou compactação) diretamente na Thread principal da interface (UI). Envolve-as sempre em Task.Run ou padrões assíncronas para manter a janela fluida.
   - Garanta que todos os novos textos ou strings da interface usem o sistema de localização RESX nativo em todos os idiomas suportados.

3. Padrão de Commits: Utiliza mensagens descritivas seguindo o padrão Conventional Commits:
   - feat(diff): add multi-pak synchronization
   - fix(pakmaker): resolve file extraction pointer offset crash

4. Envia um Pull Request (PR) para a branch 'main', explicando as tuas alterações tanto em Inglês como em Português.
