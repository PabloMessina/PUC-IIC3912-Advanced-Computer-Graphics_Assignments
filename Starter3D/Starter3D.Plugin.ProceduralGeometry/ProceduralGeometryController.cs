using OpenTK;
using Starter3D.API.controller;
using Starter3D.API.geometry;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using Starter3D.API.scene;
using Starter3D.API.scene.nodes;
using Starter3D.API.scene.persistence;
using Starter3D.API.utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using Starter3D.API.geometry.primitives;
using System.Text;
using System.IO;
using System.Xml;

namespace Starter3D.Plugin.ProceduralGeometry
{
    public class ProceduralGeometryController : ViewModelBase, IController
    {
        private const string ScenePath = @"scenes/procedural_geometry_scene.xml";
        private const string ResourcePath = @"resources/procedural_geometry_resources.xml";

        private readonly IRenderer _renderer;
        private readonly IResourceManager _resourceManager;
        private readonly ISceneReader _sceneReader;

        private readonly IScene _scene;

        private readonly ProceduralGeometryView _view;

        private double _width;
        private double _height;

        private Sphere _sphere;
        private DynamicMesh _mesh = new DynamicMesh();
        private List<ICurve> _wireframe = new List<ICurve>();

        private bool _isMouseDownLeft = false;
        private bool _isMouseDownRight = false;

        private bool _showingWireframe = false;

        private IEnumerable<IMaterial> _materials;
        private IMaterial _currMaterial;

        


        public ProceduralGeometryController(IRenderer renderer, ISceneReader sceneReader, IResourceManager resourceManager)
        {
            _renderer = renderer;
            _resourceManager = resourceManager;
            _sceneReader = sceneReader;

            _resourceManager.Load(ResourcePath);
            _scene = _sceneReader.Read(ScenePath);

            _view = new ProceduralGeometryView(this);
            _view.DataContext = this;

            _materials = _resourceManager.GetMaterials();
            _currMaterial = _resourceManager.GetMaterial("greenSpecular");

        }


        public IMaterial CurrentMaterial
        {
            get { return _currMaterial; }
            set
            {
                if (_currMaterial == value) return;
                _currMaterial = value;
                if (_showingWireframe)
                {
                    _wireframe.ForEach(c => c.Material = _currMaterial);
                }
                else
                {
                    _mesh.Material = _currMaterial;
                }
                RaisePropertyChanged("CurrentMaterial");
            }
        }

        public float Radius
        {
            get { return _sphere.Radius; }
            set
            {
                if (_sphere.Radius == value) return;
                _sphere.Radius = value;
                if (_showingWireframe)
                {
                    _sphere.GenerateWireFrame(_wireframe, _currMaterial, _renderer);
                }
                else
                {
                    _sphere.GenerateMesh(_mesh, _currMaterial, _renderer);
                }
                RaisePropertyChanged("Radius");
            }
        }

        public int Meridians
        {
            get { return _sphere.Meridians; }
            set
            {
                if (_sphere.Meridians == value) return;
                _sphere.Meridians = value;
                if (_showingWireframe)
                {
                    _sphere.GenerateWireFrame(_wireframe, _currMaterial, _renderer);
                }
                else
                {
                    _sphere.GenerateMesh(_mesh, _currMaterial, _renderer);
                }
                RaisePropertyChanged("Meridians");
            }
        }


        public int Parallels
        {
            get { return _sphere.Parallels; }
            set
            {
                if (_sphere.Parallels == value) return;
                _sphere.Parallels = value;
                if (_showingWireframe)
                {
                    _sphere.GenerateWireFrame(_wireframe, _currMaterial, _renderer);
                }
                else
                {
                    _sphere.GenerateMesh(_mesh, _currMaterial, _renderer);
                }
                RaisePropertyChanged("Parallels");
            }
        }

        public IEnumerable<IMaterial> Materials
        {
            get { return _materials; }
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
            get { return _view; }
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
            get { return "Procedural Geometry Generator"; }
        }

        public void Load()
        {
            InitRenderer();
            _resourceManager.Configure(_renderer);

            _sphere = new Sphere
            {
                CenterX = 0,
                CenterY = 0,
                Meridians = 20,
                Parallels = 20,
                Radius = 20
            };

            _sphere.GenerateMesh(_mesh, _currMaterial, _renderer);
            _showingWireframe = false;
        }

        private void InitRenderer()
        {
            _renderer.SetBackgroundColor(0.0f, 0.0f, 1.0f);
            _renderer.EnableZBuffer(false);
            _renderer.EnableWireframe(false);
            _renderer.SetCullMode(CullMode.None);
        }

        public void Render(double deltaTime)
        {
            _scene.Render(_renderer);
            if (_showingWireframe)
            {
                _wireframe.ForEach(line => line.Render(_renderer, Matrix4.Identity));
            }
            else
            {
                _mesh.Render(_renderer, Matrix4.Identity);
            }
            
        }

        public void Update(double deltaTime)
        {

        }

        public void MouseDown(ControllerMouseButton button, int x, int y)
        {
            if (button == ControllerMouseButton.Left)
                _isMouseDownLeft = true;
            else if (button == ControllerMouseButton.Right)
                _isMouseDownRight = true;
        }

        public void MouseUp(ControllerMouseButton button, int x, int y)
        {
            if (_isMouseDownRight)
                _isMouseDownRight = false;

            if (_isMouseDownLeft)
                _isMouseDownLeft = false;
        }

        public void MouseWheel(int delta, int x, int y)
        {

        }

        public void MouseMove(int x, int y, int deltaX, int deltaY)
        {
            if (_isMouseDownRight)
            {
                var cam = _scene.CurrentCamera;
                if (deltaX > 0)
                    rotateCameraRightward(cam);
                else if (deltaX < 0)
                    rotateCameraLeftward(cam);
                if (deltaY < 0)
                    rotateCameraUpward(cam);
                else if (deltaY > 0)
                    rotateCameraDownward(cam);
            }
        }

        public void KeyDown(int key)
        {
            //Controles de la camara para todos los modos
            var cam = _scene.CurrentCamera;
            switch (key)
            {
                case (34): //av pag
                    moveCameraForward(cam);
                    break;
                case (33): //av pag
                    moveCameraBackward(cam);
                    break;
                case (38): //a
                case (65 + 'w' - 'a'): //w
                    moveCameraUpward(cam);
                    break;
                case (65): //a
                case (37): //a
                    moveCameraLeftward(cam);
                    break;
                case (40): //a
                case (65 + 's' - 'a'): //s
                    moveCameraDownward(cam);
                    break;
                case (39):
                case (65 + 'd' - 'a'): //d
                    moveCameraRightward(cam);
                    break;
                case (65 + 'q' - 'a'): //q
                    moveCameraBackward(cam);
                    break;
                case (65 + 'e' - 'a'): //e
                    moveCameraForward(cam);
                    break;
                case (49 + 2 - 1): //2
                    rotateCameraCounterClockwise(cam);
                    break;
                case (49 + 3 - 1)://3
                    rotateCameraClockwise(cam);
                    break;
            }

        }

        public void UpdateSize(double width, double height)
        {
            _width = width;
            _height = height;
            var perspectiveCamera = _scene.CurrentCamera as PerspectiveCamera;
            if (perspectiveCamera != null)
                perspectiveCamera.AspectRatio = (float)(width / height);
        }

        #region Movimiento y rotación de cámara 
        
        private float _rotAngle = (float)(Math.PI / 180 * 1f);
        private float _camVelocity = 20f;
        private void rotateCameraDownward(CameraNode cam)
        {
            var forwardDir = (cam.Target - cam.Position);
            var horiztonalAxis = Vector3.Cross(forwardDir, cam.Up);
            var rotMatrix = Quaternion.FromAxisAngle(horiztonalAxis, -_rotAngle);
            cam.Up = Vector3.Transform(cam.Up, rotMatrix).Normalized();
            cam.Target = cam.Position + Vector3.Transform(forwardDir, rotMatrix);
        }

        private void rotateCameraUpward(CameraNode cam)
        {
            var forwardDir = (cam.Target - cam.Position);
            var horiztonalAxis = Vector3.Cross(forwardDir, cam.Up);
            var rotMatrix = Quaternion.FromAxisAngle(horiztonalAxis, _rotAngle);
            cam.Up = Vector3.Transform(cam.Up, rotMatrix).Normalized();
            cam.Target = cam.Position + Vector3.Transform(forwardDir, rotMatrix);
        }

        private void rotateCameraRightward(CameraNode cam)
        {
            var forwardDir = (cam.Target - cam.Position);
            var verticalAxis = cam.Up;
            var rotMatrix = Quaternion.FromAxisAngle(verticalAxis, -_rotAngle);
            cam.Target = cam.Position + Vector3.Transform(forwardDir, rotMatrix);
        }

        private void rotateCameraLeftward(CameraNode cam)
        {
            var forwardDir = (cam.Target - cam.Position);
            var verticalAxis = cam.Up;
            var rotMatrix = Quaternion.FromAxisAngle(verticalAxis, _rotAngle);
            cam.Target = cam.Position + Vector3.Transform(forwardDir, rotMatrix);
        }
        private void rotateCameraClockwise(CameraNode cam)
        {
            var forwardAxis = (cam.Target - cam.Position);
            var rotMatrix = Quaternion.FromAxisAngle(forwardAxis, _rotAngle);
            cam.Up = Vector3.Transform(cam.Up, rotMatrix).Normalized();
        }

        private void rotateCameraCounterClockwise(CameraNode cam)
        {
            var forwardAxis = (cam.Target - cam.Position);
            var rotMatrix = Quaternion.FromAxisAngle(forwardAxis, -_rotAngle);
            cam.Up = Vector3.Transform(cam.Up, rotMatrix).Normalized();
        }

        private void moveCameraRightward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            var rightDir = Vector3.Cross(forwardDir, cam.Up);
            rightDir.NormalizeFast();

            pos += rightDir * _camVelocity;
            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
        }

        private void moveCameraBackward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            pos -= forwardDir * _camVelocity;
            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
        }

        private void moveCameraLeftward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            var leftDir = Vector3.Cross(cam.Up, forwardDir);
            leftDir.NormalizeFast();

            pos += leftDir * _camVelocity;
            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
        }

        private void moveCameraForward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            pos += forwardDir * _camVelocity;

            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
        }

        private void moveCameraUpward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            pos += cam.Up * _camVelocity;
            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
        }

        private void moveCameraDownward(CameraNode cam)
        {
            var pos = cam.Position;
            var target = cam.Target;
            var forwardDir = (target - pos);
            forwardDir.NormalizeFast();

            pos -= cam.Up * _camVelocity;
            target = pos + forwardDir * 100f;

            cam.Position = pos;
            cam.Target = target;
        }

        #endregion

        public void ShowWireframe()
        {
            if (_showingWireframe) return;
            _showingWireframe = true;
            _sphere.GenerateWireFrame(_wireframe, _currMaterial, _renderer);
        }

        public void ShowMesh()
        {
            if (!_showingWireframe) return;
            _showingWireframe = false;
            _sphere.GenerateMesh(_mesh, _currMaterial, _renderer);
        }

    }
}
