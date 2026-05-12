namespace Hjg.Pngcs {

    using Hjg.Pngcs.Chunks;
    using Hjg.Pngcs.Zlib;
    using System.Collections.Generic;
    using System.IO;
    using System;
    


    /// <summary>
    /// Reads a PNG image, line by line
    /// </summary>
    /// <remarks>
    /// The typical reading sequence is as follows:
    /// 
    /// 1. At construction time, the header and IHDR chunk are read (basic image info)
    /// 
    /// 2  (Optional) you can set some global options: UnpackedMode CrcCheckDisabled
    /// 
    /// 3. (Optional) If you call GetMetadata() or or GetChunksLisk() before reading the pixels, the chunks before IDAT are automatically loaded and available
    /// 
    /// 4a. The rows are read, one by one, with the <tt>ReadRowXXX</tt> methods: (ReadRowInt() , ReadRowByte(), etc)
    /// in order, from 0 to nrows-1 (you can skip or repeat rows, but not go backwards)
    /// 
    /// 4b. Alternatively, you can read all rows, or a subset, in a single call: see ReadRowsInt(), ReadRowsByte()
	/// In general this consumes more memory, but for interlaced images this is equally efficient, and more so if reading a small subset of rows.
	///
    /// 5. Read of the last row automatically loads the trailing chunks, and ends the reader.
    /// 
    /// 6. End() forcibly finishes/aborts the reading and closes the stream
    ///
    /// </remarks>
    public class PngReader {
        /// <summary>
        /// Basic image info, inmutable
        /// </summary>
        public ImageInfo ImgInfo {get;private set;}

        /// <summary>
        /// filename, or description - merely informative, can be empty
        /// </summary>
        protected readonly String filename;

        /// <summary>
        /// Strategy for chunk loading. Default: LOAD_CHUNK_ALWAYS
        /// </summary>
        public ChunkLoadBehaviour ChunkLoadBehaviour { get; set; }

        /// <summary>
        /// Should close the underlying Input Stream when ends?
        /// </summary>
        public bool ShouldCloseStream { get; set; }

        /// <summary>
        /// Maximum amount of bytes from ancillary chunks to load in memory 
        /// </summary>
        /// <remarks>
        ///  Default: 5MB. 0: unlimited. If exceeded, chunks will be skipped
        /// </remarks>
        public long MaxBytesMetadata { get; set; }

        /// <summary>
        /// Maximum total bytes to read from stream 
        /// </summary>
        /// <remarks>
        ///  Default: 200MB. 0: Unlimited. If exceeded, an exception will be thrown
        /// </remarks>
        public long MaxTotalBytesRead { get; set; }


        /// <summary>
        /// Maximum ancillary chunk size
        /// </summary>
        /// <remarks>
        ///  Default: 2MB, 0: unlimited. Chunks exceeding this size will be skipped (nor even CRC checked)
        /// </remarks>
        public int SkipChunkMaxSize { get; set; }

        /// <summary>
        /// Ancillary chunks to skip
        /// </summary>
        /// <remarks>
        ///  Default: { "fdAT" }. chunks with these ids will be skipped (nor even CRC checked)
        /// </remarks>
        public String[] SkipChunkIds { get; set; }

        private Dictionary<string, int> skipChunkIdsSet = null; // lazily created

        /// <summary>
        /// A high level wrapper of a ChunksList : list of read chunks
        /// </summary>
        private readonly PngMetadata metadata;
        /// <summary>
        /// Read chunks
        /// </summary>
        private readonly ChunksList chunksList;

        /// <summary>
        /// buffer: last read line
        /// </summary>
        protected ImageLine imgLine;

        /// <summary>
        /// raw current row, as array of bytes,counting from 1 (index 0 is reserved for filter type)
        /// </summary>
        protected byte[] rowb;
        /// <summary>
        /// previuos raw row
        /// </summary>
        protected byte[] rowbprev; // rowb previous
        /// <summary>
        /// raw current row, after unfiltered
        /// </summary>
        protected byte[] rowbfilter;


        // only set for interlaced PNG
        public readonly bool interlaced;
        private readonly PngDeinterlacer deinterlacer;

        private bool crcEnabled = true;

        // this only influences the 1-2-4 bitdepth format
        private bool unpackedMode = false;

        /// <summary>
        /// number of chunk group (0-6) last read, or currently reading
        /// </summary>
        /// <remarks>see ChunksList.CHUNK_GROUP_NNN</remarks>
        public int CurrentChunkGroup { get; private set; }
        /// <summary>
        /// last read row number
        /// </summary>
        protected int rowNum = -1; // 
        private long offset = 0;  // offset in InputStream = bytes read
        private int bytesChunksLoaded = 0; // bytes loaded from anciallary chunks

        private readonly Stream inputStream;
        internal AZlibInputStream idatIstream;
        internal PngIDatChunkInputStream iIdatCstream;

        protected Adler32 crctest; // If set to non null, it gets a CRC of the unfiltered bytes, to check for images equality

        /// <summary>
        /// Constructs a PngReader from a Stream, with no filename information
        /// </summary>
        /// <param name="inputStream"></param>
        public PngReader(Stream inputStream)
            : this(inputStream, "[NO FILENAME AVAILABLE]") {
        }

        /// <summary>
        /// Constructs a PNGReader objet from a opened Stream
        /// </summary>
        /// <remarks>The constructor reads the signature and first chunk (IDHR)<seealso cref="FileHelper.CreatePngReader(string)"/>
        /// </remarks>
        /// 
        /// <param name="inputStream"></param>
        /// <param name="filename">Optional, can be the filename or a description.</param>
        public PngReader(Stream inputStream, String filename) {
            this.filename = (filename == null) ? "" : filename;
            this.inputStream = inputStream;
            this.chunksList = new ChunksList(null);
            this.metadata = new PngMetadata(chunksList);
            this.offset = 0;
            // set default options
            this.CurrentChunkGroup = -1;
            this.ShouldCloseStream = true;
            this.MaxBytesMetadata = 5 * 1024 * 1024;
            this.MaxTotalBytesRead = 200 * 1024 * 1024; // 200MB
            this.SkipChunkMaxSize = 2 * 1024 * 1024;
            this.SkipChunkIds = new string[] { "fdAT" };
            this.ChunkLoadBehaviour = Hjg.Pngcs.Chunks.ChunkLoadBehaviour.LOAD_CHUNK_ALWAYS;
            // starts reading: signature
            byte[] pngid = new byte[8];
            PngHelperInternal.ReadBytes(inputStream, pngid, 0, pngid.Length);
            offset += pngid.Length;
            if (!PngCsUtils.arraysEqual(pngid, PngHelperInternal.PNG_ID_SIGNATURE))
                throw new PngjInputException("Bad PNG signature");
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_0_IDHR;
            // reads first chunk IDHR
            int clen = PngHelperInternal.ReadInt4(inputStream);
            offset += 4;
            if (clen != 13)
                throw new Exception("IDHR chunk len != 13 ?? " + clen);
            byte[] chunkid = new byte[4];
            PngHelperInternal.ReadBytes(inputStream, chunkid, 0, 4);
            if (!PngCsUtils.arraysEqual4(chunkid, ChunkHelper.b_IHDR))
                throw new PngjInputException("IHDR not found as first chunk??? ["
                        + ChunkHelper.ToString(chunkid) + "]");
            offset += 4;
            PngChunkIHDR ihdr = (PngChunkIHDR)ReadChunk(chunkid, clen, false);
            bool alpha = (ihdr.Colormodel & 0x04) != 0;
            bool palette = (ihdr.Colormodel & 0x01) != 0;
            bool grayscale = (ihdr.Colormodel == 0 || ihdr.Colormodel == 4);
            // creates ImgInfo and imgLine, and allocates buffers
            ImgInfo = new ImageInfo(ihdr.Cols, ihdr.Rows, ihdr.Bitspc, alpha, grayscale, palette);
            rowb = new byte[ImgInfo.BytesPerRow + 1];
            rowbprev = new byte[rowb.Length];
            rowbfilter = new byte[rowb.Length];
            interlaced = ihdr.Interlaced == 1;
            deinterlacer = interlaced ? new PngDeinterlacer(ImgInfo) : null;
            // some checks
            if (ihdr.Filmeth != 0 || ihdr.Compmeth != 0 || (ihdr.Interlaced & 0xFFFE) != 0)
                throw new PngjInputException("compmethod or filtermethod or interlaced unrecognized");
            if (ihdr.Colormodel < 0 || ihdr.Colormodel > 6 || ihdr.Colormodel == 1
                    || ihdr.Colormodel == 5)
                throw new PngjInputException("Invalid colormodel " + ihdr.Colormodel);
            if (ihdr.Bitspc != 1 && ihdr.Bitspc != 2 && ihdr.Bitspc != 4 && ihdr.Bitspc != 8
                    && ihdr.Bitspc != 16)
                throw new PngjInputException("Invalid bit depth " + ihdr.Bitspc);
        }


        private bool FirstChunksNotYetRead() {
            return CurrentChunkGroup < ChunksList.CHUNK_GROUP_1_AFTERIDHR;
        }


        /// <summary>
        /// Internally called after having read the last line. 
        /// It reads extra chunks after IDAT, if present.
        /// </summary>
        private void ReadLastAndClose() {
            if (CurrentChunkGroup < ChunksList.CHUNK_GROUP_5_AFTERIDAT) {
                try {
                    idatIstream.Close();
                } catch (Exception ) { }
                ReadLastChunks();
            }
            Close();
        }

        private void Close() {
            if (CurrentChunkGroup < ChunksList.CHUNK_GROUP_6_END) { // this could only happen if forced close
                try {
                    idatIstream.Close();
                } catch (Exception ) {
                }
                CurrentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
            }
            if (ShouldCloseStream)
                inputStream.Close();
        }


        private void UnfilterRow(int nbytes) {
            int ftn = rowbfilter[0];
            FilterType ft = (FilterType)ftn;
            switch (ft) {
                case Hjg.Pngcs.FilterType.FILTER_NONE:
                    UnfilterRowNone(nbytes);
                    break;
                case Hjg.Pngcs.FilterType.FILTER_SUB:
                    UnfilterRowSub(nbytes);
                    break;
                case Hjg.Pngcs.FilterType.FILTER_UP:
                    UnfilterRowUp(nbytes);
                    break;
                case Hjg.Pngcs.FilterType.FILTER_AVERAGE:
                    UnfilterRowAverage(nbytes);
                    break;
                case Hjg.Pngcs.FilterType.FILTER_PAETH:
                    UnfilterRowPaeth(nbytes);
                    break;
                default:
                    throw new PngjInputException("Filter type " + ftn + " not implemented");
            }
            if (crctest != null)
                crctest.Update(rowb, 1, nbytes);
        }


        private void UnfilterRowAverage(int nbytes) {
            int i, j, x;
            for (j = 1 - ImgInfo.BytesPixel, i = 1; i <= nbytes; i++, j++) {
                x = (j > 0) ? rowb[j] : 0;
                rowb[i] = (byte)(rowbfilter[i] + (x + (rowbprev[i] & 0xFF)) / 2);
            }
        }

        private void UnfilterRowNone(int nbytes) {
            for (int i = 1; i <= nbytes; i++) {
                rowb[i] = (byte)(rowbfilter[i]);
            }
        }

        private void UnfilterRowPaeth(int nbytes) {
            int i, j, x, y;
            for (j = 1 - ImgInfo.BytesPixel, i = 1; i <= nbytes; i++, j++) {
                x = (j > 0) ? rowb[j] : 0;
                y = (j > 0) ? rowbprev[j] : 0;
                rowb[i] = (byte)(rowbfilter[i] + PngHelperInternal.FilterPaethPredictor(x, rowbprev[i], y));
            }
        }

        private void UnfilterRowSub(int nbytes) {
            int i, j;
            for (i = 1; i <= ImgInfo.BytesPixel; i++) {
                rowb[i] = (byte)(rowbfilter[i]);
            }
            for (j = 1, i = ImgInfo.BytesPixel + 1; i <= nbytes; i++, j++) {
                rowb[i] = (byte)(rowbfilter[i] + rowb[j]);
            }
        }

        private void UnfilterRowUp(int nbytes) {
            for (int i = 1; i <= nbytes; i++) {
                rowb[i] = (byte)(rowbfilter[i] + rowbprev[i]);
            }
        }



        /// <summary>
        /// Reads chunks before first IDAT. Position before: after IDHR (crc included)
        /// Position after: just after the first IDAT chunk id Returns length of first
        /// IDAT chunk , -1 if not found
        /// </summary>
        ///
        private void ReadFirstChunks() {
            if (!FirstChunksNotYetRead())
                return;
            int clen = 0;
            bool found = false;
            byte[] chunkid = new byte[4]; // it's important to reallocate in each
            this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
            while (!found) {
                clen = PngHelperInternal.ReadInt4(inputStream);
                offset += 4;
                if (clen < 0)
                    break;
                PngHelperInternal.ReadBytes(inputStream, chunkid, 0, 4);
                offset += 4;
                if (PngCsUtils.arraysEqual4(chunkid, Hjg.Pngcs.Chunks.ChunkHelper.b_IDAT)) {
                    found = true;
                    this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_4_IDAT;
                    // add dummy idat chunk to list
                    chunksList.AppendReadChunk(new PngChunkIDAT(ImgInfo, clen, offset - 8), CurrentChunkGroup);
                    break;
                } else if (PngCsUtils.arraysEqual4(chunkid, Hjg.Pngcs.Chunks.ChunkHelper.b_IEND)) {
                    throw new PngjInputException("END chunk found before image data (IDAT) at offset=" + offset);
                }
                String chunkids = ChunkHelper.ToString(chunkid);
                if (chunkids.Equals(ChunkHelper.PLTE))
                    this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_2_PLTE;
                ReadChunk(chunkid, clen, false);
                if (chunkids.Equals(ChunkHelper.PLTE))
                    this.CurrentChunkGroup = ChunksList.CHUNK_GROUP_3_AFTERPLTE;
            }
            int idatLen = found ? clen : -1;
            if (idatLen < 0)
                throw new PngjInputException("first idat chunk not found!");
            iIdatCstream = new PngIDatChunkInputStream(inputStream, idatLen, offset);
            idatIstream = ZlibStreamFactory.createZlibInputStream(iIdatCstream, true);
            if (!crcEnabled)
                iIdatCstream.DisableCrcCheck();
        }

        /// <summary>
        /// Reads (and processes ... up to a point) chunks after last IDAT.
        /// </summary>
        ///
        private void ReadLastChunks() {
            CurrentChunkGroup = ChunksList.CHUNK_GROUP_5_AFTERIDAT;
            // PngHelper.logdebug("idat ended? " + iIdatCstream.isEnded());
            if (!iIdatCstream.IsEnded())
                iIdatCstream.ForceChunkEnd();
            int clen = iIdatCstream.GetLenLastChunk();
            byte[] chunkid = iIdatCstream.GetIdLastChunk();
            bool endfound = false;
            bool first = true;
            bool skip = false;
            while (!endfound) {
                skip = false;
                if (!first) {
                    clen = PngHelperInternal.ReadInt4(inputStream);
                    offset += 4;
                    if (clen < 0)
                        throw new PngjInputException("bad len " + clen);
                    PngHelperInternal.ReadBytes(inputStream, chunkid, 0, 4);
                    offset += 4;
                }
                first = false;
                if (PngCsUtils.arraysEqual4(chunkid, ChunkHelper.b_IDAT)) {
                    skip = true; // extra dummy (empty?) idat chunk, it can happen, ignore it
                } else if (PngCsUtils.arraysEqual4(chunkid, ChunkHelper.b_IEND)) {
                    CurrentChunkGroup = ChunksList.CHUNK_GROUP_6_END;
                    endfound = true;
                }
                ReadChunk(chunkid, clen, skip);
            }
            if (!endfound)
                throw new PngjInputException("end chunk not found - offset=" + offset);
            // PngHelper.logdebug("end chunk found ok offset=" + offset);
        }

        /// <summary>
        /// Reads chunkd from input stream, adds to ChunksList, and returns it.
        /// If it's skipped, a PngChunkSkipped object is created
        /// </summary>
        /// <returns></returns>
        private PngChunk ReadChunk(byte[] chunkid, int clen, bool skipforced) {
            if (clen < 0) throw new PngjInputException("invalid chunk lenght: " + clen);
            // skipChunksByIdSet is created lazyly, if fist IHDR has already been read
            if (skipChunkIdsSet == null && CurrentChunkGroup > ChunksList.CHUNK_GROUP_0_IDHR) {
                skipChunkIdsSet = new Dictionary<string, int>();
                if (SkipChunkIds != null)
                    foreach (string id in SkipChunkIds) skipChunkIdsSet.Add(id, 1);
            }

            String chunkidstr = ChunkHelper.ToString(chunkid);
            PngChunk pngChunk = null;
            bool critical = ChunkHelper.IsCritical(chunkidstr);
            bool skip = skipforced;
            if (MaxTotalBytesRead > 0 && clen + offset > MaxTotalBytesRead)
                throw new PngjInputException("Maximum total bytes to read exceeeded: " + MaxTotalBytesRead + " offset:"
                        + offset + " clen=" + clen);
            // an ancillary chunks can be skipped because of several reasons:
            if (CurrentChunkGroup > ChunksList.CHUNK_GROUP_0_IDHR && !ChunkHelper.IsCritical(chunkidstr))
                skip = skip || (SkipChunkMaxSize > 0 && clen >= SkipChunkMaxSize) || skipChunkIdsSet.ContainsKey(chunkidstr)
                        || (MaxBytesMetadata > 0 && clen > MaxBytesMetadata - bytesChunksLoaded)
                        || !ChunkHelper.ShouldLoad(chunkidstr, ChunkLoadBehaviour);

            if (skip) {
                PngHelperInternal.SkipBytes(inputStream, clen);
                PngHelperInternal.ReadInt4(inputStream); // skip - we dont call PngHelperInternal.skipBytes(inputStream, clen + 4) for risk of overflow 
                pngChunk = new PngChunkSkipped(chunkidstr, ImgInfo, clen);
            } else {
                ChunkRaw chunk = new ChunkRaw(clen, chunkid, true);
                chunk.ReadChunkData(inputStream, crcEnabled || critical);
                pngChunk = PngChunk.Factory(chunk, ImgInfo);
                if (!pngChunk.Crit) {
                    bytesChunksLoaded += chunk.Length;
                }

            }
            pngChunk.Offset = offset - 8L;
            chunksList.AppendReadChunk(pngChunk, CurrentChunkGroup);
            offset += clen + 4L;
            return pngChunk;
        }

        /// <summary>
        /// Logs/prints a warning.
        /// </summary>
        /// <remarks>
        /// The default behaviour is print to stderr, but it can be overriden.
        /// This happens rarely - most errors are fatal.
        /// </remarks>
        /// <param name="warn"></param>
        internal void logWarn(String warn) {
            Console.Error.WriteLine(warn);
        }

        /// <summary>
        /// Returns the ancillary chunks available
        /// </summary>
        /// <remarks>
        /// If the rows have not yet still been read, this includes
        /// only the chunks placed before the pixels (IDAT)
        /// </remarks>
        /// <returns>ChunksList</returns>
        public ChunksList GetChunksList() {
            if (FirstChunksNotYetRead())
                ReadFirstChunks();
            return chunksList;
        }

        /// <summary>
        /// Returns the ancillary chunks available
        /// </summary>
        /// <remarks>
        /// see GetChunksList
        /// </remarks>
        /// <returns>PngMetadata</returns>
        public PngMetadata GetMetadata() {
            if (FirstChunksNotYetRead())
                ReadFirstChunks();
            return metadata;
        }

        /// <summary>
        /// reads the row using ImageLine as buffer
        /// </summary>
        ///<param name="nrow">row number - just as a check</param>
        /// <returns>the ImageLine that also is available inside this object</returns>
        public ImageLine ReadRow(int nrow) {
            return imgLine == null || imgLine.SampleType != ImageLine.ESampleType.BYTE ? ReadRowInt(nrow) : ReadRowByte(nrow);
        }

        public ImageLine ReadRowInt(int nrow) {
            if (imgLine == null)
                imgLine = new ImageLine(ImgInfo, ImageLine.ESampleType.INT, unpackedMode);
            if (imgLine.Rown == nrow) // already read
                return imgLine;
            ReadRowInt(imgLine.Scanline, nrow);
            imgLine.FilterUsed = (FilterType)rowbfilter[0];
            imgLine.Rown = nrow;
            return imgLine;
        }

        public ImageLine ReadRowByte(int nrow) {
            if (imgLine == null)
                imgLine = new ImageLine(ImgInfo, ImageLine.ESampleType.BYTE, unpackedMode);
            if (imgLine.Rown == nrow) // already read
                return imgLine;
            ReadRowByte(imgLine.ScanlineB, nrow);
            imgLine.FilterUsed = (FilterType)rowbfilter[0];
            imgLine.Rown = nrow;
            return imgLine;
        }

        public int[] ReadRow(int[] buffer, int nrow) {
            return ReadRowInt(buffer, nrow);
        }

        public int[] ReadRowInt(int[] buffer, int nrow) {
            if (buffer == null)
                buffer = new int[unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked];
            if (!interlaced) {
                if (nrow <= rowNum)
                    throw new PngjInputException("rows must be read in increasing order: " + nrow);
                int bytesread = 0;
                while (rowNum < nrow)
                    bytesread = ReadRowRaw(rowNum + 1); // read rows, perhaps skipping if necessary
                decodeLastReadRowToInt(buffer, bytesread);
            } else { // interlaced
                if (deinterlacer.getImageInt() == null)
                    deinterlacer.setImageInt(ReadRowsInt().Scanlines); // read all image and store it in deinterlacer
                Array.Copy(deinterlacer.getImageInt()[nrow], 0, buffer, 0, unpackedMode ? ImgInfo.SamplesPerRow
                        : ImgInfo.SamplesPerRowPacked);
            }
            return buffer;
        }

        public byte[] ReadRowByte(byte[] buffer, int nrow) {
            if (buffer == null)
                buffer = new byte[unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked];
            if (!interlaced) {
                if (nrow <= rowNum)
                    throw new PngjInputException("rows must be read in increasing order: " + nrow);
                int bytesread = 0;
                while (rowNum < nrow)
                    bytesread = ReadRowRaw(rowNum + 1); // read rows, perhaps skipping if necessary
                decodeLastReadRowToByte(buffer, bytesread);
            } else { // interlaced
                if (deinterlacer.getImageByte() == null)
                    deinterlacer.setImageByte(ReadRowsByte().ScanlinesB); // read all image and store it in deinterlacer
                Array.Copy(deinterlacer.getImageByte()[nrow], 0, buffer, 0, unpackedMode ? ImgInfo.SamplesPerRow
                        : ImgInfo.SamplesPerRowPacked);
            }
            return buffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nrow"></param>
        /// <returns></returns>
        [Obsolete("GetRow is deprecated,  use ReadRow/ReadRowInt/ReadRowByte instead.")]
        public ImageLine GetRow(int nrow) {
            return ReadRow(nrow);
        }

        private void decodeLastReadRowToInt(int[] buffer, int bytesRead) {            // see http://www.libpng.org/pub/png/spec/1.2/PNG-DataRep.html
            if (ImgInfo.BitDepth <= 8) {
                for (int i = 0, j = 1; i < bytesRead; i++)
                    buffer[i] = (rowb[j++]);
            } else { // 16 bitspc
                for (int i = 0, j = 1; j < bytesRead; i++)
                    buffer[i] = (rowb[j++] << 8) + rowb[j++];
            }
            if (ImgInfo.Packed && unpackedMode)
                ImageLine.unpackInplaceInt(ImgInfo, buffer, buffer, false);
        }

        private void decodeLastReadRowToByte(byte[] buffer, int bytesRead) {            // see http://www.libpng.org/pub/png/spec/1.2/PNG-DataRep.html
            if (ImgInfo.BitDepth <= 8) {
                Array.Copy(rowb, 1, buffer, 0, bytesRead);
            } else { // 16 bitspc
                for (int i = 0, j = 1; j < bytesRead; i++, j += 2)
                    buffer[i] = rowb[j]; // 16 bits in 1 byte: this discards the LSB!!!
            }
            if (ImgInfo.Packed && unpackedMode)
                ImageLine.unpackInplaceByte(ImgInfo, buffer, buffer, false);
        }


        public ImageLines ReadRowsInt(int rowOffset, int nRows, int rowStep) {
            if (nRows < 0)
                nRows = (ImgInfo.Rows - rowOffset) / rowStep;
            if (rowStep < 1 || rowOffset < 0 || nRows * rowStep + rowOffset > ImgInfo.Rows)
                throw new PngjInputException("bad args");
            ImageLines imlines = new ImageLines(ImgInfo, ImageLine.ESampleType.INT, unpackedMode, rowOffset, nRows, rowStep);
            if (!interlaced) {
                for (int j = 0; j < ImgInfo.Rows; j++) {
                    int bytesread = ReadRowRaw(j); // read and perhaps discards
                    int mrow = imlines.ImageRowToMatrixRowStrict(j);
                    if (mrow >= 0)
                        decodeLastReadRowToInt(imlines.Scanlines[mrow], bytesread);
                }
            } else { // and now, for something completely different (interlaced)
                int[] buf = new int[unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked];
                for (int p = 1; p <= 7; p++) {
                    deinterlacer.setPass(p);
                    for (int i = 0; i < deinterlacer.getRows(); i++) {
                        int bytesread = ReadRowRaw(i);
                        int j = deinterlacer.getCurrRowReal();
                        int mrow = imlines.ImageRowToMatrixRowStrict(j);
                        if (mrow >= 0) {
                            decodeLastReadRowToInt(buf, bytesread);
                            deinterlacer.deinterlaceInt(buf, imlines.Scanlines[mrow], !unpackedMode);
                        }
                    }
                }
            }
            End();
            return imlines;
        }

        public ImageLines ReadRowsInt() {
            return ReadRowsInt(0, ImgInfo.Rows, 1);
        }

        public ImageLines ReadRowsByte(int rowOffset, int nRows, int rowStep) {
            if (nRows < 0)
                nRows = (ImgInfo.Rows - rowOffset) / rowStep;
            if (rowStep < 1 || rowOffset < 0 || nRows * rowStep + rowOffset > ImgInfo.Rows)
                throw new PngjInputException("bad args");
            ImageLines imlines = new ImageLines(ImgInfo, ImageLine.ESampleType.BYTE, unpackedMode, rowOffset, nRows, rowStep);
            if (!interlaced) {
                for (int j = 0; j < ImgInfo.Rows; j++) {
                    int bytesread = ReadRowRaw(j); // read and perhaps discards
                    int mrow = imlines.ImageRowToMatrixRowStrict(j);
                    if (mrow >= 0)
                        decodeLastReadRowToByte(imlines.ScanlinesB[mrow], bytesread);
                }
            } else { // and now, for something completely different (interlaced)
                byte[] buf = new byte[unpackedMode ? ImgInfo.SamplesPerRow : ImgInfo.SamplesPerRowPacked];
                for (int p = 1; p <= 7; p++) {
                    deinterlacer.setPass(p);
                    for (int i = 0; i < deinterlacer.getRows(); i++) {
                        int bytesread = ReadRowRaw(i);
                        int j = deinterlacer.getCurrRowReal();
                        int mrow = imlines.ImageRowToMatrixRowStrict(j);
                        if (mrow >= 0) {
                            decodeLastReadRowToByte(buf, bytesread);
                            deinterlacer.deinterlaceByte(buf, imlines.ScanlinesB[mrow], !unpackedMode);
                        }
                    }
                }
            }
            End();
            return imlines;
        }

        public ImageLines ReadRowsByte() {
            return ReadRowsByte(0, ImgInfo.Rows, 1);
        }

        private int ReadRowRaw(int nrow) {
            //
            if (nrow == 0 && FirstChunksNotYetRead())
                ReadFirstChunks();
            if (nrow == 0 && interlaced)
                Array.Clear(rowb, 0, rowb.Length); // new subimage: reset filters: this is enough, see the swap that happens lines
            // below
            int bytesRead = ImgInfo.BytesPerRow; // NOT including the filter byte
            if (interlaced) {
                if (nrow < 0 || nrow > deinterlacer.getRows() || (nrow != 0 && nrow != deinterlacer.getCurrRowSubimg() + 1))
                    throw new PngjInputException("invalid row in interlaced mode: " + nrow);
                deinterlacer.setRow(nrow);
                bytesRead = (ImgInfo.BitspPixel * deinterlacer.getPixelsToRead() + 7) / 8;
                if (bytesRead < 1)
                    throw new PngjExceptionInternal("wtf??");
            } else { // check for non interlaced
                if (nrow < 0 || nrow >= ImgInfo.Rows || nrow != rowNum + 1)
                    throw new PngjInputException("invalid row: " + nrow);
            }
            rowNum = nrow;
            // swap buffers
            byte[] tmp = rowb;
            rowb = rowbprev;
            rowbprev = tmp;
            // loads in rowbfilter "raw" bytes, with filter
            PngHelperInternal.ReadBytes(idatIstream, rowbfilter, 0, bytesRead + 1);
            offset = iIdatCstream.GetOffset();
            if (offset < 0)
                throw new PngjExceptionInternal("bad offset ??" + offset);
            if (MaxTotalBytesRead > 0 && offset >= MaxTotalBytesRead)
                throw new PngjInputException("Reading IDAT: Maximum total bytes to read exceeeded: " + MaxTotalBytesRead
                        + " offset:" + offset);
            rowb[0] = 0;
            UnfilterRow(bytesRead);
            rowb[0] = rowbfilter[0];
            if ((rowNum == ImgInfo.Rows - 1 && !interlaced) || (interlaced && deinterlacer.isAtLastRow()))
                ReadLastAndClose();
            return bytesRead;
        }

        public void ReadSkippingAllRows() {
            if (FirstChunksNotYetRead())
                ReadFirstChunks();
            // we read directly from the compressed stream, we dont decompress nor chec CRC
            iIdatCstream.DisableCrcCheck();
            try {
                int r;
                do {
                    r = iIdatCstream.Read(rowbfilter, 0, rowbfilter.Length);
                } while (r >= 0);
            } catch (IOException e) {
                throw new PngjInputException("error in raw read of IDAT", e);
            }
            offset = iIdatCstream.GetOffset();
            if (offset < 0)
                throw new PngjExceptionInternal("bad offset ??" + offset);
            if (MaxTotalBytesRead > 0 && offset >= MaxTotalBytesRead)
                throw new PngjInputException("Reading IDAT: Maximum total bytes to read exceeeded: " + MaxTotalBytesRead
                        + " offset:" + offset);
            ReadLastAndClose();
        }


        public override String ToString() { // basic info
            return "filename=" + filename + " " + ImgInfo.ToString();
        }
        /// <summary>
        /// Normally this does nothing, but it can be used to force a premature closing
        /// </summary>
        /// <remarks></remarks>
        public void End() {
            if (CurrentChunkGroup < ChunksList.CHUNK_GROUP_6_END)
                Close();
        }

        public bool IsInterlaced() {
            return interlaced;
        }

        public void SetUnpackedMode(bool unPackedMode) {
            this.unpackedMode = unPackedMode;
        }

        /**
         * @see PngReader#setUnpackedMode(boolean)
         */
        public bool IsUnpackedMode() {
            return unpackedMode;
        }

        public void SetCrcCheckDisabled() {
            crcEnabled = false;
        }

        internal long GetCrctestVal() {
            return crctest.GetValue();
        }

        internal void InitCrctest() {
            this.crctest = new Adler32();
        }

    }
}
