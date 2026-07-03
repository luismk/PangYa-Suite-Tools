using System;
using System.Collections.Generic;
using System.Text;
namespace PangyaAPI.Utilities.Cryptography
{  
    public static class Xtea
    { 
        private const uint DELTA = 0xE3779B90u;
        private const uint SUM_CONST = 0x61C88647u;

        public static ulong Decrypt(uint[] keys, ulong src)
        {
            uint v0 = (uint)(src & 0xFFFFFFFFu);
            uint v1 = (uint)(src >> 32); 
            uint d = DELTA;
            for (uint i = 0; i < 16; i++)
            {
                v1 = unchecked(v1 - ((((v0 << 4) ^ (v0 >> 5)) + v0) ^ (d + keys[(d >> 11) & 3])));
                d = unchecked(d + SUM_CONST);
                v0 = unchecked(v0 - ((((v1 << 4) ^ (v1 >> 5)) + v1) ^ (d + keys[d & 3])));
            }
            return ((ulong)v1 << 32) | v0;
        }

        public static ulong Encrypt(uint[] keys, ulong src)
        {
            uint v0 = (uint)(src & 0xFFFFFFFFu);
            uint v1 = (uint)(src >> 32);

            uint d = 0u;
            for (uint i = 0; i < 16; i++)
            {
                v0 = unchecked(v0 + ((((v1 >> 5) ^ (v1 << 4)) + v1) ^ (d + keys[d & 3])));
                d = unchecked(d - SUM_CONST);
                v1 = unchecked(v1 + ((((v0 >> 5) ^ (v0 << 4)) + v0) ^ (d + keys[(d >> 11) & 3])));
            }
            return ((ulong)v1 << 32) | v0;
        }

        #region OLD AND CODEHARD
        private static void EncryptXTEA(uint[] keys, ref uint dst0, ref uint dst1, ref uint src0, ref uint src1)
        {
            uint num = 4u;
            uint num2 = 0u;
            dst0 = src0;
            dst1 = src1;
            uint num3 = dst0;
            while (num != 0)
            {
                uint num4 = ((dst1 >> 5) ^ (dst1 << 4)) + dst1;
                uint num5 = keys[num2 & 3] + num2;
                uint num6 = num3 + (num4 ^ num5);
                num2 -= 1640531527;
                for (int i = 0; i < 3; i++)
                {
                    num4 = ((num6 >> 5) ^ (num6 << 4)) + num6;
                    num5 = keys[(num2 >> 11) & 3] + num2;
                    uint num7 = (dst1 += num4 ^ num5);
                    num4 = ((num7 >> 5) ^ (num7 << 4)) + num7;
                    num5 = keys[num2 & 3] + num2;
                    num6 += num4 ^ num5;
                    num2 -= 1640531527;
                }
                num3 = (dst0 = num6);
                num4 = ((num6 >> 5) ^ (num6 << 4)) + num6;
                num5 = keys[(num2 >> 11) & 3] + num2;
                num6 = dst1 + (num4 ^ num5);
                dst1 = num6;
                num--;
            }
        }

        private static void DecryptXTEA(uint[] keys, ref uint dst0, ref uint dst1, ref uint src0, ref uint src1)
        {
            uint num = 4u;
            uint num2 = 3816266640u;
            dst0 = src0;
            dst1 = src1;
            uint num3 = dst1;
            while (num != 0)
            {
                uint num4 = ((dst0 >> 5) ^ (dst0 << 4)) + dst0;
                uint num5 = keys[(num2 >> 11) & 3] + num2;
                uint num6 = num3 - (num4 ^ num5);
                num2 += 1640531527;
                for (int i = 0; i < 3; i++)
                {
                    num4 = ((num6 >> 5) ^ (num6 << 4)) + num6;
                    num5 = keys[num2 & 3] + num2;
                    uint num7 = (dst0 -= num4 ^ num5);
                    num4 = ((num7 >> 5) ^ (num7 << 4)) + num7;
                    num5 = keys[(num2 >> 11) & 3] + num2;
                    num6 -= num4 ^ num5;
                    num2 += 1640531527;
                }
                num3 = (dst1 = num6);
                num4 = ((num6 >> 5) ^ (num6 << 4)) + num6;
                num5 = keys[num2 & 3] + num2;
                num6 = dst0 - (num4 ^ num5);
                dst0 = num6;
                num--;
            }
        }

        public static void EncryptUpdatelist(uint[] Keys, ref byte[] dst, byte[] src, uint tamanho)
        {
            uint num = 0u;
            uint num2 = (uint)(tamanho / 4 + ((tamanho % 4 != 0) ? 1 : 0));
            uint[] array = new uint[num2];
            uint[] array2 = new uint[num2];
            Array.Clear(array2, 0, (int)num2);
            Array.Clear(array, 0, (int)num2);
            Buffer.BlockCopy(src, 0, array2, 0, (int)tamanho);
            for (uint num3 = tamanho / 8; num3 != 0; num3--)
            {
                EncryptXTEA(Keys, ref array[num], ref array[num + 1], ref array2[num], ref array2[num + 1]);
                num += 2;
            }
            Buffer.BlockCopy(array, 0, dst, 0, (int)tamanho);
        }

        public static void DecryptUpdatelist(uint[] Keys, ref byte[] dst, byte[] src, uint tamanho)
        {
            uint num = 0u;
            uint num2 = (uint)(tamanho / 4 + ((tamanho % 4 != 0) ? 1 : 0));
            uint[] array = new uint[num2];
            uint[] array2 = new uint[num2];
            Buffer.BlockCopy(src, 0, array2, 0, (int)tamanho);
            Array.Clear(array, 0, (int)num2);
            for (uint num3 = tamanho / 8; num3 != 0; num3--)
            {
                DecryptXTEA(Keys, ref array[num], ref array[num + 1], ref array2[num], ref array2[num + 1]);
                num += 2;
            }
            Buffer.BlockCopy(array, 0, dst, 0, (int)tamanho);
        }

        private static void EncipherStream(uint[] key, Stream r, Stream w, out byte[] _result)
        {
            uint sourceLength = checked((uint)r.Length);
            uint num = (sourceLength + 7u) & ~7u;
            byte[] array = new byte[num];
            byte[] dst = new byte[num];
            r.ReadExactly(array.AsSpan(0, (int)sourceLength));
            EncryptUpdatelist(key, ref dst, array, num);
            w.Write(dst, 0, dst.Length);
            _result = ((MemoryStream)w).ToArray();
            r.Close();
            w.Close();
        }

        private static void DecipherStream(uint[] key, Stream r, Stream w, out byte[] _result)
        {
            uint num = (uint)r.Length;
            byte[] array = new byte[num];
            r.ReadExactly(array);
            byte[] dst = new byte[num];
            DecryptUpdatelist(key, ref dst, array, num);
            w.Write(dst, 0, (int)num);
            _result = ((MemoryStream)w).ToArray();
            r.Close();
            w.Close();
        }

        public static void EncipherStreamPadNull(uint[] key, Stream r, out byte[] _result)
        {
            MemoryStream w = new MemoryStream();
            EncipherStream(key, r, w, out _result);
        }

        public static void DecipherStreamTrimNull(uint[] key, Stream r, out byte[] _result)
        {
            MemoryStream w = new MemoryStream();
            DecipherStream(key, r, w, out _result);
        }

        public static void EncipherStreamPadNull(uint[] key, byte[] data_r, out byte[] _result)
        {
            MemoryStream w = new MemoryStream();
            EncipherStream(key, new MemoryStream(data_r), w, out _result);
        }

        public static void DecipherStreamTrimNull(uint[] key, byte[] data_r, out byte[] _result)
        {
            MemoryStream w = new MemoryStream();
            DecipherStream(key, new MemoryStream(data_r), w, out _result);
        }
        #endregion
    }
}
