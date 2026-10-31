using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace FireEscape
{
 public static partial class FEUniversityBuilder
 {
  static void BuildNewFurniture()
  {
   foreach(var p in new[]{new Vector3(31,0,32),new Vector3(45,0,32),new Vector3(31,0,48),new Vector3(45,0,48)})
   {
    B("Atrium_Column",p+Vector3.up*3.1f,new Vector3(.65f,6.2f,.65f),"Wall",details);
    B("Column_Shoe",p+Vector3.up*.25f,new Vector3(.73f,.5f,.73f),"Steel",details);
   }
   // An inaccessible upper gallery supplies vertical scale without a second playable floor.
   B("Upper_Gallery",new Vector3(38,3.6f,48.3f),new Vector3(15.7f,.22f,3.2f),"Concrete",details);
   B("Gallery_Handrail",new Vector3(38,4.7f,46.7f),new Vector3(15.7f,.065f,.065f),"Steel",details);
   for(float x=30.5f;x<46;x+=1)B("Gallery_Baluster",new Vector3(x,4.15f,46.7f),new Vector3(.04f,1.05f,.04f),"Steel",details);
   for(float x=32;x<46;x+=3)B("Atrium_Roof_Rib",new Vector3(x,6.15f,40),new Vector3(.14f,.25f,19.8f),"Steel",details);
   Sign("SCIENCE ATRIUM",new Vector3(38,5.4f,49.84f),Quaternion.identity,5,details);
   B("Reception_Counter",new Vector3(43.4f,.55f,33),new Vector3(3.6f,1.1f,1.3f),"Oak",details);
   B("Reception_Worktop",new Vector3(43.4f,1.13f,33),new Vector3(3.72f,.08f,1.38f),"Counter",details);
   Sign("FACULTY RECEPTION",new Vector3(43.4f,.76f,33.668f),Quaternion.Euler(0,180,0),2.3f,details);
   B("Reception_Monitor",new Vector3(43.8f,1.44f,33),new Vector3(.7f,.43f,.07f),"Black",details);
   Cabinet(new Vector3(33,0,47),2.8f,1.3f,1);Sign("CHEMISTRY COLLECTION",new Vector3(33,1.15f,46.475f),Quaternion.identity,2.4f,details);
   // Display models and clear enclosure, seen through the glass.
   for(int i=0;i<4;i++)
   {
    B("Mineral_Display_Base",new Vector3(32+i*.65f,1.36f,47),new Vector3(.38f,.10f,.38f),"Black",details);
    var mineral=B("Mineral_Specimen",new Vector3(32+i*.65f,1.57f,47),new Vector3(.25f,.34f,.22f),i%2==0?"Steel":"Yellow",details);mineral.rotation=Quaternion.Euler(15,i*27,18);
   }
   B("Display_Glass",new Vector3(33,1.62f,46.6f),new Vector3(2.7f,.65f,.03f),"Glass",details);
   Bench(new Vector3(42,0,47.8f),3);
   var fallen=B("Fallen_Atrium_Panel",new Vector3(38,.18f,41),new Vector3(2.8f,.36f,2.1f),"Char",details);
   for(int i=0;i<4;i++)B("Exposed_Rebar",new Vector3(37.1f+i*.55f,.4f,41),new Vector3(.018f,.018f,2.8f),"Steel",details);
   Quad("Atrium_Soot",new Vector3(38,.015f,41),Quaternion.Euler(90,0,0),new Vector2(5,4),materials["Soot"],details);
   foreach(float x in new[]{36.7f,41.8f})foreach(float z in new[]{24f,27f})
   {
    B("Teaching_Bench",new Vector3(x,.45f,z),new Vector3(3,.9f,1.05f),"White",details);
    B("Teaching_Worktop",new Vector3(x,.94f,z),new Vector3(3.08f,.09f,1.13f),"Counter",details);
    B("Lab_Instrument",new Vector3(x+.8f,1.2f,z),new Vector3(.45f,.44f,.45f),"Steel",details);
   }
   Sign("CH-201 / TEACHING LAB",new Vector3(39,2.96f,30.12f),Quaternion.Euler(0,180,0),1.6f,details);
   for(float z=23;z<30;z+=2)B("Classroom_Service_Rail",new Vector3(45.75f,1.4f,z),new Vector3(.18f,.13f,1.5f),"Steel",details);
   Cabinet(new Vector3(53,0,41.6f),2,1.9f,.5f);
   Sign("SERVICE PASSAGE",new Vector3(50,2.62f,41.85f),Quaternion.identity,2.3f,details);
   for(float x=47;x<66;x+=3)
   {
    B("Pipe_Support",new Vector3(x,2.9f,40.8f),new Vector3(.08f,.28f,1.4f),"Steel",details);
    B("Ceiling_Service_Duct",new Vector3(x,3.0f,40.5f),new Vector3(2.9f,.24f,.48f),"Steel",details);
   }
   for(int i=0;i<5;i++)
   {var chunk=B("Service_Rubble",new Vector3(57.5f+i*.2f,.15f+i*.05f,41.2f),new Vector3(.55f,.28f,.65f),i%2==0?"Concrete":"Char",details);chunk.rotation=Quaternion.Euler(0,i*18,8);}
   Cabinet(new Vector3(54,0,55.4f),1.2f,2.1f,.85f);
   B("Vending_Glass",new Vector3(54,1.35f,54.95f),new Vector3(.93f,1.18f,.025f),"Glass",details);
   for(int i=0;i<3;i++)for(int j=0;j<3;j++)B("Vending_Item",new Vector3(53.7f+i*.3f,.92f+j*.28f,54.99f),new Vector3(.16f,.17f,.06f),j==1?"Yellow":"Red",details);
   Sign("VENDING",new Vector3(54,1.94f,54.945f),Quaternion.identity,.95f,details);
   Bench(new Vector3(59,0,55.5f),2.4f);Sign("NORTH TEACHING WING",new Vector3(57,2.65f,55.84f),Quaternion.identity,3,details);
   // A stair lobby with a screened upper flight, leaving the ground-floor exit open.
   B("Stair_Enclosure",new Vector3(68,1.5f,41.7f),new Vector3(3.5f,3,.35f),"Glass",details);
   for(int i=0;i<6;i++)B("Upper_Flight_Step",new Vector3(66.7f+i*.46f,2.65f+i*.2f,41.2f),new Vector3(.46f,.18f,.72f),"Concrete",details);
   Sign("GROUND FLOOR / STAIR LOBBY",new Vector3(68,2.4f,41.49f),Quaternion.identity,2.8f,details);
   Sign("UPPER FLOOR CLOSED",new Vector3(68,3.9f,41.48f),Quaternion.identity,2,details);
   Sign("COURTYARD / EXIT 3",new Vector3(69.85f,2.6f,40),Quaternion.Euler(0,90,0),2.5f,details,true);
   // Door leaves are visibly held open at the final exit; no new interaction gate.
   foreach(float z in new[]{38.3f,41.7f})
   {B("Open_Exit_Leaf",new Vector3(70.65f,1.3f,z),new Vector3(1.4f,2.6f,.10f),"Blue",details);B("Panic_Bar",new Vector3(70.65f,1.1f,z+(z<40?.065f:-.065f)),new Vector3(1.1f,.07f,.07f),"Steel",details);}
   foreach(var p in new[]{new Vector3(75,0,34.5f),new Vector3(80,0,45.5f)})
   {B("Courtyard_Planter",p+Vector3.up*.4f,new Vector3(3,.8f,1.5f),"Concrete",details);B("Soil",p+Vector3.up*.82f,new Vector3(2.8f,.04f,1.3f),"Char",details);}
   B("Assembly_Post",new Vector3(79,1.15f,43),new Vector3(.09f,2.3f,.09f),"Steel",details);
   Sign("ASSEMBLY POINT",new Vector3(79,2.15f,42.94f),Quaternion.identity,2.8f,details,true);
  }
  static void Cabinet(Vector3 p,float width,float height,float depth)
  {
   B("Cabinet_Body",p+Vector3.up*height/2,new Vector3(width,height,depth),"White",details);
   for(int i=0;i<3;i++)B("Cabinet_Seam",p+new Vector3(0,height*(i+1)/4,-depth/2-.009f),new Vector3(width-.09f,.02f,.012f),"Trim",details);
   foreach(float x in new[]{-.22f,.22f})B("Cabinet_Handle",p+new Vector3(x,height*.65f,-depth/2-.025f),new Vector3(.04f,.18f,.035f),"Steel",details);
  }
  static void Bench(Vector3 p,float width)
  {
   B("Bench_Seat",p+Vector3.up*.43f,new Vector3(width,.13f,.65f),"Oak",details);
   B("Bench_Back",p+new Vector3(0,.72f,.28f),new Vector3(width,.4f,.09f),"Oak",details);
   foreach(float x in new[]{-width*.38f,width*.38f})B("Bench_Support",p+new Vector3(x,.2f,0),new Vector3(.07f,.4f,.5f),"Steel",details);
  }
  static void UpgradeExistingArchitecture()
  {
   // Door surrounds are attached to the stationary frame. Moving leaves retain their hinges.
   foreach(var door in root.GetComponentsInChildren<FEDoor>(true))
   {
    Transform frame=New("Deep_Door_Surround",door.transform);
    foreach(float x in new[]{-1.03f,1.03f})
    {var side=B("Door_Reveal",door.transform.TransformPoint(new Vector3(x,1.33f,-.04f)),new Vector3(.13f,2.66f,.32f),"Trim",frame);side.rotation=door.transform.rotation;}
    var lintel=B("Door_Lintel",door.transform.TransformPoint(new Vector3(0,2.72f,-.04f)),new Vector3(2.18f,.16f,.32f),"Steel",frame);lintel.rotation=door.transform.rotation;
   }
   // Shallow pilasters along selected wall runs break up the long corridor silhouettes.
   foreach(float x in new[]{-16f,-6f,4f,18f})
   {
    B("Teaching_Wing_Pilaster",new Vector3(x,1.65f,13.88f),new Vector3(.35f,3.3f,.15f),"Wall",details);
    B("Pilaster_Base",new Vector3(x,.22f,13.84f),new Vector3(.39f,.44f,.20f),"Steel",details);
   }
   // Existing analytical-lab windows now reveal varied silhouettes behind the glass.
   Sign("SCIENCE & DISCOVERY",new Vector3(15,2.7f,29.83f),Quaternion.identity,2.4f,details);
   Cabinet(new Vector3(16.8f,0,28.8f),1.5f,2.1f,.6f);
   // Original seating/display landmarks in the teaching wing, out of the walking lane.
   B("Chemistry_Display_Plinth",new Vector3(-3.5f,.65f,13.60f),new Vector3(1.4f,1.3f,.5f),"Oak",details);
   B("Chemistry_Display_Glass",new Vector3(-3.5f,1.6f,13.59f),new Vector3(1.35f,.7f,.48f),"Glass",details);
   Sign("CHEMISTRY COLLECTION",new Vector3(-3.5f,1.13f,13.33f),Quaternion.identity,1.25f,details);
   // Deliberate clusters of wear and damage, leaving other sections intact.
   foreach(var p in new[]{new Vector3(0,.013f,6),new Vector3(7,.013f,11),new Vector3(23,.013f,19),new Vector3(14,.013f,31),new Vector3(35,.013f,35),new Vector3(60,.013f,53)})
    Quad("Floor_Wear",p,Quaternion.Euler(90,0,0),new Vector2(3.5f,3),materials["Scuffs"],details);
   var panel=B("Hanging_Panel_Sequence",new Vector3(40,5.95f,40),new Vector3(1.8f,.05f,1.1f),"Ceiling",details);
   experience.hangingPanel=panel;experience.panelRest=panel.localPosition;experience.panelRotation=panel.localRotation;
   for(int i=0;i<3;i++)B("Exposed_Ceiling_Service",new Vector3(39.5f+i*.4f,6.12f,40),new Vector3(.06f,.08f,3),"Steel",details);
  }
  static void FixDecals()
  {
   foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
   {
    string n=r.name.ToLowerInvariant();
    if(n.Contains("soot") || n.Contains("burnt_floor") || n=="charred_worktop")r.sharedMaterial=materials["Soot"];
   }
  }
  static void ReplaceLabels()
  {
   int index=0;
   foreach(var door in root.GetComponentsInChildren<FEDoor>(true).Where(x=>x.kind==FEDoor.DoorKind.Fatal))
   {var warning=door.hinge.Find("Warning_Label");if(warning!=null)warning.gameObject.SetActive(false);}
   foreach(var tm in root.GetComponentsInChildren<TextMesh>(true).ToArray())
   {
    var r=tm.GetComponent<MeshRenderer>();if(r==null || !r.enabled || !tm.gameObject.activeInHierarchy)continue;
    if(Tex("Sign_"+SignKey(tm.text)+".png")==null)continue;
    float width=r.localBounds.size.x*Mathf.Abs(tm.transform.lossyScale.x);
    if(width<.02f)width=Mathf.Clamp(tm.text.Length*.045f,.2f,3);
    bool tiny=width<.3f,exit=tm.text.StartsWith("EXIT") || tm.text.StartsWith("< EXIT");
    width=Mathf.Clamp(width,tiny?.065f:.8f,tiny?.27f:tm.text.Contains("DEPARTMENT")?3.8f:2.4f);
    if(tiny)Quad("Printed_Equipment_Label",tm.transform.position-tm.transform.forward*.003f,tm.transform.rotation,new Vector2(width,width/4),SignMaterial(tm.text),tm.transform.parent);
    else Sign(tm.text,tm.transform.position-tm.transform.forward*.005f,tm.transform.rotation,width,tm.transform.parent,exit);
    r.enabled=false;index++;
   }
   foreach(var spec in layout.doors.Where(x=>x.mode!="passage"))
   {
    var door=root.GetComponentsInChildren<FEDoor>(true).FirstOrDefault(x=>x.name=="Door_"+spec.id);if(door==null)continue;
    string text=spec.id=="chemical"?"CH-102 / CHEMICAL STORAGE":spec.id=="solvent"?"CH-103 / SOLVENT PREPARATION":spec.id=="office"?"CH-104 / FACULTY OFFICE":spec.id=="lab2"?"CH-105 / ANALYTICAL LAB":spec.id=="toilets"?"CH-106 / TOILETS":spec.id=="utility"?"CH-107 / UTILITY ROOM":"CH-108 / RECORDS OFFICE";
    Sign(text,door.transform.TransformPoint(new Vector3(-1.62f,1.62f,-.17f)),door.transform.rotation,1.1f,door.transform);
    if(door.kind==FEDoor.DoorKind.Fatal)Sign("DANGER / DO NOT ENTER",door.transform.TransformPoint(new Vector3(0,1.77f,-.105f)),door.transform.rotation,1.35f,door.hinge);
   }
  }
  static void BuildGuidance()
  {
   // Fixed signs depict all normal exit routes, including routes that become blocked.
   foreach(var route in layout.routes)
    for(int i=1;i<route.points.Length;i++)
    {
     Vector3 a=new Vector3(route.points[i-1].x,0,route.points[i-1].z),b=new Vector3(route.points[i].x,0,route.points[i].z);
     float length=Vector3.Distance(a,b);if(length<2)continue;Vector3 direction=(b-a).normalized;
     for(float distance=2;distance<length;distance+=4.5f)
     {
      Vector3 p=a+direction*distance;float yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
      Quad("Green_Exit_Direction",p+Vector3.up*.022f,Quaternion.Euler(90,yaw,0),new Vector2(.43f,.65f),materials["GreenGuide"],details);
     }
    }
   Sign("EXIT 1 ^",new Vector3(0,.65f,15.8f),Quaternion.identity,.8f,details,true);
   Sign("EXIT 2 <",new Vector3(-5,.65f,13.83f),Quaternion.identity,.8f,details,true);
   Sign("EXIT 3 >",new Vector3(5,.65f,13.83f),Quaternion.identity,.8f,details,true);
   Sign("EXIT 3 - BOTH ROUTES",new Vector3(45.82f,2.5f,40),Quaternion.Euler(0,90,0),2.6f,details,true);
   Sign("NORTH WING >",new Vector3(48,.65f,55.83f),Quaternion.identity,1.2f,details,true);
   Sign("EXIT 3 >",new Vector3(64,.65f,41.83f),Quaternion.identity,1.1f,details,true);
  }
  static void UpdateMaps()
  {
   foreach(var plan in root.GetComponentsInChildren<FEWallPlan>(true))
   {
    plan.plan=Tex("Plan_Lab.png");
    foreach(var r in plan.GetComponentsInChildren<MeshRenderer>(true))
    {
     var old=r.sharedMaterial;if(old==null || !old.HasProperty("_BaseMap") || old.GetTexture("_BaseMap")==null)continue;
     var m=new Material(old){name="Updated_Lab_Floorplan"};m.SetTexture("_BaseMap",plan.plan);AssetDatabase.CreateAsset(m,AssetDatabase.GenerateUniqueAssetPath(output+"/Updated_Lab_Floorplan.mat"));r.sharedMaterial=m;
    }
   }
   WallMap(new Vector3(30.14f,1.65f,33),Quaternion.Euler(0,-90,0),"Plan_Atrium.png");
   WallMap(new Vector3(56,1.65f,55.86f),Quaternion.identity,"Plan_North.png");
  }
  static void WallMap(Vector3 p,Quaternion rotation,string file)
  {
   var t=New("Wall_Evacuation_Plan",details);t.SetPositionAndRotation(p,rotation);var plan=Undo.AddComponent<FEWallPlan>(t.gameObject);plan.plan=Tex(file);
   var back=B("Map_Frame",p,new Vector3(1.8f,1.35f,.045f),"Trim",t);back.rotation=rotation;
   var collider=Undo.AddComponent<BoxCollider>(t.gameObject);collider.size=new Vector3(1.8f,1.35f,.06f);
   var mat=Mat("Map_"+file.Replace(".png",""),Color.white,.12f);mat.SetTexture("_BaseMap",plan.plan);
   Quad("Map_Print",p+rotation*Vector3.back*.028f,rotation,new Vector2(1.72f,1.29f),mat,t);
  }
  static void Lighting()
  {
   // Lift shadow detail; red lamps remain accents, not the only source of visibility.
   RenderSettings.ambientMode=AmbientMode.Trilight;
   RenderSettings.ambientSkyColor=new Color(.20f,.22f,.245f);RenderSettings.ambientEquatorColor=new Color(.115f,.13f,.145f);RenderSettings.ambientGroundColor=new Color(.065f,.075f,.08f);
   RenderSettings.reflectionIntensity=.38f;
   foreach(var f in atmosphere.fixtures)if(f.light!=null){f.normalIntensity=.8f;f.light.intensity=.8f;f.light.shadows=LightShadows.None;}
   var alarms=new List<Light>(atmosphere.alarmLights.Where(x=>x!=null));
   foreach(var p in new[]{new Vector3(31,2.8f,36),new Vector3(45,2.8f,46),new Vector3(48,2.7f,48),new Vector3(59,2.7f,55.7f),new Vector3(64,2.7f,46),new Vector3(58,2.7f,41.7f),new Vector3(68,2.7f,38.3f)})
   {
    var l=Undo.AddComponent<Light>(New("Red_Emergency_Lamp",details).gameObject);l.transform.position=p;l.type=LightType.Point;l.color=new Color(1,.028f,.01f);l.range=6;l.intensity=0;l.shadows=LightShadows.None;alarms.Add(l);
    B("Emergency_Lamp_Housing",p+Vector3.forward*.04f,new Vector3(.22f,.3f,.12f),"Steel",details);
    B("Emergency_Red_Lens",p,new Vector3(.13f,.22f,.13f),"Red",details);
   }
   foreach(var light in alarms)light.range=5.5f;atmosphere.alarmLights=alarms.ToArray();
   foreach(var p in new[]{new Vector3(0,2.8f,-2),new Vector3(0,2.8f,7),new Vector3(-12,2.8f,12),new Vector3(12,2.8f,12),new Vector3(-20,2.8f,22),new Vector3(24,2.8f,23),new Vector3(14,2.8f,32),new Vector3(20,2.8f,40),new Vector3(38,5.5f,39),new Vector3(39,2.8f,26),new Vector3(48,2.8f,49),new Vector3(58,2.8f,54),new Vector3(58,2.8f,40),new Vector3(68,3.5f,40)})
   {
    var l=Undo.AddComponent<Light>(New("Soft_Reflected_Fill",details).gameObject);l.transform.position=p;l.type=LightType.Point;l.color=new Color(.63f,.74f,.82f);l.intensity=.38f;l.range=p.y>4?13:8;l.shadows=LightShadows.None;
   }
   foreach(var v in atmosphere.GetComponentsInChildren<Volume>(true))
   {
    if(v.sharedProfile==null)continue;var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,AssetDatabase.GenerateUniqueAssetPath(output+"/Readable_Grade.asset"));
    foreach(var c in v.sharedProfile.components){var clone=Object.Instantiate(c);profile.components.Add(clone);AssetDatabase.AddObjectToAsset(clone,profile);}
    if(profile.TryGet<ColorAdjustments>(out var color)){color.postExposure.Override(.3f);color.contrast.Override(6);color.saturation.Override(-6);}
    if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.15f);bloom.threshold.Override(1.35f);}
    if(profile.TryGet<Vignette>(out var vignette))vignette.intensity.Override(.10f);v.sharedProfile=profile;
   }
  }
  static void EmergencySequence()
  {
   var effectRoot=emergency.emergencyRoot.transform;
   var template=effectRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name=="Side_Fire");
   if(template==null)throw new InvalidOperationException("The existing side-fire template is missing.");
   var hazards=emergency.hazardAreas.Take(emergency.hazardAreas.Length-1).ToList();var startTimes=Enumerable.Repeat(0f,hazards.Count).ToList();
   for(int i=2;i<startTimes.Count;i++)startTimes[i]=12;
   foreach(var t in effectRoot.GetComponentsInChildren<Transform>(true).ToArray())
   {
    if(t.name=="Side_Fire" || t.name.StartsWith("Extra_Fire_"))stages.Add(new FEUniversityExperience.Stage{target=t.gameObject,atSeconds=12});
    if(t.name=="Dense_Ceiling_Smoke")stages.Add(new FEUniversityExperience.Stage{target=t.gameObject,atSeconds=8});
   }
   int number=0;
   foreach(var spec in layout.newFires)
   {
    var g=Object.Instantiate(template.gameObject,effectRoot);g.name="University_Fire_"+number++;Undo.RegisterCreatedObjectUndo(g,"University fire");g.transform.position=new Vector3(spec.x,spec.y,spec.z);
    foreach(var ps in g.GetComponentsInChildren<ParticleSystem>(true))
    {
     ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
     if(ps.name=="Smoke")ps.transform.position=new Vector3(spec.x,2.2f,spec.z);
    }
    foreach(var a in g.GetComponentsInChildren<AudioSource>(true)){a.playOnAwake=false;a.volume=.42f;}
    stages.Add(new FEUniversityExperience.Stage{target=g,atSeconds=spec.at});hazards.Add(new Bounds(new Vector3(spec.x,1,spec.z),new Vector3(spec.sx,3,spec.sz)));startTimes.Add(spec.at);
   }
   // The collapse hazard stays last; its activation still belongs to FEEmergency.
   hazards.Add(emergency.hazardAreas[emergency.hazardAreas.Length-1]);startTimes.Add(0);
   emergency.hazardAreas=hazards.ToArray();emergency.hazardStartSeconds=startTimes.ToArray();
   var smokeTemplate=effectRoot.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(x=>x.name=="Dense_Ceiling_Smoke");
   if(smokeTemplate!=null)
   {
    foreach(var p in new[]{new Vector3(36,5.1f,40),new Vector3(41,5.1f,44),new Vector3(52,2.3f,40),new Vector3(58,2.3f,40),new Vector3(63,2.5f,40)})
    {
     var g=Object.Instantiate(smokeTemplate.gameObject,effectRoot);g.name="University_Ceiling_Smoke";g.transform.position=p;Undo.RegisterCreatedObjectUndo(g,"University smoke");
     var ps=g.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var shape=ps.shape;shape.scale=p.y>4?new Vector3(7,.1f,5):new Vector3(4,.1f,2.3f);
     var emission=ps.emission;emission.rateOverTime=p.y>4?5:7;stages.Add(new FEUniversityExperience.Stage{target=g,atSeconds=30});
    }
   }
   foreach(var door in root.GetComponentsInChildren<FEDoor>(true).Where(x=>x.kind==FEDoor.DoorKind.Fatal))
   {
    var t=New("Warning_Electrical_Hiss",effectRoot);t.position=door.transform.position+Vector3.up*1.2f;
    AudioSource sound=Undo.AddComponent<AudioSource>(t.gameObject);sound.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Assets+"ElectricalHiss.wav");sound.loop=true;sound.playOnAwake=false;sound.spatialBlend=1;sound.volume=.6f;sound.minDistance=.6f;sound.maxDistance=5;roomSounds.Add(sound);
   }
   foreach(var pos in new[]{new Vector3(0,2.8f,10),new Vector3(39,4.8f,40),new Vector3(58,2.8f,54)})
   {
    var alarm=Undo.AddComponent<AudioSource>(New("Spatial_Alarm",effectRoot).gameObject);alarm.transform.position=pos;
    alarm.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Assets+"AlarmLoop.wav");alarm.loop=true;alarm.playOnAwake=false;
    alarm.spatialBlend=1;alarm.volume=.065f;alarm.minDistance=5;alarm.maxDistance=38;alarm.rolloffMode=AudioRolloffMode.Logarithmic;roomSounds.Add(alarm);
   }
   var creak=Undo.AddComponent<AudioSource>(New("Atrium_Structure_Sound",effectRoot).gameObject);creak.transform.position=new Vector3(40,4.8f,40);creak.clip=atmosphere.structureCreak;creak.loop=true;creak.playOnAwake=false;creak.spatialBlend=1;creak.volume=.12f;creak.minDistance=2;creak.maxDistance=14;roomSounds.Add(creak);
   atmosphere.fireLights=effectRoot.GetComponentsInChildren<Light>(true);emergency.emergencyLights=atmosphere.fireLights;
   atmosphere.fireAudio=effectRoot.GetComponentsInChildren<AudioSource>(true).Where(x=>x.clip!=null && x.clip.name=="FireLoop").ToArray();
   foreach(var stage in stages)if(stage.target!=null)stage.target.SetActive(false);
  }
  static void BatchDetails()
  {
   // Keep the animated panel, transparent surfaces, moving-door plaques and signs separate.
   var groups=new Dictionary<string,List<MeshFilter>>();
   foreach(var filter in details.GetComponentsInChildren<MeshFilter>(true).Concat(architecture.GetComponentsInChildren<MeshFilter>(true)))
   {
    var r=filter.GetComponent<MeshRenderer>();
    if(!filter.gameObject.activeInHierarchy || r==null || !r.enabled || r.sharedMaterial==null || r.sharedMaterial.renderQueue>=2500 || filter.sharedMesh==null || filter.transform==experience.hangingPanel || filter.GetComponentInParent<FEWallPlan>()!=null)continue;
    Vector3 p=filter.transform.position;string key=r.sharedMaterial.name+"_"+Mathf.FloorToInt(p.x/10)+"_"+Mathf.FloorToInt(p.z/10);
    if(!groups.ContainsKey(key))groups[key]=new List<MeshFilter>();groups[key].Add(filter);
   }
   int index=0;var batch=New("Batched_Detail_Meshes",expansion);
   foreach(var entry in groups)
   {
    if(entry.Value.Count<4)continue;
    var mesh=new Mesh{name="University_Batch_"+index,indexFormat=IndexFormat.UInt32};
    mesh.CombineMeshes(entry.Value.Select(f=>new CombineInstance{mesh=f.sharedMesh,subMeshIndex=0,transform=expansion.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
    mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,output+"/Meshes/Batch_"+index+".asset");index++;
    var t=New(entry.Key,batch);Undo.AddComponent<MeshFilter>(t.gameObject).sharedMesh=mesh;Undo.AddComponent<MeshRenderer>(t.gameObject).sharedMaterial=entry.Value[0].GetComponent<MeshRenderer>().sharedMaterial;
    foreach(var f in entry.Value)f.GetComponent<MeshRenderer>().enabled=false;
   }
  }
 }
}
