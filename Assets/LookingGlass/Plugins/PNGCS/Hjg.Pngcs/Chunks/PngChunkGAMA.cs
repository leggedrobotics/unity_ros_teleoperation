namespace Hjg.Pngcs.Chunks {

    using Hjg.Pngcs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// gAMA chunk, see http://www.w3.org/TR/PNG/#11gAMA
    /// </summary>
    public class PngChunkGAMA : PngChunkSingle {
        public const String ID = ChunkHelper.gAMA;

        private double gamma;

        public PngChunkGAMA(ImageInfo info)
            : base(ID, info) {
        }

        public override ChunkOrderingConstraint GetOrderingConstraint() {
            return ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT;
        }

        public override ChunkRaw CreateRawChunk() {
            ChunkRaw c = createEmptyChunk(4, true);
            int g = (int)(gamma * 100000 + 0.5d);
            Hjg.Pngcs.PngHelperInternal.WriteInt4tobytes(g, c.Data, 0);
            return c;
        }

        public override void ParseFromRaw(ChunkRaw chunk) {
            if (chunk.Length != 4)
                throw new PngjException("bad chunk " + chunk);
            int g = Hjg.Pngcs.PngHelperInternal.ReadInt4fromBytes(chunk.Data, 0);
            gamma = ((double)g) / 100000.0d;
        }

        public override void CloneDataFromRead(PngChunk other) {
            gamma = ((PngChunkGAMA)other).gamma;
        }

        public double GetGamma() {
            return gamma;
        }

        public void SetGamma(double gamma) {
            this.gamma = gamma;
        }
    }
}
