using OpenTK;
using Starter3D.API.geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.Physics
{
    public class Sphere : PhysicalObjectData
    {
        protected float _radius;
        protected Vector3 _bb_backleftbottom;
        protected Vector3 _bb_frontrightup;

        private static Vector3 GetScale(IMesh mesh, float radius)
        {
            if (mesh.VerticesCount > 0)
            {
                float scaleFactor = radius;
                foreach (IVertex v in mesh.Vertices)
                {
                    scaleFactor /= v.Position.Length;
                    break;
                }
                return new Vector3(scaleFactor);
            }
            else
            {
                throw new ArgumentException("mesh has no vertices");
            }
        }

        public float Radius { get { return _radius; } }

        public Sphere(float mass, Vector3 velocity, Vector3 position, Quaternion rotation, IMesh mesh, float radius)
            : base(mass, velocity, position, rotation, GetScale(mesh, radius), mesh)
        {
            _radius = radius;
        }

        public override Vector3 BoundingBox_Min(Instant instant)
        {
            if (instant == Instant.Current)
            {
                _bb_backleftbottom.X = _position.X - _radius;
                _bb_backleftbottom.Y = _position.Y - _radius;
                _bb_backleftbottom.Z = _position.Z - _radius;
            }
            else
            {
                _bb_backleftbottom.X = _nextPosition.X - _radius;
                _bb_backleftbottom.Y = _nextPosition.Y - _radius;
                _bb_backleftbottom.Z = _nextPosition.Z - _radius;
            }
            return _bb_backleftbottom;
        }

        public override Vector3 BoundingBox_Max(Instant instant)
        {
            if (instant == Instant.Current)
            {
                _bb_frontrightup.X = _position.X + _radius;
                _bb_frontrightup.Y = _position.Y + _radius;
                _bb_frontrightup.Z = _position.Z + _radius;
            }
            else
            {
                _bb_frontrightup.X = _nextPosition.X + _radius;
                _bb_frontrightup.Y = _nextPosition.Y + _radius;
                _bb_frontrightup.Z = _nextPosition.Z + _radius;
            }
            return _bb_frontrightup;
        }
        public override float BoundingBox_MinX { get { return _nextPosition.X - _radius; } }
        public override float BoundingBox_MaxX { get { return _nextPosition.X + _radius; } }

        public override void UpdateVariablesForNextStep()
        {
            Velocity = _nextVelocity;
            Position = _nextPosition;
        }
    }
}
