using System.Collections.Generic;
using UnityEngine;

namespace Readymade.Machinery.Shared {
    /// <inheritdoc />
    /// <summary>
    /// Compares two poses for approximate equality.
    /// </summary>
    public class PoseComparer : IEqualityComparer<Pose> {
        /// <summary>
        /// The default <see cref="PoseComparer"/> with an alignment error of 5°, XZ-position error of 0.25f and Y-position error of 2f.
        /// </summary>
        public static PoseComparer Default { get; } = new ( alignmentError: 5f, xzPosError: 2f, yPosError: 2f );

        /// <summary>
        /// The allowed rotational error for a gesture to still execute, in angles.
        /// </summary>
        private float _alignmentError = 5f;

        /// <summary>
        /// The allowed xz-position error for a gesture to still execute, in world units.
        /// </summary>
        private float _xzPosError = 0.1f;

        /// <summary>
        /// The allowed y-position error for a gesture to still execute, in world units.
        /// </summary>
        private float _yPosError;

        /// <summary>
        /// Precalculated square of the xz error.
        /// </summary>
        private float _xzErrorSquared;

        /// <summary>
        /// Disable default ctor
        /// </summary>
        private PoseComparer () {
        }

        /// <summary>
        /// Creates a new pose comparer with configurable rotation and position errors.
        /// </summary>
        /// <param name="alignmentError">The rotational error in degrees.</param>
        /// <param name="xzPosError">The position error in the XZ-plane.</param>
        /// <param name="yPosError">The position error in the Y-plane.</param>
        public PoseComparer ( float alignmentError, float xzPosError, float yPosError ) {
            _alignmentError = alignmentError;
            _xzPosError = xzPosError;
            _yPosError = yPosError;
            _xzErrorSquared = _xzPosError * _xzPosError;
        }

        /// <inheritdoc />
        public bool Equals ( Pose a, Pose b ) {
            // TODO: inline this again
            bool okYPosError = Mathf.Abs ( a.position.y - b.position.y ) < _yPosError;
            bool okXZPosError =
                ( new Vector2 ( a.position.x, a.position.z ) - new Vector2 ( b.position.x, b.position.z ) ).sqrMagnitude <
                _xzErrorSquared;
            bool okAlignmentError = a.rotation.Equals ( default ) || b.rotation.Equals ( default ) || Vector3.Angle ( a.forward, b.forward ) < _alignmentError;
            return okYPosError && okXZPosError && okAlignmentError;
        }

        /// <inheritdoc />
        public int GetHashCode ( Pose obj ) => obj.GetHashCode ();
    }
}