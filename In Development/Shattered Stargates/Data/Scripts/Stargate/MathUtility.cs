using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Phoenix
{
    // Based on code from rexxar's shipyard mod
    public static class MathUtility
    {
        /// <summary>
        ///     Create an OBB that encloses a grid
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static MyOrientedBoundingBoxD CreateOrientedBoundingBox(IMyEntity entity, Vector3D? extents = null)
        {
            //quaternion to rotate the box
            Quaternion gridQuaternion = Quaternion.CreateFromForwardUp(
                entity.WorldMatrix.Forward,
                entity.WorldMatrix.Up);

            //get the halfextents of the grid, then multiply by block size to get world halfextents
            //add one so the line sits on the outside edge of the block instead of the center
            Vector3D halfExtents;

            if (extents == null)
            {
                if (entity is IMyCubeGrid)
                {
                    var castedEntity = entity as IMyCubeGrid;
                    var blocksize = castedEntity.GridSize;

                    halfExtents = new Vector3D(
                        (Math.Abs(castedEntity.Max.X - castedEntity.Min.X) + 1) * blocksize / 2,
                        (Math.Abs(castedEntity.Max.Y - castedEntity.Min.Y) + 1) * blocksize / 2,
                        (Math.Abs(castedEntity.Max.Z - castedEntity.Min.Z) + 1) * blocksize / 2);
                }
                else if (entity is IMyCubeBlock)
                {
                    var castedEntity = entity as IMyCubeBlock;
                    var blocksize = castedEntity.CubeGrid.GridSize;

                    halfExtents = new Vector3D(
                        (Math.Abs(castedEntity.Max.X - castedEntity.Min.X) + 1) * blocksize / 2,
                        (Math.Abs(castedEntity.Max.Y - castedEntity.Min.Y) + 1) * blocksize / 2,
                        (Math.Abs(castedEntity.Max.Z - castedEntity.Min.Z) + 1) * blocksize / 2);
                }
                else
                {
                    halfExtents = entity.WorldAABB.HalfExtents;
                }
            }
            else
            {
                halfExtents = extents.Value / 2;
            }
            return new MyOrientedBoundingBoxD(entity.PositionComp.WorldAABB.Center, halfExtents, gridQuaternion);
        }

        public static void DrawOrientedBoundingBox(MyOrientedBoundingBoxD obbox, Color? color = null)
        {
            var corners = new Vector3D[8];
            obbox.GetCorners(corners, 0);
            var thickness = 0.25f;
            var refcolor = color.HasValue ? color.Value.ToVector4() : Color.Orange.ToVector4();
            var beam = MyStringId.GetOrCompute("WeaponLaser");
            MySimpleObjectDraw.DrawLine(corners[0], corners[1], beam, ref refcolor, thickness);
            MySimpleObjectDraw.DrawLine(corners[1], corners[2], beam, ref refcolor, thickness);
            MySimpleObjectDraw.DrawLine(corners[2], corners[3], beam, ref refcolor, thickness);
            MySimpleObjectDraw.DrawLine(corners[3], corners[0], beam, ref refcolor, thickness);

            MySimpleObjectDraw.DrawLine(corners[0], corners[4], beam, ref refcolor, thickness);
            MySimpleObjectDraw.DrawLine(corners[1], corners[5], beam, ref refcolor, thickness);
            MySimpleObjectDraw.DrawLine(corners[3], corners[7], beam, ref refcolor, thickness);
            MySimpleObjectDraw.DrawLine(corners[2], corners[6], beam, ref refcolor, thickness);

            MySimpleObjectDraw.DrawLine(corners[4], corners[5], beam, ref refcolor, thickness);
            MySimpleObjectDraw.DrawLine(corners[5], corners[6], beam, ref refcolor, thickness);
            MySimpleObjectDraw.DrawLine(corners[6], corners[7], beam, ref refcolor, thickness);
            MySimpleObjectDraw.DrawLine(corners[7], corners[4], beam, ref refcolor, thickness);
        }

        /// <summary>
        /// Interpolates between two quaternions, using spherical linear interpolation. This goes the long way.
        /// </summary>
        /// <param name="quaternion1">Source quaternion.</param><param name="quaternion2">Source quaternion.</param><param name="amount">Value that indicates how far to interpolate between the quaternions.</param><param name="result">[OutAttribute] Result of the interpolation.</param>
        public static void MySlerp(ref Quaternion quaternion1, ref Quaternion quaternion2, float amount, out Quaternion result)
        {
            float num1 = amount;
            float num2 = (float)((double)quaternion1.X * (double)quaternion2.X + (double)quaternion1.Y * (double)quaternion2.Y + (double)quaternion1.Z * (double)quaternion2.Z + (double)quaternion1.W * (double)quaternion2.W);
            bool flag = false;
            if ((double)num2 > 0.0)
            {
                flag = true;
                num2 = -num2;
            }
            float num3;
            float num4;
            if ((double)num2 > 0.999998986721039)
            {
                num3 = 1f - num1;
                num4 = flag ? -num1 : num1;
            }
            else
            {
                float num5 = (float)Math.Acos((double)num2);
                float num6 = (float)(1.0 / Math.Sin((double)num5));
                num3 = (float)Math.Sin((1.0 - (double)num1) * (double)num5) * num6;
                num4 = flag ? (float)-Math.Sin((double)num1 * (double)num5) * num6 : (float)Math.Sin((double)num1 * (double)num5) * num6;
            }
            result.X = (float)((double)num3 * (double)quaternion1.X + (double)num4 * (double)quaternion2.X);
            result.Y = (float)((double)num3 * (double)quaternion1.Y + (double)num4 * (double)quaternion2.Y);
            result.Z = (float)((double)num3 * (double)quaternion1.Z + (double)num4 * (double)quaternion2.Z);
            result.W = (float)((double)num3 * (double)quaternion1.W + (double)num4 * (double)quaternion2.W);
        }

        /// <summary>
        /// Performs spherical linear interpolation of position and rotation. This goes the long way.
        /// </summary>
        public static void MySlerp(ref Matrix matrix1, ref Matrix matrix2, float amount, out Matrix result)
        {
            Quaternion a, b, c;
            Quaternion.CreateFromRotationMatrix(ref matrix1, out a);
            Quaternion.CreateFromRotationMatrix(ref matrix2, out b);

            MySlerp(ref a, ref b, amount, out c);
            Matrix.CreateFromQuaternion(ref c, out result);

            // Interpolate position
            result.M41 = matrix1.M41 + (matrix2.M41 - matrix1.M41) * amount;
            result.M42 = matrix1.M42 + (matrix2.M42 - matrix1.M42) * amount;
            result.M43 = matrix1.M43 + (matrix2.M43 - matrix1.M43) * amount;
        }

        /// <summary>
        /// Performs spherical linear interpolation of position and rotation.
        /// </summary>
        public static Matrix MySlerp(Matrix matrix1, Matrix matrix2, float amount)
        {
            Matrix result;
            MySlerp(ref matrix1, ref matrix2, amount, out result);
            return result;
        }

    }
}
