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
                System.Buffers.ArrayPool<byte> arrayPool = System.Buffers.ArrayPool<byte>.Shared;
                byte[]? bufferArray = null;
                int count = 0;

                System.Buffers.OperationStatus status;
                do
                {
                    status = System.Text.Unicode.Utf8.FromUtf16(chars, buffer[count..], out int charsRead, out int bytesWritten, true, true);
                    count += bytesWritten;
                    chars = chars[charsRead..];

                    if (status == System.Buffers.OperationStatus.DestinationTooSmall)
                    {
                        Grow(ref buffer, count, ref bufferArray, arrayPool);
                        continue;
                    }
                }
                while (false);

                Span<byte> buffer1 = stackalloc byte[sizeof(uint)];
                System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(buffer1, (uint)count);
                stream.Write(buffer1);
                stream.Write(buffer[..count]);


                static void Grow(ref Span<byte> buffer, int count, ref byte[]? array, System.Buffers.ArrayPool<byte> pool)
                {
                    byte[] newArray = pool.Rent(checked(buffer.Length * 2));
                    buffer[..count].CopyTo(newArray);
                    if (array is not null)
                        pool.Return(array);
                    array = newArray;
                    buffer = array.AsSpan();
                }
            }
        }
        public static BackupCache ReadStream(Stream stream)
        {
            Span<byte> buffers = stackalloc byte[sizeof(long)];
            stream.ReadExactly(buffers);
            BackupCache stamp = new BackupCache();

            return default;
        }
    }
}
