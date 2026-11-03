using PangyaAPI.Utilities.Cryptography;
using PangyaAPI.UpdateList.Localization;
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
                throw new FileNotFoundException(UpdateListStrings.Format(
                    UpdateListStrings.UpdateReaderFileNotFound,
                    filePath));

            long encryptedLength = new FileInfo(filePath).Length;
            if (encryptedLength == 0 || encryptedLength % 8 != 0)
                throw new InvalidDataException(
                    UpdateListStrings.UpdateReaderEncryptedListEmptyOrTruncated);

            var entries = new List<UpdateEntry>();
            var header = new UpdateHeader();
            var document = XteaDecrypt(filePath);

            if (document == null || document.Length == 0)
                return (header, entries);

            // Remove the zero padding inserted by XTEA.
            int nullIndex = Array.IndexOf(document, (byte)0);
            if (nullIndex == -1) nullIndex = document.Length;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string text = Encoding.GetEncoding("euc-kr").GetString(document, 0, nullIndex);

            // Isolate the valid XML segment.
            int startIdx = text.IndexOf("<patchVer");
            int closingIdx = text.LastIndexOf("</updatefiles>", StringComparison.Ordinal);
            if (startIdx < 0 || closingIdx < startIdx)
                throw new InvalidDataException(
                    UpdateListStrings.UpdateReaderDecryptedListMissingCompleteXml);

            int endIdx = closingIdx + "</updatefiles>".Length;
            text = text.Substring(startIdx, endIdx - startIdx);

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>" + text + "</root>");

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<root>" + text + "</root>");

            header.ClientPatchVersion = xmlDoc.SelectSingleNode("//patchVer")?     .Attributes?["value"]?.Value ?? "";
            header.ClientPatchNum     = xmlDoc.SelectSingleNode("//patchNum")?     .Attributes?["value"]?.Value ?? "";
            header.UpdateVersion      = xmlDoc.SelectSingleNode("//updatelistVer")?.Attributes?["value"]?.Value ?? "";

            var fileInfoNodes = xmlDoc.SelectNodes("//fileinfo");
            if (fileInfoNodes != null)
            {
                int index = 0;
                foreach (XmlNode node in fileInfoNodes)
                {
                    var entry = ParseFileInfo(node);
                    entry.Index = ++index;
                    entries.Add(entry);
                }
            }

            return (header, entries);
        }

        /// <summary>
        /// Populates an UpdateEntry from a &lt;fileinfo&gt; node by iterating over
        /// UpdateEntryFieldMap.Fields, exactly mirroring what UpdateWriter writes.
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

            using FileStream fs = new(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            Xtea.DecipherStreamTrimNull(_cryptoKeys, fs, out byte[] result);
            return result;
        }
    }
}
