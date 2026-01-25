using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace Voxell
{
  public static class MeshUtil
  {
    /// <summary>Creates a deep copy of a mesh.</summary>
    /// <param name="originMesh">source of mesh to copy from</param>
    /// <param name="newMesh">location of copied mesh</param>
    public static void DeepCopyMesh(in Mesh originMesh, out Mesh newMesh)
    {
      newMesh = new Mesh();
      newMesh.vertices = originMesh.vertices;
      newMesh.triangles = originMesh.triangles;
      newMesh.uv = originMesh.uv;
      newMesh.normals = originMesh.normals;
      newMesh.colors = originMesh.colors;
      newMesh.tangents = originMesh.tangents;

      int subMeshCount = originMesh.subMeshCount;
      newMesh.subMeshCount = subMeshCount;
      for (int s=0; s < subMeshCount; s++)
        newMesh.SetSubMesh(s, originMesh.GetSubMesh(s));
    }

    /// <summary>Get native array of mesh vertices.</summary>
    /// <param name="meshData">mesh data</param>
    /// <param name="allocator">allocation type</param>
    public static void NativeGetVertices(
      in Mesh.MeshData meshData,
      out NativeArray<float3> na_vertices, Allocator allocator
    )
    {
      int vertexCount = meshData.vertexCount;
      NativeArray<Vector3> na_verticesVec3 = new NativeArray<Vector3>(vertexCount, allocator);
      meshData.GetVertices(na_verticesVec3);
      na_vertices = na_verticesVec3.Reinterpret<float3>();
    }

    /// <summary>Get native array of mesh normals.</summary>
    /// <param name="meshData">mesh data</param>
    /// <param name="allocator">allocation type</param>
    public static void NativeGetNormals(
      in Mesh.MeshData meshData,
      out NativeArray<float3> na_normals, Allocator allocator)
    {
      int indexCount = meshData.vertexCount;
      NativeArray<Vector3> na_normalsVec3 = new NativeArray<Vector3>(indexCount, allocator);
      meshData.GetNormals(na_normalsVec3);
      na_normals = na_normalsVec3.Reinterpret<float3>();
    }

    /// <summary>Get native array of mesh normals.</summary>
    /// <param name="meshData">mesh data</param>
    /// <param name="allocator">allocation type</param>
    public static void NativeGetTangents(
      in Mesh.MeshData meshData,
      out NativeArray<float4> na_tangents, Allocator allocator)
    {
      int indexCount = meshData.vertexCount;
      NativeArray<Vector4> na_tangentsVec3 = new NativeArray<Vector4>(indexCount, allocator);
      meshData.GetTangents(na_tangentsVec3);
      na_tangents = na_tangentsVec3.Reinterpret<float4>();
    }

    /// <summary>Get native array of uv coordinates.</summary>
    /// <param name="meshData">mesh data</param>
    /// <param name="allocator">allocation type</param>
    public static void NativeGetUVs(
      in Mesh.MeshData meshData, int channel,
      out NativeArray<float2> na_uvs, Allocator allocator
    )
    {
      int vertexCount = meshData.vertexCount;
      NativeArray<Vector2> na_verticesVec2 = new NativeArray<Vector2>(vertexCount, allocator);
      meshData.GetUVs(channel, na_verticesVec2);
      na_uvs = na_verticesVec2.Reinterpret<float2>();
    }

    /// <summary>Get native array of vertex colors.</summary>
    /// <param name="meshData">mesh data</param>
    /// <param name="allocator">allocation type</param>
    public static void NativeGetColors(
      in Mesh.MeshData meshData,
      out NativeArray<float4> na_colors, Allocator allocator)
    {
      int vertexCount = meshData.vertexCount;
      NativeArray<Color> na_colorsVec2 = new NativeArray<Color>(vertexCount, allocator);
      na_colors = new NativeArray<float4>(vertexCount, allocator);
      meshData.GetColors(na_colorsVec2);
      na_colors = na_colorsVec2.Reinterpret<float4>();
    }

    /// <summary>Get native array of triangle indices.</summary>
    /// <param name="meshData">mesh data</param>
    /// <param name="allocator">allocation type</param>
    public static void NativeGetIndices(
      in Mesh.MeshData meshData,
      out NativeArray<int> na_triangles, Allocator allocator, int submesh=0)
    {
      int indexCount = meshData.GetSubMesh(submesh).indexCount;
      na_triangles = new NativeArray<int>(indexCount, allocator);
      meshData.GetIndices(na_triangles, submesh);
    }

    /// <summary>
    /// Reverse the triangle order of the mesh to flip the mesh
    /// </summary>
    /// <param name="mesh">mesh to be reversed</param>
    public static void ReverseTriangleOrder(ref Mesh mesh)
    {
      for (int m = 0; m < mesh.subMeshCount; m++)
      {
        int[] triangles = mesh.GetTriangles(m);
        for (int t = 0; t < triangles.Length; t += 3)
        {
          int temp = triangles[t + 0];
          triangles[t + 0] = triangles[t + 1];
          triangles[t + 1] = temp;
        }
        mesh.SetTriangles(triangles, m);
      }

      Vector3[] normals = mesh.normals;
      for (int n=0; n < normals.Length; n++) normals[n] = -normals[n];
      mesh.SetNormals(normals);
    }

    public static void SeparateTriangles(
      ref Vector3[] verts,
      ref ushort[] indices,
      ref Color[] colors,
      ref Vector2[] uvs,
      out Mesh mesh
    )
    {
      int totalTris = indices.Length/3;
      Vector3[] newVerts = new Vector3[indices.Length];
      ushort[] newIndices = new ushort[indices.Length];
      Color[] newColors = new Color[indices.Length];
      Vector2[] newUvs = new Vector2[indices.Length];

      for (int t=0; t < totalTris; t++)
      {
        int t0 = indices[t*3];
        int t1 = indices[t*3 + 1];
        int t2 = indices[t*3 + 2];

        newVerts[t*3] = verts[t0];
        newVerts[t*3 + 1] = verts[t1];
        newVerts[t*3 + 2] = verts[t2];

        newColors[t*3] = colors[t0];
        newColors[t*3 + 1] = colors[t1];
        newColors[t*3 + 2] = colors[t2];

        newUvs[t*3] = uvs[t0];
        newUvs[t*3 + 1] = uvs[t1];
        newUvs[t*3 + 2] = uvs[t2];
      }

      for (ushort i=0; i < indices.Length; i++) newIndices[i] = i;

      mesh = new Mesh();
      mesh.SetVertices(newVerts);
      mesh.SetIndices(newIndices, MeshTopology.Triangles, 0);
      mesh.SetColors(newColors);
      mesh.SetUVs(0, newUvs);
    }
  }
}