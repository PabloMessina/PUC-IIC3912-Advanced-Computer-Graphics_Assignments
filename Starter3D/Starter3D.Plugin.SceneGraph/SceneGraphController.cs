using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenTK;
using Starter3D.API.controller;
using Starter3D.API.geometry.primitives;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using Starter3D.API.scene;
using Starter3D.API.scene.nodes;
using Starter3D.API.scene.persistence;
using Starter3D.API.utils;

namespace Starter3D.Plugin.SceneGraph
{
    public class SceneGraphController : ViewModelBase, IController
    {
        private const string ScenePath = @"scenes/scenegraph.xml";
        private const string ResourcePath = @"resources/scenegraphresources.xml";

        private readonly IRenderer _renderer;
        private readonly ISceneReader _sceneReader;
        private readonly IResourceManager _resourceManager;

        private readonly IScene _scene;

        private readonly SceneGraphView _centralView;

        private const float angleDelta = 1f * (float)(Math.PI/180);
        private const float _2PI = (float)Math.PI * 2;
        private const double tickSpan = 20;
        private double elapsedTime = 0;
        private DateTime startTime;


        private List<ShapeNode> rootShapeNodes = new List<ShapeNode>();
        private Dictionary<ShapeNode, ShapeTransformParams> shapeTransforms
            = new Dictionary<ShapeNode, ShapeTransformParams>();

        struct ShapeTransformParams
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        public int Width
        {
            get { return 800; }
        }

        public int Height
        {
            get { return 600; }
        }

        public bool IsFullScreen
        {
            get { return true; }
        }

        public object CentralView
        {
            get { return _centralView; }
        }

        public object LeftView
        {
            get { return null; }
        }

        public object RightView
        {
            get { return null; }
        }

        public object TopView
        {
            get { return null; }
        }

        public object BottomView
        {
            get { return null; }
        }

        public bool HasUserInterface
        {
            get { return true; }
        }

        public string Name
        {
            get { return "Scene Graph"; }
        }


        public SceneGraphController(IRenderer renderer, ISceneReader sceneReader, IResourceManager resourceManager)
        {
            if (renderer == null) throw new ArgumentNullException("renderer");
            if (sceneReader == null) throw new ArgumentNullException("sceneReader");
            if (resourceManager == null) throw new ArgumentNullException("resourceManager");
            _renderer = renderer;
            _sceneReader = sceneReader;
            _resourceManager = resourceManager;

            _resourceManager.Load(ResourcePath);
            _scene = _sceneReader.Read(ScenePath);

            _centralView = new SceneGraphView(this);
        }

        public void Load()
        {
            InitRenderer();

            _resourceManager.Configure(_renderer);
            _scene.Configure(_renderer);

            
            foreach (ShapeNode sn in _scene.Shapes)
            {
                //collect root nodes
                if (sn.Parent == null || ! (sn.Parent is ShapeNode)) {
                    rootShapeNodes.Add(sn);
                }
                //back up the model transform relative to this shape
                ShapeTransformParams transfParams;
                transfParams.Position = sn.Position;
                transfParams.Rotation = sn.Rotation;
                transfParams.Scale = sn.Scale;
                shapeTransforms[sn] = transfParams;
            }

            startTime = DateTime.Now;

        }

        private void InitRenderer()
        {
            _renderer.SetBackgroundColor(0.9f, 0.9f, 1.0f);
            _renderer.EnableZBuffer(true);
            _renderer.EnableWireframe(false);
            _renderer.SetCullMode(CullMode.None);
        }

        public void Render(double time)
        {
            _scene.Render(_renderer);            
        }


        private Matrix4 GetModelTransform(ShapeNode shapeNode)
        {
            var translation = Matrix4.CreateTranslation(shapeNode.Position);
            var rotation = Matrix4.CreateFromQuaternion(shapeNode.Rotation);
            var scale = Matrix4.CreateScale(shapeNode.Scale);
            var matrix = scale * rotation * translation;
            return matrix;
        }
        private Matrix4 GetModelTransform(Vector3 translation, Quaternion  rotation, Vector3 scale)
        {
            var _translation = Matrix4.CreateTranslation(translation);
            var _rotation = Matrix4.CreateFromQuaternion(rotation);
            var _scale = Matrix4.CreateScale(scale);
            var matrix = _scale * _rotation * _translation;
            return matrix;
        }

        private void updateTransformsTopDown(ShapeNode currentShape, Matrix4 parentTransform)
        {
            //read, increase angle and back up again
            var transfParams = shapeTransforms[currentShape];
            transfParams.Rotation = increaseRotationAngle(transfParams.Rotation, angleDelta);
            shapeTransforms[currentShape] = transfParams;

            //combine with parent trasnform and update shape's transform parameters
            var matrix = GetModelTransform(transfParams.Position, transfParams.Rotation, transfParams.Scale);
            matrix *= parentTransform;
            currentShape.Position = matrix.ExtractTranslation();
            currentShape.Rotation = matrix.ExtractRotation();
            currentShape.Scale = matrix.ExtractScale();

            //recursivily call for each child
            foreach (ISceneNode childNode in currentShape.Children)
            {
                if (childNode is ShapeNode)
                    updateTransformsTopDown((ShapeNode)childNode, matrix);
            }
            
        }
        private void updateTransformsTopDown_root(ShapeNode root)
        {
            //read, increase angle and back up again
            var transfParams = shapeTransforms[root];
            transfParams.Rotation = increaseRotationAngle(transfParams.Rotation, angleDelta);
            shapeTransforms[root] = transfParams;

            //combine with parent trasnform and update shape's transform parameters
            var matrix = GetModelTransform(transfParams.Position, transfParams.Rotation, transfParams.Scale);
            //root.Rotation = matrix.ExtractRotation();
            root.Rotation = transfParams.Rotation;

            //recursivily call for each child
            foreach (ISceneNode childNode in root.Children)
            {
                if (childNode is ShapeNode)
                    updateTransformsTopDown((ShapeNode)childNode, matrix);
            }
        }
        private Quaternion increaseRotationAngle(Quaternion quaternion, float angleDelta) {
            Vector3 axis;
            float angle;
            quaternion.ToAxisAngle(out axis, out angle);
            angle += angleDelta;
            if (angle >= _2PI)
                angle -= _2PI;
            return Quaternion.FromAxisAngle(axis, angle);
        }

        public void Update(double time)
        {

            if ((DateTime.Now - startTime).TotalMilliseconds > elapsedTime)
            {
                elapsedTime += tickSpan;
                foreach (ShapeNode root in rootShapeNodes)
                {
                    updateTransformsTopDown_root(root);
                }                
            }

            //foreach (ShapeNode shape in _scene.Shapes)
            //{

            //    Quaternion q = shape.Rotation;
            //    Vector3 axis;
            //    float angle;
            //    bool first = true;

            //    ShapeNode _shape = shape;
            //    while (true)
            //    {
            //        _shape.Rotation.ToAxisAngle(out axis, out angle);
            //        angle += angleDelta;
            //        if (angle > _2PI)
            //            angle -= _2PI;

            //        _shape.

            //        if (first)
            //        {
            //            q = Quaternion.FromAxisAngle(axis, angle);
            //            first = false;
            //        }
            //        else
            //            q = Quaternion.FromAxisAngle(axis, angle) * q;

            //        if (_shape.Parent != null && _shape.Parent is ShapeNode)
            //            _shape = (ShapeNode)_shape.Parent;
            //        else break;
            //    }

            //    shape.Rotation = q;

            //}
        }

        public void UpdateSize(double width, double height)
        {
            var perspectiveCamera = _scene.CurrentCamera as PerspectiveCamera;
            if (perspectiveCamera != null)
                perspectiveCamera.AspectRatio = (float)(width / height);
        }

        public void MouseWheel(int delta, int x, int y)
        {

        }

        public void MouseDown(ControllerMouseButton button, int x, int y)
        {

        }

        public void MouseUp(ControllerMouseButton button, int x, int y)
        {

        }

        public void MouseMove(int x, int y, int deltaX, int deltaY)
        {

        }

        public void KeyDown(int key)
        {

        }



    }
}