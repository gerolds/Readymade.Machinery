using System;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// <para><see cref="ITask"/> that invokes a delegate.</para> 
    /// <para>Identical to <see cref="Condition"/> in functionality but with the convention that <see cref="TryDo"/> does produce side
    /// effects while <see cref="Condition"/> performs a purely functional check.</para>
    /// </summary>
    public class TryDo : Condition
    {
        /// <summary>
        /// Invokes a delegate and fails/succeeds based on its return value.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="condition">The delegate to invoke.</param>
        /// <param name="decorations">Decorators to modify the execution of this task.</param>
        public TryDo(
            string name,
            Func<bool> condition,
            Decorations decorations = default
        ) : base(name, condition,
            decorations)
        {
        }

        /// <summary>
        /// Invokes a delegate and fails/succeeds based on its return value.
        /// </summary>
        /// <param name="condition">The delegate to invoke.</param>
        /// <param name="decorations">Decorators to modify the execution of this task.</param>
        public TryDo(
            Func<bool> condition,
            Decorations decorations = default
        ) : base(condition, decorations)
        {
        }
    }
}