using System;
using System.Collections.Generic;
using System.Text;

namespace FKP41.Core.RIFF
{
    public class RIFFWriter : IDisposable
    {
        protected System.IO.Stream stream;
        protected bool isLeaveOpen;
        protected bool isDisposed;

        protected bool isFlashed;

        public RIFFWriter(System.IO.Stream stream, bool isLeaveOpen)
        {
            this.stream = stream;
            this.isLeaveOpen = isLeaveOpen;
        }

        public void WriteChunk<TChunk>(TChunk chunk) where TChunk : IChunk
        {
            if (stream.Position == 0)
            {
                InitRIFFChunk();
            }
            chunk.WriteChunk(stream);
            isFlashed = false;
        }

        private void InitRIFFChunk()
        {
            stream.Write([0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0]);
            isFlashed = true;
        }

        public void Flush()
        {
            uint length = checked((uint)stream.Length);
            if (!BitConverter.IsLittleEndian)
            {
                length = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(length);
            }
            Span<byte> buffer = System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref System.Runtime.CompilerServices.Unsafe.As<uint, byte>(ref length), sizeof(uint));
            long pos = stream.Position;
            stream.Position = 4;
            stream.Write(buffer);
            stream.Position = pos;
            isFlashed = true;
        }

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (!isFlashed)
                {
                    Flush();
                }
                if (disposing)
                {
                    if (!isLeaveOpen)
                        stream.Dispose();
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                isDisposed = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~RIFFWriter()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
