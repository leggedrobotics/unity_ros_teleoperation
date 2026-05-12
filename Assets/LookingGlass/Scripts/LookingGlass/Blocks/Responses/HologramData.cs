using System;

namespace LookingGlass.Blocks {
    [Serializable]
    public class HologramData {
        public int id;
        public string title;
        public string description;
        public HologramType type;
        public float aspectRatio;
        public int quiltCols;
        public int quiltRows;
        public int quiltTileCount;
        public bool isPublished;
        public string permalink;
    }
}