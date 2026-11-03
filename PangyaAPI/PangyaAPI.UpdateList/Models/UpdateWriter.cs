using PangyaAPI.Utilities.Cryptography;
using PangyaAPI.UpdateList.Localization;
using System.Text;

namespace PangyaAPI.UpdateList.Models
{
    public class UpdateWriter
    {
        private readonly uint[] _cryptoKeys;

        public UpdateWriter(uint[] keys)
        {
            _cryptoKeys = keys ?? throw new ArgumentNullException(nameof(keys));
        }

        public void WriteUpdateList(string outputPath, UpdateHeader header, List<UpdateEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                Console.WriteLine(UpdateListStrings.UpdateWriterNoChangesToSave);
                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"euc-kr\" standalone=\"yes\" ?>")
               .Append("<patchVer value=\"").Append(XmlEscape(header.ClientPatchVersion)).AppendLine("\" />")
               .Append("<patchNum value=\"").Append(XmlEscape(header.ClientPatchNum)).AppendLine("\" />")
               .Append("<updatelistVer value=\"").Append(XmlEscape(header.UpdateVersion)).AppendLine("\" />")
               .Append("<updatefiles count=\"").Append(entries.Count).AppendLine("\">");
            foreach (UpdateEntry entry in entries)
                xml.Append('\t').AppendLine(BuildFileInfoElement(entry));
            xml.Append("</updatefiles>");

            byte[] rawXmlBytes = Encoding.GetEncoding("euc-kr").GetBytes(xml.ToString());
            byte[] encryptedData = XteaEncrypt(rawXmlBytes);
            using (FileStream output = new(
                       outputPath,
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None))
            {
                output.Write(encryptedData);
            }

            Console.WriteLine(UpdateListStrings.Format(
                UpdateListStrings.UpdateWriterGeneratedSuccessfully,
                outputPath));
        }

        /// <summary>
        /// Builds the &lt;fileinfo /&gt; element by iterating over
        /// UpdateEntryFieldMap.Fields, exactly mirroring the original FileItem.ToString()
        /// implementation (XMLParser.cs).
        /// </summary>
        private static string BuildFileInfoElement(UpdateEntry entry)
        {
            var sb = new StringBuilder("<fileinfo");
            foreach (var field in UpdateEntryFieldMap.Fields)
            {
                sb.Append(' ')
                  .Append(field.XmlAttributeName)
                  .Append("=\"")
                  .Append(XmlEscape(field.Get(entry)))
                  .Append('"');
            }
            sb.Append(" />");
            return sb.ToString();
        }

        private static string XmlEscape(string? value) =>
            (value ?? "")
                .Replace("&",  "&amp;")
                .Replace("\"", "&quot;")
                .Replace("'",  "&apos;")
                .Replace("<",  "&lt;")
                .Replace(">",  "&gt;");

        public byte[] XteaEncrypt(byte[] rawData)
        {
            using var input = new MemoryStream(rawData, writable: false);
            Xtea.EncipherStreamPadNull(_cryptoKeys, input, out byte[] result);
            return result;
        }
    }
}
