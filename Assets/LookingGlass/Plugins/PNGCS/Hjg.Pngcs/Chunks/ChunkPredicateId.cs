using System;
using System.Collections.Generic;

using System.Text;

namespace Hjg.Pngcs.Chunks {
    /// <summary>
    /// Match if have same Chunk Id
    /// </summary>
    internal class ChunkPredicateId : ChunkPredicate {
        private readonly string id;
        public ChunkPredicateId(String id) {
            this.id = id;
        }
        public bool Matches(PngChunk c) {
            return c.Id.Equals(id);
        }
    }
}
