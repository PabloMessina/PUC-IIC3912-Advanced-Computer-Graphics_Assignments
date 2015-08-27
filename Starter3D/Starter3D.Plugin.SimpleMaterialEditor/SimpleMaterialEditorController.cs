using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenTK;
using Starter3D.API.controller;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using Starter3D.API.scene;
using Starter3D.API.scene.nodes;
using Starter3D.API.scene.persistence;
using Starter3D.API.utils;

namespace Starter3D.Plugin.SimpleMaterialEditor
{
  public class SimpleMaterialEditorController : IController
  {
    private const string ScenePath = @"scenes/simpleScene.xml";
    private const string ResourcePath = @"resources/simpleResources.xml";

    private readonly IRenderer _renderer;
    private readonly ISceneReader _sceneReader;
    private readonly IResourceManager _resourceManager;

    private readonly IScene _scene;
    
    private readonly SimpleMaterialEditorView _centralView;
    private ShapeNode _currentShape;
    private IMaterial _currentMaterial;
    private List<ShapeNode> _shapes;
    private List<IMaterial> _materials;
     

    //mouse button fields
    bool isLeftDown = false;
    bool isRightDown = false;
    
    
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
      get { return "Simple Material Editor"; }
    }

    public IMaterial CurrentMaterial
    {
        get { return _currentMaterial; }
    }



    public SimpleMaterialEditorController(IRenderer renderer, ISceneReader sceneReader, IResourceManager resourceManager)
    {
      if (renderer == null) throw new ArgumentNullException("renderer");
      if (sceneReader == null) throw new ArgumentNullException("sceneReader");
      if (resourceManager == null) throw new ArgumentNullException("resourceManager");
      _renderer = renderer;
      _sceneReader = sceneReader;
      _resourceManager = resourceManager;

      _resourceManager.Load(ResourcePath);
      _scene = _sceneReader.Read(ScenePath);
      
      _centralView = new SimpleMaterialEditorView(_scene.Shapes,_resourceManager.GetMaterials(), this);
      _centralView.ShapeChanged+= ShapeChanged_EventHandler;
      _centralView.MaterialChanged += MaterialChanged_EventHandler;
      _centralView.NumericParameterChanged += NumericParameterChanged_EventHandler;
      _centralView.VectorParameterChanged += VectorParameterChanged_EventHandler;
    }

    private void NumericParameterChanged_EventHandler(string key, float val)
    {
        _currentMaterial.SetParameter(key, val);
    }

    private void VectorParameterChanged_EventHandler(string key, Vector3 val)
    {
        _currentMaterial.SetParameter(key, val);
    }

    private void ShapeChanged_EventHandler(int index)
    {
        _currentShape = _shapes[index];
        _currentShape.Shape.Material = _currentMaterial;      
        _scene.ClearShapes();
        _scene.AddShape(_currentShape);
        var vecParams = _currentMaterial.VectorParameters.ToList();
        var numParams = _currentMaterial.NumericParameters.ToList();
        foreach (var vp in vecParams)
            _currentMaterial.SetParameter(vp.Key, vp.Value);
        foreach (var np in numParams)
            _currentMaterial.SetParameter(np.Key, np.Value);
    }

    private void MaterialChanged_EventHandler(string matName)
    {
        _currentMaterial = _resourceManager.GetMaterial(matName);
        _currentShape.Shape.Material = _currentMaterial;
        var vecParams = _currentMaterial.VectorParameters.ToList();
        var numParams = _currentMaterial.NumericParameters.ToList();
        foreach (var vp in vecParams)
            _currentMaterial.SetParameter(vp.Key, vp.Value);
        foreach (var np in numParams)
            _currentMaterial.SetParameter(np.Key, np.Value);
    }
    
    public void Load()
    {
        InitRenderer();

        _resourceManager.Configure(_renderer);
        _scene.Configure(_renderer);

        _shapes = _scene.Shapes.ToList();
        _materials = _resourceManager.GetMaterials().ToList();
        
        _currentMaterial = _materials[0];

        _currentShape = _shapes[0];
        _currentShape.Shape.Material = _currentMaterial;

        _scene.ClearShapes();
        _scene.AddShape(_currentShape);
        
    }

    private void InitRenderer()
    {
      _renderer.SetBackgroundColor(0.9f,0.9f,1.0f);
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

    }

    public void UpdateSize(double width, double height)
    {
      var perspectiveCamera = _scene.CurrentCamera as PerspectiveCamera;
      if (perspectiveCamera != null)
        perspectiveCamera.AspectRatio = (float)(width / height);
    }

    public void MouseWheel(int delta, int x, int y)
    {
        _scene.CurrentCamera.Zoom(delta);
    }

    public void MouseDown(ControllerMouseButton button, int x, int y)
    {
        if (button == ControllerMouseButton.Left)
            isLeftDown = true;
        else if (button == ControllerMouseButton.Right)
            isRightDown = true;
    }

    public void MouseUp(ControllerMouseButton button, int x, int y)
    {
        if (button == ControllerMouseButton.Left)
            isLeftDown = false;
        else if (button == ControllerMouseButton.Right)
            isRightDown = false;
    }

    public void MouseMove(int x, int y, int deltaX, int deltaY)
    {
        if (isLeftDown)
            _scene.CurrentCamera.Orbit(deltaX, deltaY);
        else if (isRightDown)
            _scene.CurrentCamera.Drag(deltaX, deltaY);
    }

    public void KeyDown(int key)
    {
      
    }

  }
}