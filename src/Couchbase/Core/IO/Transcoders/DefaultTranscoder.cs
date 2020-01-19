using System;
using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Couchbase.Core.IO.Operations;
using Couchbase.Core.IO.Serializers;
using Couchbase.Utils;
using ByteConverter = Couchbase.Core.IO.Converters.ByteConverter;

namespace Couchbase.Core.IO.Transcoders
{
     /// <summary>
    /// Provides the default implementation for <see cref="ITypeTranscoder"/> interface.
    /// </summary>
    public class DefaultTranscoder : ITypeTranscoder
     {
        public DefaultTranscoder()
            : this(new DefaultSerializer())
        {
        }

        public DefaultTranscoder(ITypeSerializer serializer)
        {
            Serializer = serializer;
        }

        /// <summary>
        /// Gets or sets the serializer used by the <see cref="ITypeTranscoder" /> implementation.
        /// </summary>
        public ITypeSerializer Serializer { get; set; }

        public virtual Flags GetFormat<T>(T value)
        {
            var dataFormat = DataFormat.Json;
            var typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.Object:
                    if (typeof(T) == typeof(Byte[]))
                    {
                        dataFormat = DataFormat.Binary;
                    }
                    break;
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                    dataFormat = DataFormat.Json;
                    break;
                case TypeCode.Char:
                case TypeCode.String:
                    dataFormat = DataFormat.String;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new Flags() { Compression = Compression.None, DataFormat = dataFormat, TypeCode = typeCode };
        }

        /// <inheritdoc />
        public virtual void Encode<T>(Stream stream, T value, Flags flags, OpCode opcode)
        {
            switch (flags.DataFormat)
            {
                case DataFormat.Reserved:
                case DataFormat.Private:
                case DataFormat.String:
                    Encode(stream, value, flags.TypeCode, opcode);
                    break;

                case DataFormat.Json:
                    SerializeAsJson(stream, value);
                    break;

                case DataFormat.Binary:
                    if (value is byte[] bytes)
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        var msg = string.Format("The value of T does not match the DataFormat provided: {0}",
                            flags.DataFormat);
                        throw new ArgumentException(msg);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Encodes the specified value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream">The stream to receive the encoded value.</param>
        /// <param name="value">The value.</param>
        /// <param name="typeCode">Type to use for encoding</param>
        /// <param name="opcode"></param>
        /// <exception cref="InvalidEnumArgumentException">Invalid typeCode.</exception>
        public virtual void Encode<T>(Stream stream, T value, TypeCode typeCode, OpCode opcode)
        {
            switch (typeCode)
            {
                case TypeCode.Empty:
#if NET45
                case TypeCode.DBNull:
#endif
                case TypeCode.String:
                case TypeCode.Char:
                    var str = Convert.ToString(value);
                    using (var bufferOwner = MemoryPool<byte>.Shared.Rent(ByteConverter.GetStringByteCount(str)))
                    {
                        var length = ByteConverter.FromString(str, bufferOwner.Memory.Span);
                        stream.Write(bufferOwner.Memory.Slice(0, length));
                    }
                    break;

                case TypeCode.Int16:
                {
                    Span<byte> bytes = stackalloc byte[sizeof(short)];
                    ByteConverter.FromInt16(Convert.ToInt16(value), bytes, false);
                    WriteHelper(stream, bytes);
                    break;
                }

                case TypeCode.UInt16:
                {
                    Span<byte> bytes = stackalloc byte[sizeof(ushort)];
                    ByteConverter.FromUInt16(Convert.ToUInt16(value), bytes, false);
                    WriteHelper(stream, bytes);
                    break;
                }

                case TypeCode.Int32:
                {
                    Span<byte> bytes = stackalloc byte[sizeof(int)];
                    ByteConverter.FromInt32(Convert.ToInt32(value), bytes, false);
                    WriteHelper(stream, bytes);
                    break;
                }

                case TypeCode.UInt32:
                {
                    Span<byte> bytes = stackalloc byte[sizeof(uint)];
                    ByteConverter.FromUInt32(Convert.ToUInt32(value), bytes, false);
                    WriteHelper(stream, bytes);
                    break;
                }

                case TypeCode.Int64:
                {
                    Span<byte> bytes = stackalloc byte[sizeof(long)];
                    ByteConverter.FromInt64(Convert.ToInt64(value), bytes, false);
                    WriteHelper(stream, bytes);
                    break;
                }

                case TypeCode.UInt64:
                {
                    Span<byte> bytes = stackalloc byte[sizeof(ulong)];
                    if (opcode == OpCode.Increment || opcode == OpCode.Decrement)
                    {
                        ByteConverter.FromUInt64(Convert.ToUInt64(value), bytes, true);
                    }
                    else
                    {
                        ByteConverter.FromUInt64(Convert.ToUInt64(value), bytes, false);
                    }
                    WriteHelper(stream, bytes);
                    break;
                }

                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Object:
                    SerializeAsJson(stream, value);
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(typeCode), (int) typeCode, typeof(TypeCode));
            }
        }

        /// <inheritdoc />
        public virtual T Decode<T>(ReadOnlyMemory<byte> buffer, Flags flags, OpCode opcode)
        {
            object value = default(T);
            switch (flags.DataFormat)
            {
                case DataFormat.Reserved:
                case DataFormat.Private:
                    if (typeof (T) == typeof (byte[]))
                    {
                        value = DecodeBinary(buffer.Span);
                    }
                    else
                    {
                        value = Decode<T>(buffer, opcode);
                    }
                    break;

                case DataFormat.Json:
                    if (typeof (T) == typeof (string))
                    {
                        value = DecodeString(buffer.Span);
                    }
                    else
                    {
                        value = DeserializeAsJson<T>(buffer);
                    }
                    break;

                case DataFormat.Binary:
                    if (typeof(T) == typeof(byte[]))
                    {
                        value = DecodeBinary(buffer.Span);
                    }
                    else
                    {
                        var msg = string.Format("The value of T does not match the DataFormat provided: {0}",
                            flags.DataFormat);
                        throw new ArgumentException(msg);
                    }
                    break;

                case DataFormat.String:
                    if (typeof(T) == typeof(char))
                    {
                        value = DecodeChar(buffer.Span);
                    }
                    else
                    {
                        value = DecodeString(buffer.Span);
                    }
                    break;

                default:
                    value = DecodeString(buffer.Span);
                    break;
            }
            return (T)value;
        }

        /// <summary>
        /// Decodes the specified buffer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public virtual T Decode<T>(ReadOnlyMemory<byte> buffer, OpCode opcode)
        {
            object value = default(T);

            var typeCode = Type.GetTypeCode(typeof(T));
            switch (typeCode)
            {
                case TypeCode.Empty:
#if NET45
                case TypeCode.DBNull:
#endif
                case TypeCode.String:
                    value = DecodeString(buffer.Span);
                    break;

                case TypeCode.Char:
                    value = DecodeChar(buffer.Span);
                    break;

                case TypeCode.Int16:
                    if (buffer.Length > 0)
                    {
                        value = ByteConverter.ToInt16(buffer.Span, false);
                    }
                    break;

                case TypeCode.UInt16:
                    if (buffer.Length > 0)
                    {
                        value = ByteConverter.ToUInt16(buffer.Span, false);
                    }
                    break;

                case TypeCode.Int32:
                    if (buffer.Length > 0)
                    {
                        value = ByteConverter.ToInt32(buffer.Span, false);
                    }
                    break;

                case TypeCode.UInt32:
                    if (buffer.Length > 0)
                    {
                        value = ByteConverter.ToUInt32(buffer.Span, false);
                    }
                    break;

                case TypeCode.Int64:
                    if (buffer.Length > 0)
                    {
                        value = ByteConverter.ToInt64(buffer.Span, false);
                    }
                    break;

                case TypeCode.UInt64:
                    if (buffer.Length > 0)
                    {
                        if (opcode == OpCode.Increment || opcode == OpCode.Decrement)
                        {
                            value = ByteConverter.ToUInt64(buffer.Span, true);
                        }
                        else
                        {
                            value = ByteConverter.ToUInt64(buffer.Span, false);
                        }
                    }
                    break;

                case TypeCode.Single:
                    break;

                case TypeCode.Double:
                    break;

                case TypeCode.Decimal:
                    break;

                case TypeCode.DateTime:
                    break;

                case TypeCode.Boolean:
                    break;

                case TypeCode.SByte:
                    break;

                case TypeCode.Byte:
                    break;

                case TypeCode.Object:
                    value = DeserializeAsJson<T>(buffer);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return (T)value;
        }

        /// <summary>
        /// Deserializes as json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public virtual T DeserializeAsJson<T>(ReadOnlyMemory<byte> buffer)
        {
            return Serializer.Deserialize<T>(buffer);
        }

        /// <summary>
        /// Serializes as json.
        /// </summary>
        /// <param name="stream">The stream to receive the encoded value.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual void SerializeAsJson(Stream stream, object value)
        {
            Serializer.Serialize(stream, value);
        }

        /// <summary>
        /// Decodes the specified buffer as string.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        protected string DecodeString(ReadOnlySpan<byte> buffer)
        {
            string result = null;
            if (buffer.Length > 0)
            {
                result = ByteConverter.ToString(buffer);
            }
            return result;
        }

        /// <summary>
        /// Decodes the specified buffer as char.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        protected char DecodeChar(ReadOnlySpan<byte> buffer)
        {
            char result = default(char);
            if (buffer.Length > 0)
            {
                var str = ByteConverter.ToString(buffer);
                if (str.Length == 1)
                {
                    result = str[0];
                }
                else if (str.Length > 1)
                {
                    var msg = string.Format("Can not convert string \"{0}\" to char", str);
                    throw new InvalidCastException(msg);
                }
            }
            return result;
        }

        /// <summary>
        /// Decodes the binary.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        protected byte[] DecodeBinary(ReadOnlySpan<byte> buffer)
        {
            var temp = new byte[buffer.Length];
            buffer.CopyTo(temp.AsSpan());
            return temp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteHelper(Stream stream, ReadOnlySpan<byte> buffer)
        {
#if NETCOREAPP2_1 || NETSTANDARD2_1
            stream.Write(buffer);
#else
            var array = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(array);

                stream.Write(array, 0, buffer.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
#endif
        }
    }
}
