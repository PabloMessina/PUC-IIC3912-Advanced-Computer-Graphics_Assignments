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
    public class CatmullRomCurveHandler : CurveHandlerBase
    {

        private static Matrix4 _baseMatrix = new Matrix4(0, 1, 0, 0, -0.5f, 0, 0.5f, 0, 1, -2.5f, 2, -0.5f, -0.5f, 1.5f, -1.5f, 0.5f);
        private static CatmullRomCurveHandler _instance = null;

        private Vector4 firstPointBackup;
        private Vector4 lastPointBackup;
        private Matrix4 firstTransformBackup;
        private Matrix4 lastTransformBackup;

        private CatmullRomCurveHandler() { }

        public static CatmullRomCurveHandler GetInstance()
        {
            if (_instance == null)
                _instance = new CatmullRomCurveHandler();
            return _instance;
        }

        public override Matrix4 BaseMatrix
        {
            get { return _baseMatrix; }
        }

        public override string CannotCloseSplineFeedbackMessage
        {
            get { return "Cannot close Spline. There must be at least 5 points already."; }
        }
        public override string CannotRunAnimationFeedbackMessage {
            get { return "Cannot close Spline. There must be at least 5 points already.\nThe animation cannot start if the spline cannot be closed"; }
        }

        public override void HandleNewPoint(List<Vector4> pointList, ICurve curve, float stepSize)
        {
            if (pointList.Count < 4)
                return;
            int index = pointList.Count - 1;
            var p3 = pointList[index--];
            var p2 = pointList[index--];
            var p1 = pointList[index--];
            var p0 = pointList[index];


            if (pointList.Count == 4)
                curve.AddPoint(new Vertex(p1.Xyz, dummyNormal, dummyTextureCoord));

            var pointMatrix = new Matrix4(p0, p1, p2, p3);
            var productMatrix = _baseMatrix * pointMatrix;

            var tvector = new Vector4(1, 0, 0, 0);

            for (float t = stepSize; t < 1; t += stepSize)
            {
                tvector.X = 1;
                tvector.Y = t;
                tvector.Z = t * t;
                tvector.W = t * t * t;
                var interp = Vector4.Transform(tvector, productMatrix).Xyz;
                curve.AddPoint(new Vertex(interp, dummyNormal, dummyTextureCoord));
            }

            curve.AddPoint(new Vertex(p2.Xyz, dummyNormal, dummyTextureCoord));
        }

        public override void RefreshCurve(ICurve curve, List<Vector4> pointList, float stepSize, IRenderer renderer)
        {
            if (pointList.Count < 4)
                return;

            curve.Clear();

            //first point
            curve.AddPoint(new Vertex(pointList[1].Xyz, dummyNormal, dummyTextureCoord));

            for (int index = 3; index < pointList.Count; ++index)
            {
                var p3 = pointList[index];
                var p2 = pointList[index - 1];
                var p1 = pointList[index - 2];
                var p0 = pointList[index - 3];

                var pointMatrix = new Matrix4(p0, p1, p2, p3);
                var productMatrix = _baseMatrix * pointMatrix;

                var tvector = new Vector4(1, 0, 0, 0);

                for (float t = stepSize; t < 1; t += stepSize)
                {
                    tvector.X = 1;
                    tvector.Y = t;
                    tvector.Z = t * t;
                    tvector.W = t * t * t;
                    var interp = Vector4.Transform(tvector, productMatrix).Xyz;
                    curve.AddPoint(new Vertex(interp, dummyNormal, dummyTextureCoord));
                }

                curve.AddPoint(new Vertex(p2.Xyz, dummyNormal, dummyTextureCoord));
            }

            curve.Configure(renderer);
        }

        public override void CloseCurve(List<Vector4> pointList, List<Matrix4> pointTransforms,
            ICurve curve, float stepSize, IRenderer renderer)
        {
            int n = pointList.Count;

            firstPointBackup = pointList[0];
            firstTransformBackup = pointTransforms[0];
            lastPointBackup = pointList[n - 1];
            lastTransformBackup = pointTransforms[n - 1];

            pointList[n - 1] = pointList[1];
            pointTransforms[n - 1] = pointTransforms[1];

            pointList[0] = pointList[n - 2];
            pointTransforms[0] = pointTransforms[n - 2];

            pointTransforms.Add(pointTransforms[2]);
            pointList.Add(pointList[2]);

            RefreshCurve(curve, pointList, stepSize, renderer);
        }

        public override void OpenCurve(List<Vector4> pointList, List<Matrix4> pointTransforms,
            ICurve curve, float stepSize, IRenderer renderer)
        {
            int n = pointList.Count;

            pointTransforms.RemoveAt(n - 1);
            pointList.RemoveAt(n - 1);

            pointList[0] = firstPointBackup;
            pointTransforms[0] = firstTransformBackup;
            pointList[n - 2] = lastPointBackup;
            pointTransforms[n - 2] = lastTransformBackup;

            RefreshCurve(curve, pointList, stepSize, renderer);
        }

        public override bool CanCloseCurve(List<Vector4> pointList)
        {
            return pointList.Count > 4;
        }

        public override void GenerateRollerCoaster(
            List<Vector4> pointList,
            List<Matrix4> leftRailSegmentTransforms,
            List<Matrix4> rightRailSegmentTransforms,
            List<Matrix4> sleeperTransforms,
            float stepSize, float sleeperLength, float sleeperWidth, float sleeperHeight,
            float railWidth, float railHeight)
        {

            var p3 = pointList[2];
            var p2 = pointList[1];
            var p1 = pointList[pointList.Count - 2];
            var p0 = pointList[pointList.Count - 3];

            var tvector = new Vector4();

            Vector3
                T0 = (p2.Xyz - p0.Xyz).Normalized(),
                N0 = Vector3.Cross(T0, Up).Normalized(),
                B0 = Vector3.Cross(N0, T0).Normalized(),
                S0 = p1.Xyz,
                T1_first = T0,
                N1_first = N0,
                B1_first = B0,
                S1_first = S0,
                T1 = new Vector3(),
                N1 = new Vector3(),
                B1 = new Vector3(),
                S1 = new Vector3();

            Matrix4 pointMatrix = new Matrix4(p0, p1, p2, p3);
            Matrix4 productMatrix = _baseMatrix * pointMatrix;

            bool first = true;

            for (float t = stepSize; t < 1; t += stepSize)
            {
                tvector.X = 1;
                tvector.Y = t;
                tvector.Z = t * t;
                tvector.W = t * t * t;

                S1 = Vector4.Transform(tvector, productMatrix).Xyz;
                T1 = GetTangent(t, productMatrix);
                N1 = Vector3.Cross(B0, T1).Normalized();
                B1 = Vector3.Cross(T1, N1).Normalized();

                if (first)
                {
                    first = false;
                    S1_first = S1;
                    T1_first = T1;
                    N1_first = N1;
                    B1_first = B1;
                }
                else
                    GenerateNextRoallerCoasterSection(S0, S1, N0, N1, B0, B1,
                        sleeperLength, sleeperWidth, sleeperHeight, railWidth, railHeight,
                        rightRailSegmentTransforms,
                        leftRailSegmentTransforms,
                        sleeperTransforms);

                //update variables
                T0 = T1;
                N0 = N1;
                B0 = B1;
                S0 = S1;
            }

            S1 = p2.Xyz;
            T1 = (p3.Xyz - p1.Xyz).Normalized();
            //T1 = GetTangent(1, productMatrix);
            N1 = Vector3.Cross(B0, T1).Normalized();
            B1 = Vector3.Cross(T1, N1).Normalized();

            GenerateNextRoallerCoasterSection(S0, S1, N0, N1, B0, B1,
                   sleeperLength, sleeperWidth, sleeperHeight, railWidth, railHeight,
                   rightRailSegmentTransforms,
                   leftRailSegmentTransforms,
                   sleeperTransforms);

            T0 = T1;
            N0 = N1;
            B0 = B1;
            S0 = S1;

            Vector4 backupFirstPoint = pointList[0];
            Vector4 backupLastPoint = pointList[pointList.Count - 1];
            pointList[pointList.Count - 1] = pointList[1];
            pointList[0] = pointList[pointList.Count - 2];

            for (int index = 3; index < pointList.Count; ++index)
            {
                p3 = pointList[index];
                p2 = pointList[index - 1];
                p1 = pointList[index - 2];
                p0 = pointList[index - 3];

                pointMatrix = new Matrix4(p0, p1, p2, p3);
                productMatrix = _baseMatrix * pointMatrix;

                for (float t = stepSize; t < 1; t += stepSize)
                {
                    tvector.X = 1;
                    tvector.Y = t;
                    tvector.Z = t * t;
                    tvector.W = t * t * t;

                    S1 = Vector4.Transform(tvector, productMatrix).Xyz;
                    T1 = GetTangent(t, productMatrix);
                    N1 = Vector3.Cross(B0, T1).Normalized();
                    B1 = Vector3.Cross(T1, N1).Normalized();

                    GenerateNextRoallerCoasterSection(S0, S1, N0, N1, B0, B1,
                        sleeperLength, sleeperWidth, sleeperHeight, railWidth, railHeight,
                        rightRailSegmentTransforms,
                        leftRailSegmentTransforms,
                        sleeperTransforms);

                    //update variables
                    T0 = T1;
                    N0 = N1;
                    B0 = B1;
                    S0 = S1;
                }

                S1 = p2.Xyz;
                T1 = (p3.Xyz - p1.Xyz).Normalized();
                //T1 = GetTangent(1, productMatrix);
                N1 = Vector3.Cross(B0, T1).Normalized();
                B1 = Vector3.Cross(T1, N1).Normalized();

                GenerateNextRoallerCoasterSection(S0, S1, N0, N1, B0, B1,
                       sleeperLength, sleeperWidth, sleeperHeight, railWidth, railHeight,
                       rightRailSegmentTransforms,
                       leftRailSegmentTransforms,
                       sleeperTransforms);

                T0 = T1;
                N0 = N1;
                B0 = B1;
                S0 = S1;

            }

            GenerateNextRoallerCoasterSection(S0, S1_first, N0, N1_first, B0, B1_first,
                  sleeperLength, sleeperWidth, sleeperHeight, railWidth, railHeight,
                  rightRailSegmentTransforms,
                  leftRailSegmentTransforms,
                  sleeperTransforms);


            pointList[pointList.Count - 1] = backupLastPoint;
            pointList[0] = backupFirstPoint;


        }

        public override List<AnimationPoint> GetAnimationPointList( List<Vector4> pointList, float stepSize)
        {
            List<AnimationPoint> animationPoints = new List<AnimationPoint>();
            AnimationPoint animPoint;

            var p3 = pointList[2];
            var p2 = pointList[1];
            var p1 = pointList[pointList.Count - 2];
            var p0 = pointList[pointList.Count - 3];

            var tvector = new Vector4();

            Vector3
                T0 = (p2.Xyz - p0.Xyz).Normalized(),
                N0 = Vector3.Cross(T0, Up).Normalized(),
                B0 = Vector3.Cross(N0, T0).Normalized(),
                S0 = p1.Xyz,
                T1 = new Vector3(),
                N1 = new Vector3(),
                B1 = new Vector3(),
                S1 = new Vector3();

            Matrix4 pointMatrix = new Matrix4(p0, p1, p2, p3);
            Matrix4 productMatrix = _baseMatrix * pointMatrix;

            for (float t = stepSize; t < 1; t += stepSize)
            {
                tvector.X = 1;
                tvector.Y = t;
                tvector.Z = t * t;
                tvector.W = t * t * t;

                S1 = Vector4.Transform(tvector, productMatrix).Xyz;
                T1 = GetTangent(t, productMatrix);
                N1 = Vector3.Cross(B0, T1).Normalized();
                B1 = Vector3.Cross(T1, N1).Normalized();

                animPoint.Position = S1;
                animPoint.Direction = T1;
                animPoint.Up = B1;
                animationPoints.Add(animPoint);               

                //update variables
                T0 = T1;
                N0 = N1;
                B0 = B1;
                S0 = S1;
            }

            S1 = p2.Xyz;
            T1 = (p3.Xyz - p1.Xyz).Normalized();
            N1 = Vector3.Cross(B0, T1).Normalized();
            B1 = Vector3.Cross(T1, N1).Normalized();

            animPoint.Position = S1;
            animPoint.Direction = T1;
            animPoint.Up = B1;
            animationPoints.Add(animPoint);

            T0 = T1;
            N0 = N1;
            B0 = B1;
            S0 = S1;

            Vector4 backupFirstPoint = pointList[0];
            Vector4 backupLastPoint = pointList[pointList.Count - 1];
            pointList[pointList.Count - 1] = pointList[1];
            pointList[0] = pointList[pointList.Count - 2];

            for (int index = 3; index < pointList.Count; ++index)
            {
                p3 = pointList[index];
                p2 = pointList[index - 1];
                p1 = pointList[index - 2];
                p0 = pointList[index - 3];

                pointMatrix = new Matrix4(p0, p1, p2, p3);
                productMatrix = _baseMatrix * pointMatrix;

                for (float t = stepSize; t < 1; t += stepSize)
                {
                    tvector.X = 1;
                    tvector.Y = t;
                    tvector.Z = t * t;
                    tvector.W = t * t * t;

                    S1 = Vector4.Transform(tvector, productMatrix).Xyz;
                    T1 = GetTangent(t, productMatrix);
                    N1 = Vector3.Cross(B0, T1).Normalized();
                    B1 = Vector3.Cross(T1, N1).Normalized();

                    animPoint.Position = S1;
                    animPoint.Direction = T1;
                    animPoint.Up = B1;
                    animationPoints.Add(animPoint);

                    //update variables
                    T0 = T1;
                    N0 = N1;
                    B0 = B1;
                    S0 = S1;
                }

                S1 = p2.Xyz;
                T1 = (p3.Xyz - p1.Xyz).Normalized();
                N1 = Vector3.Cross(B0, T1).Normalized();
                B1 = Vector3.Cross(T1, N1).Normalized();

                animPoint.Position = S1;
                animPoint.Direction = T1;
                animPoint.Up = B1;
                animationPoints.Add(animPoint);

                T0 = T1;
                N0 = N1;
                B0 = B1;
                S0 = S1;

            }

            return animationPoints;

        }
    }


}
