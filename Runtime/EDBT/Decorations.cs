using System.Collections.Generic;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// A collection of decorators that can be assigned to an <see cref="ITask"/>.
    /// </summary>
    /// <remarks>Decorations are not available as an API on ITask yet.</remarks>
    public class Decorations
    {
        /// <summary>
        /// storage for the decorators in this collection.
        /// </summary>
        private readonly IDecorator[] _decorators;

        /// <summary>
        /// Create an instance of a <see cref="Decorations"/> collection.
        /// </summary>
        /// <param name="decorators"></param>
        public Decorations(params IDecorator[] decorators)
        {
            _decorators = decorators;
        }

        /// <summary>
        /// The decorators in this collection.
        /// </summary>
        public IEnumerable<IDecorator> Decorators => _decorators;
    }
}