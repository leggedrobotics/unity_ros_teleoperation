namespace Hjg.Pngcs {

    using Hjg.Pngcs.Chunks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Bunch of utility static methods to process/analyze an image line. 
    /// 
    /// Not essential at all, some methods are probably to be removed if future releases.
    /// 
    /// TODO: document this better
    /// 
    /// </summary>
    ///
    public class ImageLineHelper {

        /// <summary>
        /// Given an indexed line with a palette, unpacks as a RGB array
        /// </summary>
        /// <param name="line">ImageLine as returned from PngReader</param>
        /// <param name="pal">Palette chunk</param>
        /// <param name="trns">TRNS chunk (optional)</param>
        /// <param name="buf">Preallocated array, optional</param>
        /// <returns>R G B (one byte per sample)</returns>
        public static int[] Palette2rgb(ImageLine line, PngChunkPLTE pal, PngChunkTRNS trns, int[] buf) {
            bool isalpha = trns != null;
            int channels = isalpha ? 4 : 3;
            int nsamples = line.ImgInfo.Cols * channels;
            if (buf == null || buf.Length < nsamples)
                buf = new int[nsamples];
            if (!line.SamplesUnpacked)
                line = line.unpackToNewImageLine();
            bool isbyte = line.SampleType == Hjg.Pngcs.ImageLine.ESampleType.BYTE;
            int nindexesWithAlpha = trns != null ? trns.GetPalletteAlpha().Length : 0;
            for (int c = 0; c < line.ImgInfo.Cols; c++) {
                int index = isbyte ? (line.ScanlineB[c] & 0xFF) : line.Scanline[c];
                pal.GetEntryRgb(index, buf, c * channels);
                if (isalpha) {
                    int alpha = index < nindexesWithAlpha ? trns.GetPalletteAlpha()[index] : 255;
                    buf[c * channels + 3] = alpha;
                }
            }
            return buf;
        }

        public static int[] Palette2rgb(ImageLine line, PngChunkPLTE pal, int[] buf) {
            return Palette2rgb(line, pal, null, buf);
        }

        public static int ToARGB8(int r, int g, int b) {
            unchecked {
                return ((int)(0xFF000000)) | ((r ) << 16) | ((g ) << 8) | (b );
            }
        }

        public static int ToARGB8(int r, int g, int b, int a) {
            return ((a ) << 24) | ((r ) << 16) | ((g ) << 8) | (b );
        }

        public static int ToARGB8(int[] buff, int offset, bool alpha) {
            return alpha ? ToARGB8(buff[offset++], buff[offset++], buff[offset]) : ToARGB8(buff[offset++], buff[offset++],
                    buff[offset++], buff[offset]);
        }

        public static int ToARGB8(byte[] buff, int offset, bool alpha) {
            return alpha ? ToARGB8(buff[offset++], buff[offset++], buff[offset]) : ToARGB8(buff[offset++], buff[offset++],
                    buff[offset++], buff[offset]);
        }

        public static void FromARGB8(int val, int[] buff, int offset, bool alpha) {
            buff[offset++] = ((val >> 16) & 0xFF);
            buff[offset++] = ((val >> 8) & 0xFF);
            buff[offset] = (val & 0xFF);
            if (alpha)
                buff[offset + 1] = ((val >> 24) & 0xFF);
        }

        public static void FromARGB8(int val, byte[] buff, int offset, bool alpha) {
            buff[offset++] = (byte)((val >> 16) & 0xFF);
            buff[offset++] = (byte)((val >> 8) & 0xFF);
            buff[offset] = (byte)(val & 0xFF);
            if (alpha)
                buff[offset + 1] = (byte)((val >> 24) & 0xFF);
        }

        public static int GetPixelToARGB8(ImageLine line, int column) {
            if (line.IsInt())
                return ToARGB8(line.Scanline, column * line.channels, line.ImgInfo.Alpha);
            else
                return ToARGB8(line.ScanlineB, column * line.channels, line.ImgInfo.Alpha);
        }

        public static void SetPixelFromARGB8(ImageLine line, int column, int argb) {
            if (line.IsInt())
                FromARGB8(argb, line.Scanline, column * line.channels, line.ImgInfo.Alpha);
            else
                FromARGB8(argb, line.ScanlineB, column * line.channels, line.ImgInfo.Alpha);
        }

        public static void SetPixel(ImageLine line, int col, int r, int g, int b, int a) {
            int offset = col * line.channels;
            if (line.IsInt()) {
                line.Scanline[offset++] = r;
                line.Scanline[offset++] = g;
                line.Scanline[offset] = b;
                if (line.ImgInfo.Alpha)
                    line.Scanline[offset + 1] = a;
            } else {
                line.ScanlineB[offset++] = (byte)r;
                line.ScanlineB[offset++] = (byte)g;
                line.ScanlineB[offset] = (byte)b;
                if (line.ImgInfo.Alpha)
                    line.ScanlineB[offset + 1] = (byte)a;
            }
        }

        public static void SetPixel(ImageLine line, int col, int r, int g, int b) {
            SetPixel(line, col, r, g, b, line.maxSampleVal);
        }

        public static double ReadDouble(ImageLine line, int pos) {
            if (line.IsInt())
                return line.Scanline[pos] / (line.maxSampleVal + 0.9);
            else
                return (line.ScanlineB[pos]) / (line.maxSampleVal + 0.9);
        }

        public static void WriteDouble(ImageLine line, double d, int pos) {
            if (line.IsInt())
                line.Scanline[pos] = (int)(d * (line.maxSampleVal + 0.99));
            else
                line.ScanlineB[pos] = (byte)(d * (line.maxSampleVal + 0.99));
        }

      

        public static int Interpol(int a, int b, int c, int d, double dx, double dy) {
            // a b -> x (0-1)
            // c d
            double e = a * (1.0 - dx) + b * dx;
            double f = c * (1.0 - dx) + d * dx;
            return (int)(e * (1 - dy) + f * dy + 0.5);
        }


        public static int ClampTo_0_255(int i) {
            return i > 255 ? 255 : (i < 0 ? 0 : i);
        }

        /**
         * [0,1)
         */
        public static double ClampDouble(double i) {
            return i < 0 ? 0 : (i >= 1 ? 0.999999 : i);
        }

        public static int ClampTo_0_65535(int i) {
            return i > 65535 ? 65535 : (i < 0 ? 0 : i);
        }

        public static int ClampTo_128_127(int x) {
            return x > 127 ? 127 : (x < -128 ? -128 : x);
        }

        public static int[] Unpack(ImageInfo imgInfo, int[] src, int[] dst, bool scale) {
            int len1 = imgInfo.SamplesPerRow;
            int len0 = imgInfo.SamplesPerRowPacked;
            if (dst == null || dst.Length < len1)
                dst = new int[len1];
            if (imgInfo.Packed)
                ImageLine.unpackInplaceInt(imgInfo, src, dst, scale);
            else
                Array.Copy(src, 0, dst, 0, len0);
            return dst;
        }

        public static byte[] Unpack(ImageInfo imgInfo, byte[] src, byte[] dst, bool scale) {
            int len1 = imgInfo.SamplesPerRow;
            int len0 = imgInfo.SamplesPerRowPacked;
            if (dst == null || dst.Length < len1)
                dst = new byte[len1];
            if (imgInfo.Packed)
                ImageLine.unpackInplaceByte(imgInfo, src, dst, scale);
            else
                Array.Copy(src, 0, dst, 0, len0);
            return dst;
        }

        public static int[] Pack(ImageInfo imgInfo, int[] src, int[] dst, bool scale) {
            int len0 = imgInfo.SamplesPerRowPacked;
            if (dst == null || dst.Length < len0)
                dst = new int[len0];
            if (imgInfo.Packed)
                ImageLine.packInplaceInt(imgInfo, src, dst, scale);
            else
                Array.Copy(src, 0, dst, 0, len0);
            return dst;
        }

        public static byte[] Pack(ImageInfo imgInfo, byte[] src, byte[] dst, bool scale) {
            int len0 = imgInfo.SamplesPerRowPacked;
            if (dst == null || dst.Length < len0)
                dst = new byte[len0];
            if (imgInfo.Packed)
                ImageLine.packInplaceByte(imgInfo, src, dst, scale);
            else
                Array.Copy(src, 0, dst, 0, len0);
            return dst;
        }

    }
}
