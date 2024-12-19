using System;
using System.Text;

namespace Readymade.Machinery.Shared {
    
    /// <summary>
    /// Collection of extension methods that operate on <see cref="Type"/>.
    /// </summary>
    public static class TypeExtensions {
        
        /// <summary>
        /// Create a nice name for a given type. Particularly useful for generic type signatures.
        /// </summary>
        /// <param name="type">The type to format nicely as a string.</param>
        /// <returns>The nicely formatted type name.</returns>
        public static string GetNiceName ( this Type type ) {
            StringBuilder builder = new ();
            AppendNiceName ( builder, type );
            return builder.ToString ();
        }

        /// <summary>
        /// Perform recursive nice-name formatting as appends to the given <see cref="StringBuilder"/>.
        /// </summary>
        private static void AppendNiceName ( StringBuilder builder, Type type ) {
            ReadOnlySpan<char> typeName = type.Name.AsSpan ();

            if ( type.IsGenericType ) {
                int iTick = type.Name.IndexOf ( '`' );
                if ( iTick > 0 ) {
                    builder.Append ( typeName[ ..iTick ] );
                }

                builder.Append ( '<' );
                for ( int i = 0; i < type.GenericTypeArguments.Length; ++i ) {
                    AppendNiceName ( builder, type.GenericTypeArguments[ i ] );
                    if ( i < type.GenericTypeArguments.Length - 1 ) {
                        builder.Append ( ',' );
                    }
                }

                builder.Append ( '>' );
            } else {
                builder.Append ( type.Name );
            }
        }
    }
}