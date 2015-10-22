using OpenTK;
using Starter3D.API.geometry;
using Starter3D.API.renderer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.Physics
{
    public class MultiInstancedObject<T> : IMeshWrapper where T : PhysicalObjectData
    {
        private List<T> _instancesData = new List<T>();
        private InstancedMesh _instancedMesh;
        
        public InstancedMesh InstancedMesh { get { return _instancedMesh; } }
        public List<T> InstancesData { get { return _instancesData; } }

        public MultiInstancedObject(InstancedMesh instancedMesh)
        {            
            _instancedMesh = instancedMesh;
        }

        public void AddObject(T obj)
        {
            _instancesData.Add(obj);
            _instancedMesh.AddInstance(obj.ModelTransform);
        }

        public void RefreshTransforms()
        {
            for (int i = 0; i < _instancesData.Count; i++)
            {
                _instancedMesh.InstancedMatrices[i] = _instancesData[i].ModelTransform;
            }
        }
        public void Configure(IRenderer renderer)
        {
            _instancedMesh.Configure(renderer);
        }

        public void Render(IRenderer renderer)
        {
            _instancedMesh.Render(renderer, Matrix4.Identity);
        }

        public void ClearInstances()
        {
            _instancedMesh.ClearInstances();
            _instancesData.Clear();
        }
    }
}
