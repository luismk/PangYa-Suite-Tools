using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;

namespace PangyaAPI.Utilities.Models
{
    public class PangyaBinaryReader : BinaryReader
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
        public Encoding _Encoder { get; set; }
        public PangyaBinaryReader(Stream input) : base(input, Encoding.GetEncoding(874))
        {
            _Encoder = Encoding.GetEncoding(874);
        }


        public PangyaBinaryReader(byte[] array) : base(new MemoryStream(array), Encoding.GetEncoding(874))
        {
            _Encoder = Encoding.GetEncoding(874);
        }

        public PangyaBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
        {
            _Encoder = encoding;
        }

        public PangyaBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
            _Encoder = encoding;
        }

        public void Skip(int count)
        {
            Seek(count, 1);
        }

        public void Seek(long offset, int origin)
        {
            BaseStream.Seek(offset, (SeekOrigin)origin);
        }
        public void Seek(uint offset, int origin)
        {
            BaseStream.Seek(offset, (SeekOrigin)origin);
        }

        public void Seek(int offset, int origin)
        {
            BaseStream.Seek(offset, (SeekOrigin)origin);
        }
        public uint Size => (uint)BaseStream.Length;


        public byte[] GetRemainingData(int Count)
        {
            int previousOffset;
            previousOffset = (int)BaseStream.Position;
            var array = ReadBytes(Count);
            BaseStream.Position = previousOffset;
            return array;
        }
        public byte[] GetRemainingData()
        {
            int previousOffset;
            previousOffset = (int)BaseStream.Position;
            var array = ReadBytes((int)Size);
            BaseStream.Position = previousOffset;
            return array;
        }


        public bool ReadPStr(out string value, uint Count)
        {
            try
            {
                var data = new byte[Count];
                //ler os dados
                BaseStream.ReadExactly(data);

                value = _Encoder.GetString(data);
            }
            catch
            {
                value = string.Empty;
                return false;
            }
            return true;
        }

        public bool ReadPStr(out string[] value, uint Length, uint Count)
        {
            try
            {
                value = new string[Count / Length];
                for (int i = 0; i < Count / Length; i++)
                {
                    value[i] = ReadPStr(Length);
                }
            }
            catch
            {
                value = Array.Empty<string>();
                return false;
            }
            return true;
        }
        public bool ReadPStr(out string value)
        {
            try
            {
                var size = ReadUInt16();
                value = _Encoder.GetString(ReadBytes(size));
            }
            catch
            {
                value = string.Empty;
                return false;
            }
            return true;
        }

        public string ReadSJISString()
        {
            try
            {
                ushort length = ReadUInt16(); // Lê o tamanho da string (em bytes)
                byte[] data = ReadBytes(length); // Lê os bytes da string

                return _Encoder.GetString(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao ler string Shift_JIS: {ex.Message}");
                return string.Empty;
            }
        }
        public string ReadPStr()
        {
            try
            {
                var size = ReadUInt16();

                return _Encoder.GetString(ReadBytes(size));
            }
            catch
            {
                return "";
            }
        }

        public byte[] ReadByteStr()
        {
            try
            {
                var size = ReadUInt16();

                return ReadBytes(size);
            }
            catch
            {
                return new byte[0];
            }
        }
        public string ReadPStr(uint Count)
        {
            try
            {
                var data = new byte[Count];
                //ler os dados
                BaseStream.ReadExactly(data);

                return _Encoder.GetString(data).Replace("\0", "");
            }
            catch
            {
                return "";
            }
        }

        public short[] ReadShorts(uint Count)
        {
            try
            {
                var data = new short[Count];
                for (int i = 0; i < Count; i++)
                {
                    data[i] = ReadInt16();
                }
                return data;
            }
            catch
            {
                return new short[0];
            }
        }

        public uint GetPosition()
        {
            return (uint)BaseStream.Position;
        }

        public bool ReadDouble(out Double value)
        {
            try
            {
                value = ReadDouble();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadByte(out byte value)
        {
            try
            {
                value = ReadByte();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }
        public bool ReadInt16(out short value)
        {
            try
            {
                value = ReadInt16();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public sbyte[] ReadSBytes(int size)
        {
            sbyte[] buffer = new sbyte[size];

            for (int i = 0; i < size; i++)
                buffer[i] = unchecked((sbyte)ReadByte());

            return buffer;
        }

        public bool ReadSBytes(out sbyte[] buffer, int size)
        {
            buffer = new sbyte[size];

            try
            {
                for (int i = 0; i < size; i++)
                    buffer[i] = unchecked((sbyte)ReadByte());
                return true;
            }
            catch
            {
                buffer = Array.Empty<sbyte>();
                return false;
            }
        }




        public bool ReadBytes(out byte[] value, int size)
        {
            try
            {
#pragma warning disable CS0652 // Comparação com constante integral é inútil; a constante está fora do intervalo do tipo "int"
                if (uint.MaxValue < size)
                {
                    value = new byte[0];
                    return false;
                }
#pragma warning restore CS0652 // Comparação com constante integral é inútil; a constante está fora do intervalo do tipo "int"
                value = ReadBytes(size);
            }
            catch
            {
                value = new byte[0];
                return false;
            }
            return true;
        }

        public bool ReadBytes(out byte[] value)
        {
            try
            {
                int size = ReadInt16();

                if (ushort.MaxValue < size)
                {
                    value = new byte[0];
                    return false;
                }
                value = ReadBytes(size);
            }
            catch
            {
                value = new byte[0];
                return false;
            }
            return true;
        }
        public bool ReadUInt16(out ushort value)
        {
            try
            {
                value = ReadUInt16();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadUInt32(out uint value)
        {
            try
            {
                value = ReadUInt32();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadInt32(out int value)
        {
            try
            {
                value = ReadInt32();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadUInt64(out ulong value)
        {
            try
            {
                value = ReadUInt64();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadInt64(out long value)
        {
            try
            {
                value = ReadInt64();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public bool ReadSingle(out float value)
        {
            try
            {
                value = ReadSingle();
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }

        public DateTime ReadDateTime()
        {
            try
            {
                var result = (SystemTime)Read(new SystemTime());
                return result.ConvertTime();
            }
            catch
            {
                var result = new SystemTime();
                return result.ConvertTime();
            }
        }

        public IEnumerable<uint> Read(uint count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadUInt32();
            }
        }

        public IEnumerable<int> Read(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadInt32();
            }
        }
        //não testado
        public bool Read(out object value, int Count)
        {
            try
            {
                var obj = new object();
                value = MarshalBytes(ReadExactlyBytes(Count), obj.GetType());
            }
            catch
            {
                value = 0;
                return false;
            }
            return true;
        }


        public bool ReadBuffer<T>(ref T value, int Count)
        {
            try
            {
                object result = MarshalBytes(ReadExactlyBytes(Count), typeof(T));
                value = (T)result;
            }
            catch
            {
                value = default!;
                return false;
            }
            return true;
        }


        public T Read<T>() where T : new()
        {
            int count = (typeof(T) == typeof(bool)) ? 1 : Marshal.SizeOf(typeof(T));
            return (T)MarshalBytes(ReadExactlyBytes(count), typeof(T));
        }

        public object Read(object value)
        {
            var Count = Marshal.SizeOf(value);

            return MarshalBytes(ReadExactlyBytes(Count), value.GetType());
        }

        public object Read(object value, object value_ori)
        {
            var Count = Marshal.SizeOf(value_ori);

            return MarshalBytes(ReadExactlyBytes(Count), value.GetType());
        }

        public object Read(object value, int Count)
        {
            return MarshalBytes(ReadExactlyBytes(Count), value.GetType());
        }

        private byte[] ReadExactlyBytes(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            byte[] data = new byte[count];
            BaseStream.ReadExactly(data);
            return data;
        }

        private static object MarshalBytes(byte[] data, Type type)
        {
            int allocationSize = Math.Max(data.Length, Marshal.SizeOf(type));
            IntPtr ptr = Marshal.AllocHGlobal(allocationSize);
            try
            {
                byte[] initialized = new byte[allocationSize];
                Marshal.Copy(initialized, 0, ptr, allocationSize);
                Marshal.Copy(data, 0, ptr, data.Length);
                return Marshal.PtrToStructure(ptr, type)
                    ?? throw new InvalidDataException($"Unable to marshal {data.Length} bytes as {type.FullName}.");
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        public Object ReadObject(object obj)
        {
            foreach (var property in PropertyCache.GetOrAdd(obj.GetType(), static type => type.GetProperties()))
            {
                Type type = property.PropertyType;

                TypeCode typeCode = Type.GetTypeCode(type);
                switch (typeCode)
                {
                    case TypeCode.Empty:
                        break;
                    case TypeCode.Object:
                        {
                            if (type.Name == "Byte[]")
                            {
                                //  property.SetValue(obj, ReadBytes(obj));
                            }
                            if (type.Name == "Long[]")
                            {
                                //  property.SetValue(obj, ReadBytes(obj));
                                var obj_value = (long[])obj;
                                for (int i = 0; i < obj_value.Length; i++)
                                {

                                }
                            }
                        }
                        break;
                    case TypeCode.DBNull:
                        break;
                    case TypeCode.Boolean:
                        {
                            property.SetValue(obj, ReadBoolean());
                        }
                        break;
                    case TypeCode.Char:
                        {
                            property.SetValue(obj, ReadChar());
                        }
                        break;
                    case TypeCode.SByte:
                        {
                            property.SetValue(obj, ReadSByte());
                        }
                        break;
                    case TypeCode.Byte:
                        {
                            property.SetValue(obj, ReadByte());
                        }
                        break;
                    case TypeCode.Int16:
                        {
                            property.SetValue(obj, ReadInt16());
                        }
                        break;
                    case TypeCode.UInt16:
                        {
                            property.SetValue(obj, ReadUInt16());
                        }
                        break;
                    case TypeCode.Int32:
                        {
                            property.SetValue(obj, ReadInt32());
                        }
                        break;
                    case TypeCode.UInt32:
                        property.SetValue(obj, ReadUInt32());
                        break;
                    case TypeCode.Int64:
                        {
                            property.SetValue(obj, ReadInt64());
                        }
                        break;
                    case TypeCode.UInt64:
                        {
                            property.SetValue(obj, ReadUInt64());
                        }
                        break;
                    case TypeCode.Single:
                        {
                            property.SetValue(obj, ReadSingle());
                        }
                        break;
                    case TypeCode.Double:
                        {
                            property.SetValue(obj, ReadDouble());
                        }
                        break;
                    case TypeCode.Decimal:
                        {
                            property.SetValue(obj, ReadDecimal());
                        }
                        break;
                    case TypeCode.DateTime:
                        {
                            property.SetValue(obj, ReadDateTime());
                        }
                        break;
                    case TypeCode.String:
                        {
                            property.SetValue(obj, ReadPStr());
                        }
                        break;
                    default:
                        {
                            Console.WriteLine("Object Type Name: " + typeCode);
                        }
                        break;
                }
            }
            return obj;
        }
        public void ReadObject(out object obj)
        {
            obj = new object();
            foreach (var property in PropertyCache.GetOrAdd(obj.GetType(), static type => type.GetProperties()))
            {
                Type type = property.PropertyType;

                TypeCode typeCode = Type.GetTypeCode(type);
                switch (typeCode)
                {
                    case TypeCode.Empty:
                        break;
                    case TypeCode.Object:
                        break;
                    case TypeCode.DBNull:
                        break;
                    case TypeCode.Boolean:
                        {
                            property.SetValue(obj, ReadBoolean());
                        }
                        break;
                    case TypeCode.Char:
                        {
                            property.SetValue(obj, ReadChar());
                        }
                        break;
                    case TypeCode.SByte:
                        {
                            property.SetValue(obj, ReadSByte());
                        }
                        break;

                    case TypeCode.Byte:
                        {
                            property.SetValue(obj, ReadByte());
                        }
                        break;
                    case TypeCode.Int16:
                        {
                            property.SetValue(obj, ReadInt16());
                        }
                        break;
                    case TypeCode.UInt16:
                        {
                            property.SetValue(obj, ReadUInt16());
                        }
                        break;
                    case TypeCode.Int32:
                        {
                            property.SetValue(obj, ReadInt32());
                        }
                        break;
                    case TypeCode.UInt32:
                        property.SetValue(obj, ReadUInt32());
                        break;
                    case TypeCode.Int64:
                        {
                            property.SetValue(obj, ReadInt64());
                        }
                        break;
                    case TypeCode.UInt64:
                        {
                            property.SetValue(obj, ReadUInt64());
                        }
                        break;
                    case TypeCode.Single:
                        {
                            property.SetValue(obj, ReadSingle());
                        }
                        break;
                    case TypeCode.Double:
                        {
                            property.SetValue(obj, ReadDouble());
                        }
                        break;
                    case TypeCode.Decimal:
                        {
                            property.SetValue(obj, ReadDecimal());
                        }
                        break;
                    case TypeCode.DateTime:
                        {
                            property.SetValue(obj, ReadDateTime());
                        }
                        break;
                    case TypeCode.String:
                        {
                            property.SetValue(obj, ReadPStr());
                        }
                        break;
                    default:
                        {
                            Console.WriteLine("Object Type Name: " + typeCode);
                        }
                        break;
                }
            }
            // return obj;
        }

        public IEnumerable<uint> ReadUInt32Array(uint count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadUInt32();
            }
        }
        public IEnumerable<int> ReadInt32Array(uint count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadInt32();
            }
        }

        public IEnumerable<ushort> ReadUInt16Array(ushort count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadUInt16();
            }
        }

        public IEnumerable<short> ReadInt16Array(ushort count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return ReadInt16();
            }
        }

        public T ReadStruct<T>()
        {
            var count = Marshal.SizeOf(typeof(T));
            return (T)MarshalBytes(ReadExactlyBytes(count), typeof(T));
        }

        protected T _Read<T>(long real_size)
        {
            if (real_size < 0 || real_size > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(real_size));
            return (T)MarshalBytes(ReadExactlyBytes((int)real_size), typeof(T));
        }

        public T[] ReadStruct<T>(int count)
        {
            var array = new T[count];

            for (int i = 0; i < count; i++)
            {
                array[i] = ReadStruct<T>();
            }

            return array;
        }

        public T[] ReadStruct<T>(int count, int size)
        {
            var array = new T[count];

            for (int i = 0; i < count; i++)
            {
                array[i] = _Read<T>(size);
            }

            return array;
        }

        public object Read(object value, long real_size)
        {
            if (real_size < 0 || real_size > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(real_size));
            return MarshalBytes(ReadExactlyBytes((int)real_size), value.GetType());
        }
        public byte[] ReadBytes()
        {
            return ReadBytes((int)this.Size);
        }
    }
}
