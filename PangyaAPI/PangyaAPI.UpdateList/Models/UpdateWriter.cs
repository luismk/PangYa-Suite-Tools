using PangyaAPI.Utilities.Cryptography;
using System.Text;
using System.Xml;

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
                Console.WriteLine("Nenhuma alteração para salvar.");
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
            File.WriteAllBytes(outputPath, encryptedData);

            Console.WriteLine($"UpdateList gerada com sucesso em: {outputPath}");
        }

        /// <summary>
        /// Monta o elemento &lt;fileinfo .../&gt; iterando sobre UpdateEntryFieldMap.Fields,
        /// em vez de uma interpolação manual com os 8 nomes de atributo hardcoded — assim
        /// qualquer campo adicionado ao mapa aparece aqui automaticamente.
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

        /// <summary>Escapa caracteres especiais de XML em valores de atributo (nomes de arquivo podem conter & " etc.).</summary>
        private static string XmlEscape(string? value) => SecurityElementEscape(value ?? "");

        private static string SecurityElementEscape(string value) =>
            value.Replace("&", "&amp;")
                 .Replace("\"", "&quot;")
                 .Replace("'", "&apos;")
                 .Replace("<", "&lt;")
                 .Replace(">", "&gt;");

        public byte[] XteaEncrypt(byte[] rawData)
        {
            Xtea.EncipherStreamPadNull(_cryptoKeys, new MemoryStream(rawData), out byte[] _result);
            return _result;
        }
    }
}
