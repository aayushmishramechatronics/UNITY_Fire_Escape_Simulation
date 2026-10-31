using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace FireEscape
{
 public static partial class FEUniversityBuilder
 {
  static FECampusRun campus;static Transform interactive;
  [MenuItem("Fire Escape/Apply Phase 6 - Campus and Four Exits")]
  public static void ApplyCampus()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Stop Play mode first.");return;}
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   var department=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Department_Interior_V3");
   experience=department==null?null:department.GetComponentInChildren<FEUniversityExperience>(true);
   if(experience==null || string.IsNullOrEmpty(scene.path)){Tell("Open your working Phase 5 university scene first.");return;}
   if(department.GetComponentInChildren<FECampusRun>(true)!=null){Tell("Phase 6 is already installed in this scene.");return;}
   emergency=experience.emergency;atmosphere=department.GetComponentInChildren<FEAtmosphere>(true);
   if(emergency==null || emergency.player==null || atmosphere==null || emergency.hazardAreas.Length!=13){Tell("Expected the working Phase 5 scene with 13 hazards. No scene changes made.");return;}
   try { PreflightCampusAssets(); }
   catch(Exception ex){Debug.LogException(ex);Tell("Phase 6 files could not be imported. No scene changes made.\n\n"+ex.Message);return;}
   layout=JsonUtility.FromJson<Layout>(AssetDatabase.LoadAssetAtPath<TextAsset>(Assets+"CampusLayout.json").text);ValidateLayout();
   Folder("Assets/_Project/Backups");string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Backups/Before_Campus_Phase6.unity");
   if(!EditorSceneManager.SaveScene(scene,backup,true)){Tell("Backup could not be saved. No changes made.");return;}
   Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Campus and fourth exit");
   Undo.RegisterFullObjectHierarchyUndo(department,"Campus redesign");Undo.RegisterFullObjectHierarchyUndo(emergency.player.gameObject,"Campus equipment");
   try
   {
    root=department.transform;output=AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/CampusPhase6");Folder(output);Folder(output+"/Meshes");
    meshes=new FEVisualMeshes(output+"/Meshes");materials.Clear();ImportAssets();MakeMaterials();CampusMaterials();
    expansion=New("Campus_Phase6",root);architecture=New("Architecture",expansion);details=New("Campus_Details",expansion);interactive=New("Interactive_Props",expansion);
    campus=Undo.AddComponent<FECampusRun>(expansion.gameObject);campus.emergency=emergency;campus.player=emergency.player;campus.refinementVersion=1;
    experience.campus=campus;experience.backupScene=backup;experience.debriefPlan=Tex("CampusPlan_Debrief.png");
    OpenConnections();BuildShell();CampusInteriors();CampusExterior();CampusEquipment();CampusEvents();CampusFires();CampusMaps();CampusSigns();CampusLighting();
    foreach(var o in layout.obstacles.Where(x=>x.name.StartsWith("Gallery")))
    {var t=New("Collision_"+o.name,architecture);t.position=new Vector3(o.x,o.y,o.z);Undo.AddComponent<BoxCollider>(t.gameObject).size=new Vector3(o.sx,o.sy,o.sz);}
    emergency.campusMode=true;emergency.route2VisitArea=new Bounds(new Vector3(-21,1,24),new Vector3(3,3,3));
    emergency.assemblyArea=new Bounds(new Vector3(77,1,40),new Vector3(7,3,7));
    BatchDetails();RebatchPreviousWing();
    AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
    if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save the upgraded scene.");
    Undo.CollapseUndoOperations(group);Selection.activeGameObject=expansion.gameObject;
    Tell("Phase 6 installed and scene saved.\n\nE: collect torch from the labelled lab TOOLS cabinet. F: toggle torch. Exit 4 branches into the East Gallery. Both viable exits lead to the north assembly courtyard.\n\nBackup: "+backup);
   }
   catch(Exception ex){Undo.RevertAllDownToGroup(group);Debug.LogException(ex);Tell("Update stopped: "+ex.Message+"\n\nYour saved backup is intact:\n"+backup+"\nReopen that backup before retrying.");}
  }
  static void PreflightCampusAssets()
  {
   // Import and type-check the complete dependency set BEFORE creating or changing scene objects.
   var paths=new HashSet<string>(new[]{"CampusLayout.json","CampusPlan_Lab.png","CampusPlan_Atrium.png","CampusPlan_North.png","CampusPlan_East.png","CampusPlan_Debrief.png","PanelArc.wav","DoorSlam.wav","CeilingShift.wav","CeilingWarning.wav","CampusBrick.png","CampusBrick_Normal.png","CampusTerrazzo.png","CampusTerrazzo_Normal.png","CampusPaving.png","CampusPaving_Normal.png","CampusOak.png","CampusOak_Normal.png","CampusPlaster.png","CampusPlaster_Normal.png"});
   foreach(string label in new[]{"TOOLS / EMERGENCY TORCH","E TO TAKE TORCH","ASSEMBLY / NORTH COURT","ASSEMBLY ^","CH-301 / SEMINAR ROOM","CH-302 / STUDENT COMMONS","CH-303 / COMPUTING LAB","CH-304 / PROJECT STUDIO","EAST TEACHING GALLERY","ENGINEERING FACULTY","EXIT 3 / COURTYARD","EXIT 4 / MAIN ENTRANCE","EXIT 4 >","FIRE & RESCUE","FIRE DOOR / E TO OPEN","NORTHBRIDGE UNIVERSITY","RESEARCH SHOWCASE","SCIENCE & TECHNOLOGY","TORCH / E TO COLLECT","UNIVERSITY LIBRARY"})paths.Add("Sign_"+SignKey(label)+".png");
   foreach(string file in paths)
   {
    string path=Assets+file;
    if(!System.IO.File.Exists(path))throw new InvalidOperationException("Missing file: "+path+". Merge the complete corrected package, including .meta files.");
    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
    bool ok=file.EndsWith(".png")?AssetDatabase.LoadAssetAtPath<Texture2D>(path)!=null:
            file.EndsWith(".wav")?AssetDatabase.LoadAssetAtPath<AudioClip>(path)!=null:AssetDatabase.LoadAssetAtPath<TextAsset>(path)!=null;
    if(!ok)throw new InvalidOperationException("Unity could not import "+path+" as its required type. Replace its .meta file from the corrected package and reimport UniversityAssets.");
   }
  }
  static void CampusMaterials()
  {
   foreach(string name in new[]{"CampusBrick","CampusTerrazzo","CampusPaving","CampusOak","CampusPlaster"})
   {
    foreach(string suffix in new[]{"","_Normal"})
    {var ti=AssetImporter.GetAtPath(Assets+name+suffix+".png") as TextureImporter;ti.textureType=suffix==""?TextureImporterType.Default:TextureImporterType.NormalMap;ti.wrapMode=TextureWrapMode.Repeat;ti.mipmapEnabled=true;ti.SaveAndReimport();}
    var m=Mat(name,Color.white,name=="CampusTerrazzo"?.36f:.18f);m.SetTexture("_BaseMap",Tex(name+".png"));m.SetTexture("_BumpMap",Tex(name+"_Normal.png"));m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.35f);
   }
   var replacements=new Dictionary<string,string>{{"Wall","CampusPlaster"},{"Floor","CampusTerrazzo"},{"Oak","CampusOak"}};
   foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
   {
    var list=r.sharedMaterials;bool changed=false;
    for(int i=0;i<list.Length;i++)if(list[i]!=null)foreach(var pair in replacements)
     if(list[i].name=="P4_"+pair.Key || list[i].name=="U5_"+pair.Key){list[i]=materials[pair.Value];changed=true;break;}
    if(changed)r.sharedMaterials=list;
   }
   materials["Wall"]=materials["CampusPlaster"];materials["Floor"]=materials["CampusTerrazzo"];materials["Oak"]=materials["CampusOak"];
   Mat("Bronze",new Color(.4f,.28f,.12f),.4f).SetFloat("_Metallic",.65f);Mat("Foliage",new Color(.14f,.23f,.09f));Mat("Asphalt",new Color(.075f,.085f,.09f));Mat("Uniform",new Color(.13f,.15f,.15f));
   Mat("ReflectiveStripe",new Color(.75f,.81f,.24f),.3f);Mat("Helmet",new Color(.8f,.68f,.12f),.4f);Mat("Skin",new Color(.45f,.28f,.18f));
   var lens=Mat("BlueLens",new Color(.07f,.22f,.8f),.4f);lens.EnableKeyword("_EMISSION");lens.SetColor("_EmissionColor",new Color(.1f,.4f,1)*1.3f);
   materials["Glass"].SetColor("_BaseColor",new Color(.16f,.23f,.26f,.19f));
  }
  static void OpenConnections()
  {
   Transform old=experience.transform;var batch=old.Find("Batched_Detail_Meshes");if(batch!=null)batch.gameObject.SetActive(false);
   foreach(string name in new[]{"Architecture","University_Details"})
   {var parent=old.Find(name);if(parent!=null)foreach(var r in parent.GetComponentsInChildren<MeshRenderer>(true))r.enabled=true;}
   foreach(var t in root.GetComponentsInChildren<Transform>(true).ToArray())
   {
    Vector3 p=t.position;string n=t.name;
    bool boundary=(n=="Wall" || n=="Wall_Band" || n=="Skirting") && Mathf.Abs(p.x-26)<.25f && p.z>=10 && p.z<16;
    bool court=t.IsChildOf(old) && n=="Wall_Concrete" && Mathf.Abs(p.z-32)<.2f && p.x>70 && p.x<84;
    if(boundary || court)t.gameObject.SetActive(false);
   }
   B("Gallery_Portal_Header",new Vector3(26,3.15f,13),new Vector3(.45f,.3f,6),"Trim",details);
   foreach(float z in new[]{10.2f,15.8f})B("Gallery_Portal_Jamb",new Vector3(26,1.5f,z),new Vector3(.45f,3,.25f),"Trim",details);
  }
  static void RebatchPreviousWing()
  {
   Transform e=expansion,d=details,a=architecture;string o=output;
   expansion=experience.transform;details=expansion.Find("University_Details");architecture=expansion.Find("Architecture");
   output+="/PreviousWing";Folder(output);Folder(output+"/Meshes");BatchDetails();
   output=o;expansion=e;details=d;architecture=a;
  }
  static void CampusMaps()
  {
   foreach(var plan in root.GetComponentsInChildren<FEWallPlan>(true))
   {
    string file=plan.transform.position.x>45?"CampusPlan_North.png":plan.transform.position.x>28?"CampusPlan_Atrium.png":"CampusPlan_Lab.png";plan.plan=Tex(file);
    foreach(var r in plan.GetComponentsInChildren<MeshRenderer>(true))
    {
     var old=r.sharedMaterial;if(old==null || !old.HasProperty("_BaseMap") || old.GetTexture("_BaseMap")==null)continue;
     var m=new Material(old){name="Campus_Map"};m.SetTexture("_BaseMap",plan.plan);AssetDatabase.CreateAsset(m,AssetDatabase.GenerateUniqueAssetPath(output+"/Campus_Map.mat"));r.sharedMaterial=m;
    }
   }
   WallMap(new Vector3(31,1.65f,10.14f),Quaternion.Euler(0,180,0),"CampusPlan_East.png");
  }
  static void CampusSigns()
  {
   Sign("EXIT 4 >",new Vector3(15,2.65f,13.82f),Quaternion.identity,2.8f,details,true);
   Sign("EXIT 4 / MAIN ENTRANCE",new Vector3(26.05f,2.7f,13),Quaternion.Euler(0,90,0),3.1f,details,true);
   Sign("EXIT 3 / COURTYARD",new Vector3(24,2.65f,17.85f),Quaternion.identity,2.1f,details,true);
   Sign("EAST TEACHING GALLERY",new Vector3(45,3.2f,15.82f),Quaternion.identity,3.4f,details);
   Sign("EXIT 4 / MAIN ENTRANCE",new Vector3(69.75f,2.85f,13),Quaternion.Euler(0,90,0),3.5f,details,true);
   foreach(float x in new[]{30f,35f,44f,48f,55f,65f})Quad("Exit4_Floor_Arrow",new Vector3(x,.025f,13),Quaternion.Euler(90,90,0),new Vector2(.5f,.75f),materials["GreenGuide"],details);
   foreach(float z in new[]{16f,23f,29f,35f})
   {
    Quad("Assembly_Floor_Arrow",new Vector3(77,.027f,z),Quaternion.Euler(90,0,0),new Vector2(.6f,.85f),materials["GreenGuide"],details);
    B("Assembly_Wayfinding_Post",new Vector3(82,.9f,z),new Vector3(.08f,1.8f,.08f),"Steel",details);
    Sign("ASSEMBLY ^",new Vector3(82,1.8f,z-.06f),Quaternion.identity,1.25f,details,true);
   }
  }
  static void CampusLighting()
  {
   RenderSettings.ambientSkyColor=new Color(.24f,.265f,.29f);RenderSettings.ambientEquatorColor=new Color(.15f,.17f,.185f);RenderSettings.ambientGroundColor=new Color(.075f,.085f,.095f);atmosphere.smokeHaze=.78f;
   var red=new List<Light>(atmosphere.alarmLights);
   foreach(float x in new[]{28f,36f,45f,54f,63f,68f})
   {
    var l=Undo.AddComponent<Light>(New("East_Red_Emergency",details).gameObject);l.transform.position=new Vector3(x,2.7f,15.75f);l.type=LightType.Point;l.color=new Color(1,.03f,.012f);l.range=6;l.shadows=LightShadows.None;red.Add(l);
    B("Emergency_Lens",l.transform.position,new Vector3(.25f,.17f,.11f),"Red",details);B("Emergency_Housing",l.transform.position+Vector3.forward*.05f,new Vector3(.32f,.24f,.13f),"Steel",details);
    var fill=Undo.AddComponent<Light>(New("Reflected_Window_Light",details).gameObject);fill.transform.position=new Vector3(x,3.1f,12);fill.color=new Color(.61f,.72f,.82f);fill.range=9;fill.intensity=.52f;fill.shadows=LightShadows.None;
   }
   atmosphere.alarmLights=red.ToArray();
   foreach(float z in new[]{16f,28f,42f})
   {
    B("Campus_Lamp_Post",new Vector3(83,2,z),new Vector3(.12f,4,.12f),"Steel",details);
    var l=Undo.AddComponent<Light>(New("Campus_Lamp",details).gameObject);l.transform.position=new Vector3(80,4,z);l.type=LightType.Point;l.range=12;l.color=new Color(.85f,.88f,1);l.intensity=1.3f;l.shadows=LightShadows.None;
   }
  }
 }
}
