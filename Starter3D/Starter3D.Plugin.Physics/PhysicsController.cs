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
    public enum Solver
    {
        RungeKutta,
        Euler
    }
    public class PhysicsController : ViewModelBase, IController
    {
        private const string ScenePath = @"scenes/physicsScene.xml";
        private const string ResourcePath = @"resources/physicsResources.xml";

        private readonly IRenderer _renderer;
        private readonly ISceneReader _sceneReader;
        private readonly IResourceManager _resourceManager;

        private readonly IScene _scene;


        private float _rotAngle = (float)(Math.PI / 180 * 2f);
        private float _camVelocity = 8f;
        private bool _isRightDown = false;

        private float EARTH_MASS = 15000;
        private float G = 9.81f;
        private int _numberAsteroids = 500;
        private float DT = 0.02f;

        private Vector3 EARTH_INITIAL_POSITION = new Vector3(0, 50, -50);
        private Vector3 EARTH_INITIAL_VELOCITY = new Vector3(0, 0, 0);
        private Vector3 UP = new Vector3(1, 1, 1);
        private Vector3 NORMAL_TO_UP;

        private Matrix4 _earthToWorldTransform;
        private SingleInstancedPhysicalObject _earth;
        private MultiInstancedPhysicalObject _asteroids;

        private IMesh _sphereMesh;

        private Random _random = new Random();

        private PhysicsSolver _solver;

        private DateTime _startTime;

        private readonly object _syncLock = new object();
        private bool _simulationPaused;

        private Solver _currentSolver = Solver.RungeKutta;
        private Array _solvers = Enum.GetValues(typeof(Solver));

        private PhysicsView _view;


        private float _angleDelta = 0.01f;
        private float _earthRotAngle = 0;
        private static readonly float _2PI = (float)(2 * Math.PI);


        public Array Solvers
        {
            get { return _solvers; }
        }

        public Solver CurrentSolver
        {
            get { return _currentSolver; }
            set
            {
                if (_currentSolver != value)
                {
                    _currentSolver = value;
                    RefreshCurrentSolver();
                    RaisePropertyChanged("CurrentSolver");
                }
            }
        }

        private void RefreshCurrentSolver()
        {
            switch (_currentSolver)
            {
                case Solver.Euler:
                    _solver = EulerSolver.Instance;
                    break;
                case Solver.RungeKutta:
                    _solver = RungeKuttaSolver.Instance;
                    break;
            }
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
            get { return "Physics"; }
        }

        public PhysicsController(IRenderer renderer, ISceneReader sceneReader, IResourceManager resourceManager)
        {
            if (renderer == null) throw new ArgumentNullException("renderer");
            if (sceneReader == null) throw new ArgumentNullException("sceneReader");
            if (resourceManager == null) throw new ArgumentNullException("resourceManager");
            _renderer = renderer;
            _sceneReader = sceneReader;
            _resourceManager = resourceManager;

            _resourceManager.Load(ResourcePath);
            _scene = _sceneReader.Read(ScenePath);

            _solver = RungeKuttaSolver.Instance;
            _currentSolver = Solver.RungeKutta;
            _simulationPaused = false;

            _view = new PhysicsView(this);
            _view.DataContext = this;

        }

        public void Load()
        {
            InitRenderer();

            _resourceManager.Configure(_renderer);
          

            foreach (ShapeNode sn in _scene.Shapes)
            {
                if (sn.Shape.Name == "sphere")
                {
                    _sphereMesh = (IMesh)sn.Shape; 
                }
            }

            InitEarth();
            InitAsteroids();

            _scene.ClearShapes();
            _scene.Configure(_renderer);

            _startTime = DateTime.Now;

        }

        private void InitEarth()
        {
            UP.Normalize();
            NORMAL_TO_UP = GetNormalizedNormal(UP);
            var target = EARTH_INITIAL_POSITION + NORMAL_TO_UP;
            var earthData = new PhysicalObjectData(
                EARTH_MASS,
                EARTH_INITIAL_VELOCITY,
                EARTH_INITIAL_POSITION,
                Quaternion.Identity,
                new Vector3(70));
            var sphereClone = _sphereMesh.Clone();
            sphereClone.Material = _resourceManager.GetMaterial("earthMaterial");
            _earth = new SingleInstancedPhysicalObject(sphereClone, earthData);
            _earth.Configure(_renderer);

            _earthToWorldTransform = Matrix4.LookAt(EARTH_INITIAL_POSITION, target, UP).Inverted();

        }

        private void InitAsteroids()
        {
            var mesh = _sphereMesh.Clone();
            var instancedMesh = new InstancedMesh(mesh.Name, mesh);
            instancedMesh.Material = _resourceManager.GetMaterial("asteroidMaterial");
            _asteroids = new MultiInstancedPhysicalObject(instancedMesh);
            GenerateRandomAsteroids();
            _asteroids.Configure(_renderer);
        }

        private Vector3 RandomUnitaryVector()
        {
            var phi = _random.NextDouble() * Math.PI * 2;
            var theta = _random.NextDouble() * Math.PI;
            var x = (float)(Math.Sin(theta) * Math.Cos(phi));
            var y = (float)(Math.Sin(theta) * Math.Sin(phi));
            var z = (float)(Math.Cos(theta));
            var v = new Vector3(x, y, z);
            v.Normalize();
            return v;
        }

        internal void RestartSimulation()
        {
            lock (_syncLock)
            {
                UP = RandomUnitaryVector();
                NORMAL_TO_UP = GetNormalizedNormal(UP);
                _earthToWorldTransform = Matrix4.LookAt(
                    EARTH_INITIAL_POSITION,
                    EARTH_INITIAL_POSITION + NORMAL_TO_UP, 
                    UP).Inverted();
                _earth.Data.NextPosition = EARTH_INITIAL_POSITION;
                _earth.Data.NextVelocity = EARTH_INITIAL_VELOCITY;
                _earth.Data.UpdatePositionAndVelocity();
                _asteroids.ClearInstances();
                GenerateRandomAsteroids();
                _asteroids.Configure(_renderer);
                _simulationPaused = false;
            }
        }
        internal void PauseSimulation()
        {
            _simulationPaused = true;
        }
        internal void ResumeSimulation()
        {
            _simulationPaused = false;
        }

        private void GenerateRandomAsteroids()
        {
            for (int i = 0; i < _numberAsteroids; ++i)
            {
                var theta = _random.NextDouble() * Math.PI * 2;
                var radius = 300 + 80 * (2 * _random.NextDouble() - 1);
                var relative_position = new Vector4((float)(radius * Math.Cos(theta)), 0, (float)(radius * Math.Sin(theta)), 1);
                var position = Vector4.Transform(relative_position, _earthToWorldTransform).Xyz;
                var mass = (float)(80 + 2 * (2 * _random.NextDouble() - 1));
                var coef = 0.8 + _random.NextDouble() * 0.5;
                var speed = (float)(Math.Sqrt(G * EARTH_MASS / radius) * coef);
                var vel = Vector3.Cross(UP, position - EARTH_INITIAL_POSITION).Normalized() * speed;
                var scale = new Vector3((float)(3 + 0.8 * (2 * _random.NextDouble() - 1)));

                var data = new PhysicalObjectData(mass, vel, position, Quaternion.Identity, scale);

                _asteroids.AddPhysicalObject(data);
            }
        }


        private void InitRenderer()
        {
            _renderer.SetBackgroundColor(0,0,0);
            _renderer.EnableZBuffer(true);
            _renderer.EnableWireframe(false);
            _renderer.SetCullMode(CullMode.None);
        }

        public void Render(double time)
        {
            _scene.Render(_renderer);
            //_earth.Mesh.Render(_renderer, _earthToWorldTransform * _earth.Data.ModelTransform);
            //_earth.Mesh.Render(_renderer, _earthToWorldTransform);

            _earthToWorldTransform = Matrix4.LookAt(
                _earth.Data.Position, _earth.Data.Position + NORMAL_TO_UP, UP).Inverted();
            var transform = _earth.Data.ScaleMatrix *
                            _earth.Data.RotationMatrix *
                            _earthToWorldTransform;
            _earth.Mesh.Render(_renderer, transform);
            //_earth.Render(_renderer);
            _asteroids.Render(_renderer);
        }

        public void Update(double time)
        {
            if (_simulationPaused)
                return;

            lock (_syncLock)
            {
                //use solver to update state of earth and asteroids
                _solver.SolveNextState(_earth.Data, _asteroids.InstancesData, DT);
                _solver.SolveNextState(_asteroids.InstancesData, _earth.Data, DT);

                //update positions and velocity to the values of the next state
                _earth.Data.UpdatePositionAndVelocity();
                foreach (var data in _asteroids.InstancesData)
                    data.UpdatePositionAndVelocity();

                _earthRotAngle+=_angleDelta;
                if(_earthRotAngle > _2PI)
                    _earthRotAngle -= _2PI;
                _earth.Data.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, _earthRotAngle);

                //refresh instancing transforms of and configure the asteroids
                _asteroids.RefreshTransforms();
                _asteroids.Configure(_renderer);
            }
        }

        public void UpdateSize(double width, double height)
        {
            var perspectiveCamera = _scene.CurrentCamera as PerspectiveCamera;
            if (perspectiveCamera != null)
                perspectiveCamera.AspectRatio = (float)(width / height);
        }

        #region Mouse Input Handling

        public void MouseDown(ControllerMouseButton button, int x, int y)
        {
            if (button == ControllerMouseButton.Right) _isRightDown = true;
        }
        public void MouseUp(ControllerMouseButton button, int x, int y)
        {
            if (button == ControllerMouseButton.Right) _isRightDown = false;
        }
        public void MouseWheel(int delta, int x, int y)
        {
            _scene.CurrentCamera.Zoom((float)delta / 2f);
        }
        public void MouseMove(int x, int y, int deltaX, int deltaY)
        {
            if (_isRightDown)
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
        #endregion

        #region Keyboard Input Handling
        public void KeyDown(int key)
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
            Vector3 rightDir = Vector3.Cross(forwardDir, cam.Up).Normalized();
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

        private Vector3 GetNormalizedNormal(Vector3 v)
        {
            var normal = new Vector3(v.Z, v.Z, -v.X - v.Y);
            if (normal.X == 0 && normal.Y == 0 && normal.Z == 0)
            {
                normal.X = -v.Y - v.Z;
                normal.Y = v.X;
                normal.Z = v.X;
            }
            return normal.Normalized();
        }
       
    }
}