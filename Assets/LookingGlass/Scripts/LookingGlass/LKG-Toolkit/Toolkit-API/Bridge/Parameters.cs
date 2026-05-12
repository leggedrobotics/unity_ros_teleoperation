using System;

namespace LookingGlass.Toolkit.Bridge
{
    public enum Parameters
    {
        rows,
        cols,
        aspect,
        viewCount,
        durationMS,
        isRGBD,
        depth_loc,
        depth_inversion,
        chroma_depth,
        crop_pos_x,
        crop_pos_y,
        depthiness,
        depth_cutoff,
        focus,
        zoom,
    }

    public static class ParameterUtils
    {
        public static bool IsFloatParam(Parameters param)
        {
            switch (param)
            {
                case Parameters.rows:
                case Parameters.cols:
                case Parameters.viewCount:
                case Parameters.isRGBD:
                case Parameters.depth_loc:
                case Parameters.depth_inversion:
                case Parameters.chroma_depth:
                case Parameters.durationMS:
                    return false;
                case Parameters.aspect:
                case Parameters.crop_pos_x:
                case Parameters.crop_pos_y:
                case Parameters.depthiness:
                case Parameters.depth_cutoff:
                case Parameters.focus:
                case Parameters.zoom:
                    return true;
            }

            return false;
        }

        public static string GetParamName(Parameters param)
        {
            switch (param)
            {
                case Parameters.rows:
                    return "rows";
                case Parameters.cols:
                    return "cols";
                case Parameters.aspect:
                    return "aspect";
                case Parameters.viewCount:
                    return "viewCount";
                case Parameters.isRGBD:
                    return "isRGBD";
                case Parameters.depth_loc:
                    return "depth_loc";
                case Parameters.depth_inversion:
                    return "depth_inversion";
                case Parameters.chroma_depth:
                    return "chroma_depth";
                case Parameters.crop_pos_x:
                    return "crop_pos_x";
                case Parameters.crop_pos_y:
                    return "crop_pos_y";
                case Parameters.depthiness:
                    return "depthiness";
                case Parameters.depth_cutoff:
                    return "depth_cutoff";
                case Parameters.focus:
                    return "focus";
                case Parameters.zoom:
                    return "zoom";
                case Parameters.durationMS:
                    return "durationMS";
                default:
                    throw new ArgumentOutOfRangeException(nameof(param), param, "Invalid parameter");
            }
        }
    }
}
