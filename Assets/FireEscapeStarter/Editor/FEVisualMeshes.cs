using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace FireEscape
{
    // Rounded edges catch highlights; lathed profiles give bottles real silhouettes.
    internal sealed class FEVisualMeshes
    {
        private readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();
        private readonly string folder;
        public FEVisualMeshes(string destination) { folder = destination; }

        public Mesh Box(Vector3 size, float radius)
        {
            radius = Mathf.Min(radius, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .45f);
            string key = string.Format(CultureInfo.InvariantCulture, "Bevel_{0:F3}_{1:F3}_{2:F3}_{3:F3}", size.x,size.y,size.z,radius);
            if (cache.TryGetValue(key, out Mesh existing)) return existing;
            var vertices = new List<Vector3>(); var normals = new List<Vector3>();
            var uv = new List<Vector2>(); var triangles = new List<int>();
            Vector3 half = size * .5f, core = half - Vector3.one * radius;
            Vector3[] axes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 normal in axes)
            {
                Vector3 u = Mathf.Abs(normal.y) > .5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(normal, u);
                float hu = Vector3.Dot(half, Abs(u)), hv = Vector3.Dot(half, Abs(v));
                float hn = Vector3.Dot(half, Abs(normal));
                float[] us = { -hu, -hu + radius, hu - radius, hu };
                float[] vs = { -hv, -hv + radius, hv - radius, hv };
                int start = vertices.Count;
                for (int j=0;j<4;j++) for (int i=0;i<4;i++)
                {
                    Vector3 p = normal * hn + u * us[i] + v * vs[j];
                    Vector3 q = new Vector3(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));
                    Vector3 n = (p-q).normalized;
                    vertices.Add(q+n*radius); normals.Add(n);
                    uv.Add(new Vector2((us[i]+hu)*.5f,(vs[j]+hv)*.5f)); // Two-metre material repeats.
                }
                for(int j=0;j<3;j++) for(int i=0;i<3;i++)
                {
                    int a=start+j*4+i,b=a+1,c=a+4,d=c+1;
                    triangles.AddRange(new[]{a,b,d,a,d,c});
                }
            }
            return Store(key, vertices, normals, uv, triangles);
        }

        // Profile is radius/height, proceeding up the outside and down the inside.
        public Mesh Lathe(string key, Vector2[] profile, int sides=32)
        {
            if (cache.TryGetValue(key,out Mesh existing)) return existing;
            var vertices=new List<Vector3>(); var normals=new List<Vector3>();
            var uv=new List<Vector2>(); var triangles=new List<int>();
            for(int j=0;j<profile.Length;j++)
            {
                Vector2 tangent = profile[Mathf.Min(j+1,profile.Length-1)]-profile[Mathf.Max(j-1,0)];
                Vector2 n = new Vector2(tangent.y,-tangent.x).normalized;
                for(int i=0;i<=sides;i++)
                {
                    float theta=i*Mathf.PI*2/sides, c=Mathf.Cos(theta),s=Mathf.Sin(theta);
                    vertices.Add(new Vector3(c*profile[j].x,profile[j].y,s*profile[j].x));
                    normals.Add(new Vector3(c*n.x,n.y,s*n.x));uv.Add(new Vector2((float)i/sides,(float)j/(profile.Length-1)));
                }
            }
            for(int j=0;j<profile.Length-1;j++) for(int i=0;i<sides;i++)
            {
                int a=j*(sides+1)+i,b=a+sides+1;
                triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});
            }
            return Store(key,vertices,normals,uv,triangles);
        }
        private Mesh Store(string key,List<Vector3> vertices,List<Vector3> normals,List<Vector2> uv,List<int> triangles)
        {
            var mesh=new Mesh{name=key}; mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.RecalculateBounds();mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh,folder+"/"+key+".asset");cache.Add(key,mesh);return mesh;
        }
        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
    }
}
