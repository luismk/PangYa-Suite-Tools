using PangyaAPI.Utilities.Cryptography;
using System.Text;
using System.Xml;

namespace PangyaAPI.UpdateList.Models
{
    public class UpdateReader
    {
        private readonly uint[] _cryptoKeys;

        public UpdateReader(uint[] keys)
        {
            _cryptoKeys = keys ?? throw new ArgumentNullException(nameof(keys));
        }

        public UpdateReader()
        {
            _cryptoKeys = Array.Empty<uint>();
        }

        public (UpdateHeader Header, List<UpdateEntry> Entries) ReadUpdateList(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Arquivo de update não encontrado: {filePath}");

            long encryptedLength = new FileInfo(filePath).Length;
            if (encryptedLength == 0 || encryptedLength % 8 != 0)
                throw new InvalidDataException("A UpdateList criptografada está vazia ou truncada.");

            var entries = new List<UpdateEntry>();
            var header = new UpdateHeader();
            var document = XteaDecrypt(filePath);

            if (document == null || document.Length == 0)
            {
                return (header, entries);
            }

            int nullIndex = Array.IndexOf(document, (byte)0);
            if (nullIndex == -1)
            {
                nullIndex = document.Length;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string text = Encoding.GetEncoding("euc-kr").GetString(document, 0, nullIndex);

            int startIdx = text.IndexOf("<patchVer");
            int closingIdx = text.LastIndexOf("</updatefiles>", StringComparison.Ordinal);
            if (startIdx < 0 || closingIdx < startIdx)
                throw new InvalidDataException("A UpdateList descriptografada não contém um XML completo.");

            int endIdx = closingIdx + "</updatefiles>".Length;
            text = text.Substring(startIdx, endIdx - startIdx);

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>" + text + "</root>");

            var patchVerNode = xmlDocument.SelectSingleNode("//patchVer");
            var patchNumNode = xmlDocument.SelectSingleNode("//patchNum");
            var updateListVerNode = xmlDocument.SelectSingleNode("//updatelistVer");

            header.ClientPatchVersion = patchVerNode?.Attributes?["value"]?.Value ?? "";
            header.ClientPatchNum = patchNumNode?.Attributes?["value"]?.Value ?? "";
            header.UpdateVersion = updateListVerNode?.Attributes?["value"]?.Value ?? "";

            var fileInfoNodes = xmlDocument.SelectNodes("//fileinfo");
            if (fileInfoNodes != null)
            {
                foreach (XmlNode node in fileInfoNodes)
                {
                    entries.Add(ParseFileInfo(node));
                }
            }

            return (header, entries);
        }

        /// <summary>
        /// Popula um UpdateEntry a partir de um nó &lt;fileinfo&gt; iterando sobre
        /// UpdateEntryFieldMap.Fields — espelha exatamente o que o UpdateWriter escreve,
        /// eliminando o risco de divergência entre leitura e escrita.
        /// </summary>
        private static UpdateEntry ParseFileInfo(XmlNode node)
        {
            var entry = new UpdateEntry();
            foreach (var field in UpdateEntryFieldMap.Fields)
            {
                string value = node.Attributes?[field.XmlAttributeName]?.Value ?? "";
                field.Set(entry, value);
            }
            return entry;
        }

        public byte[] XteaDecrypt(string filePath = "")
        {
            if (!File.Exists(filePath)) return Array.Empty<byte>();

            using (FileStream r = File.OpenRead(filePath))
            {
                Xtea.DecipherStreamTrimNull(_cryptoKeys, r, out byte[] result);
                return result;
            }
        }
    }
}
