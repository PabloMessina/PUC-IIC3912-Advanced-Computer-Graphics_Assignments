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
        public static float DT;

        protected static List<BBox_X> _bboxXList = new List<BBox_X>();
        protected static HashSet<PhysicalObjectData> _actives = new HashSet<PhysicalObjectData>();

        protected Vector3 AccelerationAt(Vector3 pos, IEnumerable<PhysicalObjectData> gravitySources)
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

        public void ClearBoundingBoxList() {
            _bboxXList.Clear();
        }

        private bool CheckBoundingBoxCollision(PhysicalObjectData obj1, PhysicalObjectData obj2)
        {
            var bbmin1 = obj1.BoundingBox_Min(Instant.Next);
            var bbmax1 = obj1.BoundingBox_Max(Instant.Next);
            var bbmin2 = obj2.BoundingBox_Min(Instant.Next);
            var bbmax2 = obj2.BoundingBox_Max(Instant.Next);

            return (bbmax1.X > bbmin2.X && bbmax1.Y > bbmin2.Y && bbmax1.Z > bbmin2.Z &&
                    bbmin1.X < bbmax2.X && bbmin1.Y < bbmax2.Y && bbmin1.Z < bbmax2.Z);
        }

        private void CheckActualCollision(PhysicalObjectData obj1, PhysicalObjectData obj2)
        {
            if (obj1 is Sphere)
            {
                Sphere sph1 = (Sphere)obj1;
                if (obj2 is Sphere)
                {
                    Sphere sph2 = (Sphere)obj2;
                    CheckActualCollision(sph1, sph2);
                }
            }
        }

        private void CheckActualCollision(Sphere s1, Sphere s2)
        {
            var x1_rel = s1.Position - s2.Position;
            var v1_rel = s1.Velocity - s2.Velocity;

            var k = (s1.Radius + s2.Radius);
            var a = v1_rel.LengthSquared;
            var b = 2 * Vector3.Dot(x1_rel, v1_rel);
            var c = x1_rel.LengthSquared - k*k;

            float t = 999999;

            if (a != 0)
            {
                var delta = b * b - 4 * a * c;
                if (delta < 0)
                    return;
                else
                {
                    delta = (float)Math.Sqrt(delta);
                    var t1 = (-b - delta) / (2 * a);
                    var t2 = (-b + delta) / (2 * a);
                    if (t1 >= 0 && t1 < DT)
                        t = t1;
                    if (t2 >= 0 && t2 < DT)
                        if(t2 < t) t = t2;
                    if (t > DT)
                        return;                   
                }
            }
            else
            {
                if (b == 0)
                    return;
                else
                {
                    t = -c / b;
                    if (t < 0 || t >= DT)
                        return;
                }
            }

            //now that I have the t of the collision, I can go back to that moment

            var cX1 = s1.Position + s1.Velocity * t;
            var cX2 = s2.Position + s2.Velocity * t;

            //normal and tangent velocities of s1
            var n1 = (cX2 - cX1).Normalized();
            var v1n = n1 * Vector3.Dot(n1, s1.Velocity);
            var v1t = s1.Velocity - v1n;

            //normal and tangent velocities of s2
            var n2 = -n1;
            var v2n = n2 * Vector3.Dot(n2, s2.Velocity);
            var v2t = s2.Velocity - v2n;

            //if circles are moving away from each other, then don't check collision
            var dist2 = (cX1 - cX2).LengthSquared;
            bool away1 = (cX1 + v1n - cX2).LengthSquared >= dist2;
            bool away2 = (cX2 + v2n - cX1).LengthSquared >= dist2;
            if (away1 && away2)
                return;

            //equation: v2f = (2m1v1 + v2(m2-m1))/(m1+m2)
            //equation: v1f = (2m2v2 + v1(m1-m2))/(m1+m2)

            var v2nf = (2 * s1.Mass * v1n + v2n * (s2.Mass - s1.Mass)) / (s1.Mass + s2.Mass);
            var v1nf = (2 * s2.Mass * v2n + v1n * (s1.Mass - s2.Mass)) / (s1.Mass + s2.Mass);

            s1.NextVelocity = v1t + v1nf;
            s2.NextVelocity = v2t + v2nf;

            s1.NextPosition = cX1 + (DT - t) * s1.NextVelocity;
            s2.NextPosition = cX2 + (DT - t) * s2.NextVelocity;
            
        }

        public void AddBoundingBoxXComponents(PhysicalObjectData obj)
        {
            _bboxXList.Add(new BBox_X(obj, true));
            _bboxXList.Add(new BBox_X(obj, false));
        }
        public void AddBoundingBoxXComponents(IEnumerable<PhysicalObjectData> objs)
        {
            foreach (var obj in objs)
            {
                _bboxXList.Add(new BBox_X(obj, true));
                _bboxXList.Add(new BBox_X(obj, false));
            }
        }

        public void SortBoundingBoxXComponents()
        {
            for (int i = 1; i < _bboxXList.Count; ++i)
            {
                var p = _bboxXList[i];                
                int j = i;
                while (j > 0 && _bboxXList[j - 1].X > p.X)
                {
                    _bboxXList[j] = _bboxXList[j - 1];
                    j--;
                }
                _bboxXList[j] = p;
            }
        }

        public void SweepBoundingBoxesForCollisions()
        {
            foreach (var bbx in _bboxXList)
            {
                if (bbx.Min)
                {
                    foreach (var activeObj in _actives)
                    {
                        //check collision with this object     
                        if (CheckBoundingBoxCollision(bbx.Obj, activeObj))
                            CheckActualCollision(bbx.Obj, activeObj);
                   
                    }
                    _actives.Add(bbx.Obj);
                }
                else
                {
                    _actives.Remove(bbx.Obj);
                }
            }
        }

        public abstract void SolveNextState(PhysicalObjectData obj, PhysicalObjectData gravitySource);
        public abstract void SolveNextState(PhysicalObjectData obj, IEnumerable<PhysicalObjectData> gravitySources);
        public abstract void SolveNextState(IEnumerable<PhysicalObjectData> objs, PhysicalObjectData gravitySource);
        public abstract void SolveNextState(IEnumerable<PhysicalObjectData> objs, IEnumerable<PhysicalObjectData> gravitySources);

        
    }

}
