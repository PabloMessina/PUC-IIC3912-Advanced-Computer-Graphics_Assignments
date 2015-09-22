using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;
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
using System.Windows.Data;
using System.Globalization;

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

        private readonly SceneGraphView _view;

        private const float _2PI = (float)Math.PI * 2;
        private const double tickSpan = 10;
        private double elapsedTime = 0;
        private DateTime startTime;
        
        private ShapeNode currSphere;

        private ObservableCollection<ShapeTreeViewModel> rootShapeNodes = new ObservableCollection<ShapeTreeViewModel>();
        private ObservableCollection<SphereShapeViewModel> sphereShapeViewModels = new ObservableCollection<SphereShapeViewModel>();
        private PickedShapeViewModel _pickedShapeViewModel;
        private ShapeTreeViewModel _pickedShapeTreeViewModel = null;
        private ShapeTreeViewModel _pickedShapeTVM_byTriggers = null;

        bool animationPaused = true;

        private ShapeNode pickedRoot = null;
        bool objectPicked = false;  
        Vector3 mouse_camera;
        Vector3 pick_camera;
        Vector3 rootOriginalPosition;
        float displacement_factor;

        private bool isRightDown = false;


        private string _debugDisplayText = "";
        public string DebugDisplayText
        {
            get { return _debugDisplayText; }
            set
            {
                if (_debugDisplayText != value)
                {
                    _debugDisplayText = value;
                    OnPropertyChanged("DebugDisplayText");
                }
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
            get { return null; }
        }

        public object LeftView
        {
            get { return _view; }
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

        public ObservableCollection<ShapeTreeViewModel> RootShapeNodes
        {
            get { return rootShapeNodes; }
        }

        public ObservableCollection<SphereShapeViewModel> SphereNodes
        {
            get { return sphereShapeViewModels; }
        }

        public PickedShapeViewModel PickedShape
        {
            get { return _pickedShapeViewModel; }
        }

        public ShapeTreeViewModel PickedShapeTreeViewModel
        {
            set { _pickedShapeTVM_byTriggers = value; }
        }

        private void SetSelectedShapeNode(ShapeNode newShape)
        {
            if (_pickedShapeViewModel.ShapeNode != newShape)
            {
                if(_pickedShapeViewModel.ShapeNode != null)
                    _pickedShapeViewModel.ShapeNode.Shape.Material = _resourceManager.GetMaterial("stones");
                _pickedShapeViewModel.ShapeNode = newShape;
                _pickedShapeViewModel.ShapeNode.Shape.Material = _resourceManager.GetMaterial("justRed");
                OnPropertyChanged("PickedShape");
            }
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

            foreach (ShapeNode sn in _scene.Shapes)
            {
                //collect root nodes
                if (sn.Parent == null || !(sn.Parent is ShapeNode))
                {
                    rootShapeNodes.Add(new ShapeTreeViewModel(sn, null));
                    ShapeTreeViewModel.SetTagsRecursively(sn);
                }
                //collect spheres
                if (sn.Children.Count() > 0)
                {
                    sphereShapeViewModels.Add(new SphereShapeViewModel(sn));
                }
            }
            currSphere = sphereShapeViewModels[0].SphereShape;
            _pickedShapeViewModel = new PickedShapeViewModel();
            _pickedShapeViewModel.ShapeNode = rootShapeNodes[0].ShapeNode;
            _pickedShapeViewModel.ShapeNode.Shape.Material = _resourceManager.GetMaterial("justRed");

            startTime = DateTime.Now;

            _view = new SceneGraphView(this);



        }

        public void Load()
        {
            InitRenderer();

            _resourceManager.Configure(_renderer);
            _scene.Configure(_renderer);

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
                    foreach (SphereShapeViewModel ssvm in sphereShapeViewModels)
                    {
                        ssvm.SphereShape.changeRotationAngle(ssvm.AngularVelocityRadians);
                    }
                }
                //propagate changes top-down from roots
                foreach (ShapeTreeViewModel root in rootShapeNodes)
                {
                    root.ShapeNode.propagateModelTransformToChildren();
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
                    var window = Window.GetWindow(_view);
                    var width = (window.ActualWidth - _view.ActualWidth - 16) / 2;
                    var height = (window.ActualHeight - 38);

                    //mouse coordinates
                    int mouse_x = (x > width) ? (int)(x - width - _view.ActualWidth) : x;
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
                        SetSelectedShapeNode(pickedShape);

                        pickedRoot = findRootShape(pickedShape);
                        rootOriginalPosition = pickedRoot.Position;
                        objectPicked = true;

                    }
                }

        }

        private ShapeNode findRootShape(ShapeNode shape)
        {
            while ( shape.Parent != null && shape.Parent is ShapeNode) 
                shape = (ShapeNode)shape.Parent;
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
                var window = Window.GetWindow(_view);
                var width = (window.ActualWidth - _view.ActualWidth - 16) / 2;
                var height = (window.ActualHeight - 38);

                //mouse coordinates
                int mouse_x = (x > width) ? (int)(x - width - _view.ActualWidth) : x;
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

                if (pickedRoot == _pickedShapeViewModel.ShapeNode)
                    OnPropertyChanged("PickedShape");


            }
        }

        public void KeyDown(int key)
        {
        }

        bool treeItemPicked = false;
        public void MouseDownOnThisShapeTreeViewModel(ShapeTreeViewModel stvm)
        {
            DebugDisplayText += "Mouse DOWN on : " + stvm.DisplayText + "\n";

            treeItemPicked = true;
            _pickedShapeTreeViewModel = stvm;
            SetSelectedShapeNode(stvm.ShapeNode);

        }
        public void MouseUpOnThisShapeTreeViewModel(ShapeTreeViewModel stvm)
        {
            DebugDisplayText += "Mouse UP on : " + stvm.DisplayText +"\n";

            if (_pickedShapeTreeViewModel != null && _pickedShapeTreeViewModel != stvm && treeItemPicked)
            {
                rearrangeTreeNodes(_pickedShapeTreeViewModel, stvm);
                OnPropertyChanged("PickedShape");
            }
            treeItemPicked = false;
        }

        public void rearrangeTreeNodes(ShapeTreeViewModel source, ShapeTreeViewModel target)
        {
            if (source.IsAncestorOf(target))
            {
                ShapeNode tmp = target.ShapeNode;
                target.ShapeNode = source.ShapeNode;
                source.ShapeNode = tmp;
            }
            else
            {
                if (source.Parent != null)
                    source.Parent.Children.Remove(source);
                else
                    rootShapeNodes.Remove(source);
                target.Children.Add(source);
                source.Parent = target;
            }

            refreshNodes();

        }

        private void refreshNodes()
        {
            foreach (var viewmodel in rootShapeNodes)
            {
                refreshNodes(viewmodel, null);
                viewmodel.ShapeNode.ParentModelTransform = Matrix4.Identity;
            }
        }
        private void refreshNodes(ShapeTreeViewModel viewmodel, ShapeNode parent)
        {
            var curr = viewmodel.ShapeNode;
            curr.Parent = parent;
            var currChildren = curr.Children.ToList();
            foreach (var child in currChildren)
                curr.RemoveChild(child);

            foreach (var vmchild in viewmodel.Children)
            {
                curr.AddChild(vmchild.ShapeNode);
                refreshNodes(vmchild, curr);
            }
        }

        internal void makeSelectedTreeViewModelRoot()
        {

            var pickedShapeTVM = _pickedShapeTreeViewModel;
            if (pickedShapeTVM == null)
                pickedShapeTVM = _pickedShapeTVM_byTriggers;

            if (pickedShapeTVM != null)
            {
                DebugDisplayText += "makeSelectedTreeViewModelRoot() called\n\t_pickedShapeTreeViewModel = "
                    + pickedShapeTVM.DisplayText + "\n";

                var parent = pickedShapeTVM.Parent;
                if (parent == null)
                    return;
                parent.Children.Remove(pickedShapeTVM);
                pickedShapeTVM.Parent = null;
                rootShapeNodes.Add(pickedShapeTVM);

                var parentShape = parent.ShapeNode;
                var pickedShape = pickedShapeTVM.ShapeNode;
                parentShape.RemoveChild(pickedShape);
                pickedShape.Parent = null;
                pickedShape.ParentModelTransform = Matrix4.Identity;

            }
        }

        internal void StopAnimation()
        {
            animationPaused = true;
        }

        internal void RunAnimation()
        {
            animationPaused = false;
        }

    }

    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {            
            return (values[0] as PickedShapeViewModel).ShapeNode == (values[1] as ShapeTreeViewModel).ShapeNode;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}