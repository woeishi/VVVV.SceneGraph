using System;
using System.Linq;
using System.Collections.Generic;
using SlimDX;

namespace SceneGraph.Core.Animation
{
    public class Channel
    {
        public string Name { get; }
        public float Duration { get; }
        public float TicksPerSecond { get; }

        Track<Vector3> Positions = new Track<Vector3>("Position", Vector3.Lerp);
        public IEnumerable<float> PositionTime => Positions.Keys.Select(k => k.Marker);
        public List<Vector3> Position => Positions.Keys.Select(k => k.Value).ToList();

        Track<Vector3> Scalings = new Track<Vector3>("Scale", Vector3.Lerp);
        public IEnumerable<float> ScaleTime => Scalings.Keys.Select(k => k.Marker);
        public List<Vector3> Scale => Scalings.Keys.Select(k => k.Value).ToList();

        Track<Quaternion> Rotations = new Track<Quaternion>("Rotation", Quaternion.Slerp);
        public IEnumerable<float> RotationTime => Rotations.Keys.Select(k => k.Marker);
        public List<Quaternion> Rotation => Rotations.Keys.Select(k => k.Value).ToList();

        internal Transform Original;

        public Channel(string name, float duration, float ticksPerSecond)
        {
            Name = name;
            Duration = duration;
            TicksPerSecond = ticksPerSecond;
        }

        internal Channel(Channel other)
        {
            Name = other.Name;
            Duration = other.Duration;
            TicksPerSecond = other.TicksPerSecond;

            Positions = other.Positions;
            Scalings = other.Scalings;
            Rotations = other.Rotations;
        }

        internal void AppendPosition(float key, Vector3 value) => Positions.AppendKey(key, value);
        internal void AppendScale(float key, Vector3 value) => Scalings.AppendKey(key, value);
        internal void AppendRoatation(float key, Quaternion value) => Rotations.AppendKey(key, value);

        public Matrix GetMatrix(float lookup, bool normalize = false)
        {
            lookup = normalize ? lookup * Duration : lookup;
            var t = Matrix.Translation(Positions.GetValue(lookup));
            var s = Matrix.Scaling(Scalings.GetValue(lookup));
            var r = Matrix.RotationQuaternion(Rotations.GetValue(lookup));
            return s * r * t;
        }
    }

    class Track<T>
    {
        string Name;
        internal List<Key<T>> Keys;
        internal Func<T, T, float, T> LerpFunc;

        internal Track(string name, Func<T, T, float, T> lerpFunction)
        {
            Name = name;
            Keys = new List<Key<T>>();
            LerpFunc = lerpFunction;
        }

        internal void AppendKey(float key, T value)
        {
            if (Keys.Count == 0 || key > Keys[Keys.Count - 1].Marker)
                Keys.Add(new Key<T>(key, value));
            else
                throw new ArgumentException($"Keyframes of {Name} not sorted");
        }

        internal T GetValue(float lookup)
        {
            Key<T> start;
            Key<T> end;
            var t = GetPair(lookup, out start, out end);
            if (t == -1)
                return start.Value;
            else if (t == 2)
                return end.Value;
            else
                return LerpFunc(start.Value, end.Value, t);
        }

        float GetPair(float lookup, out Key<T> start, out Key<T> end)
        {
            if (lookup < Keys[0].Marker)
            {
                start = Keys[0];
                end = Keys[0];
                return -1;
            }
            if (lookup >= Keys[Keys.Count-1].Marker)
            {
                start = Keys[Keys.Count - 1];
                end = Keys[Keys.Count - 1];
                return 2;
            }
            int lower = 0;
            int upper = Keys.Count - 1;
            BinarySearch(lookup, Keys, ref lower, ref upper);
            start = Keys[lower];
            end = Keys[upper];
            return (lookup - start.Marker) / (end.Marker - start.Marker);

        }

        static void BinarySearch(float lookup, List<Key<T>> list, ref int lower, ref int upper)
        {
            int center = (int)Math.Truncate((upper-lower)/2.0)+lower;
            if (lookup < list[center].Marker)
            {
                upper = center;
            }
            else
            {
                lower = center;
            }
            if (upper - lower > 1)
                BinarySearch(lookup, list, ref lower, ref upper);

        }
    }

    struct Key<T>
    {
        public float Marker { get; set; }
        public T Value { get; set; }
        public Key(float marker, T value)
        {
            Marker = marker;
            Value = value;
        }
    }
}
