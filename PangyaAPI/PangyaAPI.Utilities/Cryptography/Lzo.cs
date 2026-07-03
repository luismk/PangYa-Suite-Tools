using System;

namespace PangyaAPI.Utilities.Cryptography;

public static class Lzo
{
	private static readonly int[] MultiplyDeBruijnBitPosition = new int[32]
	{
		0, 1, 28, 2, 29, 14, 24, 3, 30, 22,
		20, 15, 25, 17, 4, 8, 31, 27, 13, 23,
		21, 19, 16, 7, 26, 12, 18, 6, 11, 5,
		10, 9
	};

	private static byte[] Lzo1XDecompress(byte[] @in)
	{
		byte[] array = new byte[0];
		uint num = 0u;
		uint num2 = 0u;
		bool flag = false;
		bool flag2 = false;
		if (@in[num2] > 17)
		{
			uint num3 = (uint)(@in[num2++] - 17);
			EnsureSpace(num3, ref array, num);
			if (num3 != 0)
			{
				do
				{
					array[num++] = @in[num2++];
				}
				while (--num3 != 0);
			}
			if (num3 >= 4)
			{
				flag = true;
			}
		}
		while (true)
		{
			uint num3;
			if (flag)
			{
				flag = false;
			}
			else
			{
				num3 = @in[num2++];
				if (num3 >= 16)
				{
					goto IL_017c;
				}
				if (num3 == 0)
				{
					for (; @in[num2] == 0; num2++)
					{
						num3 += 255;
					}
					num3 += (uint)(15 + @in[num2++]);
				}
				num3 += 3;
				EnsureSpace(num3, ref array, num);
				if (num3 != 0)
				{
					do
					{
						array[num++] = @in[num2++];
					}
					while (--num3 != 0);
				}
			}
			num3 = @in[num2++];
			if (num3 < 16)
			{
				uint num4 = num - 2049;
				num4 -= num3 >> 2;
				num4 -= (uint)(@in[num2++] << 2);
				EnsureSpace(3u, ref array, num);
				array[num++] = array[num4++];
				array[num++] = array[num4++];
				array[num++] = array[num4];
				flag2 = true;
			}
			goto IL_017c;
			IL_017c:
			while (true)
			{
				if (flag2)
				{
					flag2 = false;
				}
				else if (num3 >= 64)
				{
					uint num4 = num - 1;
					num4 -= (num3 >> 2) & 7;
					num4 -= (uint)(@in[num2++] << 3);
					num3 = (num3 >> 5) - 1;
					num3 += 2;
					EnsureSpace(num3, ref array, num);
					do
					{
						array[num++] = array[num4++];
					}
					while (--num3 != 0);
				}
				else
				{
					uint num4;
					if (num3 >= 32)
					{
						num3 &= 0x1F;
						if (num3 == 0)
						{
							for (; @in[num2] == 0; num2++)
							{
								num3 += 255;
							}
							num3 += (uint)(31 + @in[num2++]);
						}
						num4 = num - 1;
						num4 -= ReadU16(@in, num2) >> 2;
						num2 += 2;
					}
					else
					{
						if (num3 < 16)
						{
							num4 = num - 1;
							num4 -= num3 >> 2;
							num4 -= (uint)(@in[num2++] << 2);
							EnsureSpace(2u, ref array, num);
							array[num++] = array[num4++];
							array[num++] = array[num4];
							goto IL_034e;
						}
						num4 = num;
						num4 -= (num3 & 8) << 11;
						num3 &= 7;
						if (num3 == 0)
						{
							for (; @in[num2] == 0; num2++)
							{
								num3 += 255;
							}
							num3 += (uint)(7 + @in[num2++]);
						}
						num4 -= ReadU16(@in, num2) >> 2;
						num2 += 2;
						if (num4 == num)
						{
							Array.Resize(ref array, (int)num);
							return array;
						}
						num4 -= 16384;
					}
					num3 += 2;
					EnsureSpace(num3, ref array, num);
					do
					{
						array[num++] = array[num4++];
					}
					while (--num3 != 0);
				}
				goto IL_034e;
				IL_034e:
				num3 = (uint)(@in[num2 - 2] & 3);
				if (num3 == 0)
				{
					break;
				}
				EnsureSpace(num3, ref array, num);
				if (num3 != 0)
				{
					do
					{
						array[num++] = @in[num2++];
					}
					while (--num3 != 0);
				}
				num3 = @in[num2++];
			}
		}
	}

	private static uint ReadU32(byte[] arr, uint i)
	{
		return (uint)(arr[i] | (arr[i + 1] << 8) | (arr[i + 2] << 16) | (arr[i + 3] << 24));
	}

	private static uint ReadU16(byte[] arr, uint i)
	{
		return (uint)(arr[i] | (arr[i + 1] << 8));
	}

	private static int LzoBitOpsCtz32(uint v)
	{
		return MultiplyDeBruijnBitPosition[(uint)((v & (0L - (long)v)) * 125613361) >> 27];
	}

	private static uint NearestPowerOfTwo(uint n)
	{
		n--;
		n |= n >> 1;
		n |= n >> 2;
		n |= n >> 4;
		n |= n >> 8;
		n |= n >> 16;
		return n + 1;
	}

	private static void AllocSpace(ref byte[] array, uint minLength)
	{
		Array.Resize(ref array, (int)NearestPowerOfTwo(minLength));
	}

	private static void EnsureSpace(uint need, ref byte[] array, uint pos)
	{
		uint num = pos + need;
		if (num > array.Length)
		{
			AllocSpace(ref array, num);
		}
	}

	private static uint Lzo1X1CompressCore(byte[] @in, uint inIndex, uint inLen, byte[] @out, uint outIndex, out uint outLen, uint ti, ushort[] dict)
	{
		uint num = inIndex + inLen;
		uint num2 = inIndex + inLen - 20;
		uint num3 = outIndex;
		uint num4 = inIndex;
		uint num5 = num4;
		num4 += ((ti < 4) ? (4 - ti) : 0);
		while (true)
		{
			num4 += 1 + (num4 - num5 >> 5);
			while (true)
			{
				if (num4 < num2)
				{
					uint num6 = ReadU32(@in, num4);
					uint num7 = (405029533 * num6 >> 18) & 0x3FFF;
					uint num8 = inIndex + dict[num7];
					dict[num7] = (ushort)(num4 - inIndex);
					if (num6 != ReadU32(@in, num8))
					{
						break;
					}
					num5 -= ti;
					ti = 0u;
					uint num9 = num4 - num5;
					if (num9 != 0)
					{
						if (num9 <= 3)
						{
							@out[num3 - 2] |= (byte)num9;
							Array.Copy(@in, num5, @out, num3, num9);
							num3 += num9;
						}
						else if (num9 <= 16)
						{
							@out[num3++] = (byte)(num9 - 3);
							Array.Copy(@in, num5, @out, num3, num9);
							num3 += num9;
						}
						else
						{
							if (num9 <= 18)
							{
								@out[num3++] = (byte)(num9 - 3);
							}
							else
							{
								uint num10 = num9 - 18;
								@out[num3++] = 0;
								while (num10 > 255)
								{
									num10 -= 255;
									@out[num3++] = 0;
								}
								@out[num3++] = (byte)num10;
							}
							Array.Copy(@in, num5, @out, num3, num9);
							num3 += num9;
						}
					}
					uint num11 = 4u;
					uint num12 = ReadU32(@in, num4 + num11) ^ ReadU32(@in, num8 + num11);
					do
					{
						if (num12 == 0)
						{
							num11 += 4;
							num12 = ReadU32(@in, num4 + num11) ^ ReadU32(@in, num8 + num11);
							continue;
						}
						num11 += (uint)LzoBitOpsCtz32(num12) / 8u;
						break;
					}
					while (num4 + num11 < num2);
					uint num13 = num4 - num8;
					num4 += num11;
					num5 = num4;
					if (num11 <= 8 && num13 <= 2048)
					{
						num13--;
						@out[num3++] = (byte)((num11 - 1 << 5) | ((num13 & 7) << 2));
						@out[num3++] = (byte)(num13 >> 3);
						continue;
					}
					if (num13 <= 16384)
					{
						num13--;
						if (num11 <= 33)
						{
							@out[num3++] = (byte)(0x20 | (num11 - 2));
						}
						else
						{
							num11 -= 33;
							@out[num3++] = 32;
							while (num11 > 255)
							{
								num11 -= 255;
								@out[num3++] = 0;
							}
							@out[num3++] = (byte)num11;
						}
						@out[num3++] = (byte)(num13 << 2);
						@out[num3++] = (byte)(num13 >> 6);
						continue;
					}
					num13 -= 16384;
					if (num11 <= 9)
					{
						@out[num3++] = (byte)(0x10 | ((num13 >> 11) & 8) | (num11 - 2));
					}
					else
					{
						num11 -= 9;
						@out[num3++] = (byte)(0x10 | ((num13 >> 11) & 8));
						while (num11 > 255)
						{
							num11 -= 255;
							@out[num3++] = 0;
						}
						@out[num3++] = (byte)num11;
					}
					@out[num3++] = (byte)(num13 << 2);
					@out[num3++] = (byte)(num13 >> 6);
					continue;
				}
				outLen = num3 - outIndex;
				return num - (num5 - ti);
			}
		}
	}

	private static void Lzo1X1Compress(byte[] @in, uint inLen, byte[] @out, out uint outLen, ushort[] dict)
	{
		uint num = 0u;
		uint num2 = 0u;
		uint num3 = inLen;
		uint num4 = 0u;
		while (num3 > 20)
		{
			uint num5 = num3;
			num5 = ((num5 <= 49152) ? num5 : 49152u);
			uint num6 = num + num5;
			if (num6 + (num4 + num5 >> 5) <= num6 || num6 + (num4 + num5 >> 5) <= num + num5)
			{
				break;
			}
			for (int i = 0; i < 32768; i++)
			{
				dict[i] = 0;
			}
			num4 = Lzo1X1CompressCore(@in, num, num5, @out, num2, out outLen, num4, dict);
			num += num5;
			num2 += outLen;
			num3 -= num5;
		}
		num4 += num3;
		if (num4 != 0)
		{
			ulong num7 = inLen - num4;
			if (num2 == 0 && num4 <= 238)
			{
				@out[num2++] = (byte)(17 + num4);
			}
			else
			{
				switch (num4)
				{
				case 0u:
				case 1u:
				case 2u:
				case 3u:
					@out[num2 - 2] |= (byte)num4;
					break;
				case 4u:
				case 5u:
				case 6u:
				case 7u:
				case 8u:
				case 9u:
				case 10u:
				case 11u:
				case 12u:
				case 13u:
				case 14u:
				case 15u:
				case 16u:
				case 17u:
				case 18u:
					@out[num2++] = (byte)(num4 - 3);
					break;
				default:
				{
					uint num8 = num4 - 18;
					@out[num2++] = 0;
					while (num8 > 255)
					{
						num8 -= 255;
						@out[num2++] = 0;
					}
					@out[num2++] = (byte)num8;
					break;
				}
				}
			}
			do
			{
				@out[num2++] = @in[num7++];
			}
			while (--num4 != 0);
		}
		@out[num2++] = 17;
		@out[num2++] = 0;
		@out[num2++] = 0;
		outLen = num2;
	}

	public static byte[] Decompress(byte[] input)
	{
		try
		{
			return Lzo1XDecompress(input);
		}
		catch
		{
			return input;
		}
	}

	public static byte[] Compress(byte[] input)
	{
		byte[] array = new byte[input.Length + input.Length / 16 + 64 + 3];
		Lzo1X1Compress(input, (uint)input.Length, array, out var outLen, new ushort[32768]);
		Array.Resize(ref array, (int)outLen);
		return array;
	}
}
