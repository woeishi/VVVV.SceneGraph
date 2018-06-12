using System;
using SlimDX;

namespace SceneGraph.Core
{
    public class CameraElement : Element
    {
        public override string Type => "Camera";
        
        public Matrix View { get; }
        
        //HVOF bug in assimp
        //public float HFOV { get; }
        public float NearPlane { get; }
        public float FarPlane { get; }
        public float AspectRatio { get; }

        internal CameraElement(int id, string name, Matrix view, float near, float far, float aspect) : base(id, name, 0)
        {
            View = view;

            NearPlane = near;
            FarPlane = far;
            AspectRatio = aspect;
        }
    }
}
