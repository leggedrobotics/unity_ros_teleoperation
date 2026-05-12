using System;
using System.Collections.Generic;
using System.Text;

namespace Hjg.Pngcs.Chunks {


    /// <summary>Image Metadata, wrapper over a ChunksList</summary>
    /// <remarks>
    /// Additional image info, apart from the ImageInfo and the pixels themselves. 
    /// Includes Palette and ancillary chunks.
    /// This class provides a wrapper over the collection of chunks of a image (read or to write) and provides some high
    /// level methods to access them
    /// </remarks>
    public class PngMetadata {
        private readonly ChunksList chunkList;
        private readonly bool ReadOnly; // readonly

        internal PngMetadata(ChunksList chunks) {
            this.chunkList = chunks;
            if (chunks is ChunksListForWrite) {
                this.ReadOnly = false;
            } else {
                this.ReadOnly = true;
            }
        }

        /// <summary>Queues the chunk at the writer</summary>
        /// <param name="chunk">Chunk, ready for write</param>
        /// <param name="lazyOverwrite">Ovewrite lazily equivalent chunks</param>
        /// <remarks>Warning: the overwriting applies to equivalent chunks, see <c>ChunkPredicateEquiv</c>
        /// and will only make sense for queued (not yet writen) chunks
        /// </remarks>
        public void QueueChunk(PngChunk chunk, bool lazyOverwrite) {
            ChunksListForWrite cl = getChunkListW();
            if (ReadOnly)
                throw new PngjException("cannot set chunk : readonly metadata");
            if (lazyOverwrite) {
                ChunkHelper.TrimList(cl.GetQueuedChunks(), new ChunkPredicateEquiv(chunk));
            }
            cl.Queue(chunk);
        }

        /// <summary>Queues the chunk at the writer</summary>
        /// <param name="chunk">Chunk, ready for write</param>
        public void QueueChunk(PngChunk chunk) {
            QueueChunk(chunk, true);
        }

        private ChunksListForWrite getChunkListW() {
            return (ChunksListForWrite)chunkList;
        }

        // ///// high level utility methods follow ////////////

        // //////////// DPI
        /// <summary>
        /// Returns physical resolution, in DPI, in both coordinates
        /// </summary>
        /// <returns>[dpix,dpiy], -1 if not set or unknown dimensions</returns>
        public double[] GetDpi() {
            PngChunk c = chunkList.GetById1(ChunkHelper.pHYs, true);
            if (c == null)
                return new double[] { -1, -1 };
            else
                return ((PngChunkPHYS)c).GetAsDpi2();
        }

        /// <summary>
        /// Sets physical resolution, in DPI
        /// </summary>
        /// <remarks>This is a utility method that creates and enqueues a PHYS chunk</remarks>
        /// <param name="dpix">Resolution in x</param>
        /// <param name="dpiy">Resolution in y</param>
        public void SetDpi(double dpix, double dpiy) {
            PngChunkPHYS c = new PngChunkPHYS(chunkList.imageInfo);
            c.SetAsDpi2(dpix, dpiy);
            QueueChunk(c);
        }

        /// <summary>
        /// Sets physical resolution, in DPI, both value in x and y dimensions
        /// </summary>
        /// <remarks>This is a utility method that creates and enqueues a PHYS chunk</remarks>
        /// <param name="dpi">Resolution in dpi</param>
        public void SetDpi(double dpi) {
            SetDpi(dpi, dpi);
        }

        /// <summary>
        /// Creates a TIME chunk,  <c>nsecs</c> in the past from now.
        /// </summary>
        /// <param name="nsecs">Seconds in the past. If negative, it's a future time</param>
        /// <returns>The created and queued chunk</returns>
        public PngChunkTIME SetTimeNow(int nsecs) {
            PngChunkTIME c = new PngChunkTIME(chunkList.imageInfo);
            c.SetNow(nsecs);
            QueueChunk(c);
            return c;
        }

        /// <summary>
        ///Creates a TIME chunk with current time.
        /// </summary>
        /// <returns>The created and queued chunk</returns>
        public PngChunkTIME SetTimeNow() {
            return SetTimeNow(0);
        }

        /// <summary>
        /// Creates a TIME chunk with given date and time
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="mon">Month (1-12)</param>
        /// <param name="day">Day of month (1-31)</param>
        /// <param name="hour">Hour (0-23)</param>
        /// <param name="min">Minute (0-59)</param>
        /// <param name="sec">Seconds (0-59)</param>
        /// <returns>The created and queued chunk</returns>
        public PngChunkTIME SetTimeYMDHMS(int year, int mon, int day, int hour, int min, int sec) {
            PngChunkTIME c = new PngChunkTIME(chunkList.imageInfo);
            c.SetYMDHMS(year, mon, day, hour, min, sec);
            QueueChunk(c, true);
            return c;
        }

        /// <summary>
        /// Gets image timestamp, TIME chunk
        /// </summary>
        /// <returns>TIME chunk, null if not present</returns>
        public PngChunkTIME GetTime() {
            return (PngChunkTIME)chunkList.GetById1(ChunkHelper.tIME);
        }

        /// <summary>
        /// Gets image timestamp, TIME chunk, as a String
        /// </summary>
        /// <returns>Formated TIME, empty string if not present</returns>
        public String GetTimeAsString() {
            PngChunkTIME c = GetTime();
            return c == null ? "" : c.GetAsString();
        }

        // //////////// TEXT

        /// <summary>
        /// Creates a text chunk and enqueues it
        /// </summary>
        /// <param name="key">Key. Short and ASCII string</param>
        /// <param name="val">Text.</param>
        /// <param name="useLatin1">Flag. If false, will use UTF-8 (iTXt)</param>
        /// <param name="compress">Flag. Uses zTXt chunk.</param>
        /// <returns>The created and enqueued chunk</returns>
        public PngChunkTextVar SetText(String key, String val, bool useLatin1, bool compress) {
            if (compress && !useLatin1)
                throw new PngjException("cannot compress non latin text");
            PngChunkTextVar c;
            if (useLatin1) {
                if (compress) {
                    c = new PngChunkZTXT(chunkList.imageInfo);
                } else {
                    c = new PngChunkTEXT(chunkList.imageInfo);
                }
            } else {
                c = new PngChunkITXT(chunkList.imageInfo);
                ((PngChunkITXT)c).SetLangtag(key); // we use the same orig tag (this is not quite right)
            }
            c.SetKeyVal(key, val);
            QueueChunk(c, true);
            return c;
        }

        /// <summary>
        /// Creates a plain text chunk (tEXT) and enqueues it
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Text</param>
        /// <returns>The created and enqueued chunk</returns>
        public PngChunkTextVar SetText(String key, String val) {
            return SetText(key, val, false, false);
        }

        /// <summary>
        /// Retrieves all text chunks with a given key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Empty list if nothing found</returns>
        /// <remarks>Can mix tEXt zTXt and iTXt chunks</remarks>
        public List<PngChunkTextVar> GetTxtsForKey(String key) {
            List<PngChunkTextVar> li = new List<PngChunkTextVar>();
            foreach (PngChunk c in chunkList.GetById(ChunkHelper.tEXt, key))
                li.Add((PngChunkTextVar)c);
            foreach (PngChunk c in chunkList.GetById(ChunkHelper.zTXt, key))
                li.Add((PngChunkTextVar)c);
            foreach (PngChunk c in chunkList.GetById(ChunkHelper.iTXt, key))
                li.Add((PngChunkTextVar)c);
            return li;
        }

        /// <summary>
        /// Joins all strings for a given key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Concatenated (with newlines) if several found, empty string if none</returns>
        /// <remarks>You'd perhaps prefer GetTxtsForKey</remarks>
        public String GetTxtForKey(String key) {
            String t = "";
            List<PngChunkTextVar> li = GetTxtsForKey(key);
            if (li.Count == 0)
                return t;
            foreach (PngChunkTextVar c in li)
                t = t + c.GetVal() + "\n";
            return t.Trim();
        }

        public PngChunkPLTE GetPLTE() {
            return (PngChunkPLTE)chunkList.GetById1(PngChunkPLTE.ID);
        }

        public PngChunkPLTE CreatePLTEChunk() {
            PngChunkPLTE plte = new PngChunkPLTE(chunkList.imageInfo);
            QueueChunk(plte);
            return plte;
        }

        /**
         * Returns the TRNS chunk, if present
         * 
         * @return null if not present
         */
        public PngChunkTRNS GetTRNS() {
            return (PngChunkTRNS)chunkList.GetById1(PngChunkTRNS.ID);
        }

        /**
         * Creates a new empty TRNS chunk, queues it for write and return it to the caller, who should fill its entries
         */
        public PngChunkTRNS CreateTRNSChunk() {
            PngChunkTRNS trns = new PngChunkTRNS(chunkList.imageInfo);
            QueueChunk(trns);
            return trns;
        }
    }
}
