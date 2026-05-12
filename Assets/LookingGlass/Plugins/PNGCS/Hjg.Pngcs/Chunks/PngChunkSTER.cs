namespace Hjg.Pngcs.Chunks {

    using Hjg.Pngcs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// sTER chunk: http://www.libpng.org/pub/png/spec/register/pngext-1.3.0-pdg.html#C.sTER
    /// </summary>
    public class PngChunkSTER : PngChunkSingle {
        public const String ID = "sTER";

        /// <summary>
        /// 0: cross-fuse layout 1: diverging-fuse layout
        /// </summary>
        public byte Mode { get; set; } 
        
        public PngChunkSTER(ImageInfo info)
            : base(ID, info) { }


        public override ChunkOrderingConstraint GetOrderingConstraint() {
            return ChunkOrderingConstraint.BEFORE_IDAT;
        }

        public override ChunkRaw CreateRawChunk() {
            ChunkRaw c = createEmptyChunk(1, true);
            c.Data[0] = (byte)Mode;
            return c;
        }

        public override void ParseFromRaw(ChunkRaw chunk) {
            if (chunk.Length != 1)
                throw new PngjException("bad chunk length " + chunk);
            Mode = chunk.Data[0];
        }

        public override void CloneDataFromRead(PngChunk other) {
            PngChunkSTER otherx = (PngChunkSTER)other;
            this.Mode = otherx.Mode;
        }
    }
}
