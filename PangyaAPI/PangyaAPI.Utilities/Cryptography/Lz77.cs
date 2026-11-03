using System;
using System.Collections.Generic;

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
                3 => 0xFF,
                4 => 0x5FF,
                _ => 0xFFF,
            };
            const ushort maxMatch = 0xF + 2;

            int size = source.Length;
            byte[] dest = new byte[size + (size / 8) + 1];

            int dIdx = 0, sIdx = 0;
            var hashChain = new Dictionary<int, List<int>>();

            while (sIdx < size)
            {
                if (dIdx >= dest.Length) return null;

                int maskPos = dIdx++;
                dest[maskPos] = 0;

                for (int bits = 0; bits < 8 && sIdx < size; bits++)
                {
                    if (dIdx >= dest.Length) return null;

                    var (matchLen, matchPos) = FindBestMatch(
                        source, sIdx, maxDicWindow, maxMatch, hashChain);

                    if (matchPos < 0)
                    {
                        IndexPosition(hashChain, source, sIdx, size);
                        dest[dIdx++] = source[sIdx++];
                    }
                    else
                    {
                        if ((dIdx + 2) > dest.Length || (sIdx + matchLen) > size) return null;

                        ushort head = (ushort)(((matchLen - 2) << 12) | (sIdx - matchPos));
                        dest[dIdx] = (byte)(head & 0xFF);
                        dest[dIdx + 1] = (byte)(head >> 8);
                        dIdx += 2;

                        for (int position = sIdx; position < sIdx + matchLen; position++)
                            IndexPosition(hashChain, source, position, size);

                        sIdx += matchLen;

                        dest[maskPos] |= (byte)((1 << bits) & 0xFF);
                    }

                    logProgress?.Invoke(sIdx, size);
                }
            }

            Array.Resize(ref dest, dIdx);
            return dest;
        }

        private const int MaxCandidatesToCheck = 32;
        private const int MaxChainLength = 64;

        private static int HashKey(byte[] source, int position) =>
            (source[position] << 16) | (source[position + 1] << 8) | source[position + 2];

        private static void IndexPosition(
            Dictionary<int, List<int>> hashChain, byte[] source, int position, int size)
        {
            if (position + 3 > size) return;

            int key = HashKey(source, position);
            if (!hashChain.TryGetValue(key, out List<int>? positions))
            {
                positions = [];
                hashChain[key] = positions;
            }

            positions.Insert(0, position);
            if (positions.Count > MaxChainLength)
                positions.RemoveAt(positions.Count - 1);
        }

        private static (int matchLen, int matchPos) FindBestMatch(
            byte[] src, int sIdx, ushort maxDicWindow, ushort maxMatch,
            Dictionary<int, List<int>> hashChain)
        {
            if (sIdx <= 2 || (sIdx + 3) > src.Length)
                return (0, -1);

            int dicStart = sIdx - Math.Min(sIdx, maxDicWindow);
            int bestLen = 0;
            int bestPos = -1;

            int key = HashKey(src, sIdx);
            if (!hashChain.TryGetValue(key, out List<int>? candidates))
                return (0, -1);

            int checkedCount = 0;
            foreach (int dicWindow in candidates)
            {
                if (dicWindow < dicStart) break;
                if (++checkedCount > MaxCandidatesToCheck) break;

                int ts = sIdx;
                int dw2 = dicWindow;

                while (dw2 < sIdx && ts < src.Length && (ts - sIdx) < maxMatch && src[dw2] == src[ts])
                { dw2++; ts++; }

                int len = ts - sIdx;

                if (len > bestLen)
                {
                    bestLen = len;
                    bestPos = dw2 - len;
                    if (bestLen == maxMatch) break;
                }
            }

            return bestLen > 2 ? (bestLen, bestPos) : (0, -1);
        }
    }

    public static partial class Lz77
    {
        /// <summary>
        /// Exposto para uso pelo Lz772, que reimplementa seu próprio loop de compressão
        /// (precisa ofuscar máscara/pares) mas reaproveita a busca de match indexada.
        /// </summary>
        internal static (int matchLen, int matchPos) FindBestMatchInternal(
            byte[] src, int sIdx, ushort maxDicWindow, ushort maxMatch,
            Dictionary<int, List<int>> hashChain) =>
            FindBestMatch(src, sIdx, maxDicWindow, maxMatch, hashChain);

        internal static void IndexPositionInternal(
            Dictionary<int, List<int>> hashChain, byte[] source, int position, int size) =>
            IndexPosition(hashChain, source, position, size);
    }

}
