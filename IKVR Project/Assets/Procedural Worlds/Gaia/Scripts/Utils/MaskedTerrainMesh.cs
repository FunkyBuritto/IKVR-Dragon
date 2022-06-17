using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;

namespace Gaia
{
    public struct MaskedMeshParamters
    {
        public UnityHeightMap Heightmap;
        public UnityHeightMap Maskmap;
        /// <summary>
        /// 0-1 below the threshold then are considered outside.
        /// </summary>
        public float MaskThreshold;
        /// <summary>
        /// terrain.terrainData.size
        /// </summary>
        public Vector3 MeshScale;
        /// <summary>
        /// //0, 1, 2, 3, 4 (full, half, quarter, eighth, sixteenth)
        /// </summary>
        public int MeshResolution; 
        /// <summary>
        /// 0 = triangles, 1 = quads
        /// </summary>
        public int MeshType;       
        /// <summary>
        /// CounterClockwise works since x in negated.
        /// </summary>
        public WindingOrder Winding;

        public enum WindingOrder
        {
            Clockwise = 1,
            CounterClockwise = 2
        }

        public MaskedMeshParamters(UnityHeightMap heightMap, UnityHeightMap maskMap, float maskThreshold, int meshResolution, Vector3 meshScale, int meshType, WindingOrder winding)
        {
            Heightmap = heightMap;
            Maskmap = maskMap;
            MaskThreshold = maskThreshold;
            MeshResolution = meshResolution;
            MeshScale = meshScale;
            MeshType = meshType;
            Winding = winding;
        }
    }

    public static class MaskedTerrainMesh
    {
        /// <summary>
        /// Creates 2 Meshes based on a mask heightmap
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="outsideMesh">The parts that are below the threshold.</param>
        /// <param name="insideMesh">The parts that are equal or above the threshold.</param>
        public static void CreateMaskedTerrainMeshes(MaskedMeshParamters parms, out MeshBuilder outsideMesh, out MeshBuilder insideMesh)
        {
            int heightmapResolution = parms.Heightmap.Width();
            outsideMesh = new MeshBuilder(1.0f / (float )heightmapResolution);
            insideMesh = new MeshBuilder(1.0f / (float)heightmapResolution);
            int tRes = 1 << parms.MeshResolution;

            Vector3 meshScale = new Vector3(parms.MeshScale.x / (heightmapResolution - 1), parms.MeshScale.y, parms.MeshScale.z / (heightmapResolution - 1));
            bool clockwise = parms.Winding == MaskedMeshParamters.WindingOrder.Clockwise;

            for (int y = 0; y < heightmapResolution - tRes; y += tRes)
            {
                float yNorm = (float )y / (float )heightmapResolution;
                float yNorm2 = (float)(y+tRes) / (float)heightmapResolution;

                for (float x = 0; x < heightmapResolution - tRes; x += tRes)
                {
                    float xNorm = (float)x / (float)heightmapResolution;
                    float xNorm2 = (float)(x+tRes) / (float)heightmapResolution;

                    // Add a quad or 2 triangles to the main or masked mesh with lower left corner given
                    
                    float h1 = parms.Heightmap[yNorm2, xNorm];
                    float h2 = parms.Heightmap[yNorm2, xNorm2];
                    float h3 = parms.Heightmap[yNorm,  xNorm];
                    float h4 = parms.Heightmap[yNorm,  xNorm2];
                    Vector3 v1 = Vector3.Scale(new Vector3(       -x, h1, y+tRes), meshScale);
                    Vector3 v2 = Vector3.Scale(new Vector3(-(x+tRes), h2, y+tRes), meshScale);
                    Vector3 v3 = Vector3.Scale(new Vector3(       -x, h3,      y), meshScale);
                    Vector3 v4 = Vector3.Scale(new Vector3(-(x+tRes), h4,      y), meshScale);
                    if (parms.Maskmap[xNorm, yNorm] < parms.MaskThreshold)
                    {
                        if (parms.MeshType == 0)
                        {
                            outsideMesh.AddTriangle(clockwise, v3, v1, v2);
                            outsideMesh.AddTriangle(clockwise, v3, v2, v4);
                        }
                        else
                        {
                            outsideMesh.AddQuad(clockwise, v1, v2, v3, v4);
                        }
                    }
                    else
                    {
                        if (parms.MeshType == 0)
                        {
                            insideMesh.AddTriangle(clockwise, v3, v1, v2);
                            insideMesh.AddTriangle(clockwise, v3, v2, v4);
                        }
                        else
                        {
                            insideMesh.AddQuad(clockwise, v1, v2, v3, v4);
                        }
                    }
                }
            }
        }
    }


    public class MeshBuilder
    {
        float m_uvScale = 1.0f;
        List<Vector3> m_vertices;
        List<Vector2> m_uvs;
        List<int> m_indices;
        Dictionary<Vector2, int> m_verticeToIndex;
        bool usingTriangles = true;

        public List<Vector3> Vertices { get { return m_vertices; } }
        public List<Vector2> UVs { get { return m_uvs; } }
        public List<int> Indices { get { return m_indices; } }

        public MeshBuilder(float uvScale)
        {
            m_uvScale = uvScale;
            m_vertices = new List<Vector3>();
            m_uvs = new List<Vector2>();
            m_indices = new List<int>();
            m_verticeToIndex = new Dictionary<Vector2, int>();
        }

        /// <summary>
        /// Add a triangle to the mesh given 3 vertices.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        public void AddTriangle(bool clockwise, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            if (clockwise)
            {
                m_indices.Add(VertexIndex(v1));
                m_indices.Add(VertexIndex(v2));
                m_indices.Add(VertexIndex(v3));
            }
            else
            {
                m_indices.Add(VertexIndex(v3));
                m_indices.Add(VertexIndex(v2));
                m_indices.Add(VertexIndex(v1));
            }
            usingTriangles = true;
        }

        public void AddQuad(bool clockwise, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            if (clockwise)
            {
                m_indices.Add(VertexIndex(v1));
                m_indices.Add(VertexIndex(v2));
                m_indices.Add(VertexIndex(v4));
                m_indices.Add(VertexIndex(v3));
            }
            else
            {
                m_indices.Add(VertexIndex(v1));
                m_indices.Add(VertexIndex(v3));
                m_indices.Add(VertexIndex(v4));
                m_indices.Add(VertexIndex(v2));
            }
            usingTriangles = false;
        }


        /// <summary>
        /// Retrieves the index of a vertex in the mesh.
        /// Adds the vertex if it does not already exist.
        /// </summary>
        /// <param name="vert">The Vector3 vertice to get the index for.</param>
        /// <returns>The index in to the m_vertices list of the vertice.</returns>
        int VertexIndex(Vector3 vert)
        {
            Vector2 v = new Vector2(vert.x, vert.z);
            if (!m_verticeToIndex.TryGetValue(v, out int vIndex))
            {
                vIndex = m_vertices.Count;
                m_verticeToIndex.Add(v, vIndex);
                m_vertices.Add(vert);
                v.x = -v.x;
                v *= m_uvScale;
                m_uvs.Add(v);
            }
            return vIndex;
        }

        public void Save(string fileName)
        {
            // Export to .obj
            StreamWriter sw = new StreamWriter(fileName);
            try
            {
                sw.WriteLine("# Unity terrain OBJ File");

                // Write vertices
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

                for (int i = 0; i < m_vertices.Count; i++)
                {
                    StringBuilder sb = new StringBuilder("v ", 64);
                    // StringBuilder stuff is done this way because it's faster than using the "{0} {1} {2}"etc. format
                    // Which is important when you're exporting huge terrains.
                    sb.Append(m_vertices[i].x.ToString()).Append(" ").
                        Append(m_vertices[i].y.ToString()).Append(" ").
                        Append(m_vertices[i].z.ToString());
                    sw.WriteLine(sb);
                }
                // Write UVs
                for (int i = 0; i < m_uvs.Count; i++)
                {
                    StringBuilder sb = new StringBuilder("vt ", 48);
                    sb.Append(m_uvs[i].x.ToString()).Append(" ").
                        Append(m_uvs[i].y.ToString());
                    sw.WriteLine(sb);
                }
                if (usingTriangles)
                {
                    // Write triangles
                    for (int i = 0; i < m_indices.Count; i += 3)
                    {
                        StringBuilder sb = new StringBuilder("f ", 43);
                        sb.Append(m_indices[i] + 1).Append("/").Append(m_indices[i] + 1).Append(" ").
                            Append(m_indices[i + 1] + 1).Append("/").Append(m_indices[i + 1] + 1).Append(" ").
                            Append(m_indices[i + 2] + 1).Append("/").Append(m_indices[i + 2] + 1);
                        sw.WriteLine(sb);
                    }
                }
                else
                {
                    // Write quads
                    for (int i = 0; i < m_indices.Count; i += 4)
                    {
                        StringBuilder sb = new StringBuilder("f ", 64);
                        sb.Append(m_indices[i] + 1).Append("/").Append(m_indices[i] + 1).Append(" ").
                            Append(m_indices[i + 1] + 1).Append("/").Append(m_indices[i + 1] + 1).Append(" ").
                            Append(m_indices[i + 2] + 1).Append("/").Append(m_indices[i + 2] + 1).Append(" ").
                            Append(m_indices[i + 3] + 1).Append("/").Append(m_indices[i + 3] + 1);
                        sw.WriteLine(sb);
                    }
                }
            }
            catch (System.Exception err)
            {
                Debug.Log("Error saving file: " + err.Message);
            }
            sw.Close();
        }
    }
}