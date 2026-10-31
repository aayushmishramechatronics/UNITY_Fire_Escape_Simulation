using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace FireEscape
{
 public static partial class FEUniversityBuilder
 {
  const string Assets="Assets/FireEscapeStarter/UniversityAssets/";
  [Serializable] class Area {public string id,label,kind;public int x0,x1,z0,z1;public float height;}
  [Serializable] class DoorSpec {public string id,label,axis,mode;public float x,z,yaw;}
  [Serializable] class WindowSpec {public string axis;public float x,z;}
  [Serializable] class Point {public float x,z;}
  [Serializable] class Route {public string label;public Point[] points;}
  [Serializable] class Obstacle {public string name;public float x,y,z,sx,sy,sz;}
  [Serializable] class FireSpec {public float x,y,z,sx,sz,at;}
  [Serializable] class Layout {public Area[] areas;public string[] expansionIds;public DoorSpec[] doors;public WindowSpec[] windows;public Route[] routes;public Obstacle[] obstacles;public FireSpec[] newFires;}
  static Transform root,expansion,architecture,details;
  static FEEmergency emergency;
  static FEAtmosphere atmosphere;
  static FEUniversityExperience experience;
  static Layout layout;
  static string output;
  static FEVisualMeshes meshes;
  static Dictionary<string,Material> materials=new Dictionary<string,Material>();
  static List<FEUniversityExperience.Stage> stages=new List<FEUniversityExperience.Stage>();
  static List<AudioSource> roomSounds=new List<AudioSource>();

  [MenuItem("Fire Escape/Apply Phase 5 - Expanded University")]
  public static void Apply()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Stop Play mode first.");return;}
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   var department=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Department_Interior_V3");
   atmosphere=department==null?null:department.GetComponentInChildren<FEAtmosphere>(true);
   if(atmosphere==null || !atmosphere.blackoutMode || string.IsNullOrEmpty(scene.path))
   {Tell("Open the working scene with the Fire and Smoke Overhaul applied first.");return;}
   if(department.GetComponentInChildren<FEUniversityExperience>(true)!=null){Tell("Phase 5 is already applied to this scene.");return;}
   emergency=atmosphere.emergency;
   if(emergency==null || emergency.player==null || emergency.hazardAreas.Length!=10)
   {Tell("Expected the ten hazards from the latest fire/smoke overhaul. Send the FEEmergency Inspector before applying to a customised scene.");return;}
   foreach(string file in new[]{"UniversityLayout.json","Plan_Lab.png","Plan_Atrium.png","Plan_North.png","Plan_Debrief.png","DirectionArrow.png","ElectricalHiss.wav","AlarmLoop.wav"})
    if(AssetDatabase.LoadMainAssetAtPath(Assets+file)==null){Tell("Missing update file: "+file+". Copy the full FireEscapeStarter folder.");return;}
   if(Shader.Find("FireEscape/SurfaceDecal")==null || ShaderUtil.ShaderHasError(Shader.Find("FireEscape/SurfaceDecal")))
   {Tell("The surface decal shader has not imported correctly. Check Console.");return;}
   layout=JsonUtility.FromJson<Layout>(AssetDatabase.LoadAssetAtPath<TextAsset>(Assets+"UniversityLayout.json").text);
   ValidateLayout();
   Folder("Assets/_Project/Backups");string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Backups/Before_Expanded_University.unity");
   if(!EditorSceneManager.SaveScene(scene,backup,true)){Tell("Could not save the scene backup. No changes made.");return;}
   Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Expand university");
   Undo.RegisterFullObjectHierarchyUndo(department,"Expand department");Undo.RegisterFullObjectHierarchyUndo(emergency.player.gameObject,"Update presentation");
   try
   {
    root=department.transform;output=AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/ExpandedUniversity");Folder(output);Folder(output+"/Meshes");
    meshes=new FEVisualMeshes(output+"/Meshes");materials.Clear();stages.Clear();roomSounds.Clear();ImportAssets();MakeMaterials();
    expansion=New("Expanded_University_Phase5",root);architecture=New("Architecture",expansion);details=New("University_Details",expansion);
    experience=Undo.AddComponent<FEUniversityExperience>(expansion.gameObject);experience.emergency=emergency;experience.player=emergency.player;experience.backupScene=backup;
    experience.debriefPlan=Tex("Plan_Debrief.png");
    RemoveOldCourtyard();BuildShell();BuildNewFurniture();UpgradeExistingArchitecture();
    FixDecals();ReplaceLabels();BuildGuidance();UpdateMaps();Lighting();EmergencySequence();
    BuildObstacles();BatchDetails();
    experience.stages=stages.ToArray();experience.roomSounds=roomSounds.ToArray();
    experience.spatialSounds=atmosphere.fireAudio.Concat(roomSounds).Where(x=>x!=null).ToArray();
    emergency.assemblyArea=new Bounds(new Vector3(77,1,40),new Vector3(9,3,10));emergency.universityPresentation=true;
    atmosphere.expandedUniversity=true;atmosphere.smokeHaze=.85f;
    AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
    Selection.activeGameObject=expansion.gameObject;
    Tell("Expanded university applied. Ctrl+S, then Play.\n\nThe final assembly point is now beyond the atrium and service wing. All wall plans have been updated.\n\nBackup: "+backup);
   }
   catch(Exception ex){Undo.RevertAllDownToGroup(undo);Debug.LogException(ex);Tell("Update stopped. Open the saved backup to restore the complete scene:\n"+backup+"\nSend the first red Console error.");}
  }
  static void ValidateLayout()
  {
   var cells=new HashSet<Vector2Int>();
   foreach(var a in layout.areas)for(int x=a.x0/2;x<a.x1/2;x++)for(int z=a.z0/2;z<a.z1/2;z++)
    if(!cells.Add(new Vector2Int(x,z)))throw new InvalidOperationException("Overlapping layout cells: "+a.id);
  }
  static void ImportAssets()
  {
   foreach(string f in Directory.GetFiles(Assets,"*.png"))
   {
    string path=f.Replace('\\','/');var t=AssetImporter.GetAtPath(path) as TextureImporter;if(t==null)continue;
    t.textureType=TextureImporterType.Default;t.sRGBTexture=true;t.alphaSource=TextureImporterAlphaSource.FromInput;
    t.alphaIsTransparency=Path.GetFileName(f)=="FloorScuffs.png";t.maxTextureSize=2048;t.wrapMode=TextureWrapMode.Clamp;
    t.mipmapEnabled=!Path.GetFileName(f).StartsWith("Plan_");t.anisoLevel=4;t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
   }
  }
  static Material Mat(string name,Color color,float smooth=.25f)
  {
   var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="U5_"+name,enableInstancing=true};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);
   AssetDatabase.CreateAsset(m,output+"/"+name+".mat");materials[name]=m;return m;
  }
  static void MakeMaterials()
  {
   var all=root.GetComponentsInChildren<Renderer>(true).SelectMany(x=>x.sharedMaterials).Where(x=>x!=null).Distinct().ToArray();
   foreach(string n in new[]{"Wall","Floor","Ceiling","Oak","Steel","Blue","White","Black","Concrete","Counter","Glass","Char"})
   {
    var old=all.FirstOrDefault(x=>x.name=="P4_"+n);
    var m=old!=null?new Material(old):new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name="U5_"+n;
    if(old==null)m.SetColor("_BaseColor",n=="Black"?new Color(.04f,.05f,.06f):new Color(.45f,.47f,.46f));
    AssetDatabase.CreateAsset(m,output+"/"+n+".mat");materials[n]=m;
   }
   Mat("Trim",new Color(.12f,.16f,.18f),.25f);Mat("Red",new Color(.58f,.025f,.01f));Mat("Yellow",new Color(.72f,.49f,.035f));
   var glow=Mat("GreenGuide",Color.white,.18f);glow.SetTexture("_BaseMap",Tex("DirectionArrow.png"));glow.EnableKeyword("_EMISSION");glow.SetTexture("_EmissionMap",Tex("DirectionArrow.png"));glow.SetColor("_EmissionColor",new Color(.55f,.8f,.6f));
   var soot=new Material(Shader.Find("FireEscape/SurfaceDecal")){name="U5_Soot"};soot.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FireEscapeStarter/VisualAssets/Soot.png"));AssetDatabase.CreateAsset(soot,output+"/Soot.mat");materials["Soot"]=soot;
   var scuffs=new Material(soot){name="U5_Scuffs"};scuffs.SetTexture("_MainTex",Tex("FloorScuffs.png"));AssetDatabase.CreateAsset(scuffs,output+"/Scuffs.mat");materials["Scuffs"]=scuffs;
   materials["Floor"].SetFloat("_Smoothness",.42f);
  }
  static void RemoveOldCourtyard()
  {
   foreach(var t in root.GetComponentsInChildren<Transform>(true).ToArray())
   {
    Vector3 p=t.position;
    bool oldFloor=t.name.StartsWith("Floor_") && p.x>30 && p.x<40 && p.z>34 && p.z<46;
    bool obsolete=t.name=="Courtyard_Parapet" || t.name=="Assembly_Sign_Post" || t.name=="Sign_ASSEMBLY POINT" || t.name=="Sign_EXIT 3 - ASSEMBLY";
    bool oldBench=t.name.StartsWith("Bench_") && p.x>30 && p.x<40 && p.z>34 && p.z<46;
    if(oldFloor || obsolete || oldBench)t.gameObject.SetActive(false);
   }
  }
  static void BuildShell()
  {
   var cells=new Dictionary<Vector2Int,Area>();var newIds=new HashSet<string>(layout.expansionIds);var built=new HashSet<string>();
   foreach(var a in layout.areas)for(int x=a.x0/2;x<a.x1/2;x++)for(int z=a.z0/2;z<a.z1/2;z++)cells[new Vector2Int(x,z)]=a;
   foreach(var cell in cells)
   {
    Area a=cell.Value;if(!newIds.Contains(a.id))continue;float x=cell.Key.x*2+1,z=cell.Key.y*2+1;
    B("Floor",new Vector3(x,-.1f,z),new Vector3(2,.2f,2),a.kind=="outdoor"?"Concrete":"Floor",architecture,true);
    if(a.kind!="outdoor")B("Ceiling",new Vector3(x,a.height+.1f,z),new Vector3(2,.2f,2),"Ceiling",architecture,true);
    foreach(var dir in new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down})
    {
     cells.TryGetValue(cell.Key+dir,out Area other);
     if(other!=null && (a.id==other.id || a.kind!="room" && other.kind!="room"))
     {
      // Close the high part of a transition between atrium and lower wing ceilings.
      if(a.height>other.height && other.height>0)
       WallSegment(x+dir.x,z+dir.y,dir.x==0?"z":"x",2,a.height-other.height,other.height+(a.height-other.height)/2,"Wall",architecture,true);
      continue;
     }
     if(other!=null && !newIds.Contains(other.id))continue; // Existing records-office wall already occupies this boundary.
     float wx=x+dir.x,wz=z+dir.y;string axis=dir.x==0?"z":"x",key=axis+wx+"_"+wz;if(!built.Add(key))continue;
     bool door=layout.doors.Any(d=>d.axis==axis && Mathf.Abs(d.x-wx)<.01f && Mathf.Abs(d.z-wz)<.01f);
     bool window=layout.windows.Any(w=>w.axis==axis && Mathf.Abs(w.x-wx)<.01f && Mathf.Abs(w.z-wz)<.01f);
     if(door){WallSegment(wx,wz,axis,2,a.height-2.7f,2.7f+(a.height-2.7f)/2,"Wall",architecture,true);continue;}
     if(a.kind=="outdoor"){WallSegment(wx,wz,axis,2,.75f,.375f,"Concrete",architecture,true);continue;}
     if(window)
     {
      WallSegment(wx,wz,axis,2,1,.5f,"Wall",architecture,true);WallSegment(wx,wz,axis,2,.45f,3.075f,"Wall",architecture,true);
      WallSegment(wx,wz,axis,2,1.85f,1.925f,"Glass",architecture,true);
      WallSegment(wx,wz,axis,.06f,1.85f,1.925f,"Steel",details);
      if(a.height>3.3f)WallSegment(wx,wz,axis,2,a.height-3.3f,3.3f+(a.height-3.3f)/2,"Wall",architecture,true);
     }
     else WallSegment(wx,wz,axis,2,a.height,a.height/2,"Wall",architecture,true);
     WallSegment(wx,wz,axis,2,.16f,.08f,"Trim",details);
    }
   }
  }
  static void WallSegment(float x,float z,string axis,float length,float height,float y,string material,Transform parent,bool collision=false)
  {B("Wall_"+material,new Vector3(x,y,z),axis=="z"?new Vector3(length+.12f,height,.18f):new Vector3(.18f,height,length+.12f),material,parent,collision);}
  static void BuildObstacles()
  {
   foreach(var o in layout.obstacles){var t=New("Collision_"+o.name,architecture);t.position=new Vector3(o.x,o.y,o.z);Undo.AddComponent<BoxCollider>(t.gameObject).size=new Vector3(o.sx,o.sy,o.sz);}
  }
  static Transform New(string name,Transform parent){var g=new GameObject(name);g.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(g,"University detail");return g.transform;}
  static Transform B(string name,Vector3 pos,Vector3 size,string mat,Transform parent,bool collision=false,float bevel=.012f)
  {
   var t=New(name,parent);t.position=pos;Undo.AddComponent<MeshFilter>(t.gameObject).sharedMesh=meshes.Box(size,Mathf.Min(bevel,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.25f));
   Undo.AddComponent<MeshRenderer>(t.gameObject).sharedMaterial=materials[mat];if(collision)Undo.AddComponent<BoxCollider>(t.gameObject).size=size;return t;
  }
  static Transform Quad(string name,Vector3 pos,Quaternion rotation,Vector2 size,Material material,Transform parent)
  {
   var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name=name;g.transform.SetParent(parent,false);Object.DestroyImmediate(g.GetComponent<Collider>());
   g.transform.SetPositionAndRotation(pos,rotation);g.transform.localScale=new Vector3(size.x,size.y,1);g.GetComponent<Renderer>().sharedMaterial=material;
   Undo.RegisterCreatedObjectUndo(g,"Surface detail");return g.transform;
  }
  static Texture2D Tex(string file)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Assets+file);
  static string SignKey(string text){uint h=2166136261;foreach(byte b in Encoding.UTF8.GetBytes(text))unchecked{h=(h^b)*16777619;}return h.ToString("x8");}
  static Material SignMaterial(string text,bool glow=false)
  {
   string key="Sign_"+SignKey(text)+(glow?"_glow":"");if(materials.TryGetValue(key,out var existing))return existing;
   Texture2D texture=Tex("Sign_"+SignKey(text)+".png");if(texture==null)throw new InvalidOperationException("Missing printed sign artwork: "+text);
   var m=Mat(key,Color.white,.16f);m.SetTexture("_BaseMap",texture);
   if(glow){m.EnableKeyword("_EMISSION");m.SetTexture("_EmissionMap",texture);m.SetColor("_EmissionColor",new Color(.45f,.65f,.48f));}
   return m;
  }
  static void Sign(string text,Vector3 pos,Quaternion rotation,float width,Transform parent,bool glow=false)
  {
   float height=width/4;var t=B("Plaque_Back",pos,new Vector3(width+.035f,height+.035f,.025f),"Trim",parent);t.rotation=rotation;
   Quad("Printed_"+text,pos+rotation*Vector3.back*.015f,rotation,new Vector2(width,height),SignMaterial(text,glow),parent);
   if(width>.6f)foreach(float x in new[]{-.48f,.48f})foreach(float y in new[]{-.43f,.43f})
    B("Mounting_Screw",pos+rotation*new Vector3(x*width,y*height,-.027f),new Vector3(.013f,.013f,.006f),"Steel",parent);
  }
  static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;string parent=Path.GetDirectoryName(p).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(p));}
  static void Tell(string s)=>EditorUtility.DisplayDialog("Expanded university",s,"OK");
 }
}
