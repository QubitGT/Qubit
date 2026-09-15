using System.Collections.Generic;
using UnityEngine;

namespace Seralyth.Classes.Menu
{
 public static class QubitMenuBody
    {
  private const float HalfDepth = .0125f;
       private static readonly Dictionary<Vector3, Mesh> meshes = new Dictionary<Vector3, Mesh>();
 private static Material material;

            public static void Attach(Transform art)
  {
       if (material == null)
    {
 material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(.025f,.02f,.035f,1));
  }
       var front = art.Find("Front");
 front.localPosition = new Vector3(0,0,-HalfDepth-.0003f);
            var back = art.Find("Back");
  back.localPosition = new Vector3(0,0,HalfDepth+.0003f);
       Place(back,"Header",76,0,544,295);
 Place(back,"Body",176,75,444,526);
            Place(back,"UpperTab",107,307,118,108);
  Place(back,"LowerTab",107,421,118,108);
       Place(back,"OuterTab",39,334,54,54);
 Place(back,"SmallTab",60,398,40,40);
            ((RectTransform)back.Find("Logo")).anchoredPosition = new Vector2(287,-192);
  ((RectTransform)back.Find("Version")).anchoredPosition = new Vector2(99,-267);
       Add(art,"Header",0,0,544,295,40);
 Add(art,"Body",0,75,444,526,40);
            Add(art,"NextTab",395,307,118,108,36);
  Add(art,"PreviousTab",395,421,118,108,36);
       Add(art,"OuterTab",527,334,54,54,18);
 Add(art,"PageTab",520,398,40,40,12);
    }

 private static void Place(Transform back,string name,float x,float y,float w,float h)
       {
  var rect = (RectTransform)back.Find(name);
            rect.anchoredPosition = new Vector2(x,-y);
 rect.sizeDelta = new Vector2(w,h);
       }
  private static void Add(Transform art,string name,float x,float y,float w,float h,float radius)
            {
 var size = new Vector3(w,h,radius);
       if (!meshes.TryGetValue(size,out var mesh))
  {
            mesh = Build(w*.001f,h*.001f,radius*.001f);
 meshes.Add(size,mesh);
       }
  var body = new GameObject("Qubit Body " + name,typeof(MeshFilter),typeof(MeshRenderer));
            body.transform.SetParent(art,false);
 body.transform.localPosition = new Vector3((x+w*.5f-310)*.001f,(310-y-h*.5f)*.001f,0);
       body.GetComponent<MeshFilter>().sharedMesh = mesh;
  body.GetComponent<MeshRenderer>().sharedMaterial = material;
            body.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
 body.GetComponent<MeshRenderer>().receiveShadows = false;
       }

  private static Mesh Build(float width,float height,float radius)
       {
 const int n = 36;
            var vertices = new Vector3[n*2+2];
  var triangles = new List<int>();
       var centers = new[] { new Vector2(width*.5f-radius,height*.5f-radius),new Vector2(-width*.5f+radius,height*.5f-radius),new Vector2(-width*.5f+radius,-height*.5f+radius),new Vector2(width*.5f-radius,-height*.5f+radius) };
 for(int i=0;i<n;i++)
            {
  float angle = (i/9*90+(i%9)*90f/8)*Mathf.Deg2Rad;
       Vector2 point = centers[i/9]+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
 vertices[i] = new Vector3(point.x,point.y,-HalfDepth);
            vertices[i+n] = new Vector3(point.x,point.y,HalfDepth);
  int next = (i+1)%n;
       triangles.AddRange(new[] { n*2,next,i,n*2+1,i+n,next+n,i,next,next+n,i,next+n,i+n });
 }
            vertices[n*2] = new Vector3(0,0,-HalfDepth);
  vertices[n*2+1] = new Vector3(0,0,HalfDepth);
       var mesh = new Mesh { name = "Qubit Rounded Body" };
 mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
  mesh.RecalculateNormals();mesh.RecalculateBounds();
       return mesh;
 }
    }
}
