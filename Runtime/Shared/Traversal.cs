using System;
using System.Collections.Generic;
using System.Linq;

namespace Readymade.Machinery.Shared {
    /// <summary>
    /// Collection of generic tree traversal algorithms.
    /// </summary>
    public static class Traversal {
        /// <summary>
        /// Allows depth first traversal of any graph (will not terminate if the graph contains cycles).
        /// </summary>
        /// <param name="root">The root node</param>
        /// <param name="childGetter">A delegate to get the children of a visited node.</param>
        /// <param name="functor">The action to perform for each node.</param>
        /// <typeparam name="T">The type of the nodes to traverse.</typeparam>
        public static void DepthFirst<T> ( T root, Func<T, IEnumerable<T>> childGetter, Action<T> functor ) {
            Stack<T> stack = new ();
            stack.Push ( root );
            while ( stack.TryPop ( out T node ) ) {
                foreach ( T child in childGetter ( node ) ?? Enumerable.Empty<T> () ) {
                    if ( child != null )
                        stack.Push ( child );
                }

                functor ( node );
            }
        }

        /// <summary>
        /// Allows breath first traversal of any graph (will not terminate if the graph contains cycles).
        /// </summary>
        /// <param name="root">The root node</param>
        /// <param name="childGetter">A delegate to get the children of a visited node.</param>
        /// <param name="functor">The action to perform for each node.</param>
        /// <typeparam name="T">The type of the nodes to traverse.</typeparam>
        public static void BreadthFirst<T> ( T root, Func<T, IEnumerable<T>> childGetter, Action<T> functor ) {
            Queue<T> queue = new ();
            queue.Enqueue ( root );
            while ( queue.TryDequeue ( out T node ) ) {
                foreach ( T child in childGetter ( node ) ?? Enumerable.Empty<T> () ) {
                    if ( child != null )
                        queue.Enqueue ( child );
                }

                functor ( node );
            }
        }
    }
}