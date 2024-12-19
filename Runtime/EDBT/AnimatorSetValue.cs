using System;
using UnityEngine;

namespace Readymade.Machinery.EDBT
{
    /// <summary>
    /// Sets a property of an animator.
    /// </summary>
    /// <typeparam name="T">The type of value to set on the <see cref="Animator"/>.</typeparam>
    public class AnimatorSetValue<T> : TaskBase where T : struct
    {
        /// <summary>
        /// Cached parameter name hash for faster access to the parameter.
        /// </summary>
        public int ParameterID { get; }

        /// <summary>
        /// The <see cref="Animator"/> controlled by this task.
        /// </summary>
        private readonly Animator _animator;

        /// <summary>
        /// The <see cref="Func{TResult}" /> to be invoked on each tick for reading the value.
        /// </summary>
        private readonly Func<T> _getValue;

        /// <summary>
        /// cached initial state of the animator before this task was executed. Used to reset the enabled state.
        /// </summary>
        private bool _originalEnabledState;

        private readonly string _name;

        /// <summary>
        /// Create a new instance of a <see cref="AnimatorSetValue{T}"/> task.
        /// </summary>
        /// <param name="name">A descriptive name for the task.</param>
        /// <param name="animator">The animator to control with this task.</param>
        /// <param name="parameterID"></param>
        /// <param name="getValue"></param>
        public AnimatorSetValue(string name, Animator animator, int parameterID, Func<T> getValue) : base(name)
        {
            ParameterID = parameterID;
            _name = name;
            _animator = animator;
            _getValue = getValue;
        }

        /// <inheritdoc />
        public AnimatorSetValue(Animator animator, string parameterName, Func<T> getValue) : this(default, animator,
            parameterID: Shader.PropertyToID(parameterName), getValue)
        {
        }


        public AnimatorSetValue(Animator animator, int parameterID, Func<T> getValue) : this(default, animator,
            parameterID, getValue)
        {
        }

        /// <inheritdoc />
        protected override void OnAborted()
        {
        }

        /// <inheritdoc />
        protected override void OnResumed()
        {
        }

        /// <inheritdoc />
        protected override void OnSuspended()
        {
        }

        /// <inheritdoc />
        protected override TaskState OnTick()
        {
            return TaskState.Success;
        }

        /// <inheritdoc />
        protected override void OnStarted()
        {
            // We need to use Convert.ChangeType here to bridge the generic implementation of this task to the non-generic
            // but runtime-type checked Animator API.
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Single:
                {
                    T value = _getValue();
                    _animator.SetFloat(ParameterID, (float)Convert.ChangeType(value, typeof(float)));
                    //Debug.Log ( $"[{nameof ( AnimatorSetValue<T> )}] Parameter {_name} = {value} on {_animator?.name}.", _animator );
                    break;
                }
                case TypeCode.Boolean:
                {
                    T value = _getValue();

                    _animator.SetBool(ParameterID, (bool)Convert.ChangeType(value, typeof(bool)));
                    //Debug.Log ( $"[{nameof ( AnimatorSetValue<T> )}] Parameter {_name} = {value} on {_animator?.name}.", _animator );
                    break;
                }
                case TypeCode.Int32:
                {
                    T value = _getValue();

                    _animator.SetInteger(ParameterID, (int)Convert.ChangeType(value, typeof(int)));
                    //Debug.Log ( $"[{nameof ( AnimatorSetValue<T> )}] Parameter {_name} = {value} on {_animator?.name}.", _animator );
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        protected override void OnReset()
        {
        }

        /// <inheritdoc />
        protected override void OnStopped(TaskState state)
        {
        }
    }
}