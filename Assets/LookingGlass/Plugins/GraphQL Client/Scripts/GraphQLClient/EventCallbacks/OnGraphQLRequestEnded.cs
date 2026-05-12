using System;

namespace GraphQLClient.EventCallbacks {
    public class OnGraphQLRequestEnded : GraphQLEvent<OnGraphQLRequestEnded> {
        private string data;
        private Exception exception;

        public bool Success => exception == null;
        public string Data => data;
        public Exception Exception => exception;

        public OnGraphQLRequestEnded(string data) {
            this.data = data;
        }

        public OnGraphQLRequestEnded(Exception exception) {
            this.exception = exception;
        }
    }
}
