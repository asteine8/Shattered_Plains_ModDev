using VRage.Game.ObjectBuilders.Definitions.SessionComponents;

namespace DefenseShields.Support
{
    using System;
    using System.Collections.Generic;
    using Sandbox.Game.Entities;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.Game.ModAPI;
    using VRage.Voxels;
    using VRageMath;

    internal class CustomCollision
    {
        public static bool FutureIntersect(DefenseShields ds, MyEntity ent, MatrixD detectMatrix, MatrixD detectMatrixInv)
        {
            var entVel = ent.Physics.LinearVelocity;
            var entCenter = ent.PositionComp.WorldVolume.Center;
            var velStepSize = entVel * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * 1;
            var futureCenter = entCenter + velStepSize;
            var testDir = Vector3D.Normalize(entCenter - futureCenter);
            var ellipsoid = IntersectEllipsoid(ds.DetectMatrixOutsideInv, ds.DetectionMatrix, new RayD(entCenter, -testDir));
            var intersect = ellipsoid == null && PointInShield(entCenter, detectMatrixInv) || ellipsoid <= velStepSize.Length();
            return intersect;
        }

        public static Vector3D PastCenter(DefenseShields ds, MyEntity ent, MatrixD detectMatrix, MatrixD detectMatrixInv, int steps)
        {
            var entVel = -ent.Physics.LinearVelocity;
            var entCenter = ent.PositionComp.WorldVolume.Center;
            var velStepSize = entVel * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * steps;
            var pastCenter = entCenter + velStepSize;
            return pastCenter;
        }

        /*
        public static Vector3D? MissileIntersect(DefenseShields ds, MyEntity missile, MatrixD detectMatrix, MatrixD detectMatrixInv)
        {
            var missileVel = missile.Physics.LinearVelocity;
            var velStepSize = missileVel * (MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * 2);
            var missileCenter = missile.PositionComp.WorldVolume.Center;
            var inflatedSphere = new BoundingSphereD(missileCenter, velStepSize.Length());
            var wDir = detectMatrix.Translation - inflatedSphere.Center;
            var wLen = wDir.Length();
            var wTest = inflatedSphere.Center + wDir / wLen * Math.Min(wLen, inflatedSphere.Radius);
            var intersect = Vector3D.Transform(wTest, detectMatrixInv).LengthSquared() <= 1;
            Vector3D? hitPos = null;

            if (intersect)
            {
                const float gameSecond = MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * 60;
                var line = new LineD(missileCenter + -missileVel * gameSecond, missileCenter + missileVel * gameSecond);
                var obbIntersect = ds.SOriBBoxD.Intersects(ref line);
                if (obbIntersect.HasValue)
                {
                    var testDir = line.From - line.To;
                    testDir.Normalize();
                    hitPos = line.From + testDir * -obbIntersect.Value;
                }
            }
            return hitPos;
        }
        */

        public static bool MissileNoIntersect(MyEntity missile, MatrixD detectMatrix, MatrixD detectMatrixInv, IMySlimBlock block)
        {
            var missileVel = missile.Physics.LinearVelocity;
            var missileCenter = missile.PositionComp.WorldVolume.Center;
            var leaving = Vector3D.Transform(missileCenter + (-missileVel * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * 2), detectMatrixInv).LengthSquared() <= 1;
            return leaving;
        }

        public static float? IntersectEllipsoid(MatrixD ellipsoidMatrixInv, MatrixD ellipsoidMatrix, RayD ray)
        {
            var normSphere = new BoundingSphereD(Vector3.Zero, 1f);
            var kRay = new RayD(Vector3D.Zero, Vector3D.Forward);

            var krayPos = Vector3D.Transform(ray.Position, ellipsoidMatrixInv);
            var krayDir = Vector3D.Normalize(Vector3D.TransformNormal(ray.Direction, ellipsoidMatrixInv));

            kRay.Direction = krayDir;
            kRay.Position = krayPos;
            var nullDist = normSphere.Intersects(kRay);
            if (!nullDist.HasValue) return null;

            var hitPos = krayPos + (krayDir * -nullDist.Value);
            var worldHitPos = Vector3D.Transform(hitPos, ellipsoidMatrix);
            return Vector3.Distance(worldHitPos, ray.Position);
        }


        public static Vector3D ClosestObbPointToPos(MyOrientedBoundingBoxD obb, Vector3D point)
        {
            var center = obb.Center;
            var directionVector = point - center;
            var halfExtents = obb.HalfExtent;
            var m = MatrixD.CreateFromQuaternion(obb.Orientation);
            m.Translation = obb.Center;
            var xAxis = m.GetDirectionVector(Base6Directions.Direction.Right);
            var yAxis = m.GetDirectionVector(Base6Directions.Direction.Up);
            var zAxis = m.GetDirectionVector(Base6Directions.Direction.Forward);

            var distanceX = Vector3D.Dot(directionVector, xAxis);
            if (distanceX > halfExtents.X) distanceX = halfExtents.X;
            else if (distanceX < -halfExtents.X) distanceX = -halfExtents.X;

            var distanceY = Vector3D.Dot(directionVector, yAxis);
            if (distanceY > halfExtents.Y) distanceY = halfExtents.Y;
            else if (distanceY < -halfExtents.Y) distanceY = -halfExtents.Y;

            var distanceZ = Vector3D.Dot(directionVector, zAxis);
            if (distanceZ > halfExtents.Z) distanceZ = halfExtents.Z;
            else if (distanceZ < -halfExtents.Z) distanceZ = -halfExtents.Z;

            return center + distanceX * xAxis + distanceY * yAxis + distanceZ * zAxis;
        }

        public static Vector3D ClosestEllipsoidPointToPos(MatrixD ellipsoidMatrixInv, MatrixD ellipsoidMatrix, Vector3D point)
        {
            var ePos = Vector3D.Transform(point, ellipsoidMatrixInv);
            var closestLPos = Vector3D.Normalize(ePos);
            var closestWPos = Vector3D.Transform(closestLPos, ellipsoidMatrix);

            return closestWPos;
        }

        public static double EllipsoidDistanceToPos(MatrixD ellipsoidMatrixInv, MatrixD ellipsoidMatrix, Vector3D point)
        {
            var ePos = Vector3D.Transform(point, ellipsoidMatrixInv);
            var closestLPos = Vector3D.Normalize(ePos);
            var closestWPos = Vector3D.Transform(closestLPos, ellipsoidMatrix);

            var distToPoint = Vector3D.Distance(closestWPos, point);
            if (ePos.LengthSquared() < 1) distToPoint *= -1;

            return distToPoint;
        }

        public static void ClosestPointPlanePoint(ref PlaneD plane, ref Vector3D point, out Vector3D result)
        {
            double result1;
            Vector3D.Dot(ref plane.Normal, ref point, out result1);
            double num = result1 - plane.D;
            result = point - num * plane.Normal;
        }

        public static bool RayIntersectsTriangle(Vector3D rayOrigin, Vector3D rayVector, Vector3D v0, Vector3D v1, Vector3D v2, Vector3D outIntersectionPoint)
        {
            const double Epsilon = 0.0000001;
            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var h = rayVector.Cross(edge2);
            var a = edge1.Dot(h);
            if (a > -Epsilon && a < Epsilon) return false;

            var f = 1 / a;
            var s = rayOrigin - v0;
            var u = f * s.Dot(h);
            if (u < 0.0 || u > 1.0) return false;

            var q = s.Cross(edge1);
            var v = f * rayVector.Dot(q);
            if (v < 0.0 || u + v > 1.0) return false;
            
            var t = f * edge2.Dot(q);
            if (t > Epsilon) 
            {
                // outIntersectionPoint = rayOrigin + rayVector * t;
                return true;
            }
            return false;
        }

        public static void ShieldX2PointsInside(Vector3D[] shield1Verts, MatrixD shield1MatrixInv, Vector3D[] shield2Verts, MatrixD shield2MatrixInv, List<Vector3D> insidePoints)
        {
            for (int i = 0; i < 642; i++) if (Vector3D.Transform(shield1Verts[i], shield2MatrixInv).LengthSquared() <= 1) insidePoints.Add(shield1Verts[i]); 
            for (int i = 0; i < 642; i++) if (Vector3D.Transform(shield2Verts[i], shield1MatrixInv).LengthSquared() <= 1) insidePoints.Add(shield2Verts[i]);
        }

        public static void ClientShieldX2PointsInside(Vector3D[] shield1Verts, MatrixD shield1MatrixInv, Vector3D[] shield2Verts, MatrixD shield2MatrixInv, List<Vector3D> insidePoints)
        {
            for (int i = 0; i < 162; i++) if (Vector3D.Transform(shield1Verts[i], shield2MatrixInv).LengthSquared() <= 1) insidePoints.Add(shield1Verts[i]);
            for (int i = 0; i < 162; i++) if (Vector3D.Transform(shield2Verts[i], shield1MatrixInv).LengthSquared() <= 1) insidePoints.Add(shield2Verts[i]);
        }

        public static bool VoxelContact(Vector3D[] physicsVerts, MyVoxelBase voxelBase)
        {
            try
            {
                if (voxelBase.RootVoxel.MarkedForClose || voxelBase.RootVoxel.Storage.Closed) return false;
                var planet = voxelBase as MyPlanet;
                var map = voxelBase as MyVoxelMap;

                if (planet != null)
                {
                    for (int i = 0; i < 162; i++)
                    {
                        var from = physicsVerts[i];
                        var localPosition = (Vector3)(from - planet.PositionLeftBottomCorner);
                        var v = localPosition / 1f;
                        Vector3I voxelCoord;
                        Vector3I.Floor(ref v, out voxelCoord);

                        var hit = new VoxelHit();
                        planet.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);

                        if (hit.HasHit) return true;
                    }
                }
                else if (map != null)
                {
                    for (int i = 0; i < 162; i++)
                    {
                        var from = physicsVerts[i];
                        var localPosition = (Vector3)(from - map.PositionLeftBottomCorner);
                        var v = localPosition / 1f;
                        Vector3I voxelCoord;
                        Vector3I.Floor(ref v, out voxelCoord);

                        var hit = new VoxelHit();
                        map.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);

                        if (hit.HasHit) return true;
                    }
                }
            }
            catch (Exception ex) { Log.Line($"Exception in VoxelContact: {ex}"); }

            return false;
        }

        public static Vector3D? VoxelEllipsoidCheck(IMyCubeGrid shieldGrid, Vector3D[] physicsVerts, MyVoxelBase voxelBase)
        {
            var collisionAvg = Vector3D.Zero;
            try
            {
                if (voxelBase.RootVoxel.MarkedForClose || voxelBase.RootVoxel.Storage.Closed) return null;
                var planet = voxelBase as MyPlanet;
                var map = voxelBase as MyVoxelMap;

                var collision = Vector3D.Zero;
                var collisionCnt = 0;
                
                if (planet != null)
                {
                    for (int i = 0; i < 162; i++)
                    {
                        var from = physicsVerts[i];
                        var localPosition = (Vector3)(from - planet.PositionLeftBottomCorner);
                        var v = localPosition / 1f;
                        Vector3I voxelCoord;
                        Vector3I.Floor(ref v, out voxelCoord);

                        var hit = new VoxelHit();
                        planet.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);

                        if (hit.HasHit)
                        {
                            collision += from;
                            collisionCnt++;
                        }
                    }
                }
                else if (map != null)
                {
                    for (int i = 0; i < 162; i++)
                    {
                        var from = physicsVerts[i];
                        var localPosition = (Vector3)(from - map.PositionLeftBottomCorner);
                        var v = localPosition / 1f;
                        Vector3I voxelCoord;
                        Vector3I.Floor(ref v, out voxelCoord);

                        var hit = new VoxelHit();
                        map.Storage.ExecuteOperationFast(ref hit, MyStorageDataTypeFlags.Content, ref voxelCoord, ref voxelCoord, notifyRangeChanged: false);

                        if (hit.HasHit)
                        {
                            collision += from;
                            collisionCnt++;
                        }
                    }
                }
                if (collisionCnt == 0) return null;
                collisionAvg = collision / collisionCnt;
            }
            catch (Exception ex) { Log.Line($"Exception in VoxelCollisionSphere: {ex}"); }

            return collisionAvg;
        }

        public static MyVoxelBase AabbInsideVoxel(MatrixD worldMatrix, BoundingBoxD localAabb)
        {
            BoundingBoxD box = localAabb.TransformFast(ref worldMatrix);
            List<MyVoxelBase> result = new List<MyVoxelBase>();
            MyGamePruningStructure.GetAllVoxelMapsInBox(ref box, result);
            foreach (MyVoxelBase voxelMap in result)
            {
                if (voxelMap.IsAnyAabbCornerInside(ref worldMatrix, localAabb)) return voxelMap;
            }
            return null;
        }

        public static Vector3D? BlockIntersect(IMySlimBlock block, bool cubeExists, Quaternion bQuaternion, MatrixD matrix, MatrixD matrixInv, ref Vector3D[] blockPoints, bool debug = false)
        {
            BoundingBoxD blockBox;
            Vector3D center;
            if (cubeExists)
            {
                blockBox = block.FatBlock.LocalAABB;
                center = block.FatBlock.WorldAABB.Center;
            }
            else
            {
                Vector3 halfExt;
                block.ComputeScaledHalfExtents(out halfExt);
                blockBox = new BoundingBoxD(-halfExt, halfExt);
                block.ComputeWorldCenter(out center);
            }
            
            // 4 + 5 + 6 + 7 = Front
            // 0 + 1 + 2 + 3 = Back
            // 1 + 2 + 5 + 6 = Top
            // 0 + 3 + 4 + 7 = Bottom
            new MyOrientedBoundingBoxD(center, blockBox.HalfExtents, bQuaternion).GetCorners(blockPoints, 0);
            blockPoints[8] = center;
            var point0 = blockPoints[0];
            if (Vector3.Transform(point0, matrixInv).LengthSquared() <= 1) return point0;
            var point1 = blockPoints[1];
            if (Vector3.Transform(point1, matrixInv).LengthSquared() <= 1) return point1;
            var point2 = blockPoints[2];
            if (Vector3.Transform(point2, matrixInv).LengthSquared() <= 1) return point2;
            var point3 = blockPoints[3];
            if (Vector3.Transform(point3, matrixInv).LengthSquared() <= 1) return point3;
            var point4 = blockPoints[4];
            if (Vector3.Transform(point4, matrixInv).LengthSquared() <= 1) return point4;
            var point5 = blockPoints[5];
            if (Vector3.Transform(point5, matrixInv).LengthSquared() <= 1) return point5;
            var point6 = blockPoints[6];
            if (Vector3.Transform(point6, matrixInv).LengthSquared() <= 1) return point6;
            var point7 = blockPoints[7];
            if (Vector3.Transform(point7, matrixInv).LengthSquared() <= 1) return point7;
            var point8 = blockPoints[8];
            if (Vector3.Transform(point8, matrixInv).LengthSquared() <= 1) return point8;

            var blockSize = (float)blockBox.HalfExtents.AbsMax() * 2;
            var testDir = Vector3D.Normalize(point0 - point1);
            var ray = new RayD(point0, -testDir);
            var intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point1) >= Vector3D.DistanceSquared(point1, point))
                {
                    //Log.Line($"ray0: {intersect} - {Vector3D.Distance(point1, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point0 - point3);
            ray = new RayD(point0, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point3) >= Vector3D.DistanceSquared(point3, point))
                {
                    //Log.Line($"ray1: {intersect} - {Vector3D.Distance(point3, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point0 - point4);
            ray = new RayD(point0, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point4) >= Vector3D.DistanceSquared(point4, point))
                {
                    //Log.Line($"ray2: {intersect} - {Vector3D.Distance(point4, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point1 - point2);
            ray = new RayD(point1, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point1, point2) >= Vector3D.DistanceSquared(point2, point))
                {
                    //Log.Line($"ray3: {intersect} - {Vector3D.Distance(point2, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point1 - point5);
            ray = new RayD(point1, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point1, point5) >= Vector3D.DistanceSquared(point5, point))
                {
                    //Log.Line($"ray4: {intersect} - {Vector3D.Distance(point5, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point2 - point3);
            ray = new RayD(point2, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point2, point3) >= Vector3D.DistanceSquared(point3, point))
                {
                    //Log.Line($"ray5: {intersect} - {Vector3D.Distance(point3, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point2 - point6);
            ray = new RayD(point2, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point2, point6) >= Vector3D.DistanceSquared(point6, point))
                {
                    //Log.Line($"ray6: {intersect} - {Vector3D.Distance(point6, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point3 - point7);
            ray = new RayD(point3, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point3, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray7: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point4 - point5);
            ray = new RayD(point4, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point4, point5) >= Vector3D.DistanceSquared(point5, point))
                {
                    //Log.Line($"ray8: {intersect} - {Vector3D.Distance(point5, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point4 - point7);
            ray = new RayD(point4, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point4, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray9: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point5 - point6);
            ray = new RayD(point5, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point5, point6) >= Vector3D.DistanceSquared(point6, point))
                {
                    //Log.Line($"ray10: {intersect} - {Vector3D.Distance(point6, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point6 - point7);
            ray = new RayD(point6, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point6, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray11: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }
            return null;
        }

        public static bool Intersecting(IMyCubeGrid breaching, IMyCubeGrid shield, Vector3D[] physicsVerts, Vector3D breachingPos)
        {
            var shieldPos = ClosestVertArray(physicsVerts, breachingPos);
            var gridVel = breaching.Physics.LinearVelocity;
            var gridCenter = breaching.PositionComp.WorldVolume.Center;
            var shieldVel = shield.Physics.LinearVelocity;
            var shieldCenter = shield.PositionComp.WorldVolume.Center;
            var gApproching = Vector3.Dot(gridVel, gridCenter - shieldPos) < 0;
            var sApproching = Vector3.Dot(shieldVel, shieldCenter - breachingPos) < 0;
            return gApproching || sApproching;
        }

        public static Vector3D ContactPointOutside(MyEntity breaching, MatrixD matrix)
        {
            var wVol = breaching.PositionComp.WorldVolume;
            var wDir = matrix.Translation - wVol.Center;
            var wLen = wDir.Length();
            var contactPoint = wVol.Center + (wDir / wLen * Math.Min(wLen, wVol.Radius));
            return contactPoint;
        }

        public static bool SphereTouchOutside(MyEntity breaching, MatrixD matrix, MatrixD detectMatrixInv)
        {
            var wVol = breaching.PositionComp.WorldVolume;
            var wDir = matrix.Translation - wVol.Center;
            var wLen = wDir.Length();
            var closestPointOnSphere = wVol.Center + (wDir / wLen * Math.Min(wLen, wVol.Radius + 1));

            var intersect = Vector3D.Transform(closestPointOnSphere, detectMatrixInv).LengthSquared() <= 1;
            return intersect;
        }

        public static bool PointInShield(Vector3D entCenter, MatrixD matrixInv)
        {
            return Vector3D.Transform(entCenter, matrixInv).LengthSquared() <= 1;
        }

        public static void ClosestCornerInShield(Vector3D[] gridCorners, MatrixD matrixInv, ref Vector3D cloestPoint)
        {
            var minValue1 = double.MaxValue;

            for (int i = 0; i < 8; i++)
            {
                var point = gridCorners[i];
                var pointInside = Vector3D.Transform(point, matrixInv).LengthSquared();
                if (!(pointInside <= 1) || !(pointInside < minValue1)) continue;
                minValue1 = pointInside;
                cloestPoint = point;
            }
        }

        public static int CornerOrCenterInShield(MyEntity ent, MatrixD matrixInv, Vector3D[] corners, bool firstMatch = false)
        {
            var c = 0;
            if (Vector3D.Transform(ent.PositionComp.WorldAABB.Center, matrixInv).LengthSquared() <= 1) c++;
            if (firstMatch && c > 0) return c;

            ent.PositionComp.WorldAABB.GetCorners(corners);
            for (int i = 0; i < 8; i++)
            {
                if (Vector3D.Transform(corners[i], matrixInv).LengthSquared() <= 1) c++;
                if (firstMatch && c > 0) return c;
            }
            return c;
        }

        public static int EntCornersInShield(MyEntity ent, MatrixD matrixInv, Vector3D[] entCorners)
        {
            var entAabb = ent.PositionComp.WorldAABB;
            entAabb.GetCorners(entCorners);

            var c = 0;
            for (int i = 0; i < 8; i++)
            {
                var pointInside = Vector3D.Transform(entCorners[i], matrixInv).LengthSquared() <= 2;
                if (pointInside) c++;
            }
            return c;
        }

        public static int NotAllCornersInShield(MyCubeGrid grid, MatrixD matrixInv, Vector3D[] gridCorners)
        {
            var gridAabb = grid.PositionComp.WorldAABB;
            gridAabb.GetCorners(gridCorners);

            var c = 0;
            for (int i = 0; i < 8; i++)
            {
                var pointInside = Vector3D.Transform(gridCorners[i], matrixInv).LengthSquared() <= 1;
                if (pointInside) c++;
                else if (c != 0) break;
            }
            return c;
        }

        public static bool AllAabbInShield(BoundingBoxD gridAabb, MatrixD matrixInv, Vector3D[] gridCorners = null)
        {
            if (gridCorners == null) gridCorners = new Vector3D[8];

            gridAabb.GetCorners(gridCorners);
            var c = 0;
            for (int i = 0; i < 8; i++)
                if (Vector3D.Transform(gridCorners[i], matrixInv).LengthSquared() <= 1) c++;
            return c == 8;
        }

        public static bool ObbCornersInShield(MyOrientedBoundingBoxD bOriBBoxD, MatrixD matrixInv, Vector3D[] gridCorners, bool anyCorner = false)
        {
            bOriBBoxD.GetCorners(gridCorners, 0);
            var c = 0;
            for (int i = 0; i < 8; i++)
            {
                if (Vector3D.Transform(gridCorners[i], matrixInv).LengthSquared() <= 1)
                {
                    if (anyCorner) return true;
                    c++;
                }
            }
            return c == 8;
        }

        public static int NewObbPointsInShield(MyEntity ent, MatrixD matrixInv, Vector3D[] gridPoints = null)
        {
            if (gridPoints == null) gridPoints = new Vector3D[9];

            var quaternion = Quaternion.CreateFromRotationMatrix(ent.WorldMatrix);
            var halfExtents = ent.PositionComp.LocalAABB.HalfExtents;
            var gridCenter = ent.PositionComp.WorldAABB.Center;
            var obb = new MyOrientedBoundingBoxD(gridCenter, halfExtents, quaternion);

            obb.GetCorners(gridPoints, 0);
            gridPoints[8] = obb.Center;
            var c = 0;
            for (int i = 0; i < 9; i++)
                if (Vector3D.Transform(gridPoints[i], matrixInv).LengthSquared() <= 1) c++;
            return c;
        }

        public static int NewObbCornersInShield(MyEntity ent, MatrixD matrixInv, Vector3D[] gridCorners = null)
        {
            if (gridCorners == null) gridCorners = new Vector3D[8];

            var quaternion = Quaternion.CreateFromRotationMatrix(ent.WorldMatrix);
            var halfExtents = ent.PositionComp.LocalAABB.HalfExtents;
            var gridCenter = ent.PositionComp.WorldAABB.Center;
            var obb = new MyOrientedBoundingBoxD(gridCenter, halfExtents, quaternion);

            obb.GetCorners(gridCorners, 0);
            var c = 0;
            for (int i = 0; i < 8; i++)
                if (Vector3D.Transform(gridCorners[i], matrixInv).LengthSquared() <= 1) c++;
            return c;
        }

        public static BoundingSphereD NewObbClosestTriCorners(MyEntity ent, Vector3D pos)
        {
            var entCorners = new Vector3D[8];

            var quaternion = Quaternion.CreateFromRotationMatrix(ent.PositionComp.GetOrientation());
            var halfExtents = ent.PositionComp.LocalAABB.HalfExtents;
            var gridCenter = ent.PositionComp.WorldAABB.Center;
            var obb = new MyOrientedBoundingBoxD(gridCenter, halfExtents, quaternion);

            var minValue = double.MaxValue;
            var minValue0 = double.MaxValue;
            var minValue1 = double.MaxValue;
            var minValue2 = double.MaxValue;
            var minValue3 = double.MaxValue;

            var minNum = -2;
            var minNum0 = -2;
            var minNum1 = -2;
            var minNum2 = -2;
            var minNum3 = -2;

            obb.GetCorners(entCorners, 0);
            for (int i = 0; i < entCorners.Length; i++)
            {
                var gridCorner = entCorners[i];
                var range = gridCorner - pos;
                var test = (range.X * range.X) + (range.Y * range.Y) + (range.Z * range.Z);
                if (test < minValue3)
                {
                    if (test < minValue)
                    {
                        minValue3 = minValue2;
                        minNum3 = minNum2;
                        minValue2 = minValue1;
                        minNum2 = minNum1;
                        minValue1 = minValue0;
                        minNum1 = minNum0;
                        minValue0 = minValue;
                        minNum0 = minNum;
                        minValue = test;
                        minNum = i;
                    }
                    else if (test < minValue0)
                    {
                        minValue3 = minValue2;
                        minNum3 = minNum2;
                        minValue2 = minValue1;
                        minNum2 = minNum1;
                        minValue1 = minValue0;
                        minNum1 = minNum0;
                        minValue0 = test;
                        minNum0 = i;
                    }
                    else if (test < minValue1)
                    {
                        minValue3 = minValue2;
                        minNum3 = minNum2;
                        minValue2 = minValue1;
                        minNum2 = minNum1;
                        minValue1 = test;
                        minNum1 = i;
                    }
                    else if (test < minValue2)
                    {
                        minValue3 = minValue2;
                        minNum3 = minNum2;
                        minValue2 = test;
                        minNum2 = i;
                    }
                    else
                    {
                        minValue3 = test;
                        minNum3 = i;
                    }
                }
            }
            var corner = entCorners[minNum];
            var corner0 = entCorners[minNum0];
            var corner1 = entCorners[minNum1];
            var corner2 = entCorners[minNum2];
            var corner3 = gridCenter;
            Vector3D[] closestCorners = { corner, corner0, corner3};

            var sphere = BoundingSphereD.CreateFromPoints(closestCorners);
            //var subObb = MyOrientedBoundingBoxD.CreateFromBoundingBox(box);
            return sphere;
        }

        public static bool NewAllObbCornersInShield(MyEntity ent, MatrixD matrixInv, bool anyCorner, Vector3D[] gridCorners = null)
        {
            if (gridCorners == null) gridCorners = new Vector3D[8];

            var quaternion = Quaternion.CreateFromRotationMatrix(ent.WorldMatrix);
            var halfExtents = ent.PositionComp.LocalAABB.HalfExtents;
            var gridCenter = ent.PositionComp.WorldAABB.Center;
            var obb = new MyOrientedBoundingBoxD(gridCenter, halfExtents, quaternion);

            obb.GetCorners(gridCorners, 0);
            var c = 0;
            for (int i = 0; i < 8; i++)
            {
                if (Vector3D.Transform(gridCorners[i], matrixInv).LengthSquared() <= 1)
                {
                    if (anyCorner) return true;
                    c++;
                }
            }
            return c == 8;
        }

        public static void IntersectSmallBox(int[] closestFace, Vector3D[] physicsVerts, BoundingBoxD bWorldAabb, List<Vector3D> intersections)
        {
            for (int i = 0; i < closestFace.Length; i += 3)
            {
                var v0 = physicsVerts[closestFace[i]];
                var v1 = physicsVerts[closestFace[i + 1]];
                var v2 = physicsVerts[closestFace[i + 2]];
                var test1 = bWorldAabb.IntersectsTriangle(v0, v1, v2);
                if (!test1) continue;
                intersections.Add(v0); 
                intersections.Add(v1);
                intersections.Add(v2);
            }
        }

        public static Vector3D ClosestVertArray(Vector3D[] physicsVerts, Vector3D pos, int limit = -1)
        {
            if (limit == -1) limit = physicsVerts.Length;
            var minValue1 = double.MaxValue;
            var closestVert = Vector3D.NegativeInfinity;
            for (int p = 0; p < limit; p++)
            {
                var vert = physicsVerts[p];
                var range = vert - pos;
                var test = (range.X * range.X) + (range.Y * range.Y) + (range.Z * range.Z);
                if (test < minValue1)
                {
                    minValue1 = test;
                    closestVert = vert;
                }
            }
            return closestVert;
        }

        public static Vector3D ClosestVertList(List<Vector3D> physicsVerts, Vector3D pos, int limit = -1)
        {
            if (limit == -1) limit = physicsVerts.Count;
            var minValue1 = double.MaxValue;
            var closestVert = Vector3D.NegativeInfinity;
            for (int p = 0; p < limit; p++)
            {
                var vert = physicsVerts[p];
                var range = vert - pos;
                var test = (range.X * range.X) + (range.Y * range.Y) + (range.Z * range.Z);
                if (test < minValue1)
                {
                    minValue1 = test;
                    closestVert = vert;
                }
            }
            return closestVert;
        }

        public static int ClosestVertNum(Vector3D[] physicsVerts, Vector3D pos)
        {
            var minValue1 = double.MaxValue;
            var closestVertNum = int.MaxValue;

            for (int p = 0; p < physicsVerts.Length; p++)
            {
                var vert = physicsVerts[p];
                var range = vert - pos;
                var test = (range.X * range.X) + (range.Y * range.Y) + (range.Z * range.Z);
                if (test < minValue1)
                {
                    minValue1 = test;
                    closestVertNum = p;
                }
            }
            return closestVertNum;
        }

        public static int GetClosestTri(Vector3D[] physicsOutside, Vector3D pos)
        {
            var triDist1 = double.MaxValue;
            var triNum = 0;

            for (int i = 0; i < physicsOutside.Length; i += 3)
            {
                var ov0 = physicsOutside[i];
                var ov1 = physicsOutside[i + 1];
                var ov2 = physicsOutside[i + 2];
                var otri = new Triangle3d(ov0, ov1, ov2);
                var odistTri = new DistPoint3Triangle3(pos, otri);
                odistTri.Update(pos, otri);

                var test = odistTri.GetSquared();
                if (test < triDist1)
                {
                    triDist1 = test;
                    triNum = i;
                }
            }
            return triNum;
        }

        public static Vector3D? ObbIntersect(MyOrientedBoundingBoxD obb, MatrixD matrix, MatrixD matrixInv)
        {
            var corners = new Vector3D[9];
            // 4 + 5 + 6 + 7 = Front
            // 0 + 1 + 2 + 3 = Back
            // 1 + 2 + 5 + 6 = Top
            // 0 + 3 + 4 + 7 = Bottom
            obb.GetCorners(corners, 0);
            corners[8] = obb.Center;
            var point0 = corners[0];
            if (Vector3.Transform(point0, matrixInv).LengthSquared() <= 1) return point0;
            var point1 = corners[1];
            if (Vector3.Transform(point1, matrixInv).LengthSquared() <= 1) return point1;
            var point2 = corners[2];
            if (Vector3.Transform(point2, matrixInv).LengthSquared() <= 1) return point2;
            var point3 = corners[3];
            if (Vector3.Transform(point3, matrixInv).LengthSquared() <= 1) return point3;
            var point4 = corners[4];
            if (Vector3.Transform(point4, matrixInv).LengthSquared() <= 1) return point4;
            var point5 = corners[5];
            if (Vector3.Transform(point5, matrixInv).LengthSquared() <= 1) return point5;
            var point6 = corners[6];
            if (Vector3.Transform(point6, matrixInv).LengthSquared() <= 1) return point6;
            var point7 = corners[7];
            if (Vector3.Transform(point7, matrixInv).LengthSquared() <= 1) return point7;
            var point8 = corners[8];
            if (Vector3.Transform(point8, matrixInv).LengthSquared() <= 1) return point8;

            var blockSize = (float)obb.HalfExtent.AbsMax() * 2;
            var testDir = Vector3D.Normalize(point0 - point1);
            var ray = new RayD(point0, -testDir);
            var intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point1) >= Vector3D.DistanceSquared(point1, point))
                {
                    //Log.Line($"ray0: {intersect} - {Vector3D.Distance(point1, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point0 - point3);
            ray = new RayD(point0, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point3) >= Vector3D.DistanceSquared(point3, point))
                {
                    //Log.Line($"ray1: {intersect} - {Vector3D.Distance(point3, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point0 - point4);
            ray = new RayD(point0, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point0, point4) >= Vector3D.DistanceSquared(point4, point))
                {
                    //Log.Line($"ray2: {intersect} - {Vector3D.Distance(point4, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point1 - point2);
            ray = new RayD(point1, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point1, point2) >= Vector3D.DistanceSquared(point2, point))
                {
                    //Log.Line($"ray3: {intersect} - {Vector3D.Distance(point2, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point1 - point5);
            ray = new RayD(point1, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point1, point5) >= Vector3D.DistanceSquared(point5, point))
                {
                    //Log.Line($"ray4: {intersect} - {Vector3D.Distance(point5, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point2 - point3);
            ray = new RayD(point2, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point2, point3) >= Vector3D.DistanceSquared(point3, point))
                {
                    //Log.Line($"ray5: {intersect} - {Vector3D.Distance(point3, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point2 - point6);
            ray = new RayD(point2, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point2, point6) >= Vector3D.DistanceSquared(point6, point))
                {
                    //Log.Line($"ray6: {intersect} - {Vector3D.Distance(point6, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point3 - point7);
            ray = new RayD(point3, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point3, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray7: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point4 - point5);
            ray = new RayD(point4, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point4, point5) >= Vector3D.DistanceSquared(point5, point))
                {
                    //Log.Line($"ray8: {intersect} - {Vector3D.Distance(point5, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point4 - point7);
            ray = new RayD(point4, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point4, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray9: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point5 - point6);
            ray = new RayD(point5, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point5, point6) >= Vector3D.DistanceSquared(point6, point))
                {
                    //Log.Line($"ray10: {intersect} - {Vector3D.Distance(point6, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }

            testDir = Vector3D.Normalize(point6 - point7);
            ray = new RayD(point6, -testDir);
            intersect = IntersectEllipsoid(matrixInv, matrix, ray);
            if (intersect != null)
            {
                var point = ray.Position + (testDir * (float)-intersect);
                if (intersect <= blockSize && Vector3D.DistanceSquared(point6, point7) >= Vector3D.DistanceSquared(point7, point))
                {
                    //Log.Line($"ray11: {intersect} - {Vector3D.Distance(point7, point)} - {Vector3D.Distance(point, center)}");
                    return point;
                }
            }
            return null;
        }
    }
}
