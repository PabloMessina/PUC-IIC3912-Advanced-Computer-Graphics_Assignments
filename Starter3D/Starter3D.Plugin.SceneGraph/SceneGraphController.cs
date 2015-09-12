using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using OpenTK;
using Starter3D.API.controller;
using Starter3D.API.geometry.primitives;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using Starter3D.API.scene;
using Starter3D.API.scene.nodes;
using Starter3D.API.scene.persistence;
using Starter3D.API.utils;
using Starter3D.API.geometry;

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

        private const float angleDelta = 0.5f * (float)(Math.PI / 180);
        private const float _2PI = (float)Math.PI * 2;
        private const double tickSpan = 5;
        private double elapsedTime = 0;
        private DateTime startTime;


        private List<ShapeNode> rootShapeNodes = new List<ShapeNode>();
        private List<ShapeNode> spheres = new List<ShapeNode>();
        private ShapeNode currSphere;
        private Dictionary<ShapeNode, float> angleDeltas = new Dictionary<ShapeNode, float>();
        bool animationPaused = false;


        private ShapeNode pickedRoot = null;
        bool objectPicked = false;
        Vector3 mouse_camera;
        Vector3 pick_camera;
        Vector3 rootOriginalPosition;
        float displacement_factor;

        //space, 32, tab, 3, left, 23, right, 25
        private const int SPACE = 32;
        private const int TAB = 3;
        private const int LEFT = 23;
        private const int RIGHT = 25;

        private bool isRightDown = false;

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
                if (sn.Parent == null || !(sn.Parent is ShapeNode))
                {
                    rootShapeNodes.Add(sn);
                }
                //collect spheres
                if (sn.Children.Count() > 0)
                {
                    spheres.Add(sn);
                    angleDeltas[sn] = 0; //set angle delta to 0 (default)
                }
            }
            currSphere = spheres.First();
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

        public void Update(double time)
        {

            if ((DateTime.Now - startTime).TotalMilliseconds > elapsedTime)
            {
                elapsedTime += tickSpan;

                if (!animationPaused)
                {
                    //rotate spheres
                    foreach (ShapeNode sphere in spheres)
                    {
                        sphere.changeRotationAngle(angleDeltas[sphere]);
                    }
                }
                //propagate changes top-down from roots
                foreach (ShapeNode root in rootShapeNodes)
                {
                    root.propagateModelTransformToChildren();
                }
            }
        }

        public void UpdateSize(double width, double height)
        {
            var perspectiveCamera = _scene.CurrentCamera as PerspectiveCamera;
            if (perspectiveCamera != null)
                perspectiveCamera.AspectRatio = (float)(width / height);
        }



        private Matrix4 getProjectionMatrix()
        {
            if (_scene.CurrentCamera is PerspectiveCamera)
            {
                var camera = _scene.CurrentCamera as PerspectiveCamera;
                return Matrix4.CreatePerspectiveFieldOfView(camera.FieldOfView,
                        camera.AspectRatio, camera.NearClip, camera.FarClip);
            }
            else
            {
                var camera = _scene.CurrentCamera as OrtographicCamera;
                return Matrix4.CreateOrthographic(camera.Width, camera.Height, camera.NearClip, camera.FarClip);
            }
        }

        private Matrix4 getViewMatrix()
        {
            var cam = _scene.CurrentCamera;
            return Matrix4.LookAt(cam.Position, cam.Target, cam.Up);
        }

        public void MouseWheel(int delta, int x, int y)
        {
            _scene.CurrentCamera.Zoom(delta);
        }
        public void MouseDown(ControllerMouseButton button, int x, int y)
        {
            if (button == ControllerMouseButton.Right)
                isRightDown = true;
            else if (button == ControllerMouseButton.Left)
                if (!objectPicked)
                {

                    //screen width and height
                    var width = _centralView.ActualWidth;
                    var height = _centralView.ActualHeight;

                    //mouse coordinates
                    int mouse_x = (x > width) ? (int)(x - width) : x;
                    int mouse_y = y;

                    //to clipping coordinates
                    var m_clipping = new Vector4(
                         (float)(2 * (mouse_x / width) - 1),
                         (float)(2 * (1 - mouse_y / height) - 1),
                         -1, 1);

                    //to camera coordinates
                    var inverse_projMatrix = getProjectionMatrix().Inverted();
                    var m_camera = Vector4.Transform(m_clipping, inverse_projMatrix);
                    m_camera /= m_camera.W;

                    var cam = _scene.CurrentCamera;
                    mouse_camera = m_camera.Xyz; //keep track in class variable
                    mouse_camera.Z = - cam.NearClip;

                    //to world coordinates
                    var viewMatrix = getViewMatrix();
                    var inverse_viewMatrix = viewMatrix.Inverted();
                    var m_mundo = Vector4.Transform(m_camera, inverse_viewMatrix);

                    //get camera position in 4 coordinates
                    var cam_position = new Vector4(_scene.CurrentCamera.Position, 1);                   


                    float min_t = float.MaxValue;
                    ShapeNode pickedShape = null;

                    foreach (ShapeNode sn in _scene.Shapes)
                    {
                        if (sn.Shape is IMesh)
                        {
                            IMesh mesh = (IMesh)sn.Shape;
                            List<IPolygon> triangles = mesh.GetTriangles().ToList();

                            //to model coordinates
                            var modelMatrix = sn.ModelTransform;
                            var inverse_modelMatrix = modelMatrix.Inverted();
                            var p1 = Vector4.Transform(m_mundo, inverse_modelMatrix).Xyz;
                            var p0 = Vector4.Transform(cam_position, inverse_modelMatrix).Xyz;
                            var dir = (p1 - p0).Normalized();

                            var model2viewMatrix = viewMatrix * modelMatrix;

                            foreach (Triangle triangle in triangles)
                            {
                                float t;
                                if (RayTriangleIntersection(p0, dir, triangle, out t) && t < min_t)
                                {
                                    min_t = t;
                                    pickedShape = sn;
                                    pick_camera = Vector4.Transform(new Vector4((p0 + t * dir), 1), model2viewMatrix).Xyz;
                                    displacement_factor = pick_camera.Length / mouse_camera.Length;

                                    if (pick_camera.Z < -(cam.FarClip+20))
                                        pickedShape = null;
                                }
                            }
                        }
                    }

                    if (pickedShape != null)
                    {
                        pickedRoot = findRootShape(pickedShape);
                        rootOriginalPosition = pickedRoot.Position;
                        objectPicked = true;
                    }
                }

        }

        private ShapeNode findRootShape(ShapeNode shape)
        {
            while (shape.Parent is ShapeNode) shape = (ShapeNode)shape.Parent;
            return shape;
        }

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

        public void MouseUp(ControllerMouseButton button, int x, int y)
        {
            if(button == ControllerMouseButton.Right)
                isRightDown = false;
            else if(button == ControllerMouseButton.Left)
                objectPicked = false;
        }

        public void MouseMove(int x, int y, int deltaX, int deltaY)
        {
            if(isRightDown)
                _scene.CurrentCamera.Orbit(deltaX, deltaY);
            if (objectPicked)
            {
                //screen width and height
                var width = _centralView.ActualWidth;
                var height = _centralView.ActualHeight;

                //mouse coordinates
                int mouse_x = x > width ? (int)(x - width) : x;
                int mouse_y = y;

                //to clipping coordinates
                var m_clipping = new Vector4(
                     (float)(2 * (mouse_x / width) - 1),
                     (float)(2 * (1 - mouse_y / height) - 1),
                     -1, 1);

                //to camera coordinates
                var cam = _scene.CurrentCamera;
                var inverse_projMatrix = getProjectionMatrix().Inverted();
                var m_camera = Vector4.Transform(m_clipping, inverse_projMatrix);
                m_camera /= m_camera.W;
                m_camera.Z = - cam.NearClip;

                var inverse_viewMatrix = getViewMatrix().Inverted();
                var shift = new Vector4((m_camera.Xyz - mouse_camera) * displacement_factor,0);
                shift = Vector4.Transform(shift, inverse_viewMatrix); //shift to world coordinates
                pickedRoot.Position = rootOriginalPosition + shift.Xyz;
            }
        }

        public void KeyDown(int key)
        {//space, 32, tab, 3, left, 23, right, 25

            if (key == SPACE)
                animationPaused = !animationPaused;

            else if (!animationPaused)
                switch (key)
                {
                    case 49:
                    case 50:
                    case 51:
                        //int index = spheres.IndexOf(currSphere);
                        //index = (index + 1) % spheres.Count;
                        int index = key - 49;
                        if (currSphere != null)
                            currSphere.Shape.Material = _resourceManager.GetMaterial("stones");
                        currSphere = spheres[index];
                        currSphere.Shape.Material = _resourceManager.GetMaterial("justRed");
                        break;
                    case RIGHT:
                        angleDeltas[currSphere] += angleDelta;
                        break;
                    case LEFT:
                        angleDeltas[currSphere] -= angleDelta;
                        break;
                }
        }

    }
}