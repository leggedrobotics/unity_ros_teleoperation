using System;

namespace LookingGlass.Blocks {
    [Serializable]
    public class S3Upload {
        public string key;
        public string preSignedUrl;
        public string url;
    }
}
