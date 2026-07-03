using System.Security.Cryptography;
using System.Text;

namespace PangyaAPI.UpdateList.Models
{
    /// <summary>
    /// Varre um diretório de arquivos de atualização e gera o arquivo updatelist
    /// finalizado (criptografado em XTEA).
    ///
    /// Baseado no XMLParser.cs original (Dashboard.RecursiveFileProcessor):
    /// - fdate/ftime usam LastWriteTime + 3 horas (padrão legado do Pangya)
    /// - fdir é apenas o nome imediato da pasta pai + "\"
    /// - pname = fname + ".zip"
    /// - psize = tamanho real do zip (preenchido após compressão)
    /// - CheckSum = MD5(nome + tamanho + data+3h) para detecção de mudanças
    /// - Extensões ignoradas: .bak .txt .lib .exp .pdb .xml .dmp .cln .json
    ///   e arquivos "uninstall.exe"
    /// </summary>
    public class UpdateMaker
    {
        private readonly Crc32 _crcCalculator = new Crc32();

        // Extensões/nomes de arquivo a ignorar na varredura (igual ao XMLParser original)
        private static readonly string[] IgnoredSuffixes =
        {
            ".bak", ".txt", ".lib", ".exp", ".pdb", ".xml",
            ".dmp", ".cln", ".json", "uninstall.exe"
        };

        /// <summary>
        /// Varre <paramref name="targetFolder"/> recursivamente, monta as entries e
        /// gera o arquivo updatelist final em <paramref name="outputPath"/>.
        /// </summary>
        public void GenerateFromDirectory(
            string targetFolder,
            string outputPath,
            uint[] regionKeys,
            string patchVersion,
            string updateVersion = "20090331",
            string clientPatchNum = "1",
            Action<int, int>? onProgress = null)
        {
            if (!Directory.Exists(targetFolder))
                throw new DirectoryNotFoundException($"Diretório alvo não existe: {targetFolder}");

            string[] files = Directory.EnumerateFiles(targetFolder, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".cln", StringComparison.OrdinalIgnoreCase) &&
                               !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                               !Path.GetFileName(path).Equals(Path.GetFileName(outputPath), StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var entries = new UpdateEntry[files.Length];

            Parallel.For(0, files.Length, new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 4))
            }, index =>
            {
                string file = files[index];
                var fileInfo = new FileInfo(file);
                string relativePath = Path.GetRelativePath(targetFolder, file);
                string? directory = Path.GetDirectoryName(relativePath);
                entries[index] = new UpdateEntry
                {
                    fname = fileInfo.Name,
                    fdir = string.IsNullOrEmpty(directory) ? "\\" : "\\" + directory,
                    fsize = fileInfo.Length,
                    fcrc = _crcCalculator.CalculateFileCRC(file),
                    fdate = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd"), // Uso do Utc como no legado
                    ftime = fileInfo.LastWriteTimeUtc.ToString("HH:mm:ss"),
                    pname = fileInfo.Name + ".zip", // Mantém o comportamento original (.zip no pname)
                    psize = 717469 // Tamanho fake inicial mantido para sua futura implementação de GUI/Compressão
                };

            });

            var header = new UpdateHeader
            {
                ClientPatchVersion = patchVersion,
                ClientPatchNum     = clientPatchNum,
                UpdateVersion      = updateVersion
            };

            var writer = new UpdateWriter(regionKeys);
            writer.WriteUpdateList(outputPath, header, entries.ToList());
        }
    }
}
