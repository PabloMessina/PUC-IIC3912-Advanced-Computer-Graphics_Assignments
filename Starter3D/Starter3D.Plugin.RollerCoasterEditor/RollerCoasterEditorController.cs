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

namespace Starter3D.Plugin.RollerCoasterEditor
{
    public struct AnimationPoint
    {
        public Vector3 Position;
        public Vector3 Up;
        public Vector3 Direction;
    }
    public enum CurveType
    {
        Bezier, Catmull_Rom
    }
    public enum Mode
    {
        PickingPoints, DroppingPoints, DisplayingRollerCoaster, DisplayingSplineClosed, RunningAnimation
    }
    public class RollerCoasterEditorController : ViewModelBase, IController
    {
        #region private fields
        private Mode _mode = Mode.DroppingPoints;
        private CurveType _selectedCurveType = CurveType.Catmull_Rom;

        private const string ScenePath = @"scenes/rollercoaster_scene.xml";
        private const string ResourcePath = @"resources/rollercoaster_resources.xml";

        private readonly IRenderer _renderer;
        private readonly IResourceManager _resourceManager;
        private readonly ISceneReader _sceneReader;

        private readonly IScene _scene;

        private readonly RollerCoasterEditorView _view;        

        private double _width;
        private double _height;        

        private float _stepSize = 0.08f;

        private Matrix4 _baseMatrix_Bezier =
            new Matrix4(1, 0, 0, 0, -3, 3, 0, 0, 3, -6, 3, 0, -1, 3, -3, 1);        

        private float _camVelocity = 2f;
        private float _rotAngle = (float)(Math.PI/180 * 2f);


        private ShapeNode _pointPrototype; 
        private List<Matrix4> _pointTransforms = new List<Matrix4>();
        private List<Vector4> _pointList = new List<Vector4>();
        private int _pickedPointIndex = -1;

        private Cube _railSegmentPrototype;
        private List<Matrix4> _leftRailSegmentTransforms = new List<Matrix4>();
        private List<Matrix4> _rightRailSegmentTransforms = new List<Matrix4>();

        private Cube _sleeperPrototype;
        private List<Matrix4> _sleeperTransforms = new List<Matrix4>();

        private ShapeNode _bunny;
        private Matrix4 _cartTransform = Matrix4.Identity;

        private Cube _skybox;
        private Matrix4 _skyboxTransform;

        private float _sleeperLength = 1.6f;
        private float _sleeperWidth = 0.5f;
        private float _sleeperHeight = 0.2f;
        private float _railWidth = 0.25f;
        private float _railHeight =0.25f;

        private bool _isRightDown;
        private bool _isLeftDown;
        private float _displacementFactor;
        private Vector3 _pickedPointLastPosition;
        private Vector3 _pickedMouseLastPosition;

        private bool _curveClosed = false;

        private ICurve _curve;
        private CurveHandlerBase _curveHandler;

        private Array _modes = Enum.GetValues(typeof(Mode));

        private bool _rollerCoasterDirty = true;

        private List<AnimationPoint> _animationPoints;
        private bool _animationRunning = false;
        private DateTime _startTime;
        private double _totalEnergy;
        private double _cartVelocity;
        private float _cartHeight;
        private AnimationPoint _currentAnimationPoint;
        private AnimationPoint _nextAnimationPoint;
        private int _animationIndex;
        private Vector3 _cartPosition;
        private Vector3 _cartDirection;
        private float _segmentLength;
        private Vector3 _cameraRelativeDirection;
        private float _maxHeight;
        private float _minHeight;

        private static float G = 9.8f;
        private static float MinVel = 2.0f;

        private Random _random = new Random();

        private bool _followCart = false;

        private static Vector3 UP = new Vector3(0, 1, 0);
        private static Vector3 RIGHT = new Vector3(1, 0, 0);
        private static Vector3 FORWARD = new Vector3(0, 0, -1);
        private static Vector3 SLIGHTLY_DOWN = new Vector3(0, -1, -5);

        private Vector3 _cameraPosition_backup;
        private Vector3 _cameraTarget_backup;
        private Vector3 _cameraUp_backup;
        private bool _hasBackup = false;
        private bool pickedInOpenGL;

        private string _userFeedback;
        #endregion

        #region  Getters and Setters

        public string UserFeedback { get { return _userFeedback; } set { _userFeedback = value; RaisePropertyChanged("UserFeedback"); } }
        public double CartVelocity
        {
            get { return _cartVelocity; }
            private set { _cartVelocity = value; RaisePropertyChanged("CartVelocity"); }
        }
        public float CartHeight
        {
            get { return _cartHeight; }
            private set { _cartHeight = value; RaisePropertyChanged("CartHeight"); }
        }
        public float MinHeight { get { return _minHeight; } set { _minHeight = value; RaisePropertyChanged("MinHeight"); } }
        public float MaxHeight { get { return _maxHeight; } set { _maxHeight = value; RaisePropertyChanged("MaxHeight"); } }
        public bool CanRunAnimation
        {
            get { return _curveHandler.CanCloseCurve(_pointList); }
        }
        public bool FollowCart
        {
            get { return _followCart; }
            set
            {
                if (_followCart != value)
                {
                    _followCart = value;

                    if (_followCart)
                    {
                        _cameraPosition_backup = _scene.CurrentCamera.Position;
                        _cameraTarget_backup = _scene.CurrentCamera.Target;
                        _cameraUp_backup = _scene.CurrentCamera.Up;
                        _hasBackup = true;
                    }
                    else if (_hasBackup)
                    {
                        _scene.CurrentCamera.Position = _cameraPosition_backup;
                        _scene.CurrentCamera.Target = _cameraTarget_backup;
                        _scene.CurrentCamera.Up = _cameraUp_backup;
                    }
                    RaisePropertyChanged("FollowCart");
                }
            }
        }
        private int PickedPointIndex
        {
            get { return _pickedPointIndex; }
            set { _pickedPointIndex = value; RaisePropertyChanged("HasPickedPoint"); }
        }
        public bool HasPickedPoint
        {
            get { return _mode == Mode.PickingPoints && _pickedPointIndex != -1; }            
        }
        public CurveType SelectedCurveType
        {
            get { return _selectedCurveType; }
            set
            {
                if (_selectedCurveType != value)
                {
                    _selectedCurveType = value;                    
                    RaisePropertyChanged("SelectedCurveType");
                    SetCurveHandler(_selectedCurveType);
                }
            }
        }
        private void SetCurveHandler(CurveType selectedCurveType)
        {
            switch (selectedCurveType)
            {
                case CurveType.Catmull_Rom:
                    _curveHandler = CatmullRomCurveHandler.GetInstance();
                    break;
            }
            _rollerCoasterDirty = true;
            RefreshMode();
        }
      

        public int Width
        {
            get { return 400; }
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
            get { return "Roller Coaster Editor"; }
        }

        #endregion

        public RollerCoasterEditorController(IRenderer renderer, ISceneReader sceneReader, IResourceManager resourceManager)
        {
            if (renderer == null) throw new ArgumentNullException("renderer");
            if (resourceManager == null) throw new ArgumentNullException("resourceManager");
            if (sceneReader == null) throw new ArgumentNullException("sceneReader");
            _renderer = renderer;
            _resourceManager = resourceManager;
            _sceneReader = sceneReader;
            
            _resourceManager.Load(ResourcePath);
            _scene = _sceneReader.Read(ScenePath);

            foreach (var sn in _scene.Shapes)
            {
                if (sn.Shape.Name == "sphere")
                {
                    _pointPrototype = sn;
                }
                else if (sn.Shape.Name == "bunny")
                {
                    _bunny = sn;
                }
            }

            _curve = new Curve("curve", 1);
            _curve.Material = _resourceManager.GetMaterial("orange");

            _pointPrototype.Shape.Material = _resourceManager.GetMaterial("greenSpecular");

            _railSegmentPrototype = new Cube("railSegment", -0.5f, -0.5f, 0, 1f, 1f, 1f);
            _railSegmentPrototype.Material = _resourceManager.GetMaterial("redSpecular");

            _sleeperPrototype = new Cube("sleeper", -0.5f, -0.5f, 0, 1f, 1f, 1f);
            _sleeperPrototype.Material = _resourceManager.GetMaterial("greenSpecular");
           
            float size = 800.0f;
            float hsize = size*0.5f;
            _skybox = new Cube("skybox", -hsize, -hsize, -hsize, size, size, size);
            _skybox.Material = _resourceManager.GetMaterial("skyboxMaterial");

            _curveHandler = CatmullRomCurveHandler.GetInstance();

            _view = new RollerCoasterEditorView(this);
            _view.DataContext = this;


        }

        public void Load()
        {
            InitRenderer();
            _resourceManager.Configure(_renderer);
            _scene.Configure(_renderer);
            _scene.ClearShapes();

            _railSegmentPrototype.Configure(_renderer);
            _sleeperPrototype.Configure(_renderer);
            _skybox.Configure(_renderer);
        }

        private void InitRenderer()
        {
            _renderer.SetBackgroundColor(0.9f, 0.9f, 1.0f);
            _renderer.EnableZBuffer(true);
            _renderer.EnableWireframe(false);
            _renderer.SetCullMode(CullMode.None);
        }

        public void Render(double deltaTime)
        {
            _scene.Render(_renderer);
            
            _skybox.Material.SetParameter("cameraPosition", _scene.CurrentCamera.Position);
            _skyboxTransform = Matrix4.CreateTranslation(_scene.CurrentCamera.Position);
            _skybox.Render(_renderer, _skyboxTransform);

            
            if (_mode == Mode.DroppingPoints)
            {
                _curve.Render(_renderer, Matrix4.Identity);

                foreach (var transf in _pointTransforms)
                {
                    _pointPrototype.Shape.Render(_renderer, transf);
                }
            }
            else if (_mode == Mode.PickingPoints)
            {
                _curve.Render(_renderer, Matrix4.Identity);

                _pointPrototype.Shape.Material = _resourceManager.GetMaterial("greenSpecular");
                for (int i = 0; i < _pointTransforms.Count; i++)
                {
                    if (i == _pickedPointIndex) continue;
                    _pointPrototype.Shape.Render(_renderer, _pointTransforms[i]);
                }
                if (_pickedPointIndex != -1)
                {
                    _pointPrototype.Shape.Material = _resourceManager.GetMaterial("redSpecular");
                    _pointPrototype.Shape.Render(_renderer, _pointTransforms[_pickedPointIndex]);
                }
            }
            else if (_mode == Mode.DisplayingSplineClosed)
            {
                _curve.Render(_renderer, Matrix4.Identity);

                foreach (var transf in _pointTransforms)
                {
                    _pointPrototype.Shape.Render(_renderer, transf);
                }
            }
            else if (_mode == Mode.DisplayingRollerCoaster)
            {
               // _curve.Render(_renderer, Matrix4.Identity);

                foreach (var transf in _leftRailSegmentTransforms)
                {
                    _railSegmentPrototype.Render(_renderer, transf);
                }
                foreach (var transf in _rightRailSegmentTransforms)
                {
                    _railSegmentPrototype.Render(_renderer, transf);
                }
                foreach (var transf in _sleeperTransforms)
                {
                    _sleeperPrototype.Render(_renderer, transf);
                }
            }
            else if (_mode == Mode.RunningAnimation)
            {
                //_curve.Render(_renderer, Matrix4.Identity);
                
                foreach (var transf in _leftRailSegmentTransforms)
                {
                    _railSegmentPrototype.Render(_renderer, transf);
                }
                foreach (var transf in _rightRailSegmentTransforms)
                {
                    _railSegmentPrototype.Render(_renderer, transf);
                }
                foreach (var transf in _sleeperTransforms)
                {
                    _sleeperPrototype.Render(_renderer, transf);
                }

                _bunny.Shape.Render(_renderer, _cartTransform);
            }
        }

        #region Animation Methods
        public void Update(double deltaTime)
        {
            if (_animationRunning)
            {
                DateTime now = DateTime.Now;
                double elapsedTime = (now - _startTime).TotalSeconds;
                _startTime = now;

                double potentialEnergy = (_cartPosition.Y - _minHeight) * G;
                CartVelocity = Math.Sqrt(2.0 *(_totalEnergy - potentialEnergy));

                float distanceToTravel = (float)(_cartVelocity * elapsedTime);

                while (distanceToTravel > _segmentLength)
                {
                    distanceToTravel -= _segmentLength;

                     _animationIndex = (_animationIndex + 1) % _animationPoints.Count;
                     _currentAnimationPoint = _nextAnimationPoint;
                     _nextAnimationPoint = _animationPoints[_animationIndex];
                     _cartDirection = (_nextAnimationPoint.Position - _currentAnimationPoint.Position).Normalized();
                     _segmentLength = (_nextAnimationPoint.Position - _currentAnimationPoint.Position).Length;

                     _cartPosition = _currentAnimationPoint.Position;

                     if (_followCart)
                         _scene.CurrentCamera.Up = _currentAnimationPoint.Up;
                }

                _segmentLength -= distanceToTravel;
                _cartPosition += distanceToTravel * _cartDirection;
             
                if (_followCart)
                {
                    var camPosition = _cartPosition + _currentAnimationPoint.Direction * -3.0f + _currentAnimationPoint.Up * 2.0f;
                    var inv_camViewMatrix = Matrix4.LookAt(camPosition, camPosition + _currentAnimationPoint.Direction, _currentAnimationPoint.Up).Inverted();
                    var camTarget = Vector3.Transform(_cameraRelativeDirection,inv_camViewMatrix);

                    _scene.CurrentCamera.Position = camPosition;
                    _scene.CurrentCamera.Target = camTarget;
                }

                var inv_viewMatrix = 
                    Matrix4.LookAt(_cartPosition,
                    _cartPosition + _currentAnimationPoint.Direction, 
                    _currentAnimationPoint.Up).Inverted();
                var scaleMatrix = Matrix4.CreateScale(_bunny.Scale);
                _cartTransform = scaleMatrix * inv_viewMatrix;

                CartHeight = _cartPosition.Y;

            }
        }
        private void StartAnimation()
        {
            _maxHeight = float.MinValue;
            _minHeight = float.MaxValue;
            foreach (var animPoint in _animationPoints)
            {
                if (_maxHeight < animPoint.Position.Y)
                    _maxHeight = animPoint.Position.Y;
                if(_minHeight > animPoint.Position.Y)
                    _minHeight = animPoint.Position.Y;
            }
            MinHeight = _minHeight;
            MaxHeight = _maxHeight;

            _totalEnergy = (_maxHeight-_minHeight) * G + 0.5f * MinVel * MinVel;
            _animationIndex = 1;
            _currentAnimationPoint = _animationPoints[0];
            _nextAnimationPoint = _animationPoints[1];
            _cartPosition = _currentAnimationPoint.Position;
            _cartDirection = (_nextAnimationPoint.Position - _currentAnimationPoint.Position).Normalized();
            _segmentLength = (_nextAnimationPoint.Position - _currentAnimationPoint.Position).Length;
            _cameraRelativeDirection = SLIGHTLY_DOWN;

            _cartTransform = Matrix4.CreateTranslation(_cartPosition);

            _startTime = DateTime.Now;

            _animationRunning = true;
        }
        #endregion

        #region Matrix methods
        private Matrix4 GenerateTransform(Vector3 pointPosition, ShapeNode originalShape)
        {
            return GetModelTransform(pointPosition, originalShape.Rotation, originalShape.Scale);
        }
        private Matrix4 GetModelTransform(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            var translation_m = Matrix4.CreateTranslation(translation);
            var rotation_m = Matrix4.CreateFromQuaternion(rotation);
            var scale_m = Matrix4.CreateScale(scale);
            return scale_m * rotation_m * translation_m;
        }  
        private Matrix4 getProjectionMatrix(CameraNode camera)
        {
            if (camera is PerspectiveCamera)
            {
                var perspCamera = camera as PerspectiveCamera;
                return Matrix4.CreatePerspectiveFieldOfView(perspCamera.FieldOfView,
                        perspCamera.AspectRatio, perspCamera.NearClip, perspCamera.FarClip);
            }
            else
            {
                var ortCamera = camera as OrtographicCamera;
                return Matrix4.CreateOrthographic(ortCamera.Width, ortCamera.Height,
                    ortCamera.NearClip, ortCamera.FarClip);
            }
        }
        private Matrix4 getViewMatrix(CameraNode camera)
        {
            return Matrix4.LookAt(camera.Position, camera.Target, camera.Up);
        }
        #endregion

        #region Mouse Input Handling

        public void MouseDown(ControllerMouseButton button, int x, int y)
        {

            if (button == ControllerMouseButton.Right)
            {
                _isRightDown = true;
            }
            else if (button == ControllerMouseButton.Left)
            {
                _isLeftDown = true;

                pickedInOpenGL = (x <= _width);

                if (x > _width) x = (int)(x - _width);

                if (_mode == Mode.DroppingPoints)
                {
                    //to clipping
                    float adjustedX = (2.0f * (float)x / (float)_width) - 1;
                    float adjustedY = (2.0f * (float)(_height - y) / (float)_height) - 1;
                    var m_clipping = new Vector4(adjustedX, adjustedY, -1, 1);

                    //to camera
                    var currCamera = _scene.CurrentCamera;
                    var inverse_projMatrix = getProjectionMatrix(currCamera).Inverted();
                    var m_camera = Vector4.Transform(m_clipping, inverse_projMatrix);
                    m_camera /= m_camera.W;
                    m_camera.Z = -currCamera.NearClip;

                    //to world
                    var viewMatrix = getViewMatrix(currCamera);
                    var inverse_viewMatrix = viewMatrix.Inverted();
                    var m_mundo = Vector4.Transform(m_camera, inverse_viewMatrix);
                    var worldPosition = currCamera.Position + (m_mundo.Xyz - currCamera.Position).Normalized() * 15;

                    AddNewPointToCurve(new Vector4(worldPosition, 1));

                }
                else if (_mode == Mode.PickingPoints)
                {
                    //to clipping
                    float adjustedX = (2.0f * (float)x / (float)_width) - 1;
                    float adjustedY = (2.0f * (float)(_height - y) / (float)_height) - 1;
                    var m_clipping = new Vector4(adjustedX, adjustedY, -1, 1);

                    //to camera
                    var currCamera = _scene.CurrentCamera;
                    var inverse_projMatrix = getProjectionMatrix(currCamera).Inverted();
                    var m_camera = Vector4.Transform(m_clipping, inverse_projMatrix);
                    m_camera /= m_camera.W;
                    m_camera.Z = -currCamera.NearClip;

                    //to world
                    var viewMatrix = getViewMatrix(currCamera);
                    var inverse_viewMatrix = viewMatrix.Inverted();
                    var m_mundo = Vector4.Transform(m_camera, inverse_viewMatrix);

                    //get camera position in 4 coordinates
                    var cam_position = new Vector4(_scene.CurrentCamera.Position, 1);

                    float min_t = float.MaxValue;
                    int pickedPointIndex = -1;

                    for (int i = 0; i < _pointTransforms.Count; i++)
                    {
                        var modelMatrix = _pointTransforms[i];
                        var inverse_modelMatrix = modelMatrix.Inverted();
                        var p1 = Vector4.Transform(m_mundo, inverse_modelMatrix).Xyz;
                        var p0 = Vector4.Transform(cam_position, inverse_modelMatrix).Xyz;
                        var dir = (p1 - p0).Normalized();
                        var model2viewMatrix = viewMatrix * modelMatrix;

                        var sphere = _pointPrototype.Shape as IMesh;
                        foreach (Triangle triangle in sphere.GetTriangles())
                        {
                            float t;
                            if (RayTriangleIntersection(p0, dir, triangle, out t) && t < min_t)
                            {
                                min_t = t;
                                pickedPointIndex = i;
                            }
                        }
                    }
                    if (pickedPointIndex != -1)
                    {
                        PickedPointIndex = pickedPointIndex;

                        var pointPos = _pointList[_pickedPointIndex];
                        _pickedPointLastPosition = Vector3.Transform(pointPos.Xyz,viewMatrix);
                        _pickedMouseLastPosition = m_camera.Xyz;
                    }
                }
            }

        }
        public void MouseUp(ControllerMouseButton button, int x, int y)
        {
            if (button == ControllerMouseButton.Right)
                _isRightDown = false;
            else if (button == ControllerMouseButton.Left)
            {
                _isLeftDown = false;
            }
        }
        public void MouseWheel(int delta, int x, int y)
        {
            _scene.CurrentCamera.Zoom((float)delta/2f);
        }
        public void MouseMove(int x, int y, int deltaX, int deltaY)
        {
            if (_isRightDown)
            {
                if (_followCart)
                {
                    int xsign = Math.Sign(deltaX);
                    int ysign = Math.Sign(deltaY);

                    Vector3 aux = _cameraRelativeDirection;
                    if(xsign != 0) {
                        var rotAngle = - xsign * _rotAngle * 5;
                        var rotation = Quaternion.FromAxisAngle(UP,rotAngle);
                        aux = Vector3.Transform(aux,rotation);
                    }
                    if(ysign != 0) {
                        var rotAngle =  - ysign * _rotAngle * 5;
                        var rotation = Quaternion.FromAxisAngle(RIGHT,rotAngle);
                        aux = Vector3.Transform(aux,rotation);
                    }
                    _cameraRelativeDirection = aux.Normalized();
                }
                else
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
            else if (_isLeftDown)
            {
                if (HasPickedPoint)
                {
                    if (!pickedInOpenGL)
                        x = (int)(x - _width);
                    //to clipping
                    float adjustedX = (2.0f * (float)x / (float)_width) - 1;
                    float adjustedY = (2.0f * (float)(_height - y) / (float)_height) - 1;
                    var m_clipping = new Vector4(adjustedX, adjustedY, -1, 1);

                    //to camera
                    var currCamera = _scene.CurrentCamera;
                    var inverse_projMatrix = getProjectionMatrix(currCamera).Inverted();
                    var m_camera = Vector4.Transform(m_clipping, inverse_projMatrix);
                    m_camera /= m_camera.W;
                    m_camera.Z = -currCamera.NearClip;

                    //to world
                    var viewMatrix = getViewMatrix(currCamera);
                    var inverse_viewMatrix = viewMatrix.Inverted();
                    var m_mundo = Vector4.Transform(m_camera, inverse_viewMatrix);

                    //get displacement factor
                    _displacementFactor = _pickedPointLastPosition.Length / _pickedMouseLastPosition.Length;

                    //shifts in camera and world coordinates
                    var shift_camera = (m_camera.Xyz - _pickedMouseLastPosition) * _displacementFactor;
                    var shift_mundo = Vector4.Transform(new Vector4(shift_camera, 0), inverse_viewMatrix);

                    //update position and transform of picked point
                    _pointList[_pickedPointIndex] += shift_mundo;
                    var newPos_mundo = _pointList[_pickedPointIndex];
                    _pointTransforms[_pickedPointIndex] = GenerateTransform(newPos_mundo.Xyz,_pointPrototype);

                    //keep track of last values in camera coordinates
                    _pickedMouseLastPosition = m_camera.Xyz;
                    _pickedPointLastPosition = Vector4.Transform(newPos_mundo, viewMatrix).Xyz;

                    //refresh the curve
                    _curveHandler.RefreshCurve(_curve, _pointList, _stepSize, _renderer);

                    _rollerCoasterDirty = true;

                }
                
            }
        }
        #endregion
        private bool RayTriangleIntersection(Vector3 p0, Vector3 dir, Triangle triangle, out float t)
        {
            List<IVertex> vertices = triangle.Vertices.ToList();
            var A = vertices[0].Position;
            var B = vertices[1].Position;
            var C = vertices[2].Position;

            var a = A.X - B.X;
            var b = A.Y - B.Y;
            var c = A.Z - B.Z;
            var d = A.X - C.X;
            var e = A.Y - C.Y;
            var f = A.Z - C.Z;
            var g = dir.X;
            var h = dir.Y;
            var i = dir.Z;
            var j = A.X - p0.X;
            var k = A.Y - p0.Y;
            var l = A.Z - p0.Z;

            var ei = e * i;
            var hf = h * f;
            var gf = g * f;
            var di = d * i;
            var dh = d * h;
            var eg = e * g;
            var ak = a * k;
            var jb = j * b;
            var jc = j * c;
            var al = a * l;
            var bl = b * l;
            var kc = k * c;

            //compute M
            var M = a * (ei - hf) + b * (gf - di) + c * (dh - eg);
            //compute t
            t = -(f * (ak - jb) + e * (jc - al) + d * (bl - kc)) / M;
            if (t < 0)
                return false;
            //compute gamma
            var gamma = (i * (ak - jb) + h * (jc - al) + g * (bl - kc)) / M;
            if (gamma < 0 || gamma > 1)
                return false;
            //compute beta
            var beta = (j * (ei - hf) + k * (gf - di) + l * (dh - eg)) / M;
            if (beta < 0 || beta > 1 - gamma)
                return false;

            return true;
        }      
        public void UpdateSize(double width, double height)
        {
            _width = width;
            _height = height;
            if (_scene.CurrentCamera is PerspectiveCamera)
                (_scene.CurrentCamera as PerspectiveCamera).AspectRatio = (float)_width / (float)_height;
            
        }

        #region Keyword Input Handling
        public void KeyDown(int key)
        {
            if (!_followCart)
            {
                var cam = _scene.CurrentCamera;
                switch (key)
                {
                    case 'w':
                        moveCameraForward(cam);
                        break;
                    case 'a':
                        moveCameraLeftward(cam);
                        break;
                    case 's':
                        moveCameraBackward(cam);
                        break;
                    case 'd':
                        moveCameraRightward(cam);
                        break;
                    case 'q':
                        moveCameraDownward(cam);
                        break;
                    case 'e':
                        moveCameraUpward(cam);
                        break;
                    case '2':
                        rotateCameraCounterClockwise(cam);
                        break;
                    case '3':
                        rotateCameraClockwise(cam);
                        break;
                    case '4':
                        setCameraUpright(cam);
                        break;
                }
            }
        }
        #endregion

        #region Mode Handling

        public void SetMode(Mode newMode)
        {
            if (_mode == newMode)
                return;

            LeaveCurrentMode();
            _mode = newMode;
            EnterNewMode();
        }

        private void RefreshMode()
        {
            LeaveCurrentMode();
            EnterNewMode();
        }

        private void EnterNewMode()
        {
            switch (_mode)
            {
                case Mode.DroppingPoints:
                    _pointPrototype.Shape.Material = _resourceManager.GetMaterial("greenSpecular");
                    break;
                case Mode.PickingPoints:
                    _pointPrototype.Shape.Material = _resourceManager.GetMaterial("greenSpecular");
                    break;
                case Mode.DisplayingSplineClosed:
                    _pointPrototype.Shape.Material = _resourceManager.GetMaterial("greenSpecular");
                    if (_curveHandler.CanCloseCurve(_pointList))
                    {
                        _curveHandler.CloseCurve(_pointList, _pointTransforms, _curve, _stepSize, _renderer);
                        _curveClosed = true;
                    }
                    else UserFeedback = _curveHandler.CannotCloseSplineFeedbackMessage;
                    break;

                case Mode.DisplayingRollerCoaster:
                    if (_curveHandler.CanCloseCurve(_pointList))
                    {
                        if (_rollerCoasterDirty)
                        {
                            _leftRailSegmentTransforms.Clear();
                            _rightRailSegmentTransforms.Clear();
                            _sleeperTransforms.Clear();

                            _curveHandler.GenerateRollerCoaster(
                                _pointList,
                                _leftRailSegmentTransforms,
                                _rightRailSegmentTransforms,
                                _sleeperTransforms,
                                _stepSize, _sleeperLength, _sleeperWidth, _sleeperHeight,
                                _railWidth, _railHeight);

                            _animationPoints = _curveHandler.GetAnimationPointList(_pointList, _stepSize);

                            _rollerCoasterDirty = false;
                        }
                        _curveHandler.CloseCurve(_pointList, _pointTransforms, _curve, _stepSize, _renderer);
                        _curveClosed = true;
                    }
                    else UserFeedback = _curveHandler.CannotCloseSplineFeedbackMessage;
                    break;

                case Mode.RunningAnimation:
                    if (_curveHandler.CanCloseCurve(_pointList))
                    {
                        if (_rollerCoasterDirty)
                        {
                            _leftRailSegmentTransforms.Clear();
                            _rightRailSegmentTransforms.Clear();
                            _sleeperTransforms.Clear();
                             
                            _curveHandler.GenerateRollerCoaster(
                                _pointList,
                                _leftRailSegmentTransforms,
                                _rightRailSegmentTransforms,
                                _sleeperTransforms,
                                _stepSize, _sleeperLength, _sleeperWidth, _sleeperHeight,
                                _railWidth, _railHeight);

                            _animationPoints = _curveHandler.GetAnimationPointList(_pointList, _stepSize);

                            _rollerCoasterDirty = false;
                        }
                        _curveHandler.CloseCurve(_pointList, _pointTransforms, _curve, _stepSize, _renderer);
                        _curveClosed = true;

                        StartAnimation();
                    }
                    else UserFeedback = _curveHandler.CannotRunAnimationFeedbackMessage;
                    break;
            }
        }

        private void LeaveCurrentMode()
        {
            switch (_mode)
            {
                case Mode.PickingPoints:
                    PickedPointIndex = -1;
                    break;
                case Mode.DisplayingSplineClosed:
                    if (_curveClosed)
                    {
                        _curveHandler.OpenCurve(_pointList, _pointTransforms, _curve, _stepSize, _renderer);
                        _curveClosed = false;
                    }
                    break;
                case Mode.DisplayingRollerCoaster:
                    if (_curveClosed)
                    {
                        _curveHandler.OpenCurve(_pointList, _pointTransforms, _curve, _stepSize, _renderer);
                        _curveClosed = false;
                    }
                    break;
                case Mode.RunningAnimation:
                    _animationRunning = false;
                    _followCart = false;
                    _hasBackup = false;
                    if (_curveClosed)
                    {
                        _curveHandler.OpenCurve(_pointList, _pointTransforms, _curve, _stepSize, _renderer);
                        _curveClosed = false;
                    }
                    break;
            }
            UserFeedback = "";
        }
        #endregion

        #region Camera Movement and Rotation Methods
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
            var rotMatrix = Quaternion.FromAxisAngle(forwardAxis, _rotAngle * 10);
            cam.Up = Vector3.Transform(cam.Up, rotMatrix).Normalized();
        }

        private void rotateCameraCounterClockwise(CameraNode cam)
        {
            var forwardAxis = (cam.Target - cam.Position);
            var rotMatrix = Quaternion.FromAxisAngle(forwardAxis, -_rotAngle * 10);
            cam.Up = Vector3.Transform(cam.Up, rotMatrix).Normalized();
        }

        private void moveCameraRightward(CameraNode cam)
        {
            Vector3 forwardDir = (cam.Target - cam.Position).Normalized();
            Vector3 rightDir = Vector3.Cross(forwardDir,cam.Up).Normalized();
            Vector3 shift = rightDir * _camVelocity;
            cam.Position += shift;
            cam.Target += shift;
        }

        private void moveCameraBackward(CameraNode cam)
        {
            Vector3 backwardDir = (cam.Position - cam.Target).Normalized();
            Vector3 shift = backwardDir * _camVelocity;
            cam.Position += shift;
            cam.Target += shift;
        }

        private void moveCameraLeftward(CameraNode cam)
        {
            Vector3 forwardDir = (cam.Target - cam.Position).Normalized();
            Vector3 leftDir = Vector3.Cross(cam.Up, forwardDir).Normalized();
            Vector3 shift = leftDir * _camVelocity;
            cam.Position += shift;
            cam.Target += shift;
        }

        private void moveCameraForward(CameraNode cam)
        {
            Vector3 forwardDir = (cam.Target - cam.Position).Normalized();
            Vector3 shift = forwardDir * _camVelocity;
            cam.Position += shift;
            cam.Target += shift;
            
        }

        private void moveCameraUpward(CameraNode cam)
        {
            Vector3 shift = cam.Up * _camVelocity;
            cam.Position += shift;
            cam.Target += shift;

        }

        private void moveCameraDownward(CameraNode cam)
        {
            Vector3 shift = cam.Up * _camVelocity;
            cam.Position -= shift;
            cam.Target -= shift;
        }

        private void setCameraUpright(CameraNode cam)
        {
            cam.Up = new Vector3(0, 1, 0);
            var dir = (cam.Target - cam.Position);
            dir.Y = 0;
            if (dir.X == 0 && dir.Z == 0)
                dir.Z = -1;
            cam.Target = cam.Position + dir;
        }

        #endregion

        #region Roller Coaster Edition Methods
        private void AddNewPointToCurve(Vector4 newPoint)
        {
            _pointList.Add(newPoint);
            _pointTransforms.Add(GenerateTransform(newPoint.Xyz,_pointPrototype));
            _curveHandler.HandleNewPoint(_pointList, _curve, _stepSize);
            _curve.Configure(_renderer);

            _rollerCoasterDirty = true;
        }
        public void DeletePickedPoint()
        {
            if (HasPickedPoint)
            {
                _pointList.RemoveAt(_pickedPointIndex);
                _pointTransforms.RemoveAt(_pickedPointIndex);
                PickedPointIndex = -1;

                _curveHandler.RefreshCurve(_curve, _pointList, _stepSize, _renderer);                
                _rollerCoasterDirty = true;
            }
        }
        public void GenerateRollerCoasterRandomly()
        {
            _pointList.Clear();
            _pointTransforms.Clear();
            _curve.Clear();

            int n =_random.Next(5, 60);
            Vector3 lastPosition = new Vector3(0, 0, 0);
            Vector3 newPosition = new Vector3();
            Vector3 lastDir = new Vector3();
            Vector3 newDir;
            bool first = true;

            while(n-- > 0) {

                while (true)
                {
                    double theta = _random.NextDouble() * Math.PI * 0.75f;
                    double phi = _random.NextDouble() * 2 * Math.PI;
                    double r = 15 + _random.NextDouble() * 35;
                    newPosition.X = (float)(r * Math.Sin(theta) * Math.Cos(phi));
                    newPosition.Y = (float)(r * Math.Sin(theta) * Math.Sin(phi));
                    newPosition.Z = (float)(r * Math.Cos(theta));
                    newDir = (newPosition - lastPosition).Normalized();

                    if (first)
                    {
                        first = false;
                        break;
                    }
                    else if (Vector3.Dot(newDir, lastDir) >= -0.2f)
                        break;
                }

                _pointList.Add(new Vector4(newPosition, 1));
                _pointTransforms.Add(GenerateTransform(newPosition,_pointPrototype));
                _curveHandler.HandleNewPoint(_pointList, _curve, _stepSize);

                lastPosition = newPosition;
                lastDir = newDir;
            }
            _curve.Configure(_renderer);

            _rollerCoasterDirty = true;

            RefreshMode();

        }
        #endregion

        public string ConvertSplineToXml()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<spline type = '");
            sb.Append(_selectedCurveType.ToString());
            sb.Append("'>\n");
            foreach (var point in _pointList)
            {
                sb.Append(string.Format("\t<point x='{0}' y='{1}' z='{2}'/>\n", point.X, point.Y, point.Z));
            }
            sb.Append("</spline>");
            return sb.ToString();
        }
        public void LoadSplineFromXmlFile(string path) {

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);

                XmlNode node = doc.DocumentElement.SelectSingleNode("/spline");

                string type = node.Attributes["type"].InnerText;
                CurveType curveType = (CurveType)Enum.Parse(typeof(CurveType),type);

                List<Vector4> pointList = new List<Vector4>();
                List<Matrix4> pointTransforms = new List<Matrix4>();

                foreach (XmlNode child in node)
                {
                    var point = new Vector4(
                        float.Parse(child.Attributes["x"].InnerText),
                        float.Parse(child.Attributes["y"].InnerText),
                        float.Parse(child.Attributes["z"].InnerText),
                        1);                    
                    pointList.Add(point);
                    pointTransforms.Add(GenerateTransform(point.Xyz, _pointPrototype));
                }

                SelectedCurveType = curveType;
                _pointList = pointList;
                _pointTransforms = pointTransforms;
                _curveHandler.RefreshCurve(_curve, _pointList, _stepSize, _renderer);
                _rollerCoasterDirty = true;
                _curveClosed = false;
                RefreshMode();

            }
            catch (Exception e)
            {
                UserFeedback = "Error Message:\n\n" + e.Message + "\n\nError Source:\n\n" + e.Source + "\n\nStackTrace\n\n" + e.StackTrace;
            }
        }
        public void ClearRollerCoaster()
        {
            _pointList.Clear();
            _pointTransforms.Clear();
            _curve.Clear();
            _leftRailSegmentTransforms.Clear();
            _rightRailSegmentTransforms.Clear();
            _sleeperTransforms.Clear();
            _rollerCoasterDirty = true;
            _curveClosed = false;
            RefreshMode();
        }

    }
}
