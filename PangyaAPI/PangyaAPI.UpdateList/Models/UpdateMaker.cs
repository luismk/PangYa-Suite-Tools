using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PangyaAPI.UpdateList.Models
{
    public class UpdateMaker
    {
        private readonly Crc32 _crcCalculator = new Crc32();

        /// <summary>
        /// Varre uma pasta de arquivos de atualização e gera o arquivo updatelist finalizado
        /// </summary>
        public void GenerateFromDirectory(string targetFolder, string outputPath, uint[] regionKeys, string patchVersion, string updateVersion = "20090331", string clientPatchNum = "1")
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

            // Monta o Header utilizando os parâmetros recebidos
            var header = new UpdateHeader
            {
                ClientPatchVersion = patchVersion,
                ClientPatchNum = clientPatchNum,
                UpdateVersion = updateVersion
            };

            // Invoca o Writer passando as chaves da região selecionada
            var writer = new UpdateWriter(regionKeys);
            writer.WriteUpdateList(outputPath, header, entries.ToList());
        }
    }
}
