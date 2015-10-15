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
    public class PhysicalObjectData
    {
        float _mass;

        Vector3 _nextPosition;
        Vector3 _position;
        Vector3 _nextVelocity;
        Vector3 _velocity;
        Quaternion _rotation;
        Vector3 _scale;

        Matrix4 _transMatrix;
        Matrix4 _rotMatrix;
        Matrix4 _scaleMatrix;

        Matrix4 _modelTransform;
        bool _dirty = true;

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

        public PhysicalObjectData(float mass, Vector3 velocity, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            _velocity = velocity;
            _mass = mass;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            _nextPosition = position;
            _nextVelocity = velocity;
        }

        public void UpdatePositionAndVelocity()
        {
            Position = _nextPosition;
            Velocity = _nextVelocity;
        }


    }
}
