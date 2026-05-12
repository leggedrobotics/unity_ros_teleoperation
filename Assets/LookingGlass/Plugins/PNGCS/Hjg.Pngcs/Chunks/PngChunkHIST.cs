namespace Hjg.Pngcs.Chunks {

    using Hjg.Pngcs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;


    /// <summary>
    /// hIST chunk, see http://www.w3.org/TR/PNG/#11hIST
    /// Only for palette images
    /// </summary>
    public class PngChunkHIST : PngChunkSingle {
        public readonly static String ID = ChunkHelper.hIST;

        private int[] hist = new int[0]; // should have same lenght as palette

        public PngChunkHIST(ImageInfo info)
            : base(ID, info) { }

        public override ChunkOrderingConstraint GetOrderingConstraint() {
            return ChunkOrderingConstraint.AFTER_PLTE_BEFORE_IDAT;
        }

        public override ChunkRaw CreateRawChunk() {
            ChunkRaw c = null;
            if (!ImgInfo.Indexed)
                throw new PngjException("only indexed images accept a HIST chunk");

            c = createEmptyChunk(hist.Length * 2, true);
            for (int i = 0; i < hist.Length; i++) {
                PngHelperInternal.WriteInt2tobytes(hist[i], c.Data, i * 2);
            }
            return c;
        }

        public override void ParseFromRaw(ChunkRaw c) {
            if (!ImgInfo.Indexed)
                throw new PngjException("only indexed images accept a HIST chunk");
            int nentries = c.Data.Length / 2;
            hist = new int[nentries];
            for (int i = 0; i < hist.Length; i++) {
                hist[i] = PngHelperInternal.ReadInt2fromBytes(c.Data, i * 2);
            }
        }

        public override void CloneDataFromRead(PngChunk other) {
            PngChunkHIST otherx = (PngChunkHIST)other;
            hist = new int[otherx.hist.Length];
            System.Array.Copy((Array)(otherx.hist), 0, (Array)(this.hist), 0, otherx.hist.Length);
        }

        public int[] GetHist() {
            return hist;
        }
        /// <summary>
        /// should have same length as palette
        /// </summary>
        /// <param name="hist"></param>
        public void SetHist(int[] hist) {
            this.hist = hist;
        }

    }
}
