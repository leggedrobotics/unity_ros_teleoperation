using System;

namespace GraphQLClient.EventCallbacks {
    public class OnSubscriptionDataReceived : GraphQLEvent<OnSubscriptionDataReceived> {
        private string data;

        public string Data => data;

        public OnSubscriptionDataReceived(string data) {
            this.data = data;
        }
    }
}
