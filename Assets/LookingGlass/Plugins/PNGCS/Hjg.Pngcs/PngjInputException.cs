namespace Hjg.Pngcs {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Exception associated with input (reading) operations
    /// </summary>
    [Serializable]
    public class PngjInputException : PngjException {
        private const long serialVersionUID = 1L;

        public PngjInputException(String message, Exception cause)
            : base(message, cause) {
        }

        public PngjInputException(String message)
            : base(message) {
        }

        public PngjInputException(Exception cause)
            : base(cause) {
        }
    }
}
