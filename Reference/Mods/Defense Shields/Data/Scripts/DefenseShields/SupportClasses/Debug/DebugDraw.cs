using System.Collections.Generic;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace DefenseShields.Support
{
    public static class DsDebugDraw
    {
        #region Debug and Utils
        private static MyStringId LineId = MyStringId.GetOrCompute("Square");

        public static int GetVertNum(Vector3D[] physicsVerts, Vector3D vec)
        {
            var pmatch = false;
            var pNum = -1;
            foreach (var pvert in physicsVerts)
            {
                pNum++;
                if (vec == pvert) pmatch = true;
                if (pmatch) return pNum;
            }
            return pNum;
        }

        public static void FindRoots(Vector3D[] physicsVerts, Vector3D[] rootVerts)
        {
            for (int i = 0, j = 0; i < physicsVerts.Length; i++, j++)
            {
                var vec = physicsVerts[i];
                foreach (var magic in rootVerts)
                {
                    for (int num = 0; num < 12; num++)
                    {
                        if (vec == magic && rootVerts[num] == vec) Log.Line($"Found root {num} at index: {i}");
                    }

                }
            }
        }

        public static void SmallIntersectDebugDraw(Vector3D[] physicsOutside, int face, int[][] vertLines, int[] rangedVert, Vector3D bWorldCenter, List<Vector3D> intersections)
        {
            //DrawNums(_physicsOutside,zone, Color.AntiqueWhite);
            DsDebugDraw.DrawLineToNum(physicsOutside, rangedVert[0], bWorldCenter, Color.Red);
            DsDebugDraw.DrawLineToNum(physicsOutside, rangedVert[1], bWorldCenter, Color.Green);
            DsDebugDraw.DrawLineToNum(physicsOutside, rangedVert[2], bWorldCenter, Color.Gold);

            int[] closestLineFace;
            switch (face)
            {
                case 0:
                    closestLineFace = vertLines[rangedVert[0]];
                    break;
                case 1:
                    closestLineFace = vertLines[rangedVert[1]];
                    break;
                default:
                    closestLineFace = vertLines[rangedVert[2]];
                    break;
            }

            var c1 = Color.Black;
            var c2 = Color.Black;
            //if (checkBackupFace1) c1 = Color.Green;
            //if (checkBackupFace2) c2 = Color.Gold;
            c1 = Color.Green;
            c2 = Color.Gold;

            DsDebugDraw.DrawLineNums(physicsOutside, closestLineFace, Color.Red);
            //DrawLineNums(_physicsOutside, closestLineFace1, c1);
            //DrawLineNums(_physicsOutside, closestLineFace2, c2);

            DsDebugDraw.DrawTriVertList(intersections);

            //DrawLineToNum(_physicsOutside, rootVerts, bWorldCenter, Color.HotPink);
            //DrawLineToNum(_physicsOutside, rootVerts[1], bWorldCenter, Color.Green);
            //DrawLineToNum(_physicsOutside, rootVerts[2], bWorldCenter, Color.Gold);
        }

        public static void DrawTriNumArray(Vector3D[] physicsVerts, int[] array)
        {
            var lineId = MyStringId.GetOrCompute("Square");
            var c = Color.Red.ToVector4();

            for (int i = 0; i < array.Length; i += 3)
            {
                var vn0 = array[i];
                var vn1 = array[i + 1];
                var vn2 = array[i + 2];

                var v0 = physicsVerts[vn0];
                var v1 = physicsVerts[vn1];
                var v2 = physicsVerts[vn2];

                MySimpleObjectDraw.DrawLine(v0, v1, lineId, ref c, 0.25f);
                MySimpleObjectDraw.DrawLine(v0, v2, lineId, ref c, 0.25f);
                MySimpleObjectDraw.DrawLine(v1, v2, lineId, ref c, 0.25f);

            }
        }

        public static void DrawTriVertList(List<Vector3D> list)
        {
            var lineId = MyStringId.GetOrCompute("Square");
            var c = Color.DarkViolet.ToVector4();
            for (int i = 0; i < list.Count; i += 3)
            {
                var v0 = list[i];
                var v1 = list[i + 1];
                var v2 = list[i + 2];

                MySimpleObjectDraw.DrawLine(v0, v1, lineId, ref c, 0.25f);
                MySimpleObjectDraw.DrawLine(v0, v2, lineId, ref c, 0.25f);
                MySimpleObjectDraw.DrawLine(v1, v2, lineId, ref c, 0.25f);

            }
        }

        public static void DrawLineNums(Vector3D[] physicsVerts, int[] lineArray, Color color)
        {
            var c = color.ToVector4();
            var lineId = MyStringId.GetOrCompute("Square");

            for (int i = 0; i < lineArray.Length; i += 2)
            {
                var v0 = physicsVerts[lineArray[i]];
                var v1 = physicsVerts[lineArray[i + 1]];
                MySimpleObjectDraw.DrawLine(v0, v1, lineId, ref c, 0.25f);
            }
        }

        public static void DrawLineToNum(Vector3D[] physicsVerts, int num, Vector3D fromVec, Color color)
        {
            var c = color.ToVector4();
            var lineId = MyStringId.GetOrCompute("Square");

            var v0 = physicsVerts[num];
            var v1 = fromVec;
            MySimpleObjectDraw.DrawLine(v0, v1, lineId, ref c, 0.35f);
        }

        public static void DrawLineToVec(Vector3D fromVec, Vector3D toVec, Vector4 color, float lineWidth)
        {
            var c = color;
            var lineId = MyStringId.GetOrCompute("Square");

            var v0 = toVec;
            var v1 = fromVec;
            MySimpleObjectDraw.DrawLine(v0, v1, lineId, ref c, lineWidth);
        }

        public static void DrawX(Vector3D center, MatrixD referenceMatrix, double lineLength)
        {
            var halfLineLength = lineLength * 0.5;
            var lineWdith = (float)(lineLength * 0.1);
            var color1 = (Vector4)Color.Red;
            var color2 = (Vector4)Color.Yellow;
            var testDir0 = Vector3D.Normalize(referenceMatrix.Backward - referenceMatrix.Forward);
            var testDir1 = Vector3D.Normalize(referenceMatrix.Left - referenceMatrix.Right);
            var line0Vec0 = center + (testDir0 * -halfLineLength);
            var line0Vec1 = center + (testDir0 * halfLineLength);

            var line1Vec0 = center + (testDir1 * -halfLineLength);
            var line1Vec1 = center + (testDir1 * halfLineLength);
            MySimpleObjectDraw.DrawLine(line0Vec0, line0Vec1, LineId, ref color1, lineWdith);
            MySimpleObjectDraw.DrawLine(line1Vec0, line1Vec1, LineId, ref color2, lineWdith);
        }

        public static void DrawLosBlocked(Vector3D center, MatrixD referenceMatrix, double length)
        {
            var halfLength = length * 0.5;
            var width = (float)length * 0.05f;
            var color1 = (Vector4)Color.DarkOrange;
            var testDir0 = Vector3D.Normalize(referenceMatrix.Backward - referenceMatrix.Forward);
            var line0Vec0 = center + (testDir0 * -halfLength);
            var line0Vec1 = center + (testDir0 * halfLength);

            MySimpleObjectDraw.DrawLine(line0Vec0, line0Vec1, LineId, ref color1, width);
        }

        public static void DrawLosClear(Vector3D center, MatrixD referenceMatrix, double length)
        {
            var halfLength = length * 0.5;
            var width = (float)length * 0.05f;
            var color1 = (Vector4)Color.Green;
            var testDir0 = Vector3D.Normalize(referenceMatrix.Backward - referenceMatrix.Forward);
            var line0Vec0 = center + (testDir0 * -halfLength);
            var line0Vec1 = center + (testDir0 * halfLength);

            MySimpleObjectDraw.DrawLine(line0Vec0, line0Vec1, LineId, ref color1, width);
        }

        public static void DrawMark(Vector3D center, MatrixD referenceMatrix, int length)
        {
            var halfLength = length * 0.5;
            var width = (float)(halfLength * 0.1);

            var color1 = (Vector4)Color.Green;
            var testDir0 = Vector3D.Normalize(referenceMatrix.Backward - referenceMatrix.Forward);
            var line0Vec0 = center + (testDir0 * -halfLength);
            var line0Vec1 = center + (testDir0 * halfLength);

            MySimpleObjectDraw.DrawLine(line0Vec0, line0Vec1, LineId, ref color1, width);
        }

        public static void DrawLine(Vector3D start, Vector3D end, Vector4 color, float width)
        {
            var c = color;
            MySimpleObjectDraw.DrawLine(start, end, LineId, ref c, width);
        }

        public static void DrawSingleNum(Vector3D[] physicsVerts, int num)
        {
            //Log.Line($"magic: {magic}");
            var c = Color.Black;
            DrawScaledPoint(physicsVerts[num], 7, c, 20);
        }

        public static void DrawBox(MyOrientedBoundingBoxD obb, Color color)
        {
            var box = new BoundingBoxD(-obb.HalfExtent, obb.HalfExtent);
            var wm = MatrixD.CreateFromTransformScale(obb.Orientation, obb.Center, Vector3D.One);
            MySimpleObjectDraw.DrawTransparentBox(ref wm, ref box, ref color, MySimpleObjectRasterizer.Solid, 1);
        }

        public static void DrawBox1(BoundingBoxD box, Color color, MySimpleObjectRasterizer raster = MySimpleObjectRasterizer.Wireframe, float thickness = 0.01f)
        {
            var wm = box.Matrix;
            MySimpleObjectDraw.DrawTransparentBox(ref wm, ref box, ref color, raster, 1, thickness, MyStringId.GetOrCompute("Square"), MyStringId.GetOrCompute("Square"));
        }

        public static void DrawBox2(BoundingBoxD box, MatrixD wm, Color color, MySimpleObjectRasterizer raster = MySimpleObjectRasterizer.Wireframe, float thickness = 0.01f)
        {
            wm.Translation = box.Center;
            var lbox = box.TransformSlow(Matrix.Identity);
            MySimpleObjectDraw.DrawTransparentBox(ref wm, ref lbox, ref color, raster, 1, thickness, MyStringId.GetOrCompute("Square"), MyStringId.GetOrCompute("Square"));
        }

        public static void DrawBox3(MatrixD matrix, BoundingBoxD box, Color color, MySimpleObjectRasterizer raster = MySimpleObjectRasterizer.Wireframe, float thickness = 0.01f)
        {
            MySimpleObjectDraw.DrawTransparentBox(ref matrix, ref box, ref color, raster, 1, thickness, MyStringId.GetOrCompute("Square"), MyStringId.GetOrCompute("Square"));
        }

        public static void DrawOBB(MyOrientedBoundingBoxD obb, Color color, MySimpleObjectRasterizer raster = MySimpleObjectRasterizer.Wireframe, float thickness = 0.01f)
        {
            var box = new BoundingBoxD(-obb.HalfExtent, obb.HalfExtent);
            var wm = MatrixD.CreateFromQuaternion(obb.Orientation);
            wm.Translation = obb.Center;
            MySimpleObjectDraw.DrawTransparentBox(ref wm, ref box, ref color, MySimpleObjectRasterizer.Solid, 1);
        }

        public static void DrawSingleVec(Vector3D vec, float size, Color color)
        {
            DrawScaledPoint(vec, size, color, 20);
        }

        public static void DrawVertArray(Vector3D[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                var c = Color.Red;
                DrawScaledPoint(array[i], 1, c, 20);
                i++;
            }
        }

        public static void DrawScaledPoint(Vector3D pos, double radius, Color color, int lineWidth = 1)
        {
            var posMatCenterScaled = MatrixD.CreateTranslation(pos);
            var posMatScaler = MatrixD.Rescale(posMatCenterScaled, radius);
            var material = MyStringId.GetOrCompute("square");
            MySimpleObjectDraw.DrawTransparentSphere(ref posMatScaler, 1f, ref color, MySimpleObjectRasterizer.Solid, lineWidth, null, material, -1, -1);
        }

        public static void DrawSphere(BoundingSphereD sphere, Color color)
        {
            var rangeGridResourceId = MyStringId.GetOrCompute("Build new");
            var radius = sphere.Radius;
            var transMatrix = MatrixD.CreateTranslation(sphere.Center);
            //var wm = MatrixD.Rescale(transMatrix, radius);

            MySimpleObjectDraw.DrawTransparentSphere(ref transMatrix, (float)radius, ref color, MySimpleObjectRasterizer.Wireframe, 20, null, rangeGridResourceId, -1, -1);
        }
        #endregion
    }
}
