﻿namespace DefenseShields.Support
{
    using System;
    using System.Collections.Generic;
    using VRage.Game;
    using VRage.Game.Entity;
    using VRage.Utils;
    using VRageMath;
    using static VRageMath.MathHelper;

    public class Icosphere 
    {   
        public readonly Vector3[] VertexBuffer;
        public readonly int[][] IndexBuffer;

        public Icosphere(int lods)
        {
            const float X = 0.525731112119133606f;
            const float Z = 0.850650808352039932f;
            const float Y = 0;
            Vector3[] data =
            {
                new Vector3(-X, Y, Z), new Vector3(X, Y, Z), new Vector3(-X, Y, -Z), new Vector3(X, Y, -Z),
                new Vector3(Y, Z, X), new Vector3(Y, Z, -X), new Vector3(Y, -Z, X), new Vector3(Y, -Z, -X),
                new Vector3(Z, X, Y), new Vector3(-Z, X, Y), new Vector3(Z, -X, Y), new Vector3(-Z, -X, Y)
            };
            var points = new List<Vector3>(12 * (1 << (lods - 1)));
            points.AddRange(data);
            var index = new int[lods][];
            index[0] = new int[]
            {
                0, 4, 1, 0, 9, 4, 9, 5, 4, 4, 5, 8, 4, 8, 1,
                8, 10, 1, 8, 3, 10, 5, 3, 8, 5, 2, 3, 2, 7, 3, 7, 10, 3, 7,
                6, 10, 7, 11, 6, 11, 0, 6, 0, 1, 6, 6, 1, 10, 9, 0, 11, 9,
                11, 2, 9, 2, 5, 7, 2, 11
            };
            for (var i = 1; i < lods; i++)
                index[i] = Subdivide(points, index[i - 1]);

            IndexBuffer = index;
            VertexBuffer = points.ToArray();
        }
        private static int SubdividedAddress(IList<Vector3> pts, IDictionary<string, int> assoc, int a, int b)
        {
            var key = a < b ? (a.ToString() + "_" + b.ToString()) : (b.ToString() + "_" + a.ToString());
            int res;
            if (assoc.TryGetValue(key, out res))
                return res;
            var np = pts[a] + pts[b];
            np.Normalize();
            pts.Add(np);
            assoc.Add(key, pts.Count - 1);
            return pts.Count - 1;
        }

        private static int[] Subdivide(IList<Vector3> vbuffer, IReadOnlyList<int> prevLod)
        {
            var assoc = new Dictionary<string, int>();
            var res = new int[prevLod.Count * 4];
            var rI = 0;
            for (int i = 0; i < prevLod.Count; i += 3)
            {
                var v1 = prevLod[i];
                var v2 = prevLod[i + 1];
                var v3 = prevLod[i + 2];
                var v12 = SubdividedAddress(vbuffer, assoc, v1, v2);
                var v23 = SubdividedAddress(vbuffer, assoc, v2, v3);
                var v31 = SubdividedAddress(vbuffer, assoc, v3, v1);

                res[rI++] = v1;
                res[rI++] = v12;
                res[rI++] = v31;

                res[rI++] = v2;
                res[rI++] = v23;
                res[rI++] = v12;

                res[rI++] = v3;
                res[rI++] = v31;
                res[rI++] = v23;

                res[rI++] = v12;
                res[rI++] = v23;
                res[rI++] = v31;
            }

            return res;
        }

        private static long VertsForLod(int lod)
        {
            var shift = lod * 2;
            var k = (1L << shift) - 1;
            return 12 + (30 * (k & 0x5555555555555555L));
        }

        public class Instance
        {
            private const string ShieldEmissiveAlpha = "ShieldEmissiveAlpha";
            private const int SideSteps = 60;
            private const int ImpactSteps = 60;
            private const int RefreshSteps = 30;
            private const int SmallImpact = ImpactSteps / 5;
            private const float WaveMultiplier = Pi / ImpactSteps;
            private static readonly Random Random = new Random();

            private readonly Icosphere _backing;

            private readonly Vector4 _waveColor = Color.FromNonPremultiplied(0, 0, 0, 84);
            private readonly Vector4 _refreshColor = Color.FromNonPremultiplied(255, 255, 255, 255);

            private readonly Vector2 _v20 = new Vector2(.5f);
            private readonly Vector2 _v21 = new Vector2(0.25f);
            private readonly Vector2 _v22 = new Vector2(0.25f);

            private readonly int[] _impactCnt = new int[6];
            private readonly int[] _sideLoops = new int[6];

            private readonly List<int> _hitFaces = new List<int>();

            private readonly MyEntitySubpart[] _sidePartArray = { null, null, null, null, null, null };

            private readonly Vector3D[] _impactPos =
                {
                    Vector3D.NegativeInfinity, Vector3D.NegativeInfinity, Vector3D.NegativeInfinity,
                    Vector3D.NegativeInfinity, Vector3D.NegativeInfinity, Vector3D.NegativeInfinity
                };

            private readonly Vector3D[] _localImpacts =
                {
                    Vector3D.NegativeInfinity, Vector3D.NegativeInfinity, Vector3D.NegativeInfinity,
                    Vector3D.NegativeInfinity, Vector3D.NegativeInfinity, Vector3D.NegativeInfinity
                };

            private readonly MyStringId _faceCharge = MyStringId.GetOrCompute("Charge"); 
            private readonly MyStringId _faceWave = MyStringId.GetOrCompute("GlassOutside"); 

            private Vector3D[] _preCalcNormLclPos;
            private Vector3D[] _vertexBuffer;
            private Vector3D[] _physicsBuffer;

            private Vector3D[] _normalBuffer;
            private int[] _triColorBuffer;

            private Vector3D _refreshPoint;
            private MatrixD _matrix;

            private int _mainLoop = -1;
            private int _lCount;
            private int _longerLoop;
            private int _refreshDrawStep;
            private int _lod;

            private Color _activeColor = Color.Transparent;

            private bool _impact;
            private bool _refresh;
            private bool _active;
            private bool _flash;

            private MyStringId _faceMaterial;

            internal Instance(Icosphere backing)
            {
                _backing = backing;
            }

            internal bool ImpactsFinished { get; set; } = true;

            internal MyEntity ShellActive { get; set; }

            internal Vector3D ImpactPosState { get; set; }

            internal void CalculateTransform(MatrixD matrix, int lod)
            {
                _lod = lod;
                var count = checked((int)VertsForLod(lod));
                Array.Resize(ref _vertexBuffer, count);
                Array.Resize(ref _normalBuffer, count);

                var normalMatrix = MatrixD.Transpose(MatrixD.Invert(matrix.GetOrientation()));
                for (var i = 0; i < count; i++)
                    Vector3D.Transform(ref _backing.VertexBuffer[i], ref matrix, out _vertexBuffer[i]);

                for (var i = 0; i < count; i++)
                    Vector3D.TransformNormal(ref _backing.VertexBuffer[i], ref normalMatrix, out _normalBuffer[i]);

                var ib = _backing.IndexBuffer[_lod];
                Array.Resize(ref _preCalcNormLclPos, ib.Length / 3);
            }

            internal Vector3D[] CalculatePhysics(MatrixD matrix, int lod)
            {
                var count = checked((int)VertsForLod(lod));
                Array.Resize(ref _physicsBuffer, count);

                for (var i = 0; i < count; i++)
                    Vector3D.Transform(ref _backing.VertexBuffer[i], ref matrix, out _physicsBuffer[i]);

                var ib = _backing.IndexBuffer[lod];
                var vecs = new Vector3D[ib.Length];
                for (int i = 0; i < ib.Length; i += 3)
                {
                    var i0 = ib[i];
                    var i1 = ib[i + 1];
                    var i2 = ib[i + 2];
                    var v0 = _physicsBuffer[i0];
                    var v1 = _physicsBuffer[i1];
                    var v2 = _physicsBuffer[i2];

                    vecs[i] = v0;
                    vecs[i + 1] = v1;
                    vecs[i + 2] = v2;
                }
                return vecs;
            }

            internal void ReturnPhysicsVerts(MatrixD matrix, Vector3D[] physicsArray)
            {
                for (var i = 0; i < physicsArray.Length; i++)
                {
                    Vector3D tmp = _backing.VertexBuffer[i];
                    Vector3D.TransformNoProjection(ref tmp, ref matrix, out physicsArray[i]);
                }
            }

            internal void ComputeEffects(MatrixD matrix, Vector3D impactPos, MyEntity shellPassive, MyEntity shellActive, int prevLod, float shieldPercent, bool activeVisible, bool refreshAnim)
            {
                if (ShellActive == null) ComputeSides(shellActive);

                _flash = shieldPercent <= 10;
                if (_flash && _mainLoop < 30) shieldPercent += 10;

                var newActiveColor = UtilsStatic.GetShieldColorFromFloat(shieldPercent);
                _activeColor = newActiveColor;

                _matrix = matrix;
                ImpactPosState = impactPos;
                _active = activeVisible && _activeColor != Session.Instance.Color90;

                if (prevLod != _lod)
                {
                    var ib = _backing.IndexBuffer[_lod];
                    Array.Resize(ref _preCalcNormLclPos, ib.Length / 3);
                    Array.Resize(ref _triColorBuffer, ib.Length / 3);
                }

                StepEffects();

                if (refreshAnim && _refresh && ImpactsFinished && prevLod == _lod) RefreshColorAssignments(prevLod);
                if (ImpactsFinished && prevLod == _lod) return;

                ImpactColorAssignments(prevLod);
                //// vec3 localSpherePositionOfImpact;
                //    foreach (vec3 triangleCom in triangles) {
                //    var surfDistance = Math.acos(dot(triangleCom, localSpherePositionOfImpact));
                // }
                // surfDistance will be the distance, along the surface, between the impact point and the triangle
                // Equinox - It won't distort properly for anything that isn't a sphere
                // localSpherePositionOfImpact = a direction
                // triangleCom is another direction
                // Dot product is the cosine of the angle between them
                // Acos gives you that angle in radians
                // Multiplying by the sphere radius(1 for the unit sphere in question) gives the arc length.
            }

            internal void StepEffects()
            {
                _mainLoop++;
                if (_mainLoop == 60)
                {
                    _mainLoop = 0;
                    _lCount++;
                    if (_lCount == 10)
                    {
                        _lCount = 0;
                        if ((_longerLoop == 2 && Random.Next(0, 3) == 2))
                        {
                            if (ShellActive != null)
                            {
                                _refresh = true;
                                var localImpacts = ShellActive.PositionComp.LocalMatrix.Forward;
                                localImpacts.Normalize();
                                _refreshPoint = localImpacts;
                            }
                        }
                        _longerLoop++;
                        if (_longerLoop == 6) _longerLoop = 0;
                    }
                }

                if (ImpactPosState != Vector3D.NegativeInfinity) ComputeImpacts();
                else if (_flash && _mainLoop == 0 || _mainLoop == 30) for (int i = 0; i < _hitFaces.Count; i++) UpdateColor(_sidePartArray[_hitFaces[i]]);


                if (_impact)
                {
                    _impact = false;
                    if (_active) HitFace();

                    ImpactsFinished = false;
                    _refresh = false;
                    _refreshDrawStep = 0;
                }

                if (_refresh) RefreshEffect();

                if (!ImpactsFinished) UpdateImpactState();
            }
            
            internal void Draw(uint renderId)
            {
                try
                {
                    if (ImpactsFinished && !_refresh) return;
                    var ib = _backing.IndexBuffer[_lod];
                    Vector4 color;
                    if (!ImpactsFinished)
                    {
                        color = _waveColor;
                        _faceMaterial = _faceWave;
                    }
                    else
                    {
                        color = _refreshColor;
                        _faceMaterial = _faceCharge;
                    }
                    for (int i = 0, j = 0; i < ib.Length; i += 3, j++)
                    {
                        var face = _triColorBuffer[j];
                        if (face != 1 && face != 2) continue;

                        var i0 = ib[i];
                        var i1 = ib[i + 1];
                        var i2 = ib[i + 2];

                        var v0 = _vertexBuffer[i0];
                        var v1 = _vertexBuffer[i1];
                        var v2 = _vertexBuffer[i2];

                        var n0 = _normalBuffer[i0];
                        var n1 = _normalBuffer[i1];
                        var n2 = _normalBuffer[i2];

                        MyTransparentGeometry.AddTriangleBillboard(v0, v1, v2, n0, n1, n2, _v20, _v21, _v22, _faceMaterial, renderId, (v0 + v1 + v2) / 3, color);
                    }
                }
                catch (Exception ex) { Log.Line($"Exception in IcoSphere Draw - renderId {renderId.ToString()}: {ex}"); }
            }

            private static void GetIntersectingFace(MatrixD matrix, Vector3D hitPosLocal, ICollection<int> impactFaces)
            {
                var boxMax = matrix.Backward + matrix.Right + matrix.Up;
                var boxMin = -boxMax;
                var box = new BoundingBoxD(boxMin, boxMax);

                var maxWidth = box.Max.LengthSquared();
                var testLine = new LineD(Vector3D.Zero, Vector3D.Normalize(hitPosLocal) * maxWidth); //This is to ensure we intersect the box
                LineD testIntersection;
                box.Intersect(ref testLine, out testIntersection);

                var intersection = testIntersection.To;

                var projFront = VectorProjection(intersection, matrix.Forward);
                if (projFront.LengthSquared() >= 0.65 * matrix.Forward.LengthSquared()) //if within the side thickness
                    impactFaces.Add(intersection.Dot(matrix.Forward) > 0 ? 5 : 4);

                var projLeft = VectorProjection(intersection, matrix.Left);
                if (projLeft.LengthSquared() >= 0.65 * matrix.Left.LengthSquared()) //if within the side thickness
                    impactFaces.Add(intersection.Dot(matrix.Left) > 0 ? 1 : 0);

                var projUp = VectorProjection(intersection, matrix.Up);
                if (projUp.LengthSquared() >= 0.65 * matrix.Up.LengthSquared()) //if within the side thickness
                    impactFaces.Add(intersection.Dot(matrix.Up) > 0 ? 2 : 3);
            }

            private static Vector3D VectorProjection(Vector3D a, Vector3D b)
            {
                if (Vector3D.IsZero(b))
                    return Vector3D.Zero;

                return a.Dot(b) / b.LengthSquared() * b;
            }

            private void ImpactColorAssignments(int prevLod)
            {
                try
                {
                    var ib = _backing.IndexBuffer[_lod];
                    for (int i = 0, j = 0; i < ib.Length; i += 3, j++)
                    {
                        var i0 = ib[i];
                        var i1 = ib[i + 1];
                        var i2 = ib[i + 2];

                        var v0 = _vertexBuffer[i0];
                        var v1 = _vertexBuffer[i1];
                        var v2 = _vertexBuffer[i2];

                        if (prevLod != _lod)
                        {
                            var lclPos = ((v0 + v1 + v2) / 3) - _matrix.Translation;
                            var normlclPos = Vector3D.Normalize(lclPos);
                            _preCalcNormLclPos[j] = normlclPos;
                            for (int c = 0; c < _triColorBuffer.Length; c++)
                                _triColorBuffer[c] = 0;
                        }
                        if (!ImpactsFinished)
                        {
                            for (int s = 0; s < 6; s++)
                            {
                                //// basically the same as for a sphere: offset by radius, except the radius will depend on the axis
                                //// if you already have the mesh generated, it's easy to get the vector from point - origin
                                //// when you have the vector, save the magnitude as the length (radius at that point), then normalize the vector 
                                //// so it's length is 1, then multiply by length + wave offset you would need the original vertex points for each iteration

                                if (_localImpacts[s] == Vector3D.NegativeInfinity || _impactCnt[s] > SmallImpact + 1) continue;
                                var dotOfNormLclImpact = Vector3D.Dot(_preCalcNormLclPos[i / 3], _localImpacts[s]);
                                var impactFactor = (((-0.69813170079773212 * dotOfNormLclImpact * dotOfNormLclImpact) - 0.87266462599716477) * dotOfNormLclImpact) + 1.5707963267948966;
                                var wavePosition = WaveMultiplier * _impactCnt[s];
                                var relativeToWavefront = Math.Abs(impactFactor - wavePosition);
                                if (impactFactor < wavePosition && relativeToWavefront >= 0 && relativeToWavefront < 0.25)
                                {
                                    if (_impactCnt[s] != SmallImpact + 1) _triColorBuffer[j] = 1;
                                    else _triColorBuffer[j] = 0;
                                    break;
                                }

                                if ((impactFactor < wavePosition && relativeToWavefront >= -0.25 && relativeToWavefront < 0) || (relativeToWavefront > 0.25 && relativeToWavefront <= 0.5))
                                {
                                    _triColorBuffer[j] = 0;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { Log.Line($"Exception in ImpactColorAssignments {ex}"); }
            }

            private void RefreshEffect()
            {
                _refreshDrawStep++;
                if (_refreshDrawStep == RefreshSteps + 1)
                {
                    _refresh = false;
                    _refreshDrawStep = 0;
                    for (int i = 0; i < _triColorBuffer.Length; i++) _triColorBuffer[i] = 0;
                }
            }

            private void UpdateImpactState()
            {
                //Log.Line($"{_impactCnt[0]} - {_impactCnt[1]} - {_impactCnt[2]} - {_impactCnt[3]} - {_impactCnt[4]} - {_impactCnt[5]}");

                var lengthMulti = 1;
                if (_flash) lengthMulti = 3;

                for (int i = 0; i < _sideLoops.Length; i++)
                {
                    if (_sideLoops[i] != 0) _sideLoops[i]++;
                    else continue;

                    if (_sideLoops[i] >= (SideSteps * lengthMulti) + 1)
                    {
                        _sidePartArray[i].Render.UpdateRenderObject(false);
                        _sideLoops[i] = 0;
                    }
                }
                for (int i = 0; i < _impactCnt.Length; i++)
                {
                    if (_impactPos[i] != Vector3D.NegativeInfinity)
                    {
                        _impactCnt[i]++;
                    }
                    if (_impactCnt[i] >= (ImpactSteps * lengthMulti)+ 1)
                    {
                        _impactCnt[i] = 0;
                        _impactPos[i] = Vector3D.NegativeInfinity;
                        _localImpacts[i] = Vector3D.NegativeInfinity;
                    }
                }
                if (_impactCnt[0] == 0 && _impactCnt[1] == 0 && _impactCnt[2] == 0 && _impactCnt[3] == 0 && _impactCnt[4] == 0 && _impactCnt[5] == 0)
                {
                    ShellActive?.Render.UpdateRenderObject(false);
                    ImpactsFinished = true;
                    for (int i = 0; i < _triColorBuffer.Length; i++) _triColorBuffer[i] = 0;
                }
            }

            private void RefreshColorAssignments(int prevLod)
            {
                try
                {
                    var ib = _backing.IndexBuffer[_lod];
                    for (int i = 0, j = 0; i < ib.Length; i += 3, j++)
                    {
                        var i0 = ib[i];
                        var i1 = ib[i + 1];
                        var i2 = ib[i + 2];

                        var v0 = _vertexBuffer[i0];
                        var v1 = _vertexBuffer[i1];
                        var v2 = _vertexBuffer[i2];

                        if (prevLod != _lod)
                        {
                            var lclPos = ((v0 + v1 + v2) / 3) - _matrix.Translation;
                            var normlclPos = Vector3D.Normalize(lclPos);
                            _preCalcNormLclPos[j] = normlclPos;
                            for (int c = 0; c < _triColorBuffer.Length; c++)
                                _triColorBuffer[c] = 0;
                        }

                        var dotOfNormLclImpact = Vector3D.Dot(_preCalcNormLclPos[i / 3], _refreshPoint);
                        var impactFactor = (((-0.69813170079773212 * dotOfNormLclImpact * dotOfNormLclImpact) - 0.87266462599716477) * dotOfNormLclImpact) + 1.5707963267948966;
                        var waveMultiplier = Pi / RefreshSteps;
                        var wavePosition = waveMultiplier * _refreshDrawStep;
                        var relativeToWavefront = Math.Abs(impactFactor - wavePosition);
                        if (relativeToWavefront < .05) _triColorBuffer[j] = 2;
                        else _triColorBuffer[j] = 0;
                    }
                }
                catch (Exception ex) { Log.Line($"Exception in ChargeColorAssignments {ex}"); }
            }

            private void ComputeImpacts()
            {
                _impact = true;
                for (int i = 0; i < _impactPos.Length; i++)
                {
                    if (_impactPos[i] == Vector3D.NegativeInfinity)
                    {
                        _impactPos[i] = ImpactPosState;
                        _localImpacts[i] = _impactPos[i] - _matrix.Translation;
                        _localImpacts[i].Normalize();
                        break;
                    }
                }
            }

            private void HitFace()
            {
                var impactTransNorm = ImpactPosState - _matrix.Translation;
                _hitFaces.Clear();
                GetIntersectingFace(_matrix, impactTransNorm, _hitFaces);
                foreach (var face in _hitFaces)
                {
                    _sideLoops[face] = 1;
                    _sidePartArray[face].Render.UpdateRenderObject(true);
                    UpdateColor(_sidePartArray[face]);
                }
            }

            private void ComputeSides(MyEntity shellActive)
            {
                if (shellActive == null) return;
                shellActive.TryGetSubpart("ShieldLeft", out _sidePartArray[0]);
                shellActive.TryGetSubpart("ShieldRight", out _sidePartArray[1]);
                shellActive.TryGetSubpart("ShieldTop", out _sidePartArray[2]);
                shellActive.TryGetSubpart("ShieldBottom", out _sidePartArray[3]);
                shellActive.TryGetSubpart("ShieldFront", out _sidePartArray[4]);
                shellActive.TryGetSubpart("ShieldBack", out _sidePartArray[5]);
                ShellActive = shellActive;
            }

            private void UpdateColor(MyEntitySubpart shellSide)
            {
                shellSide.SetEmissiveParts(ShieldEmissiveAlpha, _activeColor, 100f);
            }
        }
    }
}
