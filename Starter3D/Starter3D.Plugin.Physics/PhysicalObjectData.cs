using System;
using System.Linq;
using OpenTK;
using Starter3D.API.controller;
using Starter3D.API.geometry;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using Starter3D.API.scene;
using Starter3D.API.scene.nodes;
using Starter3D.API.scene.persistence;
using Starter3D.API.utils;

namespace Starter3D.Plugin.Physics
{
    public enum Instant { Current, Next }
    public abstract class PhysicalObjectData
    {
        protected float _mass;

        protected Vector3 _nextPosition;
        protected Vector3 _position;
        protected Vector3 _nextVelocity;
        protected Vector3 _velocity;
        protected Quaternion _rotation;
        protected Vector3 _scale;

        protected Matrix4 _transMatrix;
        protected Matrix4 _rotMatrix;
        protected Matrix4 _scaleMatrix;

        protected Matrix4 _modelTransform;
        protected bool _dirty = true;
        protected IMesh _mesh;

        public Matrix4 ModelTransform
        {
            get
            {
                if (_dirty)
                {
                    _modelTransform = _scaleMatrix * _rotMatrix * _transMatrix;
                    _dirty = false;
                }
                return _modelTransform;
            }
        }
        public Quaternion Rotation
        {
            get { return _rotation; }
            set { _rotation = value; _rotMatrix = Matrix4.CreateFromQuaternion(_rotation); _dirty = true; }
        }
        public Vector3 Scale
        {
            get { return _scale; }
            set { _scale = value; _scaleMatrix = Matrix4.CreateScale(_scale); _dirty = true; }
        }
        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; _transMatrix = Matrix4.CreateTranslation(_position); _dirty = true; }
        }
        public Matrix4 TranslationMatrix { get { return _transMatrix; } }
        public Matrix4 RotationMatrix { get { return _rotMatrix; } }
        public Matrix4 ScaleMatrix { get { return _scaleMatrix; } }

        public Vector3 Velocity { get { return _velocity; } set { _velocity = value; } }
        public float Mass { get { return _mass; } set { _mass = value; } }
        public Vector3 NextPosition { get { return _nextPosition; } set { _nextPosition = value; } }
        public Vector3 NextVelocity { get { return _nextVelocity; } set { _nextVelocity = value; } }

        public PhysicalObjectData(float mass, Vector3 velocity, Vector3 position, Quaternion rotation, Vector3 scale, IMesh mesh)
        {
            _velocity = velocity;
            _mass = mass;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            _nextPosition = position;
            _nextVelocity = velocity;
            _mesh = mesh;
        }

        public abstract Vector3 BoundingBox_Min(Instant instant);
        public abstract Vector3 BoundingBox_Max(Instant instant);
        public abstract float BoundingBox_MinX { get; }
        public abstract float BoundingBox_MaxX { get; }
        public abstract void UpdateVariablesForNextStep();

    }
    public class BBox_X
    {
        public PhysicalObjectData Obj;
        public bool Min;
        public BBox_X(PhysicalObjectData obj, bool min)
        {
            Obj = obj;
            Min = min;
        }
        public float X
        {
            get { return Min ? Obj.BoundingBox_MinX : Obj.BoundingBox_MaxX; }
        }
    }
}
