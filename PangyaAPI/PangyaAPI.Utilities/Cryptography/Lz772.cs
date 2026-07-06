using System;
namespace PangyaAPI.Utilities.Cryptography
{

    // ─────────────────────────────────────────────────────────────────────────
    // LZ772 Descompressão / Compressão (com ofuscação)
    // ─────────────────────────────────────────────────────────────────────────

    public static class Lz772
    {
        private static readonly ushort[] ObfuscationKeys =
            { 0xFF21, 0x834F, 0x675F, 0x34, 0xF237, 0x815F, 0x4765, 0x233 };

        public static byte[]? Decompress(byte[] source, uint uncompressedSize, uint compressSize,
                                          Action<int, int>? logProgress = null)
        {
            if (source == null || uncompressedSize == 0 || compressSize == 0)
                return null;

            byte[] dest = new byte[uncompressedSize];
            uint effectiveCompressSize = Math.Min(compressSize, (uint)source.Length);
            uint sIdx = 0, dIdx = 0;
            try
            {


                while (sIdx < effectiveCompressSize && dIdx < uncompressedSize)
                {
                    try
                    {
                        byte origMask = source[sIdx++];
                        byte tmpMask = (byte)(origMask ^ 0xC8);

                        for (int bits = 0; bits < 8 && dIdx < uncompressedSize && sIdx < effectiveCompressSize; bits++)
                        {
                            if ((tmpMask & 1) != 0)
                            {
                                if ((sIdx + 2) > effectiveCompressSize)
                                    return null;

                                ushort head = (ushort)(source[sIdx] | (source[sIdx + 1] << 8));
                                sIdx += 2;

                                head ^= ObfuscationKeys[(origMask >> 3) & 7];

                                uint offsetCopy = (uint)(head & 0xFFF);
                                uint sizeCopy = (uint)(2 + (head >> 12));

                                if (offsetCopy > dIdx || (dIdx + sizeCopy) > uncompressedSize) 
                                    return null;

                                uint src2 = dIdx - offsetCopy;
                                for (uint k = 0; k < sizeCopy; k++)
                                    dest[dIdx++] = dest[src2 + k];
                            }
                            else
                            {
                                dest[dIdx++] = source[sIdx++];
                            }

                            tmpMask >>= 1;
                            logProgress?.Invoke((int)sIdx, (int)effectiveCompressSize);
                        }
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }
            }
            catch (Exception)
            {

                throw;
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
            int[] maskPtrPositions = new int[8]; // posições das ushorts de cada bit

            while (sIdx < size)
            {
                if (dIdx >= dest.Length) return null;

                int maskPos = dIdx++;
                dest[maskPos] = 0;
                Array.Fill(maskPtrPositions, -1);

                for (int bits = 0; bits < 8 && sIdx < size; bits++)
                {
                    if (dIdx >= dest.Length) return null;

                    var (matchLen, matchPos) = Lz77.FindBestMatchInternal(source, sIdx, maxDicWindow, maxMatch);

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

                        maskPtrPositions[bits] = dIdx;

                        dIdx += 2;

                        sIdx += matchLen;

                        dest[maskPos] |= (byte)((1 << bits) & 0xFF);
                    }
                }

                // Ofusca o mask
                dest[maskPos] ^= 0xC8;

                // Ofusca os pares com a chave derivada do mask ofuscado
                for (int i = 0; i < 8; i++)
                {
                    if (maskPtrPositions[i] >= 0)
                    {
                        int pos = maskPtrPositions[i];
                        ushort pair = (ushort)(dest[pos] | (dest[pos + 1] << 8));
                        pair ^= ObfuscationKeys[(dest[maskPos] >> 3) & 7];
                        dest[pos] = (byte)(pair & 0xFF);
                        dest[pos + 1] = (byte)(pair >> 8);
                    }
                }

                logProgress?.Invoke(sIdx, size);
            }

            Array.Resize(ref dest, dIdx);
            return dest;
        }

    }

}
