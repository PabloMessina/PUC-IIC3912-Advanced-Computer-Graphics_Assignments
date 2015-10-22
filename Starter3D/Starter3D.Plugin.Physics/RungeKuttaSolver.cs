using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.Physics
{
    public class RungeKuttaSolver : PhysicsSolver
    {
        private RungeKuttaSolver() { }

        private static RungeKuttaSolver _instance;
        public static RungeKuttaSolver Instance
        {
            get { if (_instance == null) _instance = new RungeKuttaSolver(); return _instance; }
        }
    
        public override void SolveNextState(PhysicalObjectData obj, PhysicalObjectData gravitySource)
        {
            var x1 = obj.Position;
            var v1 =  obj.Velocity;
            var a1 = AccelerationAt(x1,gravitySource);

            var x2 = x1 + 0.5f*v1*DT;
            var v2 = v1 + 0.5f*a1*DT;
            var a2 = AccelerationAt(x2,gravitySource);

            var x3 = x1 + 0.5f*v2*DT;
            var v3 = v1 + 0.5f*a2*DT;
            var a3 = AccelerationAt(x3,gravitySource);

            var x4 = x1 + v3*DT;
            var v4 = v1 + a3*DT;
            var a4 = AccelerationAt(x4,gravitySource);

            obj.NextPosition += (DT/6.0f)*(v1 + 2*v2 + 2*v3 + v4);
            obj.NextVelocity += (DT/6.0f)*(a1 + 2*a2 + 2*a3 + a4);
        }

        public override void SolveNextState(PhysicalObjectData obj, IEnumerable<PhysicalObjectData> gravitySources)
        {
            var x1 = obj.Position;
            var v1 = obj.Velocity;
            var a1 = AccelerationAt(x1, gravitySources);

            var x2 = x1 + 0.5f * v1 * DT;
            var v2 = v1 + 0.5f * a1 * DT;
            var a2 = AccelerationAt(x2, gravitySources);

            var x3 = x1 + 0.5f * v2 * DT;
            var v3 = v1 + 0.5f * a2 * DT;
            var a3 = AccelerationAt(x3, gravitySources);

            var x4 = x1 + v3 * DT;
            var v4 = v1 + a3 * DT;
            var a4 = AccelerationAt(x4, gravitySources);

            obj.NextPosition += (DT / 6.0f) * (v1 + 2 * v2 + 2 * v3 + v4);
            obj.NextVelocity += (DT / 6.0f) * (a1 + 2 * a2 + 2 * a3 + a4);
        }

        public override void SolveNextState(IEnumerable<PhysicalObjectData> objs, PhysicalObjectData gravitySource)
        {
            foreach (var obj in objs)
            {
                SolveNextState(obj, gravitySource);
            }
        }

        public override void SolveNextState(IEnumerable<PhysicalObjectData> objs, IEnumerable<PhysicalObjectData> gravitySources)
        {
            foreach (var obj in objs)
            {
                SolveNextState(obj, gravitySources);
            }
        }



    }
}
