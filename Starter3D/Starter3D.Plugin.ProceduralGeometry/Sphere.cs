using OpenTK;
using Starter3D.API.geometry;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.ProceduralGeometry
{
    class Sphere : ViewModelBase
    {

        private static int CurveCount = 0;

        private const float PI = (float)Math.PI;

        private float _centerX;
        private float _centerY;
        private float _radius;
        private int _meridians;
        private int _parallels;

        public float CenterX { get { return _centerX; } set { _centerX = value; RaisePropertyChanged("CenterX"); } }
        public float CenterY { get { return _centerY; } set { _centerY = value; RaisePropertyChanged("CenterY"); } }
        public float Radius { get { return _radius; } set { _radius = value; RaisePropertyChanged("Radius"); } }
        public int Meridians { get { return _meridians; } set { _meridians = value; RaisePropertyChanged("Meridians"); } }
        public int Parallels { get { return _parallels; } set { _parallels = value; RaisePropertyChanged("Parallels"); } }

        public void GenerateMesh(DynamicMesh mesh, IMaterial mat, IRenderer renderer)
        {
            mesh.ClearFaces();
            mesh.ClearVertices();

            float deltaTheta = PI / (_parallels + 1);
            float deltaPhi = 2 * PI / _meridians;

            //top vertex
            mesh.AddVertex(GetVertex(0, 0));

            //top
            int offset = 1;
            for (int i = 0; i < _meridians; ++i)
            {
                var phi = deltaPhi * i;
                mesh.AddVertex(GetVertex(deltaPhi, deltaTheta));
                int index1 = offset + i;
                int index2 = offset + (i + 1) % _meridians;
                mesh.AddFace(new Face(0, index1, index2));
            }

            //middle          
            for (int i = 2; i <= _parallels; ++i)
            {
                offset += _meridians;
                float theta = deltaTheta * i;
                for (int j = 0; j < _meridians; ++j)
                {
                    float phi = deltaPhi * j;
                    mesh.AddVertex(GetVertex(phi, theta)); 
                    
                    int index1 = offset - _meridians + j;
                    int index2 = offset - _meridians + (j + 1) % _meridians;
                    int index3 = offset + j;
                    int index4 = offset + (j + 1) % _meridians;
                    mesh.AddFace(new Face(index1, index2, index3));
                    mesh.AddFace(new Face(index2, index3, index4));
                }
            }

            //bottom
            mesh.AddVertex(GetVertex(0, PI));
            offset += _meridians;
            for (int j = 0; j < _meridians; ++j)
            {
                int index1 = offset - _meridians + j;
                int index2 = offset - _meridians + (j + 1) % _meridians;
                mesh.AddFace(new Face(index1, index2, offset));
            }

            mesh.Material = mat;
            mesh.Configure(renderer);
        }

        public void GenerateWireFrame(List<ICurve> curves, IMaterial mat, IRenderer renderer)
        {
            curves.Clear();
            float deltaTheta = PI / (_parallels + 1);
            float deltaPhi = 2 * PI / _meridians;            

            //parallels
            for (int i = 1; i <= _parallels; ++i)
            {
                var theta = deltaTheta * i;
                Curve curve = new Curve("sphere-line" + CurveCount++, 1);
                curve.Material = mat;
                for (int j = 0; j <= _meridians; ++j)
                {
                    var phi = deltaPhi * j;
                    curve.AddPoint(new Vertex { Position = GetPosition(phi, theta) });
                }
                curve.Configure(renderer);
                curves.Add(curve);                
            }

            //meridians
            var posTop = Vector3.UnitY * _radius;
            var posBottom = - Vector3.UnitY * _radius;
            for (int i = 0; i < _meridians; ++i)
            {
                var phi = deltaPhi * i;
                Curve curve = new Curve("sphere-line" + CurveCount++, 1);
                curve.Material = mat;
                curve.AddPoint(new Vertex { Position = posTop });                
                for (int j = 1; j <= _parallels; ++j)
                {
                    var theta = deltaTheta * j;
                    curve.AddPoint(new Vertex { Position = GetPosition(phi, theta) });
                }
                curve.AddPoint(new Vertex { Position = posBottom });
                curve.Configure(renderer);
                curves.Add(curve);
            }

        }

        public Vector2 GetTexCoords(float phi, float theta) {
            float u = 0.5f * phi / PI;
            float v = theta / PI;
            return new Vector2(u, v);
        }

        public Vertex GetVertex(float phi, float theta)
        {
            float z = (float)(_radius * Math.Sin(theta) * Math.Cos(phi));
            float x = (float)(_radius * Math.Sin(theta) * Math.Sin(phi));
            float y = (float)(_radius * Math.Cos(theta));

            var normal = (new Vector3(x, y, z)).Normalized();
            var pos = new Vector3(x, y, z);
            var tex = GetTexCoords(phi, theta);

            return new Vertex(pos, normal, tex);
        }

        public Vector3 GetPosition(float phi, float theta)
        {
            float z = (float)(_radius * Math.Sin(theta) * Math.Cos(phi));
            float x = (float)(_radius * Math.Sin(theta) * Math.Sin(phi));
            float y = (float)(_radius * Math.Cos(theta));
            return new Vector3(x, y, z);
        }

    }
}
