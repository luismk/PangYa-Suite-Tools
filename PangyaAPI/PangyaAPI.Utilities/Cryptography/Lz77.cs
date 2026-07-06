using System;

namespace PangyaAPI.Utilities.Cryptography
{ 
    // ─────────────────────────────────────────────────────────────────────────
    // LZ77 Descompressão
    // ─────────────────────────────────────────────────────────────────────────

    public static partial class Lz77
    {
        public static byte[]? Decompress(byte[] source, uint uncompressedSize, uint compressSize,
                                          Action<int, int>? logProgress = null)
        {
            if (source == null || uncompressedSize == 0 || compressSize == 0)
                return null;

            byte[] dest = new byte[uncompressedSize];

            uint effectiveCompressSize = Math.Min(compressSize, (uint)source.Length);
            uint sIdx = 0, dIdx = 0;

            while (sIdx < effectiveCompressSize && dIdx < uncompressedSize)
            {
                byte mask = source[sIdx++];

                for (int bits = 0; bits < 8 && dIdx < uncompressedSize && sIdx < effectiveCompressSize; bits++)
                {
                    if ((mask & 1) != 0)
                    {
                        if ((sIdx + 2) > effectiveCompressSize) return null;

                        ushort head = (ushort)(source[sIdx] | (source[sIdx + 1] << 8));
                        sIdx += 2;

                        uint offsetCopy = (uint)(head & 0xFFF);
                        uint sizeCopy = (uint)(2 + (head >> 12));

                        if (offsetCopy > dIdx || (dIdx + sizeCopy) > uncompressedSize) return null; 

                        uint src2 = dIdx - offsetCopy;
                        for (uint k = 0; k < sizeCopy; k++)
                            dest[dIdx++] = dest[src2 + k];
                    }
                    else
                    {
                        dest[dIdx++] = source[sIdx++];
                    }

                    mask >>= 1;
                    logProgress?.Invoke((int)sIdx, (int)effectiveCompressSize);
                }
            }

            return dest;
        }

        public static byte[]? Compress(byte[] source, byte level = 5,
                                        Action<int, int>? logProgress = null)
        {
            if (source == null || source.Length == 0) return null;

            ushort maxDicWindow = level switch
            {
                0 => 0x5,
                1 => 0xF,
                2 => 0x5F,
                // The reference implementation falls through from case 3 to case 4.
                3 => 0x5FF,
                4 => 0x5FF,
                _ => 0xFFF,
            };
            const ushort maxMatch = 0xF + 2;

            int size = source.Length;
            byte[] dest = new byte[size + (size / 8) + 1];

            int dIdx = 0, sIdx = 0;

            while (sIdx < size)
            {
                if (dIdx >= dest.Length) return null;

                int maskPos = dIdx++;
                dest[maskPos] = 0;

                for (int bits = 0; bits < 8 && sIdx < size; bits++)
                {
                    if (dIdx >= dest.Length) return null;

                    var (matchLen, matchPos) = FindBestMatch(source, sIdx, maxDicWindow, maxMatch);

                    if (matchPos < 0)
                    {
                        dest[dIdx++] = source[sIdx++];
                    }
                    else
                    {
                        if ((dIdx + 2) > dest.Length || (sIdx + matchLen) > size) return null;

                        ushort head = (ushort)(((matchLen - 2) << 12) | (sIdx - matchPos));
                        dest[dIdx] = (byte)(head & 0xFF);
                        dest[dIdx + 1] = (byte)(head >> 8);
                        dIdx += 2;

                        sIdx += matchLen;

                        dest[maskPos] |= (byte)((1 << bits) & 0xFF);
                    }

                    logProgress?.Invoke(sIdx, size);
                }
            }

            Array.Resize(ref dest, dIdx);
            return dest;
        }

        private static (int matchLen, int matchPos) FindBestMatch(byte[] src, int sIdx,
                                                                   ushort maxDicWindow, ushort maxMatch)
        {
            if (sIdx <= 2 || (sIdx + 3) > src.Length)
                return (0, -1);

            int dicWindow = sIdx - Math.Min(sIdx, maxDicWindow);
            int bestLen = 0;
            int bestPos = -1;

            while (dicWindow < sIdx - 3)
            {
                int ts = sIdx;
                int dw2 = dicWindow;

                while (dw2 < sIdx && ts < src.Length && (ts - sIdx) < maxMatch && src[dw2] == src[ts])
                { dw2++; ts++; }

                int len = ts - sIdx;

                if (len > 2 && bestLen <= len)
                {
                    bestLen = len;
                    bestPos = dw2 - len;
                    if (bestLen == maxMatch || ts == src.Length || dw2 == sIdx)
                        break;
                }

                dicWindow = (dw2 - len) + 1;
            }

            return (bestLen, bestPos);
        }
    }

    public static partial class Lz77
    {
        /// <summary>
        /// Exposto para uso pelo Lz772, que reimplementa seu próprio loop de compressão
        /// (precisa ofuscar máscara/pares) mas reaproveita a busca de match de referência.
        /// </summary>
        internal static (int matchLen, int matchPos) FindBestMatchInternal(byte[] src, int sIdx,
                                                                            ushort maxDicWindow, ushort maxMatch)
            => FindBestMatch(src, sIdx, maxDicWindow, maxMatch);
    }

}
