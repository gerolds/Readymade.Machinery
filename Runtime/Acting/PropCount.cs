/* MIT License
 * Copyright 2023 Gerold Schneider
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the “Software”), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Readymade.Machinery;
using Readymade.Machinery.Acting;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif

namespace Readymade.Machinery.Acting
{
    /// <summary>
    /// Represents a quantity of <see cref="SoProp"/> items in an account or inventory.
    /// </summary>
    [Serializable]
    public struct PropCount : IPropCount<SoProp>, IEquatable<PropCount>
    {
        [Tooltip("The prop.")]
        [SerializeField]
        private SoProp prop;
        
        [Tooltip("The number of props.")]
        [SerializeField]
        [MinValue(0)]
        private long count;

        /// <summary>
        /// Create a new <see cref="PropCount"/> instance.
        /// </summary>
        /// <param name="prop">The prop.</param>
        /// <param name="count">The number of props.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="prop"/> is null.</exception>
        public PropCount([NotNull] SoProp prop, long count)
        {
            if (!prop)
            {
                throw new ArgumentNullException();
            }

            this.prop = prop;
            this.count = count;
        }
        
        public PropCount([NotNull] SoProp prop)
        {
            if (!prop)
            {
                throw new ArgumentNullException();
            }

            this.prop = prop;
            count = 1;
        }

        /// <inheritdoc />
        public readonly long Count => count;

        /// <inheritdoc />
        public readonly SoProp Identity => prop;

        /// <inheritdoc />
        public override string ToString() => $"({Count}x {Identity.Name})";

        public bool Equals(PropCount other)
        {
            return Equals(prop, other.prop) && Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is PropCount other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(prop, count);
        }
        
        public static bool operator ==(PropCount a, PropCount b)
        {
            return a.Identity == b.Identity && a.Count == b.Count;
        }

        public static bool operator !=(PropCount a, PropCount b) => !(a == b);
    }
}