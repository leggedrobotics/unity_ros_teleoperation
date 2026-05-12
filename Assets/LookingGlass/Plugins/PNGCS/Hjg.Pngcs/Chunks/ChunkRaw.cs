namespace Hjg.Pngcs.Chunks {

    using Hjg.Pngcs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using Hjg.Pngcs.Zlib;
    
    /// <summary>
    /// Wraps the raw chunk data
    /// </summary>
    /// <remarks>
    /// Short lived object, to be created while
    /// serialing/deserializing 
    /// 
    /// Do not reuse it for different chunks
    /// 
    /// See http://www.libpng.org/pub/png/spec/1.2/PNG-Chunks.html
    ///</remarks>
    public class ChunkRaw {
        /// <summary>
        /// The length counts only the data field, not itself, the chunk type code, or the CRC. Zero is a valid length.
        /// Although encoders and decoders should treat the length as unsigned, its value must not exceed 2^31-1 bytes.
        /// </summary>
        public readonly int Length;
        /// <summary>
        /// Chunk Id, as array of 4 bytes
        /// </summary>
        public readonly byte[] IdBytes;
        /// <summary>
        /// Raw data, crc not included
        /// </summary>
        public byte[] Data;
        private int crcval;

        /// <summary>
        /// Creates an empty raw chunk
        /// </summary>
        /// <param name="length"></param>
        /// <param name="idbytes"></param>
        /// <param name="alloc">pre allocate the data buffer?</param>
        internal ChunkRaw(int length, byte[] idbytes, bool alloc) {
            this.IdBytes = new byte[4];
            this.Data = null;
            this.crcval = 0;
            this.Length = length;
            System.Array.Copy((Array)(idbytes), 0, (Array)(this.IdBytes), 0, 4);
            if (alloc)
                AllocData();
        }

        /// <summary>
        /// Called after setting data, before writing to os
        /// </summary>
        private int ComputeCrc() {
            CRC32 crcengine = Hjg.Pngcs.PngHelperInternal.GetCRC();
            crcengine.Reset();
            crcengine.Update(IdBytes, 0, 4);
            if (Length > 0)
                crcengine.Update(Data, 0, Length); //
            return (int)crcengine.GetValue();
        }


        internal void WriteChunk(Stream os) {
            if (IdBytes.Length != 4)
                throw new PngjOutputException("bad chunkid [" + Hjg.Pngcs.Chunks.ChunkHelper.ToString(IdBytes) + "]");
            crcval = ComputeCrc();
            Hjg.Pngcs.PngHelperInternal.WriteInt4(os, Length);
            Hjg.Pngcs.PngHelperInternal.WriteBytes(os, IdBytes);
            if (Length > 0)
                Hjg.Pngcs.PngHelperInternal.WriteBytes(os, Data, 0, Length);
            //Console.WriteLine("writing chunk " + this.ToString() + "crc=" + crcval);
            Hjg.Pngcs.PngHelperInternal.WriteInt4(os, crcval);
        }

        /// <summary>
        /// Position before: just after chunk id. positon after: after crc Data should
        /// be already allocated. Checks CRC Return number of byte read.
        /// </summary>
        ///
        internal int ReadChunkData(Stream stream, bool checkCrc) {
            Hjg.Pngcs.PngHelperInternal.ReadBytes(stream, Data, 0, Length);
            crcval = Hjg.Pngcs.PngHelperInternal.ReadInt4(stream);
            if (checkCrc) {
                int crc = ComputeCrc();
                if (crc != crcval)
                    throw new PngjBadCrcException("crc invalid for chunk " + ToString() + " calc="
                            + crc + " read=" + crcval);
            }
            return Length + 4;
        }

        internal MemoryStream GetAsByteStream() { // only the data
            return new MemoryStream(Data);
        }

        private void AllocData() {
            if (Data == null || Data.Length < Length)
                Data = new byte[Length];
        }
        /// <summary>
        /// Just id and length
        /// </summary>
        /// <returns></returns>
        public override String ToString() {
            return "chunkid=" + Hjg.Pngcs.Chunks.ChunkHelper.ToString(IdBytes) + " len=" + Length;
        }
    }
}
