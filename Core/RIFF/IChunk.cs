using System;
using System.Collections.Generic;
using System.Text;

namespace FKP41.Core.RIFF
{
    public interface IChunk
    {
        public uint ChunkId { get; }
        public uint ChunkSize { get; }

        public void WriteChunk(System.IO.Stream stream);
    }
}
