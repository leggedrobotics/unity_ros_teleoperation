namespace Hjg.Pngcs {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// A few utility static methods to read and write files
    /// </summary>
    ///
    public class FileHelper {

        public static Stream OpenFileForReading(String file) {
            Stream isx = null;
            if (file == null || !File.Exists(file))
                throw new PngjInputException("Cannot open file for reading (" + file + ")");
            isx = new FileStream(file, FileMode.Open);
            return isx;
        }

        public static Stream OpenFileForWriting(String file, bool allowOverwrite) {
            Stream osx = null;
            if (File.Exists(file) && !allowOverwrite)
                throw new PngjOutputException("File already exists (" + file + ") and overwrite=false");
            osx = new FileStream(file, FileMode.Create);
            return osx;
        }

        /// <summary>
        /// Given a filename and a ImageInfo, produces a PngWriter object, ready for writing.</summary>
        /// <param name="fileName">Path of file</param>
        /// <param name="imgInfo">ImageInfo object</param>
        /// <param name="allowOverwrite">Flag: if false and file exists, a PngjOutputException is thrown</param>
        /// <returns>A PngWriter object, ready for writing</returns>
        public static PngWriter CreatePngWriter(String fileName, ImageInfo imgInfo, bool allowOverwrite) {
            return new PngWriter(OpenFileForWriting(fileName, allowOverwrite), imgInfo,
                    fileName);
        }

        /// <summary>
        /// Given a filename, produces a PngReader object, ready for reading.
        /// </summary>
        /// <param name="fileName">Path of file</param>
        /// <returns>PngReader, ready for reading</returns>
        public static PngReader CreatePngReader(String fileName) {
            return new PngReader(OpenFileForReading(fileName), fileName);
        }
    }
}
