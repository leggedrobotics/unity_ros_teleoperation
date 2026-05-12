namespace Hjg.Pngcs.Chunks {

    using Hjg.Pngcs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    /// <summary>
    /// general class for textual chunks
    /// </summary>
    public abstract class PngChunkTextVar : PngChunkMultiple {
        protected internal String key; // key/val: only for tEXt. lazy computed
        protected internal String val;

        protected internal PngChunkTextVar(String id, ImageInfo info)
            : base(id, info) {
        }

        public const String KEY_Title = "Title"; // Short (one line) title or caption for image
        public const String KEY_Author = "Author"; // Name of image's creator
        public const String KEY_Description = "Description"; // Description of image (possibly long)
        public const String KEY_Copyright = "Copyright"; // Copyright notice
        public const String KEY_Creation_Time = "Creation Time"; // Time of original image creation
        public const String KEY_Software = "Software"; // Software used to create the image
        public const String KEY_Disclaimer = "Disclaimer"; // Legal disclaimer
        public const String KEY_Warning = "Warning"; // Warning of nature of content
        public const String KEY_Source = "Source"; // Device used to create the image
        public const String KEY_Comment = "Comment"; // Miscellaneous comment

        public class PngTxtInfo {
            public String title;
            public String author;
            public String description;
            public String creation_time;// = (new Date()).toString();
            public String software;
            public String disclaimer;
            public String warning;
            public String source;
            public String comment;
        }

        public override ChunkOrderingConstraint GetOrderingConstraint() {
            return ChunkOrderingConstraint.NONE;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public String GetKey() {
            return key;
        }

        public String GetVal() {
            return val;
        }

        public void SetKeyVal(String key, String val) {
            this.key = key;
            this.val = val;
        }
    }
}
