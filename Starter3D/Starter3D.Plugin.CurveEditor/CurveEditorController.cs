using OpenTK;
using Starter3D.API.controller;
using Starter3D.API.geometry;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using Starter3D.API.scene.persistence;
using Starter3D.API.utils;
using System;
using System.Collections.Generic;
namespace Starter3D.Plugin.CurveEditor
{

    public enum CurveType {
        Bezier, Catmull_Rom
    }
    public class CurveEditorController : ViewModelBase, IController
    {
        private const string ResourcePath = @"resources/curveEditorResources.xml";

        private readonly IRenderer _renderer;
        private readonly IResourceManager _resourceManager;
        private readonly CurveEditorView _view;

        private ICurve _curve;
        private IPoints _points;

        private double _width;
        private double _height;

        private List<Vector4> _pointList = new List<Vector4>();

        private CurveType _selectedCurveType = CurveType.Catmull_Rom;
        private Array _curveTypes = Enum.GetValues(typeof(CurveType));

        private float _stepSize = 1;

        private Matrix4 _baseMatrix_CatmullRom =
            new Matrix4(0, 1, 0, 0, -0.5f, 0, 0.5f, 0, 1, -2.5f, 2, -0.5f, -0.5f, 1.5f, -1.5f, 0.5f);

        private Matrix4 _baseMatrix_Bezier =
            new Matrix4(1, 0, 0, 0, -3, 3, 0, 0, 3, -6, 3, 0, -1, 3, -3, 1);

        private static Vector3 dummyNormal = new Vector3();
        private static Vector3 dummyTextureCoord = new Vector3();


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
            get { return "CurveEditor"; }
        }

        public Array CurveTypes
        {
            get { return _curveTypes; }
        }

        public CurveType SelectedCurveType
        {
            get { return _selectedCurveType; }
            set
            {
                if (_selectedCurveType != value)
                {
                    _selectedCurveType = value;
                    RefreshCurve();
                    RaisePropertyChanged("SelectedCurveType");                    
                }
            }
        }

        public float StepSize
        {
            get { return _stepSize; }
            set
            {
                if (value != _stepSize)
                {
                    _stepSize = value;
                    RefreshCurve();
                    RaisePropertyChanged("StepSize");
                }
            }
        }


        public CurveEditorController(IRenderer renderer, ISceneReader sceneReader, IResourceManager resourceManager)
        {
            if (renderer == null) throw new ArgumentNullException("renderer");
            if (resourceManager == null) throw new ArgumentNullException("resourceManager");
            _renderer = renderer;
            _resourceManager = resourceManager;

            _resourceManager.Load(ResourcePath);

            _view = new CurveEditorView();
            _view.DataContext = this;
        }

        public void Load()
        {
            InitRenderer();
            _resourceManager.Configure(_renderer);
            _curve = new Curve("curve", 2);
            _curve.Material = _resourceManager.GetMaterial("lineMaterial");
            _curve.Configure(_renderer);

            _points = new Points("points", 5);
            _points.Material = _resourceManager.GetMaterial("pointMaterial");
            _points.Configure(_renderer);
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
            _curve.Render(_renderer, Matrix4.Identity);
            _points.Render(_renderer, Matrix4.Identity);
        }

        public void Update(double deltaTime)
        {
        }

        public void MouseDown(ControllerMouseButton button, int x, int y)
        {
            if (x > _width) x = (int) (x - _width);
            float adjustedX = (2.0f * (float)x / (float)_width) - 1;
            float adjustedY = (2.0f * (float)(_height - y) / (float)_height) - 1;
            var mousePoint = new Vector3(adjustedX, adjustedY, 0);

            AddNewPointToCurve(new Vector4(adjustedX, adjustedY, 0, 1));

        }

        private void AddNewPointToCurve(Vector4 newPoint)
        {
            _pointList.Add(newPoint); 
            _points.AddPoint(new Vertex(newPoint.Xyz, dummyNormal, dummyTextureCoord));

            switch (_selectedCurveType)
            {
                case CurveType.Bezier:
                    handleNewPoint_Bezier();
                    break;
                case CurveType.Catmull_Rom:
                    handleNewPoint_CatmullRom();
                    break;
            }

            _curve.Configure(_renderer);
            _points.Configure(_renderer);
        }

        private void handleNewPoint_CatmullRom()
        {
            if (_pointList.Count < 4)
                return;
            int index = _pointList.Count - 1;
            var p3 = _pointList[index--];
            var p2 = _pointList[index--];
            var p1 = _pointList[index--];
            var p0 = _pointList[index];


            if (_pointList.Count == 4)
                _curve.AddPoint(new Vertex(p1.Xyz, dummyNormal, dummyTextureCoord));

            var pointMatrix = new Matrix4(p0, p1, p2, p3);           
            var productMatrix = _baseMatrix_CatmullRom * pointMatrix;

            var tvector = new Vector4(1, 0, 0, 0);

            for (float t = _stepSize; t < 1; t += _stepSize)
            {
                tvector.X = 1;
                tvector.Y = t;
                tvector.Z = t * t;
                tvector.W = t * t * t;
                var interp = Vector4.Transform(tvector, productMatrix).Xyz;
                _curve.AddPoint(new Vertex(interp, dummyNormal, dummyTextureCoord));
            }

            _curve.AddPoint(new Vertex(p2.Xyz, dummyNormal, dummyTextureCoord));
        }

        private void handleNewPoint_Bezier()
        {
            if (_pointList.Count < 4 || (_pointList.Count - 4) % 3 != 0)
                return;

            int index = _pointList.Count - 1;
            var p3 = _pointList[index--];
            var p2 = _pointList[index--];
            var p1 = _pointList[index--];
            var p0 = _pointList[index];

            if (_pointList.Count == 4)
                _curve.AddPoint(new Vertex(p0.Xyz, dummyNormal, dummyTextureCoord));

            var pointMatrix = new Matrix4(p0, p1, p2, p3);
            var productMatrix = _baseMatrix_Bezier * pointMatrix;

            var tvector = new Vector4(1, 0, 0, 0);

            for (float t = _stepSize; t < 1; t += _stepSize)
            {
                tvector.X = 1;
                tvector.Y = t;
                tvector.Z = t * t;
                tvector.W = t * t * t;
                var interp = Vector4.Transform(tvector, productMatrix).Xyz;
                _curve.AddPoint(new Vertex(interp, dummyNormal, dummyTextureCoord));
            }

            _curve.AddPoint(new Vertex(p3.Xyz, dummyNormal, dummyTextureCoord));
        }

        private void RefreshCurve()
        {
            _curve.Clear();

            switch (_selectedCurveType)
            {
                case CurveType.Bezier:
                    RefreshCurve_Bezier();
                    break;
                case CurveType.Catmull_Rom:
                    RefreshCurve_CatmullRom();
                    break;
            }

            _curve.Configure(_renderer);

        }

        private void RefreshCurve_CatmullRom()
        {
            if (_pointList.Count < 4)
                return;

            //first point
            _curve.AddPoint(new Vertex(_pointList[1].Xyz, dummyNormal, dummyTextureCoord));

            for (int index = 3; index < _pointList.Count; ++index)
            {
                var p3 = _pointList[index];
                var p2 = _pointList[index-1];
                var p1 = _pointList[index-2];
                var p0 = _pointList[index-3];

                var pointMatrix = new Matrix4(p0, p1, p2, p3);
                var productMatrix = _baseMatrix_CatmullRom * pointMatrix;

                var tvector = new Vector4(1, 0, 0, 0);

                for (float t = _stepSize; t < 1; t += _stepSize)
                {
                    tvector.X = 1;
                    tvector.Y = t;
                    tvector.Z = t * t;
                    tvector.W = t * t * t;
                    var interp = Vector4.Transform(tvector, productMatrix).Xyz;
                    _curve.AddPoint(new Vertex(interp, dummyNormal, dummyTextureCoord));
                }

                _curve.AddPoint(new Vertex(p2.Xyz, dummyNormal, dummyTextureCoord));
            }
        }

        private void RefreshCurve_Bezier()
        {
            if (_pointList.Count < 4)
                return;

            //first point
            _curve.AddPoint(new Vertex(_pointList[0].Xyz, dummyNormal, dummyTextureCoord));

            for (int index = 3; index < _pointList.Count; index+=3)
            {
                var p3 = _pointList[index];
                var p2 = _pointList[index-1];
                var p1 = _pointList[index-2];
                var p0 = _pointList[index-3];

                var pointMatrix = new Matrix4(p0, p1, p2, p3);
                var productMatrix = _baseMatrix_Bezier * pointMatrix;

                var tvector = new Vector4(1, 0, 0, 0);

                for (float t = _stepSize; t < 1; t += _stepSize)
                {
                    tvector.X = 1;
                    tvector.Y = t;
                    tvector.Z = t * t;
                    tvector.W = t * t * t;
                    var interp = Vector4.Transform(tvector, productMatrix).Xyz;
                    _curve.AddPoint(new Vertex(interp, dummyNormal, dummyTextureCoord));
                }

                _curve.AddPoint(new Vertex(p3.Xyz, dummyNormal, dummyTextureCoord));
            }
        }

        internal void ClearAll()
        {
            _pointList.Clear();
            _curve.Clear();
            _points.Clear();
        }

        public void MouseUp(ControllerMouseButton button, int x, int y)
        {
        }

        public void MouseWheel(int delta, int x, int y)
        {
        }

        public void MouseMove(int x, int y, int deltaX, int deltaY)
        {
        }

        public void KeyDown(int key)
        {
        }

        public void UpdateSize(double width, double height)
        {
            _width = width;
            _height = height;
        }
    }
}
