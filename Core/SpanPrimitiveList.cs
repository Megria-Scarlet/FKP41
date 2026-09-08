using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace FKP41
{
    public ref struct SpanPrimitiveList<T> : IDisposable
        where T : unmanaged
    {
        private const int DefaultCapacity = 16;
        private Span<T> span;
        internal int count;
        private byte[]? array;
        private ArrayPool<byte> pool;

        /// <inheritdoc cref="SpanPrimitiveList{T}.SpanPrimitiveList(Span{T}, ArrayPool{byte})"/>
        public SpanPrimitiveList(Span<T> initializeBuffer) : this(initializeBuffer, ArrayPool<byte>.Shared)
        {

        }
        /// <summary>
        /// 指定した初期バッファーを内部で使用して、新しい
        /// <see cref="SpanPrimitiveList{T}"/> 型のオブジェクトを作成します。
        /// </summary>
        /// <param name="initializeBuffer">初期バッファー。</param>
        /// <param name="pool"><see cref="byte"/> 型の配列リソースを管理する <see cref="ArrayPool{T}"/> 型のオブジェクト。</param>
        public SpanPrimitiveList(Span<T> initializeBuffer, ArrayPool<byte> pool)
        {
            this.span = initializeBuffer;
            this.pool = pool;
            this.array = null;
            this.count = 0;
        }
        public SpanPrimitiveList(ArrayPool<byte> pool) : this([], pool)
        {

        }
        /// <summary>
        /// 指定した容量を確保して、新しい
        /// <see cref="SpanPrimitiveList{T}"/> 型のオブジェクトを作成します。
        /// </summary>
        /// <param name="capacity">初期容量。</param>
        /// <param name="pool"><typeparamref name="T"/> 型の配列リソースを管理する <see cref="ArrayPool{T}"/> 型のオブジェクト。</param>
        public SpanPrimitiveList(int capacity, ArrayPool<byte> pool)
        {
            this.pool = pool;
            int byteSize = checked(capacity * Unsafe.SizeOf<T>());
            array = pool.Rent(byteSize);
            span = MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), capacity);
        }

        /// <summary>
        /// 指定したインデックス位置にある要素を取得または設定します。
        /// </summary>
        /// <param name="index">取得または設定する要素の 0 から始まるインデックス位置。</param>
        /// <returns>指定したインデックス位置にある要素。</returns>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public readonly ref T this[int index] => ref AsSpan()[index];

        /// <summary>
        /// 現在の容量を取得します。
        /// </summary>
        /// <returns>現在の容量を示す 32 ビット符号付き整数。</returns>
        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => span.Length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ref T GetLastReference() => ref Unsafe.Add(ref MemoryMarshal.GetReference(span), count);

        /// <summary>
        /// 格納されている全ての要素を示す書き込み可能な <see cref="Span{T}"/> を取得します。
        /// </summary>
        /// <remarks>
        /// <see cref="Span{T}"/> の取得後にこのオブジェクトに変更を加えた場合、
        /// <see cref="Span{T}"/> は不正な領域を示します。
        /// </remarks>
        /// <returns>全ての要素を示す書き込み可能な <see cref="Span{T}"/> 。</returns>
        /// <exception cref="ObjectDisposedException"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan()
        {
            ThrowIfDisposed();
            return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), count);
        }

        /// <summary>
        /// 現在の <see cref="array"/> を <see cref="pool"/> に返却し、指定した配列を <see cref="array"/> に設定します。
        /// </summary>
        /// <param name="newArray"><see cref="array"/> に設定する新しい <typeparamref name="T"/> 型の配列。</param>
        private void Return(byte[]? newArray)
        {
            if (this.array is not null)
            {
                pool.Return(this.array);
            }
            this.array = newArray;
        }


        /// <summary>
        /// 必要な容量を指定して、現在の <see cref="Capacity"/> を拡張します。
        /// </summary>
        /// <param name="capacity">最低限確保する新しい容量サイズ。</param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="OutOfMemoryException"/>
        [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(array))]
        internal bool Grow(int capacity)
        {
            int oldCapacity;
            byte[] newArray;
            Span<T> newSpan;
            if (this.span.IsEmpty)
            {
                oldCapacity = 0;
                GrowRent(capacity, out newArray, out newSpan); // 破棄されている場合はここで ObjectDisposedException を throw
            }
            else
            {
                oldCapacity = span.Length;
                GrowRent(capacity, out newArray, out newSpan); // 破棄されている場合はここで ObjectDisposedException を throw
                this.span[..count].CopyTo(newSpan);
            }
            Return(newArray);
            this.span = newSpan;

#pragma warning disable CS8774 // 終了時にメンバーには null 以外の値が含まれている必要があります。
            return this.span.Length > oldCapacity;
#pragma warning restore CS8774 // 終了時にメンバーには null 以外の値が含まれている必要があります。
        }
        /// <summary>
        /// <see cref="pool"/> から指定した容量以上の配列を確保します。
        /// </summary>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="OutOfMemoryException"/>
        private readonly void GrowRent(int capacity, out byte[] newArray, out Span<T> newSpan)
        {
            ThrowIfDisposed();

            int newCapacity = span.Length == 0 ? DefaultCapacity : 2 * span.Length;

            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // リストがオーバーフローする前に、可能な限り最大の容量（約2G要素）まで拡張できるようにします。
            if (((ulong)newCapacity) * (ulong)Unsafe.SizeOf<T>() > (ulong)Array.MaxLength) newCapacity = (Array.MaxLength / Unsafe.SizeOf<T>());

            // If the computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
            // 計算された容量が指定値よりも小さい場合は、元の引数の値に戻します。
            // Array.Resize によって Array.MaxLength を超える容量が発生した場合、OutOfMemoryException としてエラーが発生します。
            if (newCapacity < capacity) newCapacity = capacity;

            int byteSize = checked(newCapacity * Unsafe.SizeOf<T>());
            newArray = pool.Rent(byteSize);
            newSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(newArray)), newCapacity);
        }

        /// <summary>
        /// 指定した <typeparamref name="T"/> 型のオブジェクトをこのリストの末尾に追加します。
        /// </summary>
        /// <param name="item">末尾に追加する <typeparamref name="T"/> 型のオブジェクト。</param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="OutOfMemoryException"/>
        public void Add(T item)
        {
            if (count + 1 > Capacity)
            {
                System.Diagnostics.Debug.Assert(Grow(-1)); // 破棄されている場合はここで ObjectDisposedException を throw
            }
            else
            {
                ThrowIfDisposed(); // 破棄されている場合はここで ObjectDisposedException を throw
            }
            GetLastReference() = item;
            count++;
        }

        public void AddRange(scoped ReadOnlySpan<T> items)
        {
            int i = count + items.Length;
            if (i > Capacity)
            {
                System.Diagnostics.Debug.Assert(Grow(i)); // 破棄されている場合はここで ObjectDisposedException を throw
            }
            else
            {
                ThrowIfDisposed(); // 破棄されている場合はここで ObjectDisposedException を throw
            }
            items.CopyTo(MemoryMarshal.CreateSpan(ref GetLastReference(), items.Length));
            count = i;
        }
        /// <inheritdoc cref="Span{T}.CopyTo(Span{T})"/>
        /// <exception cref="ObjectDisposedException"/>
        public readonly void CopyTo(scoped Span<T> destination) => AsSpan().CopyTo(destination);
        /// <inheritdoc cref="Span{T}.TryCopyTo(Span{T})"/>
        /// <exception cref="ObjectDisposedException"/>
        public readonly bool TryCopyTo(scoped Span<T> destination) => AsSpan().TryCopyTo(destination);

        /// <summary>
        /// 現在のリストから全ての要素を削除します。
        /// </summary>
        /// <exception cref="ObjectDisposedException"/>
        public void Clear()
        {
            ThrowIfDisposed();
            count = 0;
        }

        internal readonly Span<T> GetRemainderSpan() => MemoryMarshal.CreateSpan(ref GetLastReference(), span.Length - count);

        #region Dispose

        /// <summary>
        /// このオブジェクトが破棄されているかどうかを示す値を取得します。
        /// </summary>
        /// <returns>このオブジェクトが破棄されている場合は <see langword="true"/> 。それ以外の場合は <see langword="false"/> 。</returns>
        public readonly bool IsDisposed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => pool is null;
        }

        public void Dispose()
        {
            if (this.pool is not null)
            {
                Return(null);
                this.pool = null!;
            }
            this.count = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.StackTraceHidden]
        private readonly void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, typeof(SpanPrimitiveList<>));
        }
        #endregion
    }
    public static class SpanPrimitiveListEx
    {
        public static int WriteUtf8(this scoped ref SpanPrimitiveList<byte> list, scoped ReadOnlySpan<char> utf16)
        {
            int i = 0;

            do
            {
                Span<byte> buffer = list.GetRemainderSpan();
                OperationStatus status = System.Text.Unicode.Utf8.FromUtf16(utf16, buffer, out int charsRead, out int bytesWritten, true, true);
                i += bytesWritten;
                list.count += bytesWritten;
                utf16 = utf16[charsRead..];

                if (status == OperationStatus.DestinationTooSmall)
                {
                    System.Diagnostics.Debug.Assert(list.Grow(-1)); // 破棄されている場合はここで ObjectDisposedException を throw
                    continue;
                }
            }
            while (false);
            return i;
        }
        public static int WriteUtf16(this scoped ref SpanPrimitiveList<char> list, scoped ReadOnlySpan<byte> utf8)
        {
            int i = 0;

            do
            {
                Span<char> buffer = list.GetRemainderSpan();
                OperationStatus status = System.Text.Unicode.Utf8.ToUtf16(utf8, buffer, out int bytesRead, out int charsWritten, true, true);
                i += charsWritten;
                list.count += charsWritten;
                utf8 = utf8[bytesRead..];

                if (status == OperationStatus.DestinationTooSmall)
                {
                    System.Diagnostics.Debug.Assert(list.Grow(-1)); // 破棄されている場合はここで ObjectDisposedException を throw
                    continue;
                }
            }
            while (false);
            return i;
        }
    }
}
