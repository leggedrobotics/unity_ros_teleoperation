using System;

namespace GraphQLClient {
    /// <summary>
    /// Represents the type of GraphQL query, such as a query, mutation, or a subscription.
    /// <para>See also: <seealso href="https://graphql.org/learn/queries/"/></para>
    /// </summary>
    [Serializable]
    public enum GraphQLQueryType {
        Query,
        Mutation,
        Subscription
    }
}
