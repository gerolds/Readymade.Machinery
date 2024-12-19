using System.Diagnostics.CodeAnalysis;

namespace Readymade.Machinery.Acting
{
    public enum SpatialHeuristic
    {
        /// <summary>
        /// Do not specify a selection heuristic, use whatever is implemented as the default.
        /// </summary>
        Default,

        /// <summary>
        /// Select the item that is closest to the query source.
        /// </summary>
        Closest,

        /// <summary>
        /// Select the item that is farthest from the query source.
        /// </summary>
        Farthest,

        /// <summary>
        /// Select the first item that is found.
        /// </summary>
        Greedy,

        /// <summary>
        /// Select a random item from all items that are available.
        /// </summary>
        Random,

        /// <summary>
        /// Select a random item from all items that are available with a bias towards closer items.
        /// </summary>
        RandomProximity
    }

    /// <inheritdoc />
    /// <summary>
    /// A broker that can aggregate and route prop requests and serve as a repository of props. Brokers are composable.
    /// </summary>
    /// <typeparam name="TProp">The prop base type this broker can handle.</typeparam>
    public interface IPropBroker<TProp> where TProp : SoProp
    {
        /// <summary>
        /// Adds a provider for <see cref="TProp"/> that will be queried if no stored <see cref="TProp"/> is available. 
        /// </summary>
        /// <param name="provider"></param>
        public void AddProvider(IProvider<TProp> provider);

        /// <summary>
        /// Remove a provider from the broker. 
        /// </summary>
        /// <param name="provider">The provider to remove.</param>
        public void RemoveProvider(IProvider<TProp> provider);

        /// <summary>
        /// Tries to find a <see cref="IProvider{TProp}"/> that can supply the given <paramref name="prop"/>.
        /// </summary>
        /// <param name="prop">The prop to find.</param>
        /// <param name="result">The provider that can satisfy a <paramref name="prop"/> claim.</param>
        /// <param name="actor">The actor that would be making the claim.</param>
        /// <param name="quantity">The quantity that would be claimed. Must be &gt; 1.</param>
        /// <param name="heuristic">The desired heuristic to use when finding a prop.</param>
        /// <returns>Whether a suitable <see cref="IProvider{TProp}"/> was found.</returns>
        public bool TryFindProp(
            [NotNull] TProp prop,
            IActor actor,
            long quantity,
            out IProvider<TProp> result,
            SpatialHeuristic heuristic = SpatialHeuristic.Default
        );

        public bool TryClaimProp(
            [NotNull] TProp prop,
            IActor actor,
            long quantity,
            out PropClaim<TProp, IActor> result,
            SpatialHeuristic heuristic = SpatialHeuristic.Default
        );

        /// <summary>
        /// Tries to find a <see cref="IProvider{TProp}"/> that can supply the given <paramref name="prop"/>.
        /// </summary>
        /// <param name="prop">The prop to find.</param>
        /// <param name="actor">The actor that would be making the claim.</param>
        /// <param name="quantity">The quantity that would be claimed. Must be &gt; 1.</param>
        /// <param name="heuristic">The desired heuristic to use when finding a prop.</param>
        /// <returns>Whether a suitable <see cref="IProvider{TProp}"/> was found.</returns>
        public bool TryFindProp(
            [NotNull] TProp prop,
            IActor actor = default,
            long quantity = 1,
            SpatialHeuristic heuristic = SpatialHeuristic.Default
        ) => TryFindProp(prop, actor, quantity, out _);
    }
}