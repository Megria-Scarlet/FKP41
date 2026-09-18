using System.IO;
using System.Runtime.CompilerServices;

namespace FKP41.Core.RIFF
{
    public class WriteableListChunk : IChunk
    {
        private readonly uint chunkId;
        private List<IChunk> chunks;
        protected uint listId;

        public WriteableListChunk(uint listId)
        {
            this.chunkId = RIFFWriter.GetListFourCC();
            this.chunks = new(4);
            this.listId = listId;
        }

        public uint ChunkId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => chunkId;
        }

        public uint ChunkSize
        {
            get
            {
                ReadOnlySpan<IChunk> chunks = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(this.chunks);
                ulong total = 4;
                for (int i = 0; i < chunks.Length; i++)
                {
                    IChunk chunk = chunks[i];
                    total += 8 + chunk.ChunkSize;
                    if (ulong.IsOddInteger(total))
                        total++;
                }
                return checked((uint)total);
            }
        }

        public uint ListId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => listId;
        }

        public void WriteChunk(Stream stream)
        {
            WriteableCommonChunk.ThrowIfOddStreamPosition(stream);
            WriteChunkHeadToStream(stream);

            ReadOnlySpan<IChunk> chunks = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(this.chunks);
            for (int i = 0; i < chunks.Length; i++)
            {
                IChunk chunk = chunks[i];
                if (chunk is WriteableListChunk writeableListChunk) // WriteableListChunk 型はパディング処理を行うため再帰処理。
                {
                    writeableListChunk.WriteChunk(stream);
                }
                else if (chunk.GetType() == typeof(WriteableCommonChunk))
                {
                    ((WriteableCommonChunk)chunk).WriteChunk(stream); // WriteableCommonChunk 型はパディング処理を行うため再帰処理。
                }
                else // パディング処理を確認しながら書き込み。
                {
                    WriteChunkSafe(stream, chunks[i]);
                }
            }
        }
        /// <summary>
        /// パディング処理を厳密にチェックをして、指定した <see cref="IChunk"/> 型のオブジェクトのデータを <see cref="Stream"/> に書き込みます。
        /// </summary>
        /// <param name="stream"><see cref="IChunk"/> 型のオブジェクトのデータを書き込む <see cref="Stream"/> 型のオブジェクト。</param>
        /// <param name="chunk"><paramref name="stream"/> にデータを書き込む <see cref="IChunk"/> 型のオブジェクト。</param>
        private static void WriteChunkSafe(Stream stream, IChunk chunk)
        {
            long pos = stream.Position;
            chunk.WriteChunk(stream);
            uint written = checked((uint)unchecked(stream.Position - pos));

            uint totalByteSize = GetTotalByteSize(chunk); // ChunkId, ChunkSize, 実データ, パディングを合算した byte 数。

            if (written < totalByteSize)
            {
                uint write = totalByteSize - written; // 書き込みしなければいけない byte 数。
                do
                {
                    stream.WriteByte(0);
                }
                while (--write > 0);
            }

            static uint GetTotalByteSize(IChunk chunk)
            {
                uint totalByteSize = chunk.ChunkSize;
                ArgumentOutOfRangeException.ThrowIfGreaterThan(totalByteSize, uint.MaxValue - 9);
                totalByteSize = chunk.ChunkSize + 8;

                if (uint.IsOddInteger(totalByteSize))
                    totalByteSize++;
                return totalByteSize;
            }
        }
        private void WriteChunkHeadToStream(Stream stream)
        {
            uint u = this.chunkId;
            Span<byte> src = System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref u), sizeof(uint));
            stream.Write(src);
            u = ChunkSize;
            if (!BitConverter.IsLittleEndian)
                u = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(u);
            stream.Write(src);
            u = BitConverter.IsLittleEndian ? listId : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(listId);
            stream.Write(src);
        }
    }
}
