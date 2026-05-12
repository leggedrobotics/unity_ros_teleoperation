namespace Hjg.Pngcs.Chunks {

    using Hjg.Pngcs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// sBIT chunk: http://www.w3.org/TR/PNG/#11sBIT
    /// 
    /// this chunk structure depends on the image type
    /// </summary>
    public class PngChunkSBIT : PngChunkSingle {
        public const String ID = ChunkHelper.sBIT;

        //	significant bits
        public int Graysb { get; set; }
        public int Alphasb { get; set; }
        public int Redsb { get; set; }
        public int Greensb { get; set; }
        public int Bluesb { get; set; }

        public PngChunkSBIT(ImageInfo info)
            : base(ID, info) {
        }


        public override ChunkOrderingConstraint GetOrderingConstraint() {
            return ChunkOrderingConstraint.BEFORE_PLTE_AND_IDAT;
        }


        public override void ParseFromRaw(ChunkRaw c) {
            if (c.Length != GetLen())
                throw new PngjException("bad chunk length " + c);
            if (ImgInfo.Greyscale) {
                Graysb = PngHelperInternal.ReadInt1fromByte(c.Data, 0);
                if (ImgInfo.Alpha)
                    Alphasb = PngHelperInternal.ReadInt1fromByte(c.Data, 1);
            } else {
                Redsb = PngHelperInternal.ReadInt1fromByte(c.Data, 0);
                Greensb = PngHelperInternal.ReadInt1fromByte(c.Data, 1);
                Bluesb = PngHelperInternal.ReadInt1fromByte(c.Data, 2);
                if (ImgInfo.Alpha)
                    Alphasb = PngHelperInternal.ReadInt1fromByte(c.Data, 3);
            }
        }

        public override ChunkRaw CreateRawChunk() {
            ChunkRaw c = null;
            c = createEmptyChunk(GetLen(), true);
            if (ImgInfo.Greyscale) {
                c.Data[0] = (byte)Graysb;
                if (ImgInfo.Alpha)
                    c.Data[1] = (byte)Alphasb;
            } else {
                c.Data[0] = (byte)Redsb;
                c.Data[1] = (byte)Greensb;
                c.Data[2] = (byte)Bluesb;
                if (ImgInfo.Alpha)
                    c.Data[3] = (byte)Alphasb;
            }
            return c;
        }


        public override void CloneDataFromRead(PngChunk other) {
            PngChunkSBIT otherx = (PngChunkSBIT)other;
            Graysb = otherx.Graysb;
            Redsb = otherx.Redsb;
            Greensb = otherx.Greensb;
            Bluesb = otherx.Bluesb;
            Alphasb = otherx.Alphasb;
        }

        private int GetLen() {
            int len = ImgInfo.Greyscale ? 1 : 3;
            if (ImgInfo.Alpha) len += 1;
            return len;
        }
    }
}
