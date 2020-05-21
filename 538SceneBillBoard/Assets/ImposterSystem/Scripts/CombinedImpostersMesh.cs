using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ImposterSystem
{

    [System.Serializable]
    internal class CombinedImpostersMesh
    {

        public List<Vector3> verts;
        public List<Vector3> normals;
        public List<int> triangles;
        public List<Vector4> uvs;
        public List<Color> colors;
        [SerializeField] List<ImposterDrawMesh> _imposters;
        [SerializeField] bool needRebuildMesh;
        [SerializeField] Mesh _mesh;

        private static int _id;

        public CombinedImpostersMesh(int maxSize)
        {
            verts = new List<Vector3>(maxSize * 4);
            normals = new List<Vector3>(maxSize * 4);
            triangles = new List<int>(maxSize * 6);
            uvs = new List<Vector4>(maxSize * 4);
            colors = new List<Color>(maxSize * 4);
            _imposters = new List<ImposterDrawMesh>(maxSize);
            _mesh = new Mesh();
            _mesh.name = "mesh for atlas " + (_id++).ToString();
            _mesh.MarkDynamic();
            needRebuildMesh = true;
        }

        public void AddImposter(ImposterDrawMesh imposter)
        {
            needRebuildMesh = true;
            _imposters.Add(imposter);
            imposter.impostersMesh = this;
            imposter.placeInMesh = _imposters.Count - 1;
            //_verts.AddRange (verts);
            verts.Add(imposter.locVertsPos[0]);
            verts.Add(imposter.locVertsPos[1]);
            verts.Add(imposter.locVertsPos[2]);
            verts.Add(imposter.locVertsPos[3]);

            int vertsCount = (_imposters.Count - 1) * 4;
            triangles.Add(vertsCount + 0);
            triangles.Add(vertsCount + 1);
            triangles.Add(vertsCount + 2);
            triangles.Add(vertsCount + 2);
            triangles.Add(vertsCount + 3);
            triangles.Add(vertsCount + 0);
            //_uv.AddRange (uv);
            uvs.Add(imposter.uv[0]);
            uvs.Add(imposter.uv[1]);
            uvs.Add(imposter.uv[2]);
            uvs.Add(imposter.uv[3]);
            Color color = imposter.GetVertexColor;
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            Vector3 normal = Vector3.zero;
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
        }

        public void UpdatePosition(int index, Color color)
        {
            if (index == -1)
            {
                Debug.LogError("Error!!!");
                return;
            }
            index *= 4;
            colors[index] = color;
            colors[index + 1] = color;
            colors[index + 2] = color;
            colors[index + 3] = color;
            needRebuildMesh = true;
        }

        public void UpdateUVs(ImposterDrawMesh imposter)
        {
            int index = imposter.placeInMesh;
            if (index == -1)
            {
                Debug.LogError("Error!!!");
                return;
            }
            index *= 4;
            for (int i = 0; i < 4; i++)
            {
                uvs[index + i] = imposter.uv[i];
            }
            needRebuildMesh = true;
        }

        internal void UpdateVertices(ImposterDrawMesh imposter)
        {
            int index = imposter.placeInMesh;
            if (index == -1)
            {
                Debug.LogError("Error!!!");
                return;
            }
            index *= 4;
            for (int i = 0; i < 4; i++)
            {
                verts[index + i] = imposter.locVertsPos[i];
            }
            needRebuildMesh = true;
        }

        public void UpdateNormals(int index, Vector3 normal)
        {
            if (index == -1)
            {
                Debug.LogError("Error!!!");
                return;
            }
            index *= 4;
            for (int i = 0; i < 4; i++)
            {
                normals[index + i] = normal;
            }
            needRebuildMesh = true;
        }

        public void RemoveImposter(int index)
        {
            if (index == -1)
            {
                //Debug.LogError("Error!!!");
                return;
            }
            needRebuildMesh = true;
            ImposterDrawMesh removed = _imposters[index];
            if (index != _imposters.Count - 1)
            {
                SetMeshFromTo(_imposters.Count - 1, index);
                _imposters[index] = _imposters[_imposters.Count - 1];
                if (_imposters[index].prevImposterMesh == this && _imposters[index].placeInPrevMesh == _imposters.Count - 1)
                {
                    _imposters[index].placeInPrevMesh = index;
                }
                else
                {
                    if (_imposters[index].impostersMesh == this && _imposters[index].placeInMesh == _imposters.Count - 1)
                        _imposters[index].placeInMesh = index;
                    else
                        Debug.LogError("Error " + index + " " + _imposters.Count);
                }
            }
            if (removed.prevImposterMesh == this && removed.placeInPrevMesh == index)
            {
                removed.placeInPrevMesh = -1;
            }
            else
            {
                if (removed.impostersMesh == this && removed.placeInMesh == index)
                    removed.placeInMesh = -1;
                else
                    Debug.LogError("Error2");
            }
            index = _imposters.Count - 1;
            _imposters.RemoveAt(index);
            verts.RemoveRange(index * 4, 4);
            normals.RemoveRange(index * 4, 4);
            triangles.RemoveRange(index * 6, 6);
            uvs.RemoveRange(index * 4, 4);
            colors.RemoveRange(index * 4, 4);
        }


        private void SetMeshFromTo(int from, int to)
        {
            int vertIndexFrom = from * 4;
            int vertIndexTo = to * 4;

            for (int i = 0; i < 4; i++)
            {
                verts[vertIndexTo + i] = verts[vertIndexFrom + i];
                normals[vertIndexTo + i] = normals[vertIndexFrom + i];
                uvs[vertIndexTo + i] = uvs[vertIndexFrom + i];
                colors[vertIndexTo + i] = colors[vertIndexFrom + i];
            }
        }

        public Mesh GetMesh(bool recalculateNormals = true)
        {
            if (needRebuildMesh)
            {
                _mesh.Clear(true);
                _mesh.SetVertices(verts);
                _mesh.SetNormals(normals);
                _mesh.SetTriangles(triangles, 0);
                _mesh.SetUVs(0, uvs);
                _mesh.SetColors(colors);
                _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
                needRebuildMesh = false;
            }
            return _mesh;

        }

        internal void Destroy()
        {
            verts.Clear();
            normals.Clear();
            triangles.Clear();
            uvs.Clear();
            colors.Clear();
            _imposters.Clear();
            _mesh.Clear();
            UnityEngine.Object.Destroy(_mesh);
        }
    }

}