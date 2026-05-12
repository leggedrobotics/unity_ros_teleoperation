using System;

namespace LookingGlass.Toolkit {
    [Flags]
    public enum BridgeLoggingFlags : uint {
        None = 0,
        Timing = 1,
        Messages = 2,
        Responses = 4,

        All = 2147483647
    }
}
