using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace FireEscape
{
 public static partial class FEUniversityBuilder
 {
  static void CampusEquipment()
  {
   BuildTorchCabinet();
   var held=New("Equipped_Torch",emergency.player.viewCamera.transform);held.localPosition=new Vector3(.22f,-.24f,.4f);held.localRotation=Quaternion.identity;TorchModel(held,true);campus.heldTorch=held.gameObject;
   var lamp=New("Torch_Beam",emergency.player.viewCamera.transform);lamp.localPosition=new Vector3(.12f,-.1f,.12f);lamp.localRotation=Quaternion.identity;
   var light=Undo.AddComponent<Light>(lamp.gameObject);light.type=LightType.Spot;light.range=16;light.spotAngle=54;light.innerSpotAngle=28;light.color=new Color(1,.94f,.79f);light.intensity=3.4f;light.shadows=LightShadows.Soft;light.shadowBias=.03f;light.shadowNormalBias=.15f;light.enabled=false;
   campus.torchLight=light;held.gameObject.SetActive(false);
  }
  static void BuildTorchCabinet()
  {
   // Open-front cabinet beside the lab exit: the complete front is interactive.
   var cabinet=New("Lab_Tools_Cabinet",interactive);
   Vector3 p=new Vector3(-2.05f,1.5f,1.64f);
   B("Tools_Back",p,new Vector3(1.35f,1.1f,.12f),"Yellow",cabinet);
   foreach(float x in new[]{-.66f,.66f})B("Tools_Side",p+new Vector3(x,0,-.16f),new Vector3(.06f,1.1f,.36f),"Red",cabinet);
   foreach(float y in new[]{-.53f,.53f})B("Tools_Rim",p+new Vector3(0,y,-.16f),new Vector3(1.35f,.06f,.36f),"Red",cabinet);
   Sign("TOOLS / EMERGENCY TORCH",p+new Vector3(0,.78f,-.22f),Quaternion.identity,1.7f,cabinet,true);
   Sign("E TO TAKE TORCH",p+new Vector3(0,-.32f,-.24f),Quaternion.identity,.95f,cabinet);
   var pickup=New("Collectable_Lab_Torch",cabinet);pickup.position=p+new Vector3(0,.02f,-.25f);
   Undo.AddComponent<FETorchPickup>(pickup.gameObject).campus=campus;
   var c=Undo.AddComponent<BoxCollider>(pickup.gameObject);c.size=new Vector3(1.2f,.95f,.2f);
   TorchModel(pickup,false);var model=pickup.Find("Torch_Model");model.localRotation=Quaternion.identity;model.localPosition=new Vector3(0,-.18f,-.02f);model.localScale=Vector3.one*1.35f;
   campus.torchPickup=pickup.gameObject;
   var l=Undo.AddComponent<Light>(New("Equipment_Cabinet_Light",cabinet).gameObject);l.transform.position=p+new Vector3(0,.42f,-.48f);
   l.type=LightType.Point;l.color=new Color(.75f,1,.82f);l.intensity=.85f;l.range=2.8f;l.shadows=LightShadows.None;
  }
  static void TorchModel(Transform parent,bool held)
  {
   var model=New("Torch_Model",parent);model.localRotation=Quaternion.Euler(90,0,0);
   var profile=new[]{new Vector2(0,0),new Vector2(.055f,0),new Vector2(.06f,.03f),new Vector2(.049f,.24f),new Vector2(.085f,.29f),new Vector2(.085f,.37f),new Vector2(.073f,.38f),new Vector2(0,.38f)};
   Undo.AddComponent<MeshFilter>(model.gameObject).sharedMesh=meshes.Lathe("Torch_Body",profile,24);Undo.AddComponent<MeshRenderer>(model.gameObject).sharedMaterial=materials["Black"];
   var lens=New("Torch_Lens",model);lens.localPosition=new Vector3(0,.382f,0);Undo.AddComponent<MeshFilter>(lens.gameObject).sharedMesh=meshes.Box(new Vector3(.11f,.008f,.11f),.003f);Undo.AddComponent<MeshRenderer>(lens.gameObject).sharedMaterial=materials["Steel"];
   if(held)foreach(var r in parent.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
  }
  static AudioSource CueSound(string file,Vector3 p)
  {
   var s=Undo.AddComponent<AudioSource>(New(file+"_Spatial",interactive).gameObject);s.transform.position=p;s.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Assets+file+".wav");s.playOnAwake=false;s.loop=false;s.spatialBlend=1;s.volume=1;s.minDistance=5;s.maxDistance=26;s.rolloffMode=AudioRolloffMode.Linear;s.priority=32;return s;
  }
  static void CampusEvents()
  {
   var cues=new List<FECampusRun.Cue>();
   if(experience.hangingPanel!=null){Undo.SetTransformParent(experience.hangingPanel,interactive,"Restore atrium panel");experience.hangingPanel=null;}
   CommonApproachEvents(cues);
   var doorRoot=New("Pressure_Fire_Door",interactive);doorRoot.position=new Vector3(57,0,11);
   var door=Undo.AddComponent<FEPressureDoor>(doorRoot.gameObject);door.campus=campus;door.swingArea=new Bounds(new Vector3(55.4f,1,13),new Vector3(4.7f,3,5.4f));
   var hinge=New("Pressure_Door_Hinge",doorRoot);door.hinge=hinge;
   var leaf=B("Fire_Door_Leaf",new Vector3(57,1.3f,13),new Vector3(.12f,2.6f,4),"Blue",hinge,true);door.leaf=leaf.GetComponent<Collider>();
   B("Door_Push_Bar",new Vector3(56.89f,1.1f,13),new Vector3(.07f,.075f,3.4f),"Steel",hinge);
   Sign("FIRE DOOR / E TO OPEN",new Vector3(56.88f,1.7f,13),Quaternion.Euler(0,90,0),2.5f,hinge);
   foreach(float z in new[]{10.5f,15.5f})B("Fire_Door_Partition",new Vector3(57,1.9f,z),new Vector3(.18f,3.8f,1),"Wall",architecture,true);
   B("Fire_Door_Lintel",new Vector3(57,3.2f,13),new Vector3(.18f,1.2f,4),"Wall",architecture,true);door.ResetDoor();
   cues.Add(new FECampusRun.Cue{description="Pressure disturbance swung a fire door shut",approach=new Bounds(new Vector3(51.5f,1,13),new Vector3(2,3,5.5f)),pressureDoor=door,sound=CueSound("DoorSlam",new Vector3(57,1.5f,13))});campus.cues=cues.ToArray();
  }
  static void CommonApproachEvents(List<FECampusRun.Cue> cues)
  {
   // Both cues sit on the shared lab-to-junction approach, before any exit branch.
   var panel=B("Shifting_Ceiling_Section",new Vector3(0,3.18f,9),new Vector3(1.5f,.1f,1.65f),"Ceiling",interactive);
   foreach(float x in new[]{-.65f,.65f})B("Loose_Ceiling_Rail",new Vector3(x,3.12f,9),new Vector3(.045f,.06f,1.6f),"Steel",panel);
   var ceilingLight=Undo.AddComponent<Light>(New("Ceiling_Cue_Fill",interactive).gameObject);ceilingLight.transform.position=new Vector3(0,2.7f,7.7f);ceilingLight.type=LightType.Point;ceilingLight.range=4;ceilingLight.intensity=.6f;ceilingLight.color=new Color(.75f,.8f,1);ceilingLight.shadows=LightShadows.None;
   cues.Add(new FECampusRun.Cue{description="Ceiling section shifted above the corridor",warning="METAL CREAKING AHEAD - watch the ceiling",warningSeconds=.85f,
    approach=new Bounds(new Vector3(0,1,6),new Vector3(3.8f,3,4)),movingPart=panel,shift=new Vector3(.1f,-.4f,0),tilt=new Vector3(13,0,7),sound=CueSound("CeilingShift",panel.position),warningSound=CueSound("CeilingWarning",panel.position)});
   Vector3 pos=new Vector3(3.8f,1.5f,10.22f);
   B("Electrical_Panel_Box",pos,new Vector3(.95f,1.1f,.24f),"Steel",interactive);
   B("Electrical_Panel_Door",pos+Vector3.forward*.14f,new Vector3(.85f,1,.035f),"Trim",interactive);
   B("Electrical_Warning_Plate",pos+new Vector3(0,.2f,.17f),new Vector3(.34f,.32f,.02f),"Yellow",interactive);
   var lamp=Undo.AddComponent<Light>(New("Panel_Inspection_Lamp",interactive).gameObject);lamp.transform.position=pos+new Vector3(0,1,.8f);lamp.type=LightType.Point;lamp.intensity=1.5f;lamp.range=5;lamp.color=new Color(.68f,.78f,.9f);lamp.shadows=LightShadows.None;
   var arc=Undo.AddComponent<Light>(New("Panel_Arc_Flash",interactive).gameObject);arc.transform.position=pos+Vector3.forward*.4f;arc.type=LightType.Point;arc.range=5;arc.intensity=4;arc.color=new Color(.48f,.72f,1);arc.shadows=LightShadows.None;arc.enabled=false;
   var sparks=Undo.AddComponent<ParticleSystem>(New("Electrical_Arc_Burst",interactive).gameObject);sparks.transform.position=pos+Vector3.forward*.3f;sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=sparks.main;main.playOnAwake=false;main.loop=false;main.duration=3;main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.85f);main.startSpeed=new ParticleSystem.MinMaxCurve(1.5f,4.5f);main.startSize=new ParticleSystem.MinMaxCurve(.04f,.11f);main.startColor=new Color(1,.85f,.45f);main.gravityModifier=.7f;main.maxParticles=180;main.simulationSpace=ParticleSystemSimulationSpace.World;
   var emission=sparks.emission;emission.rateOverTime=12;emission.SetBursts(new[]{new ParticleSystem.Burst(0,55),new ParticleSystem.Burst(.95f,45)});
   var shape=sparks.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=40;shape.radius=.12f;
   var mat=new Material(Shader.Find("FireEscape/SmokeAndFire")){name="Panel_Sparks"};mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FireEscapeStarter/VisualAssets/Spark.png"));mat.SetColor("_Tint",new Color(4,3,1.5f));mat.SetFloat("_DstBlend",1);AssetDatabase.CreateAsset(mat,output+"/Panel_Sparks.mat");
   var renderer=sparks.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mat;renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
   cues.Add(new FECampusRun.Cue{description="Electrical panel arced; nearby lighting failed",warning="ELECTRICAL CRACKLING - panel ahead on the right",warningSeconds=.65f,approach=new Bounds(new Vector3(0,1,10.7f),new Vector3(3.8f,3,2.6f)),sparks=sparks,failedLight=lamp,arcLight=arc,sound=CueSound("PanelArc",sparks.transform.position)});
  }
  static void CampusFires()
  {
   var effects=emergency.emergencyRoot.transform;var template=effects.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name=="Side_Fire");if(template==null)throw new InvalidOperationException("Missing flame template.");
   var hazards=emergency.hazardAreas.Take(emergency.hazardAreas.Length-1).ToList();var times=emergency.hazardStartSeconds.Take(hazards.Count).ToList();var updated=new List<FEUniversityExperience.Stage>(experience.stages);
   foreach(var f in layout.newFires.Where(x=>x.z<18))
   {
    var fire=Object.Instantiate(template.gameObject,effects);fire.name="East_Gallery_Fire";Undo.RegisterCreatedObjectUndo(fire,"Gallery fire");fire.transform.position=new Vector3(f.x,f.y,f.z);
    fire.transform.localScale=new Vector3(1.35f,1.4f,1.45f);
    foreach(var ps in fire.GetComponentsInChildren<ParticleSystem>(true)){ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.scalingMode=ParticleSystemScalingMode.Hierarchy;if(ps.name=="Smoke")ps.transform.position=new Vector3(f.x,2.5f,f.z);}
    updated.Add(new FEUniversityExperience.Stage{target=fire,atSeconds=f.at});fire.SetActive(false);hazards.Add(new Bounds(new Vector3(f.x,1,f.z),new Vector3(f.sx,3,f.sz)));times.Add(f.at);
   }
   hazards.Add(emergency.hazardAreas[emergency.hazardAreas.Length-1]);times.Add(0);emergency.hazardAreas=hazards.ToArray();emergency.hazardStartSeconds=times.ToArray();
   var smoke=effects.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(x=>x.name=="Dense_Ceiling_Smoke");
   if(smoke!=null)foreach(float x in new[]{32f,39f,46f,53f,61f,67f})
   {
    var g=Object.Instantiate(smoke.gameObject,effects);Undo.RegisterCreatedObjectUndo(g,"Gallery smoke");g.name="East_Gallery_Dense_Smoke";g.transform.position=new Vector3(x,2.65f,13);
    var ps=g.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var shape=ps.shape;shape.scale=new Vector3(4,.15f,3);var em=ps.emission;em.rateOverTime=8;
    updated.Add(new FEUniversityExperience.Stage{target=g,atSeconds=2});g.SetActive(false);
   }
   experience.stages=updated.ToArray();atmosphere.fireLights=effects.GetComponentsInChildren<Light>(true);emergency.emergencyLights=atmosphere.fireLights;
   atmosphere.fireAudio=effects.GetComponentsInChildren<AudioSource>(true).Where(x=>x.clip!=null && x.clip.name=="FireLoop").ToArray();experience.spatialSounds=atmosphere.fireAudio.Concat(experience.roomSounds).Where(x=>x!=null).ToArray();
  }
 }
}
