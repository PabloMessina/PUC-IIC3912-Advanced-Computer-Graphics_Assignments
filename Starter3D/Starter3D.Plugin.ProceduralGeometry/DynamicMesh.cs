using Starter3D.API.geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.ProceduralGeometry
{
    public class DynamicMesh : Mesh
    {
        public void ClearVertices()
        {
            _vertices.Clear();            
        }

        public void ClearFaces()
        {
            _vertices.Clear();
        }
    }
}
