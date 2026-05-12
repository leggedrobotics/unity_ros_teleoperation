using System;

namespace GraphQLClient {
    [Serializable]
    public enum NetworkErrorBehaviour {
        Exception = 0,
        ErrorLog = 1,
        Silent = 2
    }
}
