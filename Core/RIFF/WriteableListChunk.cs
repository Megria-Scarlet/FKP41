using System.IO;
using System.Collections;
using System.Runtime.CompilerServices;

namespace FKP41.Core.RIFF
{
    public class WriteableListChunk : IChunk, IReadOnlyDictionary<uint, IChunk>, IList<IChunk>
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

        IEnumerable<uint> IReadOnlyDictionary<uint, IChunk>.Keys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.chunks.Select(x => x.ChunkId);
        }

        IEnumerable<IChunk> IReadOnlyDictionary<uint, IChunk>.Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.chunks;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.chunks.Count;
        }

        public bool IsReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => false;
        }

        public IChunk this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.chunks[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.chunks[index] = value;
        }

        IChunk IReadOnlyDictionary<uint, IChunk>.this[uint key]
        {
            get
            {
                if (TryGetChunk(chunkId, out IChunk? chunk))
                {
                    return chunk;
                }
                throw new KeyNotFoundException();
            }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IReadOnlyDictionary<uint, IChunk>.ContainsKey(uint key) => this.chunks.Any(x => x.ChunkId == key);

        bool IReadOnlyDictionary<uint, IChunk>.TryGetValue(uint key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out IChunk value) => TryGetChunk(key, out value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<KeyValuePair<uint, IChunk>> IEnumerable<KeyValuePair<uint, IChunk>>.GetEnumerator()
        {
            return this.chunks.Select(x => new KeyValuePair<uint, IChunk>(x.ChunkId, x)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        public bool TryGetChunk(uint chunkId, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out IChunk value)
        {
            ReadOnlySpan<IChunk> chunks = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(this.chunks);
            for (int i = 0; i < chunks.Length; i++)
            {
                ref readonly IChunk chunk = ref chunks[i];
                if (chunk.ChunkId == chunkId)
                {
                    value = chunk;
                    return true;
                }
            }
            value = null;
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<IChunk> GetChunks(uint chunkId) => this.chunks.Where(x => x.ChunkId == chunkId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(IChunk item) => this.chunks.IndexOf(item);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, IChunk item) => this.chunks.Insert(index, item);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index) => this.chunks.RemoveAt(index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(IChunk item) => this.chunks.Add(item);
        /// <inheritdoc cref="List{T}.AddRange(IEnumerable{T})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(IEnumerable<IChunk> collection) => this.chunks.AddRange(collection);
        /// <summary>Adds the elements of the specified span to the end of the <see cref="List{T}"/>.</summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to which the elements should be added.</param>
        /// <param name="source">The span whose elements should be added to the end of the <see cref="List{T}"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="list"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRange(params ReadOnlySpan<IChunk> collection) => this.chunks.AddRange(collection);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => this.chunks.Clear();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(IChunk item) => this.chunks.Contains(item);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(IChunk[] array, int arrayIndex) => this.chunks.CopyTo(array, arrayIndex);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(IChunk item) => !this.chunks.Remove(item);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<IChunk> GetEnumerator() => this.chunks.GetEnumerator();
    }
}
