using OpenTK;
using Starter3D.API.geometry;
using Starter3D.API.geometry.primitives;
using Starter3D.API.renderer;
using Starter3D.API.resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.RollerCoasterEditor
{
    public abstract class CurveHandlerBase
    {
        protected static Vector3 dummyNormal = new Vector3();
        protected static Vector3 dummyTextureCoord = new Vector3();
        protected static Vector3 Up = new Vector3(0, 1, 0);

        public abstract Matrix4 BaseMatrix { get; }
        public abstract string CannotCloseSplineFeedbackMessage { get; }
        public abstract string CannotRunAnimationFeedbackMessage { get; }

        public CurveHandlerBase()
        {
        }        


        protected Matrix4 ComputeCubeTransform(Vector3 eye, Vector3 target, Vector3 up, float width, float height)
        {
            var length = (target - eye).Length;
            var inverse_viewMatrix = Matrix4.LookAt(eye, target, up).Inverted();
            var scaleMatrix = Matrix4.CreateScale(width, height, -length);
            return scaleMatrix * inverse_viewMatrix;
        }

        protected void GenerateNextRoallerCoasterSection(Vector3 S0, Vector3 S1, Vector3 N0, Vector3 N1, Vector3 B0, Vector3 B1,
            float sleeperLength, float sleeperWidth, float sleeperHeight, float railWidth, float railHeight,
            List<Matrix4> rightRailSegmentTransforms,
            List<Matrix4> leftRailSegmentTransforms,
            List<Matrix4> sleeperTransforms)
        {
            //=========================
            //create right rail segment
            //=========================                 
            var eye = S0 + N0 * sleeperLength * 0.5f;
            var target = S1 + N1 * sleeperLength * 0.5f;
            var up = (B1 + B0) * 0.5f;
            var transform = ComputeCubeTransform(eye, target, up, railWidth, railHeight);
            rightRailSegmentTransforms.Add(transform);

            //=========================
            //create left rail segment
            //=========================
            eye = S0 - N0 * sleeperLength * 0.5f;
            target = S1 - N1 * sleeperLength * 0.5f;
            transform = ComputeCubeTransform(eye, target, up, railWidth, railHeight);
            leftRailSegmentTransforms.Add(transform);


            //================
            //create sleeper
            //================
            eye = S1 - N1 * sleeperLength * 0.5f;
            target = S1 + N1 * sleeperLength * 0.5f;
            up = B1;
            transform = ComputeCubeTransform(eye, target, up, sleeperWidth, sleeperHeight);
            sleeperTransforms.Add(transform);
        }

        protected Vector3 GetTangent(float t, Matrix4 transform)
        {
            float t1 = t - 0.01f;
            float t2 = t + 0.01f;
            var v1 = new Vector4(1, t1, t1 * t1, t1 * t1 * t1);
            var v2 = new Vector4(1, t2, t2 * t2, t2 * t2 * t2);
            return (Vector4.Transform(v2, transform).Xyz -
                Vector4.Transform(v1, transform).Xyz).Normalized();
        }

        public abstract List<AnimationPoint> GetAnimationPointList(List<Vector4> pointList, float stepSize);
        public abstract void HandleNewPoint(List<Vector4> pointList, ICurve curve, float stepSize);
        public abstract void RefreshCurve(ICurve curve, List<Vector4> pointList, float stepSize, IRenderer renderer);
        public abstract void CloseCurve(List<Vector4> pointList, List<Matrix4> pointTransforms,
            ICurve curve, float stepSize, IRenderer renderer);
        public abstract void OpenCurve(List<Vector4> pointList, List<Matrix4> pointTransforms,
            ICurve curve, float stepSize, IRenderer renderer);
        public abstract bool CanCloseCurve(List<Vector4> pointList);

        public abstract void GenerateRollerCoaster(
            List<Vector4> pointList,
            List<Matrix4> leftRailSegmentTransforms,
            List<Matrix4> rightRailSegmentTransforms,
            List<Matrix4> sleeperTransforms,
            float stepSize, float sleeperLength, float sleeperWidth, float sleeperHeight,
            float railWidth, float railHeight);

    }
}
