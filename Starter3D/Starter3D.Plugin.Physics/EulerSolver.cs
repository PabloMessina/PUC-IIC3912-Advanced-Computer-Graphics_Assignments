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
        public override void SolveNextState(PhysicalObjectData obj, PhysicalObjectData gravitySource, float dt)
        {
            var x1 = obj.Position;
            var v1 = obj.Velocity;
            obj.NextPosition = x1 + v1 * dt;
            obj.NextVelocity = v1 + AccelerationAt(x1, gravitySource) * dt;
        }

        public override void SolveNextState(PhysicalObjectData obj, List<PhysicalObjectData> gravitySources, float dt)
        {
            var x1 = obj.Position;
            var v1 = obj.Velocity;
            obj.NextPosition = x1 + v1 * dt;
            obj.NextVelocity = v1 + AccelerationAt(x1, gravitySources) * dt;
        }

        public override void SolveNextState(List<PhysicalObjectData> objs, PhysicalObjectData gravitySource, float dt)
        {
            foreach (var obj in objs)
            {
                SolveNextState(obj, gravitySource, dt);
            }
        }

        public override void SolveNextState(List<PhysicalObjectData> objs, List<PhysicalObjectData> gravitySources, float dt)
        {
            foreach (var obj in objs)
            {
                SolveNextState(obj, gravitySources, dt);
            }
        }
    }
}
