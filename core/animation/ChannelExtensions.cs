using System;
using System.Collections.Generic;
using SlimDX;

namespace SceneGraph.Core.Animations
{
    internal static class ChannelExtensions
    {
        internal static Channel<Vector3> ToTranslation(this IEnumerable<Marker<Vector3>> markers) =>
            new Channel<Vector3>("translation", markers, Vector3.Lerp, Matrix.Translation);

        internal static Channel<Vector3> ToScale(this IEnumerable<Marker<Vector3>> markers) =>
            new Channel<Vector3>("scale", markers, Vector3.Lerp, Matrix.Scaling);

        internal static Channel<Quaternion> ToRotation(this IEnumerable<Marker<Quaternion>> markers) =>
            new Channel<Quaternion>("rotation", markers, Quaternion.Slerp, Matrix.RotationQuaternion);
    }
}
