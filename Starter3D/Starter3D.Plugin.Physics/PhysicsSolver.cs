using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.Physics
{
    public abstract class PhysicsSolver
    {
        public static readonly float G = 9.81f;
        protected Vector3 AccelerationAt(Vector3 pos, List<PhysicalObjectData> gravitySources)
        {
            Vector3 acc = Vector3.Zero;
            foreach (var source in gravitySources)
            {
                var dir = (source.Position - pos);
                var r = dir.Length;
                dir.Normalize();

                acc += dir * (G * source.Mass / (r * r));
            }
            return acc; 
        }

        protected Vector3 AccelerationAt(Vector3 pos, PhysicalObjectData gravitySource)
        {
            var dir = (gravitySource.Position - pos);
            var r = dir.Length;
            dir.Normalize();
            return dir * (G * gravitySource.Mass / (r * r));
        }

        public abstract void SolveNextState(PhysicalObjectData obj, PhysicalObjectData gravitySource, float dt);
        public abstract void SolveNextState(PhysicalObjectData obj, List<PhysicalObjectData> gravitySources, float dt);
        public abstract void SolveNextState(List<PhysicalObjectData> objs, PhysicalObjectData gravitySource, float dt);
        public abstract void SolveNextState(List<PhysicalObjectData> objs, List<PhysicalObjectData> gravitySources, float dt);
    }
}
