using UnityEngine;

namespace LookingGlass.Blocks {
    /// <summary>
    /// Allows the user to upload quilts to <a href="https://blocks.glass">Looking Glass Blocks</a>.
    /// </summary>
    public class BlockUploader : MonoBehaviour {
        [Tooltip("The file path of the quilt you wish to upload.")]
        [SerializeField] internal string quiltFilePath;

        [Tooltip("The title that this quilt will be given, once uploaded to Looking Glass Blocks.")]
        [SerializeField] internal string blockTitle;

        [Tooltip("The description that this quilt will be given, once uploaded to Looking Glass Blocks.")]
        [TextArea(5, 8)]
        [SerializeField] internal string blockDescription;

        public string QuiltFilePath => quiltFilePath;
        public string BlockTitle => blockTitle;
        public string BlockDescription => blockDescription;
    }
}
