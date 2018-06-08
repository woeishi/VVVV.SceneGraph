using System;

using AssimpNet;
using SlimDX;

namespace SceneGraph.Core
{
    public class CameraElement : Element
    {
        public override string Type => "Camera";
        
        AssimpCamera Camera { get; }
        public Matrix View { get; }
        
        //HVOF bug in assimp
        //public float HFOV { get; }
        public float NearPlane { get; }
        public float FarPlane { get; }
        public float AspectRatio { get; }

        internal CameraElement(Scene scene, AssimpNode self, int id, AssimpCamera camera) : base(scene, self, id)
        {
            Camera = camera;

            View = Matrix.LookAtLH(Camera.Position, Camera.LookAt, Camera.UpVector);
            //Projection = Matrix.PerspectiveFovLH(Camera.HFOV, 1, Camera.NearPlane, Camera.FarPlane);

            NearPlane = Camera.NearPlane;
            FarPlane = Camera.FarPlane;
            AspectRatio = Camera.AspectRatio;
        }
    }
}
