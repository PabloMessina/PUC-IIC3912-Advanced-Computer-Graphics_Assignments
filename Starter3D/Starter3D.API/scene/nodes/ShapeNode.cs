using System;
using System.IO;
using OpenTK;
using Starter3D.API.geometry;
using Starter3D.API.geometry.factories;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using Starter3D.API.scene.persistence;
using Starter3D.API.utils;

namespace Starter3D.API.scene.nodes
{
    public class ShapeNode : BaseSceneNode
    {
        private static float _2PI = (float)Math.PI * 2;

        private IShape _shape;
        private readonly IShapeFactory _shapeFactory;
        private readonly IResourceManager _resourceManager;
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _scale;
        private float _orientationAngle;
        private Vector3 _orientationAxis;

        private Matrix4 _localModelTransform;
        private Matrix4 _parentModelTransform;
        private Matrix4 _modelTransform;

        public IShape Shape
        {
            get { return _shape; }
            set { _shape = value; }
        }

        public Quaternion Rotation
        {
            get { return _rotation; }
            set { _rotation = value; localModelTransformChanged(); }
        }

        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; localModelTransformChanged(); }
        }

        public Vector3 Scale
        {
            get { return _scale; }
            set { _scale = value; localModelTransformChanged(); }
        }

        public Vector3 OrientationAxis
        {
            get { return _orientationAxis; }
        }

        public float OrientationAngle
        {
            get { return _orientationAngle; }
        }

        public Matrix4 ModelTransform
        {
            get { return _modelTransform; }
        }

        public Matrix4 ParentModelTransform
        {
            get { return _parentModelTransform; }
            set
            {
                _parentModelTransform = value;
                _modelTransform = _localModelTransform * _parentModelTransform;                
            }
        }

        public ShapeNode(IShape shape, IShapeFactory shapeFactory, IResourceManager resourceManager, Vector3 scale = default(Vector3), Vector3 position = default(Vector3),
          Vector3 orientationAxis = default(Vector3), float orientationAngle = 0)
            : this(shapeFactory, resourceManager, scale, position, orientationAxis, orientationAngle)
        {
            _shape = shape;
        }

        public ShapeNode(IShapeFactory shapeFactory, IResourceManager resourceManager, Vector3 scale = default(Vector3), Vector3 position = default(Vector3),
          Vector3 orientationAxis = default(Vector3), float orientationAngle = 0)
        {
            _shapeFactory = shapeFactory;
            _resourceManager = resourceManager;
            Init(scale, position, orientationAxis, orientationAngle);
        }

        private void Init(Vector3 scale = default(Vector3), Vector3 position = default(Vector3),
          Vector3 orientationAxis = default(Vector3), float orientationAngle = 0)
        {
            _scale = scale;
            _position = position;
            _rotation = Quaternion.FromAxisAngle(orientationAxis, orientationAngle);
            _orientationAngle = orientationAngle;
            _orientationAxis = orientationAxis;
            _localModelTransform = GetModelTransform(_position, _rotation, _scale);
            _parentModelTransform = Matrix4.Identity;
            _modelTransform = _localModelTransform * _parentModelTransform;            
        }

        public override void Load(ISceneDataNode sceneDataNode)
        {
            var scale = new Vector3(1, 1, 1);
            var position = new Vector3();
            var orientationAxis = new Vector3(0, 0, 1);
            var orientationAngle = 0.0f;
            if (sceneDataNode.HasParameter("scale"))
            {
                scale = sceneDataNode.ReadVectorParameter("scale");
            }
            if (sceneDataNode.HasParameter("position"))
            {
                position = sceneDataNode.ReadVectorParameter("position");
            }
            if (sceneDataNode.HasParameter("orientationAxis"))
            {
                orientationAxis = sceneDataNode.ReadVectorParameter("orientationAxis");
                orientationAngle = sceneDataNode.ReadFloatParameter("angle");
            }
            Init(scale, position, orientationAxis, orientationAngle);

            var name = sceneDataNode.ReadParameter("shapeName");

            if (sceneDataNode.HasParameter("filePath"))
            {
                var shapeTypeString = sceneDataNode.ReadParameter("shapeType");
                var shapeType = (ShapeType)Enum.Parse(typeof(ShapeType), shapeTypeString);
                var filePath = sceneDataNode.ReadParameter("filePath");
                var fileTypeString = Path.GetExtension(filePath).TrimStart('.');

                var fileType = (FileType)Enum.Parse(typeof(FileType), fileTypeString);
                _shape = _shapeFactory.CreateShape(shapeType, fileType, name);
                _shape.Load(filePath);
            }
            else
            {
                var primitveTypeString = sceneDataNode.ReadParameter("primitiveType");
                var primitveType = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), primitveTypeString);
                _shape = _shapeFactory.CreateShape(primitveType, name);
            }

            var materialKey = sceneDataNode.ReadParameter("material");
            if (_resourceManager.HasMaterial(materialKey))
                _shape.Material = _resourceManager.GetMaterial(materialKey);
        }

        public override void Configure(IRenderer renderer)
        {
            _shape.Configure(renderer);
        }

        public override void Render(IRenderer renderer)
        {
           // var modelTransform = GetModelTransform();
            _shape.Render(renderer, _modelTransform);
        }

        internal Matrix4 GetModelTransform(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            var translation_m = Matrix4.CreateTranslation(translation);
            var rotation_m = Matrix4.CreateFromQuaternion(rotation);
            var scale_m = Matrix4.CreateScale(scale);
            return scale_m * rotation_m * translation_m;
        }

        private void localModelTransformChanged() {
            _localModelTransform = GetModelTransform(_position, _rotation, _scale);
            _modelTransform = _localModelTransform * _parentModelTransform;
        }

        private void parentModelTransformChanged(Matrix4 newMatrix)
        {
            _parentModelTransform = newMatrix;
            _modelTransform = _localModelTransform * _parentModelTransform;
            propagateModelTransformToChildren();
        }

        public void propagateModelTransformToChildren()
        {
            foreach (ShapeNode sn in _children)
            {
                sn.parentModelTransformChanged(_modelTransform);
            }
        }

        public void changeRotationAngle(float angleDelta)
        {
            /*
            Vector3 axis;
            float angle;
            _rotation.ToAxisAngle(out axis, out angle);
            angle += angleDelta;
             */
            if (float.IsNaN(angleDelta))
                return;

            _orientationAngle += angleDelta;

            while (_orientationAngle >= _2PI)
                _orientationAngle -= _2PI;
            while (_orientationAngle < 0)
                _orientationAngle += _2PI;

            _rotation = Quaternion.FromAxisAngle(_orientationAxis,_orientationAngle);

            localModelTransformChanged();
        }

        public void setRotationAngle(float newAngle)
        {

            if (float.IsNaN(newAngle))
                return;

            _orientationAngle = newAngle;
            while (_orientationAngle >= _2PI)
                _orientationAngle -= _2PI;
            while (_orientationAngle < 0)
                _orientationAngle += _2PI;

            _rotation = Quaternion.FromAxisAngle(_orientationAxis, _orientationAngle);
            localModelTransformChanged();
        }

        public void setRotationAxis(Vector3 newAxis)
        {
            _orientationAxis = newAxis;
            _rotation = Quaternion.FromAxisAngle(_orientationAxis,_orientationAngle);
            localModelTransformChanged();
        }

    }
}