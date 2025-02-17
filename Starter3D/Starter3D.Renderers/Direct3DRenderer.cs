﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D10;
using SlimDX.Direct3D10_1;
using SlimDX.DXGI;
using Starter3D.API.renderer;
using Buffer = SlimDX.Direct3D10.Buffer;
using CullMode = Starter3D.API.utils.CullMode;
using Device = SlimDX.Direct3D10_1.Device1;
using MapFlags = SlimDX.Direct3D10.MapFlags;
using Vector3 = SlimDX.Vector3;
using Vector4 = SlimDX.Vector4;

namespace Starter3D.Renderers
{
    public class Direct3DRenderer : IRenderer
    {
        class RenderObject
        {
            public List<InputElement> InputElements = new List<InputElement>();
            public Buffer VertexBuffer;
            public Buffer IndexBuffer;
            public Buffer InstanceBuffer;
            public int IndexCount;
            public int VertexBufferSize;
            public int IndexBufferSize;
            public int InstanceBufferSize;
        }

        class ShaderProgram
        {
            public Effect Effect;

            public ShaderProgram(Effect effect)
            {
                Effect = effect;
            }
        }

        class TextureInfo
        {
            public ShaderResourceView TextureResourceView;
            public Texture2D Texture2D;
        }


        private const string ShaderBasePath = @"shaders/direct3d";
        private const string EffectExtension = ".fx";
        private const string ShaderVersion = "fx_4_0";

        private string _currentShader;
        private readonly Dictionary<string, ShaderProgram> _shaderHandleDictionary = new Dictionary<string, ShaderProgram>();
        private readonly Dictionary<string, RenderObject> _objectsHandleDictionary = new Dictionary<string, RenderObject>();
        private readonly Dictionary<string, TextureInfo> _textureHandleDictionary = new Dictionary<string, TextureInfo>();
        private readonly Dictionary<string, List<Vector4>> _vectorArrayShaderParameterDictionary = new Dictionary<string, List<Vector4>>();

        private readonly Dictionary<string, string> _semanticsTable = new Dictionary<string, string>();
        private readonly Dictionary<InputElement[], Dictionary<EffectPass, InputLayout>> _inputLayouts = new Dictionary<InputElement[], Dictionary<EffectPass, InputLayout>>();

        private readonly Device _device;

        private RasterizerStateDescription _rasterizerStateDescription;
        private DepthStencilStateDescription _depthStencilStateDescription;

        private Color4 _background = new Color4(new Vector3(1, 1, 1));

        public Device Direct3DDevice
        {
            get { return _device; }
        }

        public Color4 Background
        {
            get { return _background; }
            set { _background = value; }
        }


        public Direct3DRenderer()
        {
            _device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport, FeatureLevel.Level_10_0);
            _rasterizerStateDescription = new RasterizerStateDescription()
            {
                CullMode = SlimDX.Direct3D10.CullMode.None,
                FillMode = FillMode.Solid,
                IsFrontCounterclockwise = true
            };
            var rasterizerState = RasterizerState.FromDescription(_device, _rasterizerStateDescription);
            _device.Rasterizer.State = rasterizerState;

            _depthStencilStateDescription = new DepthStencilStateDescription()
            {
                IsDepthEnabled = true,
                DepthComparison = Comparison.Less,
                DepthWriteMask = DepthWriteMask.All
            };
            var depthStencilState = DepthStencilState.FromDescription(_device, _depthStencilStateDescription);
            _device.OutputMerger.DepthStencilState = depthStencilState;

            _semanticsTable.Add("inPosition", "POSITION");
            _semanticsTable.Add("inNormal", "NORMAL");
            _semanticsTable.Add("inTextureCoords", "TEXCOORD");
            _semanticsTable.Add("instanceMatrix", "INSTANCE_TRANSFORM");
        }

        public void LoadObject(string objectName)
        {
            if (_objectsHandleDictionary.ContainsKey(objectName))
            {
                _objectsHandleDictionary[objectName].IndexBuffer.Dispose();
                _objectsHandleDictionary[objectName].VertexBuffer.Dispose();
                if (_objectsHandleDictionary[objectName].InstanceBuffer != null)
                    _objectsHandleDictionary[objectName].InstanceBuffer.Dispose();
                _objectsHandleDictionary[objectName].InputElements.Clear();
                return;
            }
            _objectsHandleDictionary[objectName] = new RenderObject();
        }

        public void DrawTriangles(string objectName, int triangleCount)
        {
            if (!_objectsHandleDictionary.ContainsKey(objectName))
                throw new ApplicationException("Object must be added to the renderer before drawing");
            var effect = _shaderHandleDictionary[_currentShader].Effect;
            var technique = effect.GetTechniqueByIndex(0);
            var pass = technique.GetPassByIndex(0);
            pass.Apply();
            _device.InputAssembler.SetInputLayout(GetInputLayout(pass, _objectsHandleDictionary[objectName].InputElements.ToArray()));
            _device.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _device.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_objectsHandleDictionary[objectName].VertexBuffer, OpenTK.Vector3.SizeInBytes * _objectsHandleDictionary[objectName].InputElements.Count, 0));
            _device.InputAssembler.SetIndexBuffer(_objectsHandleDictionary[objectName].IndexBuffer, Format.R32_UInt, 0);
            _device.DrawIndexed(_objectsHandleDictionary[objectName].IndexCount, 0, 0);
        }

        public void DrawLines(string objectName, int lineCount, float lineWidth)
        {
            if (!_objectsHandleDictionary.ContainsKey(objectName))
                throw new ApplicationException("Object must be added to the renderer before drawing");
            var effect = _shaderHandleDictionary[_currentShader].Effect;
            var technique = effect.GetTechniqueByIndex(0);
            var pass = technique.GetPassByIndex(0);
            pass.Apply();
            _device.InputAssembler.SetInputLayout(GetInputLayout(pass, _objectsHandleDictionary[objectName].InputElements.ToArray()));
            _device.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.LineStrip);
            _device.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_objectsHandleDictionary[objectName].VertexBuffer, OpenTK.Vector3.SizeInBytes * _objectsHandleDictionary[objectName].InputElements.Count, 0));
            _device.InputAssembler.SetIndexBuffer(_objectsHandleDictionary[objectName].IndexBuffer, Format.R32_UInt, 0);
            _device.DrawIndexed(_objectsHandleDictionary[objectName].IndexCount, 0, 0);
        }

        public void DrawPoints(string objectName, int pointCount, float pointSize)
        {
            if (!_objectsHandleDictionary.ContainsKey(objectName))
                throw new ApplicationException("Object must be added to the renderer before drawing");
            var effect = _shaderHandleDictionary[_currentShader].Effect;
            var technique = effect.GetTechniqueByIndex(0);
            var pass = technique.GetPassByIndex(0);
            pass.Apply();
            _device.InputAssembler.SetInputLayout(GetInputLayout(pass, _objectsHandleDictionary[objectName].InputElements.ToArray()));
            _device.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.PointList);
            _device.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_objectsHandleDictionary[objectName].VertexBuffer, OpenTK.Vector3.SizeInBytes * _objectsHandleDictionary[objectName].InputElements.Count, 0));
            _device.InputAssembler.SetIndexBuffer(_objectsHandleDictionary[objectName].IndexBuffer, Format.R32_UInt, 0);
            _device.DrawIndexed(_objectsHandleDictionary[objectName].IndexCount, 0, 0);
        }

        public void SetVerticesData(string objectName, List<OpenTK.Vector3> data)
        {
            var dataSize = data.Count;
            CreateVerticesData(objectName, data, dataSize);
        }

        private void CreateVerticesData(string objectName, List<OpenTK.Vector3> data, int dataSize)
        {
            if (!_objectsHandleDictionary.ContainsKey(objectName))
                throw new ApplicationException("Object must be added to the renderer before setting its vertex data");
            var verticesStream = new DataStream(dataSize * OpenTK.Vector3.SizeInBytes, true, true);
            foreach (var vector in data)
            {
                verticesStream.Write(new Vector3(vector.X, vector.Y, vector.Z));
            }
            verticesStream.Position = 0;
            var bufferDesc = new BufferDescription
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = dataSize * OpenTK.Vector3.SizeInBytes,
                Usage = ResourceUsage.Dynamic
            };
            var vertexBuffer = new Buffer(_device, verticesStream, bufferDesc);
            _objectsHandleDictionary[objectName].VertexBuffer = vertexBuffer;
            _objectsHandleDictionary[objectName].VertexBufferSize = dataSize;
        }

        public void SetIndexData(string objectName, List<int> indices)
        {
            var indicesSize = indices.Count;
            CreateIndexData(objectName, indices, indicesSize);
        }

        private void CreateIndexData(string objectName, List<int> indices, int indicesSize)
        {
            if (!_objectsHandleDictionary.ContainsKey(objectName))
                throw new ApplicationException("Object must be added to the renderer before setting its index data");
            var indicesStream = new DataStream(indicesSize * sizeof(int), true, true);
            foreach (var index in indices)
            {
                indicesStream.Write(index);
            }
            indicesStream.Position = 0;
            var bufferDesc = new BufferDescription
            {
                BindFlags = BindFlags.IndexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = indicesSize * sizeof(int),
                Usage = ResourceUsage.Dynamic
            };
            var indexBuffer = new Buffer(_device, indicesStream, bufferDesc);
            _objectsHandleDictionary[objectName].IndexBuffer = indexBuffer;
            _objectsHandleDictionary[objectName].IndexCount = indices.Count;
            _objectsHandleDictionary[objectName].IndexBufferSize = indicesSize;
        }

        public void SetVertexAttribute(string objectName, string shaderName, int index, string vertexPropertyName, int stride, int offset)
        {
            if (!_objectsHandleDictionary.ContainsKey(objectName))
                throw new ApplicationException("Object must be added to the renderer before setting its index data");
            var inputElement = new InputElement(_semanticsTable[vertexPropertyName], 0, Format.R32G32B32A32_Float, offset, 0);
            _objectsHandleDictionary[objectName].InputElements.Add(inputElement);
        }

        public void DrawInstancedTriangles(string objectName, int triangleCount, int instanceCount)
        {
            if (!_objectsHandleDictionary.ContainsKey(objectName))
                throw new ApplicationException("Object must be added to the renderer before drawing");
            var effect = _shaderHandleDictionary[_currentShader].Effect;
            var technique = effect.GetTechniqueByIndex(0);
            var pass = technique.GetPassByIndex(0);
            pass.Apply();
            _device.InputAssembler.SetInputLayout(GetInputLayout(pass, _objectsHandleDictionary[objectName].InputElements.ToArray()));
            _device.InputAssembler.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
            var vertexBufferBinding = new VertexBufferBinding(_objectsHandleDictionary[objectName].VertexBuffer,
              OpenTK.Vector3.SizeInBytes * _objectsHandleDictionary[objectName].InputElements.Count(ie => ie.Classification != InputClassification.PerInstanceData), 0);
            var instanceBufferBinding = new VertexBufferBinding(_objectsHandleDictionary[objectName].InstanceBuffer,
              OpenTK.Vector4.SizeInBytes * _objectsHandleDictionary[objectName].InputElements.Count(ie => ie.Classification == InputClassification.PerInstanceData), 0);

            _device.InputAssembler.SetVertexBuffers(0, new[] { vertexBufferBinding, instanceBufferBinding });
            _device.InputAssembler.SetIndexBuffer(_objectsHandleDictionary[objectName].IndexBuffer, Format.R32_UInt, 0);
            _device.DrawIndexedInstanced(_objectsHandleDictionary[objectName].IndexCount, instanceCount, 0, 0, 0);
        }

        public void SetInstanceData(string objectName, List<Matrix4> instanceData)
        {
            CreateInstanceBuffer(objectName, instanceData, instanceData.Count);
        }

        private void CreateInstanceBuffer(string objectName, List<Matrix4> instanceData, int bufferSize)
        {
            var verticesStream = new DataStream(bufferSize * OpenTK.Vector4.SizeInBytes * 4, true, true);
            foreach (var matrix in instanceData)
            {
                verticesStream.Write(matrix.ToSlimDXMatrix());
            }
            verticesStream.Position = 0;
            var bufferDesc = new BufferDescription
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = bufferSize * OpenTK.Vector4.SizeInBytes * 4,
                Usage = ResourceUsage.Dynamic
            };
            var instanceBuffer = new Buffer(_device, verticesStream, bufferDesc);
            _objectsHandleDictionary[objectName].InstanceBuffer = instanceBuffer;
            _objectsHandleDictionary[objectName].InstanceBufferSize = bufferSize;
        }

        public void UpdateVerticesData(string objectName, List<OpenTK.Vector3> data)
        {
            if (data.Count > _objectsHandleDictionary[objectName].VertexBufferSize)
            {
                _objectsHandleDictionary[objectName].VertexBuffer.Dispose();
                CreateVerticesData(objectName, data, data.Count * 2);
            }
            else
            {
                using (
                  var dataStream = _objectsHandleDictionary[objectName].VertexBuffer.Map(MapMode.WriteDiscard, MapFlags.None))
                {
                    foreach (var vector in data)
                    {
                        dataStream.Write(new Vector3(vector.X, vector.Y, vector.Z));
                    }
                    dataStream.Position = 0;
                    _objectsHandleDictionary[objectName].VertexBuffer.Unmap();
                }
            }
        }

        public void UpdateIndexData(string objectName, List<int> indices)
        {
            if (indices.Count > _objectsHandleDictionary[objectName].IndexBufferSize)
            {
                _objectsHandleDictionary[objectName].IndexBuffer.Dispose();
                CreateIndexData(objectName, indices, indices.Count * 2);
            }
            else
            {
                _objectsHandleDictionary[objectName].IndexCount = indices.Count;
                using (
                  var indicesStream = _objectsHandleDictionary[objectName].IndexBuffer.Map(MapMode.WriteDiscard, MapFlags.None))
                {
                    foreach (var index in indices)
                    {
                        indicesStream.Write(index);
                    }
                    indicesStream.Position = 0;
                    _objectsHandleDictionary[objectName].IndexBuffer.Unmap();
                }
            }
        }

        public void UpdateInstanceData(string objectName, List<Matrix4> instanceData)
        {
            if (instanceData.Count > _objectsHandleDictionary[objectName].InstanceBufferSize)
            {
                _objectsHandleDictionary[objectName].InstanceBuffer.Dispose();
                CreateInstanceBuffer(objectName, instanceData, instanceData.Count * 2);
            }
            else
            {
                using (
                  var instancesStream = _objectsHandleDictionary[objectName].InstanceBuffer.Map(MapMode.WriteDiscard, MapFlags.None))
                {
                    foreach (var matrix in instanceData)
                    {
                        instancesStream.Write(matrix.ToSlimDXMatrix());
                    }
                    instancesStream.Position = 0;
                    _objectsHandleDictionary[objectName].InstanceBuffer.Unmap();
                }
            }
        }

        public void SetInstanceAttribute(string objectName, string shaderName, int index, string instancePropertyName, int stride,
          int offset)
        {
            if (!_objectsHandleDictionary.ContainsKey(objectName))
                throw new ApplicationException("Object must be added to the renderer before setting its index data");
            var inputElement = new InputElement(_semanticsTable[instancePropertyName], 0, Format.R32G32B32A32_Float, 0, 1, InputClassification.PerInstanceData, 1);
            _objectsHandleDictionary[objectName].InputElements.Add(inputElement);
            inputElement = new InputElement(_semanticsTable[instancePropertyName], 1, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1);
            _objectsHandleDictionary[objectName].InputElements.Add(inputElement);
            inputElement = new InputElement(_semanticsTable[instancePropertyName], 2, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1);
            _objectsHandleDictionary[objectName].InputElements.Add(inputElement);
            inputElement = new InputElement(_semanticsTable[instancePropertyName], 3, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1);
            _objectsHandleDictionary[objectName].InputElements.Add(inputElement);
        }

        public void LoadShaders(string shaderName, string vertexShaderFileName, string fragmentShaderFileName)
        {
            if (_shaderHandleDictionary.ContainsKey(shaderName))
                return;
            try
            {
                var shaderText = File.ReadAllText(Path.Combine(ShaderBasePath, fragmentShaderFileName + EffectExtension));
                var shaderEffect = Effect.FromString(_device, shaderText, ShaderVersion, ShaderFlags.None, EffectFlags.None, null, null, null);
                _shaderHandleDictionary.Add(shaderName, new ShaderProgram(shaderEffect));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        public void UseShader(string shaderName)
        {
            if (!_shaderHandleDictionary.ContainsKey(shaderName))
                throw new ApplicationException("Shader must be loaded before using it");
            _currentShader = shaderName;

        }

        public void EnableZBuffer(bool enable)
        {
            _depthStencilStateDescription.IsDepthEnabled = enable;
            var depthStencilState = DepthStencilState.FromDescription(_device, _depthStencilStateDescription);
            _device.OutputMerger.DepthStencilState = depthStencilState;
        }

        public void EnableWireframe(bool enable)
        {
            _rasterizerStateDescription.FillMode = enable ? FillMode.Wireframe : FillMode.Solid;
            var rasterizerState = RasterizerState.FromDescription(_device, _rasterizerStateDescription);
            _device.Rasterizer.State = rasterizerState;
        }

        public void SetCullMode(CullMode cullMode)
        {
            switch (cullMode)
            {
                case CullMode.None:
                    _rasterizerStateDescription.CullMode = SlimDX.Direct3D10.CullMode.None;
                    break;
                case CullMode.Back:
                    _rasterizerStateDescription.CullMode = SlimDX.Direct3D10.CullMode.Back;
                    break;
                case CullMode.Front:
                    _rasterizerStateDescription.CullMode = SlimDX.Direct3D10.CullMode.Front;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("cullMode");
            }
            var rasterizerState = RasterizerState.FromDescription(_device, _rasterizerStateDescription);
            _device.Rasterizer.State = rasterizerState;
        }

        public void SetBackgroundColor(float r, float g, float b)
        {
            _background = new Color4(new Vector3(r, g, b));
        }


        public void SetNumericParameter(string name, float number)
        {
            foreach (var shaderProgram in _shaderHandleDictionary)
            {
                var effect = shaderProgram.Value.Effect;
                var variable = effect.GetVariableByName(name).AsScalar();
                if (variable != null)
                    variable.Set(number);
            }
        }

        public void SetNumericParameter(string name, float number, string shader)
        {
            var effect = _shaderHandleDictionary[shader].Effect;
            var variable = effect.GetVariableByName(name).AsScalar();
            if (variable != null)
                variable.Set(number);
        }

        public void SetVectorParameter(string name, OpenTK.Vector3 vector, string shader)
        {
            var effect = _shaderHandleDictionary[shader].Effect;
            var variable = effect.GetVariableByName(name).AsVector();
            if (variable != null)
                variable.Set(vector.ToSlimDXVector3());
        }

        public void SetVectorParameter(string name, OpenTK.Vector3 vector)
        {
            foreach (var shaderProgram in _shaderHandleDictionary)
            {
                var effect = shaderProgram.Value.Effect;
                var variable = effect.GetVariableByName(name).AsVector();
                if (variable != null)
                    variable.Set(vector.ToSlimDXVector3());
            }
        }

        public void SetVectorArrayParameter(string name, int index, OpenTK.Vector3 vector, string shader)
        {
            if (!_vectorArrayShaderParameterDictionary.ContainsKey(name))
                _vectorArrayShaderParameterDictionary[name] = new List<Vector4>();
            _vectorArrayShaderParameterDictionary[name].Insert(index, vector.ToSlimDXVector4());
            var effect = _shaderHandleDictionary[shader].Effect;
            var variable = effect.GetVariableByName(name).AsVector();
            if (variable != null)
                variable.Set(_vectorArrayShaderParameterDictionary[name].ToArray());
        }


        public void SetVectorArrayParameter(string name, int index, OpenTK.Vector3 vector)
        {
            if (!_vectorArrayShaderParameterDictionary.ContainsKey(name))
                _vectorArrayShaderParameterDictionary[name] = new List<Vector4>();
            _vectorArrayShaderParameterDictionary[name].Insert(index, vector.ToSlimDXVector4());

            foreach (var shaderProgram in _shaderHandleDictionary)
            {
                var effect = shaderProgram.Value.Effect;
                var variable = effect.GetVariableByName(name).AsVector();
                if (variable != null)
                    variable.Set(_vectorArrayShaderParameterDictionary[name].ToArray());
            }
        }



        public void SetMatrixParameter(string name, Matrix4 matrix, string shader)
        {
            var effect = _shaderHandleDictionary[shader].Effect;
            var variable = effect.GetVariableByName(name).AsMatrix();
            if (variable != null)
                variable.SetMatrix(matrix.ToSlimDXMatrix());
        }

        public void SetMatrixParameter(string name, Matrix4 matrix)
        {
            foreach (var shaderProgram in _shaderHandleDictionary)
            {
                var effect = shaderProgram.Value.Effect;
                var variable = effect.GetVariableByName(name).AsMatrix();
                if (variable != null)
                    variable.SetMatrix(matrix.ToSlimDXMatrix());
            }
        }

        public void LoadTexture(string textureName, int index, Bitmap texture, TextureMinFilter minFilter, TextureMagFilter magFilter)
        {
            if (_textureHandleDictionary.ContainsKey(textureName))
                return;
            var texture2D = CreateTexture(texture.Width, texture.Height);
            LoadBitmapInTexture(texture, texture2D);
            var textureResource = CreateTextureResource(texture2D);
            _textureHandleDictionary.Add(textureName, new TextureInfo() { Texture2D = texture2D, TextureResourceView = textureResource });
        }

        public void UseTexture(string textureName, string shader, string uniformName)
        {
            if (!_textureHandleDictionary.ContainsKey(textureName))
                throw new ApplicationException("Texture has to be added before using");
            var effect = _shaderHandleDictionary[shader].Effect;

            var textureUniform = effect.GetVariableByName(uniformName).AsResource();
            if (textureUniform != null)
                textureUniform.SetResource(_textureHandleDictionary[textureName].TextureResourceView);
        }

        public void Dispose()
        {
            foreach (var renderObject in _objectsHandleDictionary)
            {
                renderObject.Value.IndexBuffer.Dispose();
                renderObject.Value.VertexBuffer.Dispose();
                if (renderObject.Value.InstanceBuffer != null)
                    renderObject.Value.InstanceBuffer.Dispose();
            }

            foreach (var inputLayout in _inputLayouts)
            {
                foreach (var inputLayoutPair in inputLayout.Value)
                {
                    inputLayoutPair.Value.Dispose();
                }
            }

            foreach (var shaderProgram in _shaderHandleDictionary)
            {
                shaderProgram.Value.Effect.Dispose();
            }

            foreach (var textureInfo in _textureHandleDictionary)
            {
                textureInfo.Value.Texture2D.Dispose();
                textureInfo.Value.TextureResourceView.Dispose();
            }
        }

        private Texture2D CreateTexture(int width, int height)
        {
            var desc2 = new Texture2DDescription
            {
                SampleDescription = new SampleDescription(1, 0),
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R8G8B8A8_UNorm,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write
            };
            return new Texture2D(_device, desc2);
        }

        private void LoadBitmapInTexture(Bitmap bitmap, Texture2D texture)
        {
            var rect = texture.Map(0, MapMode.WriteDiscard, MapFlags.None);
            if (rect.Data.CanWrite)
            {
                for (int j = 0; j < texture.Description.Height; j++)
                {
                    int rowStart = j * rect.Pitch;
                    rect.Data.Seek(rowStart, SeekOrigin.Begin);
                    for (int i = 0; i < texture.Description.Width; i++)
                    {
                        var color = bitmap.GetPixel(i, j);
                        rect.Data.WriteByte((byte)color.R);
                        rect.Data.WriteByte((byte)color.G);
                        rect.Data.WriteByte((byte)color.B);
                        rect.Data.WriteByte((byte)color.A);
                    }
                }
            }
            texture.Unmap(0);
        }

        private ShaderResourceView CreateTextureResource(Texture2D texture)
        {
            var desc = new ShaderResourceViewDescription
            {
                Format = texture.Description.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                MostDetailedMip = 0,
                MipLevels = 1
            };
            return new ShaderResourceView(_device, texture, desc);
        }

        private InputLayout GetInputLayout(EffectPass pass, InputElement[] elements)
        {
            if (!_inputLayouts.ContainsKey(elements))
                _inputLayouts.Add(elements, new Dictionary<EffectPass, InputLayout>());
            if (!_inputLayouts[elements].ContainsKey(pass))
                _inputLayouts[elements].Add(pass, new InputLayout(_device, pass.Description.Signature, elements));
            return _inputLayouts[elements][pass];
        }



    }
}