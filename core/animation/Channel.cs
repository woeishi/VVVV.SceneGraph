using System;
using System.Collections.Generic;
using System.Linq;
using SlimDX;

namespace SceneGraph.Core.Animations
{
    public interface IChannel
    {
        string Type { get; }
        IMarker[] Markers { get; }
    }

    public interface IChannel<T> : IChannel
    {
        T Sample(float lookup);
    }

    class Channel<T> : IChannel<Matrix>
    {
        public string Type { get; }
        Marker<T>[] _markers;
        public IMarker[] Markers => _markers.Cast<IMarker>().ToArray();

        Func<T, T, float, T> InterpolationFunc;
        Func<T, Matrix> OutputFunc;

        internal Channel(string type, IEnumerable<Marker<T>> markers, Func<T, T, float, T> interpolationFunc, Func<T, Matrix> toMatrixFunc)
        {
            Type = type;
            _markers = markers.ToArray();
            InterpolationFunc = interpolationFunc;
            OutputFunc = toMatrixFunc;
        }

        public Matrix Sample(float lookup) => OutputFunc(GetValue(lookup));

        T GetValue(float lookup)
        {
            Marker<T> start;
            Marker<T> end;
            var t = GetPair(lookup, out start, out end);
            if (t == -1)
                return start.Value;
            else if (t == 2)
                return end.Value;
            else
                return InterpolationFunc(start.Value, end.Value, t);
        }

        float GetPair(float lookup, out Marker<T> start, out Marker<T> end)
        {
            if (lookup < _markers[0].Key)
            {
                start = _markers[0];
                end = _markers[0];
                return -1;
            }
            if (lookup >= _markers[_markers.Length - 1].Key)
            {
                start = _markers[_markers.Length - 1];
                end = _markers[_markers.Length - 1];
                return 2;
            }
            int lower = 0;
            int upper = _markers.Length - 1;
            BinarySearch(lookup, _markers, ref lower, ref upper);
            start = _markers[lower];
            end = _markers[upper];
            return (lookup - start.Key) / (end.Key - start.Key);

        }

        static void BinarySearch(float lookup, Marker<T>[] markers, ref int lower, ref int upper)
        {
            int center = (int)Math.Truncate((upper - lower) / 2.0) + lower;
            if (lookup < markers[center].Key)
            {
                upper = center;
            }
            else
            {
                lower = center;
            }
            if (upper - lower > 1)
                BinarySearch(lookup, markers, ref lower, ref upper);
        }
    }
}