using System;
using System.Collections.Generic;

namespace Readymade.Machinery.Shared {
    
    /// <summary>
    /// Extension methods acting on <see cref="IEnumerable{T}"/> instances.
    /// </summary>
    public static class IEnumerableExtensions {
        /// <summary>
        /// Performs the specified action on each element of the <see name="IEnumerable{T}" />.
        /// </summary>
        /// <param name="source">The <see name="IEnumerable{T}" /> to iterate.</param>
        /// <param name="action">The <see cref="Action{T}"/> delegate to perform on each element of the <see name="IEnumerable{T}" />.</param>
        public static void ForEach<T> ( this IEnumerable<T> source, Action<T> action ) {
            foreach ( T item in source ) {
                action ( item );
            }
        }
    }
}