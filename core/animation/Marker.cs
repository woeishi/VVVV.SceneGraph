namespace SceneGraph.Core.Animations
{
    public interface IMarker
    {
        float Key { get; }
    }

    public interface IMarker<T> : IMarker
    {
        T Value { get; }
    }

    struct Marker<T> : IMarker<T>
    {
        public float Key { get; set; }
        public T Value { get; set; }
        public Marker(float marker, T value)
        {
            Key = marker;
            Value = value;
        }
    }
}