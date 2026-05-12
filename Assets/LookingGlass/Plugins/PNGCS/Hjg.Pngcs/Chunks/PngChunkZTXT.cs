namespace Hjg.Pngcs.Chunks {

    using Hjg.Pngcs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    /// <summary>
    /// zTXt chunk: http://www.w3.org/TR/PNG/#11zTXt
    /// 
    /// </summary>
    public class PngChunkZTXT : PngChunkTextVar {
        public const String ID = ChunkHelper.zTXt;

        public PngChunkZTXT(ImageInfo info)
            : base(ID, info) {
        }

        public override ChunkRaw CreateRawChunk() {
            if (key.Length == 0)
                throw new PngjException("Text chunk key must be non empty");
            MemoryStream ba = new MemoryStream();
            ChunkHelper.WriteBytesToStream(ba, ChunkHelper.ToBytes(key));
            ba.WriteByte(0); // separator
            ba.WriteByte(0); // compression method: 0
            byte[] textbytes = ChunkHelper.compressBytes(ChunkHelper.ToBytes(val), true);
            ChunkHelper.WriteBytesToStream(ba, textbytes);
            byte[] b = ba.ToArray();
            ChunkRaw chunk = createEmptyChunk(b.Length, false);
            chunk.Data = b;
            return chunk;
        }

        public override void ParseFromRaw(ChunkRaw c) {
            int nullsep = -1;
            for (int i = 0; i < c.Data.Length; i++) { // look for first zero
                if (c.Data[i] != 0)
                    continue;
                nullsep = i;
                break;
            }
            if (nullsep < 0 || nullsep > c.Data.Length - 2)
                throw new PngjException("bad zTXt chunk: no separator found");
            key = ChunkHelper.ToString(c.Data, 0, nullsep);
            int compmet = (int)c.Data[nullsep + 1];
            if (compmet != 0)
                throw new PngjException("bad zTXt chunk: unknown compression method");
            byte[] uncomp = ChunkHelper.compressBytes(c.Data, nullsep + 2, c.Data.Length - nullsep - 2, false); // uncompress
            val = ChunkHelper.ToString(uncomp);
        }

        public override void CloneDataFromRead(PngChunk other) {
            PngChunkZTXT otherx = (PngChunkZTXT)other;
            key = otherx.key;
            val = otherx.val;
        }
    }
}
