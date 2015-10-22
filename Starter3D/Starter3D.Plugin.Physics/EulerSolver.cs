using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.Physics
{
    public class EulerSolver : PhysicsSolver
    {
        private EulerSolver() { }

        private static EulerSolver _instance;
        public static EulerSolver Instance
        {
            get { if (_instance == null) _instance = new EulerSolver(); return _instance; }
        }
        public override void SolveNextState(PhysicalObjectData obj, PhysicalObjectData gravitySource)
        {
            var x1 = obj.Position;
            var v1 = obj.Velocity;
            obj.NextPosition = x1 + v1 * DT;
            obj.NextVelocity = v1 + AccelerationAt(x1, gravitySource) * DT;
        }

        public override void SolveNextState(PhysicalObjectData obj, IEnumerable<PhysicalObjectData> gravitySources)
        {
            var x1 = obj.Position;
            var v1 = obj.Velocity;
            obj.NextPosition = x1 + v1 * DT;
            obj.NextVelocity = v1 + AccelerationAt(x1, gravitySources) * DT;
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
