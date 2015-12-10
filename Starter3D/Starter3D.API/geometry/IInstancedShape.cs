using OpenTK;
using System.Collections.Generic;

namespace Starter3D.API.geometry
{
    public interface IInstancedMesh : IShape
    {
        void AddInstance(Matrix4 instanceMatrix);
        //void ClearInstances();
        //List<Matrix4> InstancedMatrices { get; set; }
    }
}