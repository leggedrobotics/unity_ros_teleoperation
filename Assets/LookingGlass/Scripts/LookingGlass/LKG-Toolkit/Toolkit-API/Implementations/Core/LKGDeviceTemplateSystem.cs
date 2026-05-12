using System.Collections.Generic;
using System.Linq;

namespace LookingGlass.Toolkit {
    public class LKGDeviceTemplateSystem : ILKGDeviceTemplateSystem {
        //WARNING: For now, this is manually kept in-sync with LKG Bridge manually.
        //  These values are written in Constants.h (Ctrl + P to find it in VS Code when you have the Git repo open locally).
        //Waiting on LKG-637 for LKG Bridge HTTP endpoints to query this data instead
        private static readonly Dictionary<LKGDeviceType, LKGDeviceTemplate> Templates = new() {
            //Gen1
            {
                LKGDeviceType._8_9inGen1,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "1.0",
                        serial = "LKG-2K-00000",
                        pitch = 49.79978561401367f,
                        slope = 5.48f,
                        center = 0,
                        viewCone = 40,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 338,
                        screenW = 2560,
                        screenH = 1600,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0
                    },
                    new QuiltSettings(4096, 4096, 5, 9, 1.6f)
                )
            },
            {
                LKGDeviceType._15_6inGen1,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "1.0",
                        serial = "LKG-4K-00000",
                        pitch = 50.07143783569336f,
                        slope = -7.7f,
                        center = 0,
                        viewCone = 40,
                        fringe = 0,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 283,
                        screenW = 3840,
                        screenH = 2160,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0
                    },
                    new QuiltSettings(4096, 4096, 5, 9, 1.77778f)
                )
            },
            {
                LKGDeviceType._8KGen1,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "1.0",
                        serial = "LKG-8K-00000",
                        pitch = 38.054588317871097f,
                        slope = -7.7f,
                        center = 0,
                        viewCone = 40,
                        fringe = 0,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 280,
                        screenW = 7680,
                        screenH = 4320,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0
                    },
                    new QuiltSettings(8192, 8192, 5, 9, 1.77778f)
                )
            },

            //Gen2
            {
                LKGDeviceType.PortraitGen2,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-PORT-",
                        pitch = 52,
                        slope = -7,
                        center = 0.5f,
                        fringe = 0,
                        viewCone = 40,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 324,
                        screenW = 1536,
                        screenH = 2048,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0
                    },
                    new QuiltSettings(3360, 3360, 8, 6, 0.75f)
                )
            },
            {
                LKGDeviceType._16inGen2,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-A",
                        pitch = 50,
                        slope = -7,
                        center = 0.5f,
                        fringe = 0,
                        viewCone = 40,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 283,
                        screenW = 3840,
                        screenH = 2160,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0
                    },
                    new QuiltSettings(4096, 4096, 5, 9, 1.77778f)
                )
            },
            {
                LKGDeviceType._32inGen2,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-B",
                        pitch = 42,
                        slope = -6,
                        center = 0.3f,
                        fringe = 0,
                        viewCone = 40,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 280,
                        screenW = 7680,
                        screenH = 4320,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0
                    },
                    new QuiltSettings(8192, 8192, 5, 9, 1.77778f)
                )
            },
            {
                LKGDeviceType.ThirdParty,
                new LKGDeviceTemplate(
                    default(Calibration),
                    default(QuiltSettings)
                )
            },
            {
                LKGDeviceType._65inLandscapeGen2,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-D",
                        pitch = 42,
                        slope = -6,
                        center = 0.3f,
                        fringe = 0,
                        viewCone = 40,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 136,
                        screenW = 7680,
                        screenH = 4320,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0
                    },
                    new QuiltSettings(8192, 8192, 8, 9, 1.77778f)
                )
            },
            {
                LKGDeviceType.Prototype,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-Q",
                        pitch = 42,
                        slope = -6,
                        center = 0.3f,
                        fringe = 0,
                        viewCone = 40,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 136.6f,
                        screenW = 7680,
                        screenH = 4320,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0
                    },
                    new QuiltSettings(4096, 4096, 5, 9, 1.77778f)
                )
            },

            //Gen3
            {
                LKGDeviceType.GoPortrait,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-E",
                        pitch = 80.7f,
                        slope = -6.3f,
                        center = -0.3f,
                        fringe = 0,
                        viewCone = 54,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 491,
                        screenW = 1440,
                        screenH = 2560,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0
                    },
                    new QuiltSettings(4092, 4092, 11, 6, 0.5625f)
                )
            },
            {
                LKGDeviceType.Kiosk,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-F",
                        pitch = 52,
                        slope = -7,
                        center = 0.5f,
                        fringe = 0,
                        viewCone = 40,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 324,
                        screenW = 1536,
                        screenH = 2048,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0
                    },
                    new QuiltSettings(4092, 4092, 11, 6, 0.5625f)
                )
            },
            {
                LKGDeviceType._16inPortraitGen3,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-H",
                        pitch = 44.72f,
                        slope = -7.45f,
                        center = 0,
                        fringe = 0,
                        viewCone = 50,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 283,
                        screenW = 2160,
                        screenH = 3840,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0,
                        cellPatternMode = 4,
                        subpixelCells = new SubpixelCell[] {
                            new SubpixelCell {
                                ROffsetX = -0.25f,
                                ROffsetY = -0.31f,
                                GOffsetX = -0.25f,
                                GOffsetY = 0.28f,
                                BOffsetX = 0.25f,
                                BOffsetY = 0
                            },
                            new SubpixelCell {
                                ROffsetX = 0.25f,
                                ROffsetY = -0.31f,
                                GOffsetX = 0.25f,
                                GOffsetY = 0.28f,
                                BOffsetX = -0.25f,
                                BOffsetY = 0
                            }
                        }
                    },
                    new QuiltSettings(5995, 6000, 11, 6, 0.5625f)
                )
            },
            {
                LKGDeviceType._16inLandscapeGen3,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-J",
                        pitch = 44.72f,
                        slope = -6.9f,
                        center = 0.5f,
                        fringe = 0,
                        viewCone = 50,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 283,
                        screenW = 3840,
                        screenH = 2160,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0,
                        cellPatternMode = 2,
                        subpixelCells = new SubpixelCell[] {
                            new SubpixelCell {
                                ROffsetX = -0.31f,
                                ROffsetY = 0.25f,
                                GOffsetX = 0.28f,
                                GOffsetY = 0.25f,
                                BOffsetX = 0,
                                BOffsetY = -0.25f
                            },
                            new SubpixelCell {
                                ROffsetX = -0.31f,
                                ROffsetY = -0.25f,
                                GOffsetX = 0.28f,
                                GOffsetY = -0.25f,
                                BOffsetX = 0,
                                BOffsetY = 0.25f
                            }
                        }
                    },
                    new QuiltSettings(5999, 5999, 7, 7, 1.77778f)
                )
            },
            {
                LKGDeviceType._32inPortraitGen3,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-K",
                        pitch = 80.7f,
                        slope = -6.3f,
                        center = -0.3f,
                        fringe = 0,
                        viewCone = 54,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 280,
                        screenW = 4320,
                        screenH = 7680,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0,
                        cellPatternMode = 2,
                        subpixelCells = new SubpixelCell[] {
                            new SubpixelCell {
                                ROffsetX = -0.31f,
                                ROffsetY = 0.25f,
                                GOffsetX = 0.28f,
                                GOffsetY = 0.25f,
                                BOffsetX = 0,
                                BOffsetY = -0.25f
                            },
                            new SubpixelCell {
                                ROffsetX = -0.31f,
                                ROffsetY = -0.25f,
                                GOffsetX = 0.28f,
                                GOffsetY = -0.25f,
                                BOffsetX = 0,
                                BOffsetY = 0.25f
                            }
                        }
                    },
                    new QuiltSettings(8184, 8184, 11, 6, 0.5625f)
                )
            },
            {
                LKGDeviceType._32inLandscapeGen3,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-L",
                        pitch = 75,
                        slope = -7.2f,
                        center = -0.2f,
                        fringe = 0,
                        viewCone = 50,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 280,
                        screenW = 7680,
                        screenH = 4320,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0,
                    },
                    new QuiltSettings(8190, 8190, 7, 7, 1.77778f)
                )
            },
            {
                LKGDeviceType._27inPortraitGen3,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-N",
                        pitch = 37.8156623840332f,
                        slope = -6.621088981628418f,
                        center = 0.4956521987915039f,
                        fringe = 0,
                        viewCone = 40,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 217,
                        screenW = 2880,
                        screenH = 5120,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0,
                        cellPatternMode = 0,
                        subpixelCells = new SubpixelCell[] {
                            new SubpixelCell {
                                ROffsetX = 0,
                                ROffsetY = -0.3330000042915344f,
                                GOffsetX = 0,
                                GOffsetY = 0,
                                BOffsetX = 0,
                                BOffsetY = 0.3330000042915344f
                            }
                        }
                    },
                    new QuiltSettings(7680, 4320, 12, 4, 0.5625f)
                )
            },
            {
                LKGDeviceType._27inLandscapeGen3,
                new LKGDeviceTemplate(
                    new Calibration {
                        configVersion = "3.0",
                        serial = "LKG-R",
                        pitch = 37.811866760253909f,
                        slope = -6.678580284118652f,
                        center = 0.27391302585601809f,
                        fringe = 0,
                        viewCone = 40,
                        invView = 1,
                        verticalAngle = 0,
                        dpi = 217,
                        screenW = 5120,
                        screenH = 2880,
                        flipImageX = 0,
                        flipImageY = 0,
                        flipSubp = 0,
                        cellPatternMode = 0,
                    },
                    new QuiltSettings(7680, 4320, 8, 6, 1.7778f)
                )
            },
        };

        public IEnumerable<LKGDeviceTemplate> GetAllTemplates() => Templates.OrderBy(p => p.Key).Select(p => p.Value);
        public LKGDeviceTemplate GetTemplate(LKGDeviceType deviceType) {
            if (Templates.TryGetValue(deviceType, out LKGDeviceTemplate result))
                return new LKGDeviceTemplate(result);
            return null;
        }
    }
}
