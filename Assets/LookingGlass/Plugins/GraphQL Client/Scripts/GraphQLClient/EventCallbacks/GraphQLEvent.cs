using System;

namespace GraphQLClient.EventCallbacks {
    [Serializable]
    public abstract class GraphQLEvent<T> where T : GraphQLEvent<T> {
        //NOTE: Because this is a generic class, there is MORE THAN ONE of this event:
        //One of this event for EACH of the child classes of GraphQLEvent<T>:
        private static Action<T> listeners;

        public static void RegisterListener(Action<T> listener) => listeners += listener;
        public static void UnregisterListener(Action<T> listener) => listeners += listener;

        private bool hasFired;

        public void FireEvent() {
            if (hasFired)
                throw new InvalidOperationException("This event has already fired, to prevent infinite loops you can't refire an event");
            hasFired = true;
            listeners?.Invoke(this as T);
        }
    }
}
