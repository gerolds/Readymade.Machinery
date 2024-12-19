using UnityEngine;

namespace Readymade.Machinery.Shared {
    /// <summary>
    /// Extension methods acting on <see cref="Pose"/> instances.
    /// </summary>
    public static class PoseExtensions {
        /// <summary>
        /// A default value for an invalid position.
        /// </summary>
        public static readonly Vector3 InvalidPosition = new ( float.NaN, float.NaN, float.NaN );

        /// <summary>
        /// A default value for an invalid rotation.
        /// </summary>
        public static readonly Quaternion InvalidRotation = new ( 0, 0, 0, 0 );

        /// <summary>
        /// Set the rotation component of the <see name="Pose" /> to <see cref="InvalidRotation"/>.
        /// </summary>
        /// <param name="pose">The <see cref="Pose"/> to operate on.</param>
        /// <returns>The <see cref="Pose"/> with the modified component.</returns>
        public static Pose AnyRotation ( this Pose pose ) => new Pose ( pose.position, default );

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pose">The <see cref="Pose"/> to operate on.</param>
        /// <returns>The <see cref="Pose"/> with the modified component.</returns>
        public static Pose AnyPosition ( this Pose pose ) => new Pose ( InvalidPosition, default );

        /// <summary>
        /// Checks whether the given <see cref="Pose"/> has a valid rotation.
        /// </summary>
        /// <param name="pose">The <see cref="Pose"/> to operate on.</param>
        /// <returns>Whether this pose has a valid rotation.</returns>
        public static bool HasRotation ( this Pose pose ) => !pose.rotation.Equals ( InvalidRotation );

        /// <summary>
        /// Checks whether the given <see cref="Pose"/> has a valid position.
        /// </summary>
        /// <param name="pose">The <see cref="Pose"/> to operate on.</param>
        /// <returns>Whether this pose has a valid position.</returns>
        public static bool HasPosition ( this Pose pose ) => !pose.position.Equals ( InvalidPosition );


        /// <summary>
        /// Create a <see cref="Pose"/> from a position and rotation. Provided for code symmetry.
        /// </summary>
        /// <param name="position">The position to be used in the pose.</param>
        /// <param name="rotation">The rotation to be used in the pose.</param>
        /// <returns>The constructed pose.</returns>
        public static Pose PoseFrom ( Vector3 position, Quaternion rotation ) => new ( position, rotation );

        /// <summary>
        /// Create a <see cref="Pose"/> from just a position and an invalid rotation.
        /// </summary>
        /// <param name="position">The position to be used in the pose.</param>
        /// <returns>The constructed pose.</returns>
        public static Pose PoseFrom ( Vector3 position ) => new Pose ( position, InvalidRotation );

        /// <summary>
        /// Create a <see cref="Pose"/> from a <see cref="Transform"/> components rotation and position.
        /// </summary>
        /// <param name="transform">The <see cref="Transform"/> to be used to derive the pose.</param>
        /// <returns>The constructed pose.</returns>
        public static Pose PoseFrom ( Transform transform ) => new Pose ( transform.position, transform.rotation );

        /// <summary>
        /// Create a <see cref="Pose"/> from just a rotation and an invalid position.
        /// </summary>
        /// <param name="rotation">The position to be used in the pose.</param>
        /// <returns>The constructed pose.</returns>
        public static Pose PoseFrom ( Quaternion rotation ) => new Pose ( InvalidPosition, rotation );

        /// <summary>
        /// Gets the distance between this and another pose.
        /// </summary>
        /// <param name="source">The first pose to compare with.</param>
        /// <param name="destination">The first pose to compare with.</param>
        /// <returns>The distance in world units.</returns>
        public static float DistanceTo ( this Pose source, Pose destination ) {
            return Vector3.Distance ( source.position, destination.position );
        }

        /// <summary>
        /// Gets the angle in degrees between this and another pose.
        /// </summary>
        /// <param name="source">The first pose to compare with.</param>
        /// <param name="destination">The first pose to compare with.</param>
        /// <returns>The angle in degrees.</returns>
        public static float AngleBetween ( this Pose source, Pose destination ) {
            return Quaternion.Angle ( source.rotation, destination.rotation );
        }
    }
}