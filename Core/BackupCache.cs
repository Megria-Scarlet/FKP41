using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FKP41
{
    public struct BackupCache
    {
        private DateTime lastWriteTime;
        private ulong fileLength;
        private string fileName;

        public BackupCache(FileInfo fileInfo)
        {
            lastWriteTime = fileInfo.LastWriteTimeUtc;
            fileLength = (ulong)fileInfo.Length;
            fileName = fileInfo.FullName;
        }

        public readonly bool IsFileUpdated(FileInfo fileInfo)
        {
            return fileInfo.FullName == fileName && ((ulong)fileInfo.Length != fileLength || fileInfo.LastWriteTimeUtc != lastWriteTime);
        }

        public readonly void WriteStream(Stream stream)
        {
            Span<byte> buffers = stackalloc byte[sizeof(long)];
            System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(buffers, lastWriteTime.ToBinary());
            stream.Write(buffers);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(buffers, fileLength);
            stream.Write(buffers);
            byte[] utf8Bytes = System.Text.Encoding.UTF8.GetBytes(fileName);
            WriteUtf8(fileName, stream);

            static void WriteUtf8(ReadOnlySpan<char> chars, Stream stream)
            {
                Span<byte> buffer = stackalloc byte[128];

                SpanPrimitiveList<byte> primitiveList = new(buffer);
                int write = primitiveList.WriteUtf8(chars);

                Span<byte> buffer1 = stackalloc byte[sizeof(uint)];
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(buffer1, (uint)write);
                stream.Write(buffer1);
                stream.Write(primitiveList.AsSpan());
            }
        }
        public static BackupCache ReadStream(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            stream.ReadExactly(buffer);
            BackupCache cache = new BackupCache();
            cache.lastWriteTime = DateTime.FromBinary(System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(buffer));
            stream.ReadExactly(buffer);
            cache.fileLength = System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(buffer);
            Span<byte> buffer1 = buffer[..sizeof(uint)];
            stream.ReadExactly(buffer1);
            uint utf8Length = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(buffer1);
            if (utf8Length <= 128)
            {
                Span<byte> buffer2 = stackalloc byte[(int)utf8Length];
                stream.ReadExactly(buffer2);
                cache.fileName = A(buffer2);
            }
            else
            {
                var pool = System.Buffers.ArrayPool<byte>.Shared;
                byte[] array = pool.Rent((int)utf8Length);
                try
                {
                    Span<byte> buffer2 = array.AsSpan(0, (int)utf8Length);
                    stream.ReadExactly(buffer2);
                    cache.fileName = A(buffer2);
                }
                finally
                {
                    pool.Return(array);
                }
            }

            return cache;

            static string A(scoped Span<byte> buffer)
            {
                SpanPrimitiveList<char> list = new(stackalloc char[32]);
                _ = list.WriteUtf16(buffer);
                return list.AsSpan().ToString();
            }
        }
    }
}
