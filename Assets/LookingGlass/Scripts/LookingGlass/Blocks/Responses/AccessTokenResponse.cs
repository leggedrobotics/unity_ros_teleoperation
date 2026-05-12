using System;

namespace LookingGlass.Blocks {
    [Serializable]
    public class AccessTokenResponse {
        public string access_token;
        public string refresh_token;
        public string token_type;
        public int expires_in;
        public string error;
        public string error_descriptions;
    }
}
