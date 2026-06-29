//criado por LUISMK -> github.com/luismk
using PangyaAPI.PAK.Flags;
using System.Linq;

namespace PangyaAPI.PAK.Models
{
    /// <summary>
    /// Configuração usada para reconstruir um PAK (mesmas opções do PakWriter).
    /// </summary>
    public readonly record struct PakRebuildOptions(
        PakFileEntryVersion EntryVersion,
        PakFileEntryType EntryType,
        byte CompressLevel,
        uint[] LocationKeys,
        string Author);
    
        /// <summary>
        /// Par de arquivo de origem + pasta relativa explícita dentro do PAK (pode ser null,
        /// caso em que o destino é resolvido automaticamente via FindExistingRelativeFolder).
        /// </summary>
        public readonly record struct PakInjectItem(string SourcePath, string? RelativeFolder);


    /// <summary>
    /// Operações de alto nível sobre um PAK existente: injetar/atualizar arquivos
    /// e remover arquivos, sempre preservando a estrutura de pastas original.
    /// Estratégia: extrai o conteúdo atual para uma pasta temporária, aplica a
    /// mutação desejada e reconstrói com o PakWriter — com backup automático
    /// do .pak original em caso de falha.
    /// </summary>
    public static class PakManager
    {
        /// <summary>
        /// Extrai todas as entradas (exceto as filtradas por <paramref name="skip"/>)
        /// preservando a estrutura de pastas original do PAK.
        /// </summary>
        private static void ExtractAllPreservingStructure(PakReader reader, string tempDir,
                                                            Func<PakFileEntry, bool>? skip = null,
                                                            Action<int, int>? onProgress = null)
        {
            var files = reader.Entries.Where(e => e.Type != PakFileEntryType.Directory).ToList();
            int total = files.Count;
            int done = 0;

            foreach (var entry in files)
            {
                done++;

                if (skip == null || !skip(entry))
                {
                    string relativePath = entry.Name.Replace('/', '\\');
                    string destPath = Path.Combine(tempDir, relativePath);
                    reader.ExtractEntry(entry, destPath);
                }

                onProgress?.Invoke(done, total);
            }
        }

        /// <summary>
        /// Procura, dentro das entries atuais do PAK, em qual pasta interna já existe
        /// um arquivo com o mesmo nome (case-insensitive). Usado para saber onde colocar
        /// um arquivo "atualizado" na pasta temporária antes de reconstruir o PAK.
        /// Retorna string.Empty se não encontrar (o arquivo é tratado como novo, na raiz).
        /// </summary>
        public static string FindExistingRelativeFolder(PakReader reader, string fileName)
        {
            var match = reader.Entries.FirstOrDefault(e =>
                e.Type != PakFileEntryType.Directory &&
                string.Equals(Path.GetFileName(e.Name.Replace('/', '\\')), fileName, StringComparison.OrdinalIgnoreCase));

            if (match == null) return "";

            string normalized = match.Name.Replace('/', '\\');
            return Path.GetDirectoryName(normalized) ?? "";
        }

        /// <summary>
        /// Sobrecarga que permite informar explicitamente em qual pasta interna cada arquivo
        /// deve cair (útil ao arrastar uma pasta inteira, preservando sua estrutura). Quando
        /// RelativeFolder é null, cai no comportamento antigo: procura pasta existente pelo
        /// nome do arquivo, ou usa defaultRelativeFolder.
        /// </summary>
        public static void InjectFiles(string pakPath, PakReader reader, IEnumerable<PakInjectItem> items,
                                        PakRebuildOptions options, string defaultRelativeFolder = "",
                                        Action<string>? log = null, Action<int, int>? onProgress = null)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "PakTemp_" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                log?.Invoke("Extraindo conteúdo atual do PAK...");
                ExtractAllPreservingStructure(reader, tempDir, onProgress: onProgress);

                foreach (var item in items)
                {
                    string fileName = Path.GetFileName(item.SourcePath);

                    string relFolder;
                    if (item.RelativeFolder != null)
                    {
                        // Pasta explícita (ex: vinda de uma pasta arrastada) — respeita sempre,
                        // mesmo que já exista um arquivo de mesmo nome em outro lugar do PAK.
                        relFolder = item.RelativeFolder;
                    }
                    else
                    {
                        relFolder = FindExistingRelativeFolder(reader, fileName);
                        if (string.IsNullOrEmpty(relFolder))
                            relFolder = defaultRelativeFolder;
                    }

                    string destDir = string.IsNullOrEmpty(relFolder) ? tempDir : Path.Combine(tempDir, relFolder);
                    Directory.CreateDirectory(destDir);

                    string destPath = Path.Combine(destDir, fileName);
                    File.Copy(item.SourcePath, destPath, true);

                    log?.Invoke(string.IsNullOrEmpty(relFolder)
                        ? $"Novo arquivo adicionado na raiz: {fileName}"
                        : $"Atualizado/adicionado em \"{relFolder}\": {fileName}");
                }

                reader.Dispose();
                RebuildFromTemp(pakPath, tempDir, options, log);
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }

        public static void InjectFiles(string pakPath, PakReader reader, IEnumerable<string> sourceFiles,
                                PakRebuildOptions options, string defaultRelativeFolder = "",
                                Action<string>? log = null, Action<int, int>? onProgress = null)
        {
            var items = sourceFiles.Select(f => new PakInjectItem(f, null));
            InjectFiles(pakPath, reader, items, options, defaultRelativeFolder, log, onProgress);
        }

        /// <summary>
        /// Reconstrói o PAK usando uma chave/região diferente, mantendo todo o conteúdo
        /// (arquivos e estrutura de pastas) idêntico. Útil para "migrar" um PAK entre
        /// regiões/versões do cliente que usam chaves XTEA diferentes.
        /// </summary>
        public static void ChangeEncryptionKey(string pakPath, PakReader reader, PakRebuildOptions newOptions,
                                                Action<string>? log = null, Action<int, int>? onProgress = null)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "PakTemp_" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                log?.Invoke("Extraindo conteúdo atual do PAK (chave original)...");
                ExtractAllPreservingStructure(reader, tempDir, onProgress: onProgress);

                log?.Invoke("Reconstruindo PAK com a nova chave...");

                // Fecha o handle do .pak original antes do File.Move dentro de RebuildFromTemp.
                reader.Dispose();

                RebuildFromTemp(pakPath, tempDir, newOptions, log);
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }

        /// <summary>
        /// Remove uma ou mais entradas (pelo nome interno completo, ex.:
        /// "data/round20_abbot/ase/ab_abbot01.pet") e reconstrói o PAK sem elas.
        /// </summary>
        public static void RemoveFiles(string pakPath, PakReader reader, IEnumerable<string> namesToRemove,
                                        PakRebuildOptions options, Action<string>? log = null,
                                        Action<int, int>? onProgress = null)
        {
            var removeSet = new HashSet<string>(
                namesToRemove.Select(n => n.Replace('/', '\\')),
                StringComparer.OrdinalIgnoreCase);

            string tempDir = Path.Combine(Path.GetTempPath(), "PakTemp_" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                log?.Invoke("Extraindo conteúdo atual do PAK (ignorando arquivo(s) removido(s))...");
                ExtractAllPreservingStructure(reader, tempDir,
                    skip: e => removeSet.Contains(e.Name.Replace('/', '\\')),
                    onProgress: onProgress);

                foreach (var name in removeSet)
                    log?.Invoke($"Removido: {name}");

                // Fecha o handle do .pak original antes do File.Move dentro de RebuildFromTemp.
                reader.Dispose();

                RebuildFromTemp(pakPath, tempDir, options, log);
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }

        private static void RebuildFromTemp(string pakPath, string tempDir, PakRebuildOptions options, Action<string>? log)
        {
            string backupPak = pakPath + ".bak";
            if (File.Exists(backupPak)) File.Delete(backupPak);
            File.Move(pakPath, backupPak);

            try
            {
                var writer = new PakWriter
                {
                    EntryVersion = options.EntryVersion,
                    EntryType = options.EntryType,
                    CompressLevel = options.CompressLevel,
                    LocationKeys = options.LocationKeys,
                    Author = options.Author,
                };

                writer.CreateFromDirectoryContents(tempDir, pakPath, log);
            }
            catch
            {
                // Falhou ao reconstruir: restaura o backup para não perder o PAK original.
                if (File.Exists(pakPath)) File.Delete(pakPath);
                File.Move(backupPak, pakPath);
                throw;
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch { /* limpeza best-effort, não deve interromper o fluxo principal */ }
        }
    }
}
