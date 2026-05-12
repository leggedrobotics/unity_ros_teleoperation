using System;

#if HAS_NEWTONSOFT_JSON
using Newtonsoft.Json.Linq;
#endif

namespace LookingGlass.Toolkit {
    /// <summary>
    /// <para>
    /// Defines the resolution and layout of tiles in a quilt.
    /// </para>
    /// <para>
    /// See also:
    /// <list type="bullet">
    /// <item>https://docs.lookingglassfactory.com/keyconcepts/quilts</item>
    /// </list>
    /// </para>
    /// </summary>
    [Serializable]
    public struct QuiltSettings {
        public const int MinSize = 256;
        public const int MaxSize = 8192 * 2;
        public const int MinRowColumnCount = 1;
        public const int MaxRowColumnCount = 32;

        /// <summary>
        /// Provides default quilt settings of 3360x3360 at 8x6 tiling, with a 0.75 aspect ratio (which conveniently matches the <see cref="TileAspect"/> of these settings).
        /// </summary>
        /// <remarks>
        /// Use this only if all else fails to load, to avoid having zeroed out <see cref="QuiltSettings"/>.
        /// </remarks>
        public static QuiltSettings Fallback => new QuiltSettings(3360, 3360, 8, 6, 0.75f);

        /// <summary>
        /// Provides settings for a blank 256x256 quilt at 1x1 tiling and 1 aspect ratio, to avoid issues with 0 quiltAspect and camera projetion matrices resulting with Infinity.
        /// </summary>
        public static QuiltSettings Blank => new QuiltSettings(MinSize, MinSize, 1, 1, 1);

        /// <summary>
        /// The total width of the quilt texture, in pixels.
        /// </summary>
        public int quiltWidth;

        /// <summary>
        /// The total height of the quilt texture, in pixels.
        /// </summary>
        public int quiltHeight;

        /// <summary>
        /// The number of quilt tiles counted along the x-axis (the number of columns).
        /// </summary>
        public int columns;

        /// <summary>
        /// The number of quilt tiles counted along the y-axis (the number of rows).
        /// </summary>
        public int rows;

        /// <summary>
        /// <para>
        /// The aspect ratio of the camera or source (2D/RGBD) image, when this quilt was originally rendered.<br />
        /// If you are rendering new quilts from a 3D scene, full-screen to a LKG display, this should be set to the aspect ratio of your LKG display's native screen resolution (<see cref="Calibration.screenW"/> / <see cref="Calibration.screenH"/>).
        /// </para>
        /// <para>This aspect ratio is NOT necessarily equal to the aspect ratio of each tile's (width / height).</para>
        /// </summary>
        public float renderAspect;

        /// <summary>
        /// The total number of tiles to render with when lenticularizing this quilt to the Looking Glass display.
        /// Valid values are in the range <c>[1, columns * rows]</c>.
        /// </summary>
        public int tileCount;

        public bool IsDefaultOrBlank => Equals(default) || Equals(Blank);

        public void ResetTileCount() {
            tileCount = columns * rows;
        }

        public int TileWidth {
            get {
                if (columns <= 0)
                    return quiltWidth;
                return quiltWidth / columns;
            }
        }

        public int TileHeight {
            get {
                if (rows <= 0)
                    return quiltHeight;
                return quiltHeight / rows;
            }
        }
        //NOTE: THIS IS DIFFERENT from what we use pretty much everywhere else.
        //This does NOT necessarily match the quiltAspect (of the native display resolution, if rendering fullscreen)
        public float TileAspect => ((float) quiltWidth / columns) / ((float) quiltHeight / rows);

        public int PaddingHorizontal => quiltWidth - columns * TileWidth;
        public int PaddingVertical => quiltHeight - rows * TileHeight;
        public float ViewPortionHorizontal => ((float) columns * TileWidth) / quiltWidth;
        public float ViewPortionVertical => ((float) rows * TileHeight) / quiltHeight;

        /// <summary>
        /// Creates new arbitrary quilt settings.
        /// </summary>
        public QuiltSettings(
            int quiltWidth,
            int quiltHeight,
            int columns,
            int rows,
            float renderAspect) {

            this.quiltWidth = Math.Clamp(quiltWidth, MinSize, MaxSize);
            this.quiltHeight = Math.Clamp(quiltHeight, MinSize, MaxSize);
            this.columns = Math.Clamp(columns, MinRowColumnCount, MaxRowColumnCount);
            this.rows = Math.Clamp(rows, MinRowColumnCount, MaxRowColumnCount);
            this.renderAspect = renderAspect;
            tileCount = this.columns * this.rows;
        }

        /// <inheritdoc cref="QuiltSettings.QuiltSettings(int, int, int, int, float, int)"/>
        /// <summary>
        /// Creates new arbitrary quilt settings, with a custom tile count.
        /// </summary>
        public QuiltSettings(
            int quiltWidth,
            int quiltHeight,
            int columns,
            int rows,
            float renderAspect,
            int tileCount) {

            this.quiltWidth = Math.Clamp(quiltWidth, MinSize, MaxSize);
            this.quiltHeight = Math.Clamp(quiltHeight, MinSize, MaxSize);
            this.columns = Math.Clamp(columns, MinRowColumnCount, MaxRowColumnCount);
            this.rows = Math.Clamp(rows, MinRowColumnCount, MaxRowColumnCount);
            this.renderAspect = renderAspect;
            this.tileCount = Math.Clamp(tileCount, 1, this.columns * this.rows);
        }

#if HAS_NEWTONSOFT_JSON
        public static QuiltSettings Parse(JObject obj) {
            QuiltSettings result = new QuiltSettings();
            if (!obj.TryGet<int>("quiltWidth", out result.quiltWidth))
                obj.TryGet<int>("quiltX", out result.quiltWidth);               //NOTE: Old naming

            if (!obj.TryGet<int>("quiltHeight", out result.quiltHeight))
                obj.TryGet<int>("quiltY", out result.quiltHeight);              //NOTE: Old naming

            if (!obj.TryGet<int>("columns", out result.columns))
                obj.TryGet<int>("tileX", out result.columns);                   //NOTE: Old naming

            if (!obj.TryGet<int>("rows", out result.rows))
                obj.TryGet<int>("tileY", out result.rows);                      //NOTE: Old naming

            if (!obj.TryGet<float>("renderAspect", out result.renderAspect))
                obj.TryGet<float>("quiltAspect", out result.renderAspect);      //NOTE: Old naming

            if (!obj.TryGet<int>("tileCount", out result.tileCount))
                result.ResetTileCount();
            return result;
        }
#endif

        public bool Equals(QuiltSettings other) {
            if (renderAspect == other.renderAspect
                && quiltWidth == other.quiltWidth
                && quiltHeight == other.quiltHeight
                && columns == other.columns
                && rows == other.rows)
                return true;
            return false;
        }

        public static QuiltSettings GetDefaultFor(LKGDeviceType deviceType) {
            ILKGDeviceTemplateSystem system = LookingGlass.Toolkit.ServiceLocator.Instance.GetSystem<ILKGDeviceTemplateSystem>();
            if (system != null) {
                LKGDeviceTemplate template = system.GetTemplate(deviceType);
                if (template != null)
                    return template.defaultQuilt;
            }
            return QuiltSettings.Blank;
        }
    }
}
