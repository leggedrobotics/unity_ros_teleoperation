namespace Hjg.Pngcs {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Hjg.Pngcs.Zlib;

    /// <summary>
    /// Some utility static methods for internal use.
    /// </summary>
    public class PngHelperInternal {

        [ThreadStatic]
        private static CRC32 crc32Engine = null;

        /// <summary>
        /// thread-singleton crc engine 
        /// </summary>
        ///
        public static CRC32 GetCRC() {
            if (crc32Engine == null) crc32Engine = new CRC32();
            return crc32Engine;
        }

        public static readonly byte[] PNG_ID_SIGNATURE = { 256 - 119, 80, 78, 71, 13, 10, 26, 10 }; // png magic

        public static Encoding charsetLatin1 = System.Text.Encoding.GetEncoding("ISO-8859-1"); // charset
        public static Encoding charsetUtf8 = System.Text.Encoding.GetEncoding("UTF-8"); // charset used for some chunks

        public static bool DEBUG = false;

        public static int DoubleToInt100000(double d) {
            return (int)(d * 100000.0 + 0.5);
        }

        public static double IntToDouble100000(int i) {
            return i / 100000.0;
        }

        public static void WriteInt2(Stream os, int n) {
            byte[] temp = { (byte)((n >> 8) & 0xff), (byte)(n & 0xff) };
            WriteBytes(os, temp);
        }

        /// <summary>
        /// -1 si eof
        /// </summary>
        ///
        public static int ReadInt2(Stream mask0) {
            try {
                int b1 = mask0.ReadByte();
                int b2 = mask0.ReadByte();
                if (b1 == -1 || b2 == -1)
                    return -1;
                return (b1 << 8) + b2;
            } catch (IOException e) {
                throw new PngjInputException("error reading readInt2", e);
            }
        }

        /// <summary>
        /// -1 si eof
        /// </summary>
        ///
        public static int ReadInt4(Stream mask0) {
            try {
                int b1 = mask0.ReadByte();
                int b2 = mask0.ReadByte();
                int b3 = mask0.ReadByte();
                int b4 = mask0.ReadByte();
                if (b1 == -1 || b2 == -1 || b3 == -1 || b4 == -1)
                    return -1;
                return (b1 << 24) + (b2 << 16) + (b3 << 8) + b4;
            } catch (IOException e) {
                throw new PngjInputException("error reading readInt4", e);
            }
        }

        public static int ReadInt1fromByte(byte[] b, int offset) {
            return (b[offset] & 0xff);
        }

        public static int ReadInt2fromBytes(byte[] b, int offset) {
            return ((b[offset] & 0xff) << 16) | ((b[offset + 1] & 0xff));
        }

        public static int ReadInt4fromBytes(byte[] b, int offset) {
            return ((b[offset] & 0xff) << 24) | ((b[offset + 1] & 0xff) << 16)
                    | ((b[offset + 2] & 0xff) << 8) | (b[offset + 3] & 0xff);
        }

        public static void WriteInt2tobytes(int n, byte[] b, int offset) {
            b[offset] = (byte)((n >> 8) & 0xff);
            b[offset + 1] = (byte)(n & 0xff);
        }

        public static void WriteInt4tobytes(int n, byte[] b, int offset) {
            b[offset] = (byte)((n >> 24) & 0xff);
            b[offset + 1] = (byte)((n >> 16) & 0xff);
            b[offset + 2] = (byte)((n >> 8) & 0xff);
            b[offset + 3] = (byte)(n & 0xff);
        }

        public static void WriteInt4(Stream os, int n) {
            byte[] temp = new byte[4];
            WriteInt4tobytes(n, temp, 0);
            WriteBytes(os, temp);
            //Console.WriteLine("writing int " + n + " b=" + (sbyte)temp[0] + "," + (sbyte)temp[1] + "," + (sbyte)temp[2] + "," + (sbyte)temp[3]);
        }

        /// <summary>
        /// guaranteed to read exactly len bytes. throws error if it cant
        /// </summary>
        ///
        public static void ReadBytes(Stream mask0, byte[] b, int offset, int len) {
            if (len == 0)
                return;
            try {
                int read = 0;
                while (read < len) {
                    int n = mask0.Read(b, offset + read, len - read);
                    if (n < 1)
                        throw new Exception("error reading, " + n + " !=" + len);
                    read += n;
                }
            } catch (IOException e) {
                throw new PngjInputException("error reading", e);
            }
        }

        public static void SkipBytes(Stream ist, int len) {
            byte[] buf = new byte[8192 * 4];
            int read, remain = len;
            try {
                while (remain > 0) {
                    read = ist.Read(buf, 0, remain > buf.Length ? buf.Length : remain);
                    if (read < 0)
                        throw new PngjInputException("error reading (skipping) : EOF");
                    remain -= read;
                }
            } catch (IOException e) {
                throw new PngjInputException("error reading (skipping)", e);
            }
        }

        public static void WriteBytes(Stream os, byte[] b) {
            try {
                os.Write(b, 0, b.Length);
            } catch (IOException e) {
                throw new PngjOutputException(e);
            }
        }

        public static void WriteBytes(Stream os, byte[] b, int offset, int n) {
            try {
                os.Write(b, offset, n);
            } catch (IOException e) {
                throw new PngjOutputException(e);
            }
        }

        public static int ReadByte(Stream mask0) {
            try {
                return mask0.ReadByte();
            } catch (IOException e) {
                throw new PngjOutputException(e);
            }
        }

        public static void WriteByte(Stream os, byte b) {
            try {
                os.WriteByte((byte)b);
            } catch (IOException e) {
                throw new PngjOutputException(e);
            }
        }



        public static int UnfilterRowPaeth(int r, int a, int b, int c) { // a = left, b = above, c = upper left
            return (r + FilterPaethPredictor(a, b, c)) & 0xFF;
        }

        public static int FilterPaethPredictor(int a, int b, int c) {
            // from http://www.libpng.org/pub/png/spec/1.2/PNG-Filters.html
            // a = left, b = above, c = upper left
            int p = a + b - c;// ; initial estimate
            int pa = p >= a ? p - a : a - p;
            int pb = p >= b ? p - b : b - p;
            int pc = p >= c ? p - c : c - p;
            // ; return nearest of a,b,c,
            // ; breaking ties in order a,b,c.
            if (pa <= pb && pa <= pc)
                return a;
            else if (pb <= pc)
                return b;
            else
                return c;
        }


        public static void Logdebug(String msg) {
            if (DEBUG)
                System.Console.Out.WriteLine(msg);
        }


   	public static void InitCrcForTests(PngReader pngr) {
		pngr.InitCrctest();
	}

	public static long GetCrctestVal(PngReader pngr) {
		return pngr.GetCrctestVal();
	}

    }
}
