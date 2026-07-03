//criado por LUISMK -> github.com/luismk
using PangyaAPI.PAK.Flags; 
using System.Text;

namespace PangyaAPI.PAK.Models
{
    public class PakFileEntry
    {
        private byte[] _nameRaw = Array.Empty<byte>();
        private Encoding _fileNameEncoding = PakFileNameEncoding.CreateDefault();

        public byte NameLength { get; set; }
        public PakFileEntryType Type { get; set; }
        public PakFileEntryVersion Version { get; set; }

        public uint Offset { get; set; }
        public uint CompressSize { get; set; }
        public uint Size { get; set; }

        public byte[] NameRaw
        {
            get => _nameRaw;
            set => _nameRaw = SanitizeNameRaw(value);
        }

        /// <summary>
        /// Define o NameRaw SEM sanitização (sem remover o padding de zeros).
        /// Uso exclusivo do LzPakWriter: o array precisa permanecer com o tamanho
        /// alinhado (NameLength) para a criptografia XTEA em blocos de 8 bytes
        /// funcionar corretamente. NUNCA usar para dados vindos da leitura de um
        /// PAK existente (use o setter normal de NameRaw nesse caso).
        /// </summary>
        internal void SetRawNameForWrite(byte[] value) => _nameRaw = value;

        internal Encoding FileNameEncoding
        {
            get => _fileNameEncoding;
            set => _fileNameEncoding = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Name => FileNameEncoding.GetString(_nameRaw).Replace('/', '\\').Trim();

        internal static byte[] EncodeName(string name, Encoding encoding) => encoding.GetBytes(name);

        /// <summary>
        /// Varre o array de bytes recebido do arquivo PAK e remove os terminadores nulos (\0) 
        /// e o lixo de alinhamento XTEA/V3 posterior.
        /// </summary>
        private static byte[] SanitizeNameRaw(byte[] raw)
        {
            if (raw == null || raw.Length == 0)
                return Array.Empty<byte>();

            // Encontra o índice do primeiro byte nulo (0x00)
            int validLength = Array.IndexOf(raw, (byte)0);

            // Se não encontrou nulo, o array original está 100% limpo
            if (validLength < 0)
                return raw;

            // Se o nulo for o primeiro caractere, retorna vazio
            if (validLength == 0)
                return Array.Empty<byte>();

            // Copia apenas os bytes que antecedem o caractere nulo
            byte[] cleanBytes = new byte[validLength];
            Array.Copy(raw, cleanBytes, validLength);
            return cleanBytes;
        }

        /// <summary>Calcula o tamanho total do entry no arquivo.</summary>
        public static int CalcSize(byte nameLength, PakFileEntryVersion version)
        {
            bool hasNullTerm = version < PakFileEntryVersion.V3 || version == PakFileEntryVersion.Raw;
            return 2 + 4 + 4 + 4 + nameLength + (hasNullTerm ? 1 : 0);
        }
    }

    internal static class PakFileNameEncoding
    {
        internal const int DefaultCodePage = 51949;

        static PakFileNameEncoding() =>
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        internal static Encoding CreateDefault() => Encoding.GetEncoding(DefaultCodePage);
    }
}
