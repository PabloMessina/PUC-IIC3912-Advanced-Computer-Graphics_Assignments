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
    public class SingleInstancedObject<T> : IMeshWrapper where T : PhysicalObjectData
    {
        private T _data;
        private IMesh _mesh;

        public IMesh Mesh { get { return _mesh; } }
        public T InstanceData { get { return _data; } }

        public SingleInstancedObject(IMesh mesh, T physicalData)
        {
           _mesh = mesh;
           _data = physicalData;
        }

        public void Configure(IRenderer renderer)
        {
            _mesh.Configure(renderer);
        }

        public void Render(IRenderer renderer)
        {
            _mesh.Render(renderer, _data.ModelTransform);
        }
    }
}
