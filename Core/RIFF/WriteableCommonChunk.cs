using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace FKP41.Core.RIFF
{
    public class WriteableCommonChunk : IDisposable, IChunk
    {
        protected bool isDisposed;
        protected byte[] array;
        protected System.Buffers.ArrayPool<byte> arrayPool;

        protected uint chunkId;
        public uint ChunkId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => chunkId;
        }
        protected uint chunkSize;
        public uint ChunkSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => chunkSize;
        }

        public WriteableCommonChunk(uint chunkId)
        {
            this.chunkId = chunkId;
            this.chunkSize = 0;
            this.array = [];
            this.arrayPool = System.Buffers.ArrayPool<byte>.Shared;
        }

        public virtual void WriteChunk(Stream stream)
        {
            ThrowIfOddStreamPosition(stream);
            WriteChunkHeadToStream(stream);
            stream.Write(array.AsSpan(0, (int)chunkSize));

            if (uint.IsOddInteger(chunkSize)) // 奇数の場合は偶数位置にパディング
            {
                stream.WriteByte(0);
            }
        }

        private void WriteChunkHeadToStream(Stream stream)
        {
            uint u = this.chunkId;
            Span<byte> src = System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref u), sizeof(uint));
            stream.Write(src);
            u = BitConverter.IsLittleEndian ? chunkSize : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(chunkSize);
            stream.Write(src);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value, bool isLittleEndian)
        {
            Write(isLittleEndian == BitConverter.IsLittleEndian ? value : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(value));
        }
        public void Write(uint value)
        {
            if (chunkSize + sizeof(uint) > array.Length)
            {
                Grow((int)(chunkSize + sizeof(uint)));
            }

            Span<byte> src = System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref value), sizeof(uint));
            src.CopyTo(array.AsSpan((int)chunkSize));
            chunkSize += sizeof(uint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value, bool isLittleEndian) => Write((uint)value, isLittleEndian);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value) => Write((uint)value);

        protected void Grow(int minimumLength)
        {
            byte[] array = arrayPool.Rent(minimumLength);
            this.array.AsSpan(0, (int)chunkSize).CopyTo(array);
            (array, this.array) = (this.array, array);
            arrayPool.Return(array);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowIfOddStreamPosition(Stream stream)
        {
            if (long.IsOddInteger(stream.Position))
            {
                ThrowOddStreamPosition();
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        private static void ThrowOddStreamPosition()
        {
            throw new InvalidOperationException("The current stream position is odd. The RIFF format requires 2 byte alignment.");
        }

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                }
                if (array.Length != 0)
                {
                    arrayPool.Return(array);
                    array = null!;
                }
                arrayPool = null!;

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                isDisposed = true;
            }
        }

        // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        ~WriteableCommonChunk()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
