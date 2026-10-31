using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace FireEscape
{
 public static partial class FEUniversityBuilder
 {
  static void CampusInteriors()
  {
   foreach(float x in new[]{28f,34f,40f,46f,52f,58f,64f,68f})
   {
    foreach(float z in new[]{10.15f,15.85f})
    {B("Gallery_Column",new Vector3(x,1.85f,z),new Vector3(.26f,3.7f,.22f),"Wall",details);B("Column_Steel_Shoe",new Vector3(x,.15f,z),new Vector3(.29f,.3f,.26f),"Steel",details);}
    B("Acoustic_Ceiling_Raft",new Vector3(x,3.58f,13),new Vector3(3.8f,.13f,4.1f),"Ceiling",details);
    foreach(float z in new[]{11f,15f})B("Unlit_Fluorescent_Batten",new Vector3(x,3.43f,z),new Vector3(2.4f,.065f,.17f),"White",details);
    B("Gallery_Floor_Inlay",new Vector3(x,.009f,13),new Vector3(.04f,.007f,5.6f),"Steel",details);
   }
   foreach(var w in layout.windows.Where(w=>w.z==10 || w.z==16))
   {
    foreach(float dx in new[]{-.94f,0,.94f})B("Window_Mullion",new Vector3(w.x+dx,1.93f,w.z),new Vector3(.045f,1.9f,.25f),"Trim",details);
    foreach(float y in new[]{.98f,2.87f})B("Window_Transom",new Vector3(w.x,y,w.z),new Vector3(1.94f,.065f,.25f),"Trim",details);
   }
   foreach(var spec in layout.doors.Where(d=>d.id.StartsWith("campus_")))
   {
    var t=New("Door_"+spec.id,interactive);t.SetPositionAndRotation(new Vector3(spec.x,0,spec.z),Quaternion.Euler(0,spec.yaw,0));
    var door=Undo.AddComponent<FEDoor>(t.gameObject);door.emergency=emergency;door.kind=FEDoor.DoorKind.Locked;
    string label=spec.z==10?(spec.x==37?"CH-301 / SEMINAR ROOM":"CH-302 / STUDENT COMMONS"):(spec.x==37?"CH-303 / COMPUTING LAB":"CH-304 / PROJECT STUDIO");door.roomName=label;
    var hinge=New("Hinge",t);door.hinge=hinge;
    var leaf=B("Timber_Door",new Vector3(spec.x,1.3f,spec.z),new Vector3(1.86f,2.6f,.13f),"Oak",hinge,true);leaf.rotation=t.rotation;door.leafCollider=leaf.GetComponent<Collider>();
    foreach(float dx in new[]{-1f,1f})B("Door_Frame",new Vector3(spec.x+dx,1.34f,spec.z),new Vector3(.1f,2.68f,.25f),"Trim",t);
    Sign(label,t.TransformPoint(new Vector3(0,2.96f,-.14f)),t.rotation,1.9f,t);
    B("Door_Kick_Plate",t.TransformPoint(new Vector3(0,.25f,-.085f)),new Vector3(1.7f,.32f,.025f),"Steel",hinge);
    B("Door_Handle",t.TransformPoint(new Vector3(.65f,1.05f,-.14f)),new Vector3(.25f,.045f,.065f),"Steel",hinge);
   }
   var interaction=emergency.player.GetComponent<FEInteriorInteraction>();if(interaction==null)interaction=root.GetComponentInChildren<FEInteriorInteraction>(true);if(interaction!=null)interaction.doors=root.GetComponentsInChildren<FEDoor>(true);
   foreach(float x in new[]{33f,37f,41f})foreach(float z in new[]{4.5f,7.2f})StudyDesk(new Vector3(x,0,z),false);
   foreach(float x in new[]{36f,40f})foreach(float z in new[]{18f,20.2f})StudyDesk(new Vector3(x,0,z),true);
   foreach(float x in new[]{51f,55f,59f})StudyDesk(new Vector3(x,0,21),false);
   Bench(new Vector3(52,0,4),3);Bench(new Vector3(60,0,4),3);
   B("Commons_Table",new Vector3(56,.42f,6),new Vector3(2,.12f,1),"Oak",details);B("Commons_Table_Base",new Vector3(56,.2f,6),new Vector3(.5f,.4f,.5f),"Steel",details);
   B("Seminar_Whiteboard",new Vector3(37,1.7f,2.13f),new Vector3(3,1.3f,.06f),"White",details);
   Sign("RESEARCH SHOWCASE",new Vector3(56,1.65f,25.86f),Quaternion.identity,2.7f,details);
   Bench(new Vector3(29,0,15.5f),2.3f);
   B("Overturned_Cupboard",new Vector3(40,.8f,11.6f),new Vector3(2,1.6f,2.8f),"Char",details);
   foreach(float x in new[]{39.4f,40.1f,40.7f})B("Broken_Shelving",new Vector3(x,1.68f,11.6f),new Vector3(.08f,.09f,2.6f),"Steel",details);
   for(int i=0;i<6;i++){var chunk=B("Gallery_Fallen_Plaster",new Vector3(50.3f+i*.26f,.18f+(i%2)*.1f,14.5f),new Vector3(.8f,.3f,1.9f),"Concrete",details);chunk.rotation=Quaternion.Euler(i*3,i*13,i*5);}
   B("Overturned_Equipment_Trolley",new Vector3(61,.43f,11.4f),new Vector3(2,.85f,2.2f),"Steel",details);
   foreach(var p in new[]{new Vector3(40,.012f,11.6f),new Vector3(51,.012f,14.5f),new Vector3(61,.012f,11.4f)})Quad("Gallery_Soot",p,Quaternion.Euler(90,0,0),new Vector2(4,4),materials["Soot"],details);
   foreach(float z in new[]{10.15f,15.85f})
   {
    B("Entrance_Jamb",new Vector3(69.9f,1.9f,z),new Vector3(.25f,3.8f,.22f),"Bronze",details);
    B("Entrance_Open_Leaf",new Vector3(70.7f,1.3f,z),new Vector3(1.5f,2.6f,.1f),"Glass",details);
    B("Entrance_Panic_Bar",new Vector3(70.7f,1.1f,z+(z<13?.08f:-.08f)),new Vector3(1.15f,.07f,.06f),"Steel",details);
   }
   var oldArchitecture=experience.transform.Find("Architecture");
   if(oldArchitecture!=null)foreach(var t in oldArchitecture.GetComponentsInChildren<Transform>(true))
    if(t.name=="Wall_Wall" && Mathf.Abs(t.position.z-50)<.15f && t.position.x>32 && t.position.x<44)t.gameObject.SetActive(false);
   for(float x=33;x<44;x+=2)
   {
    B("Atrium_Glazed_Wall",new Vector3(x,3.1f,50),new Vector3(2,6.2f,.12f),"Glass",details,true);
    B("Atrium_Vertical_Mullion",new Vector3(x-1,3.1f,49.9f),new Vector3(.09f,6.2f,.2f),"Bronze",details);
    foreach(float y in new[]{.1f,3.2f,6.2f})B("Atrium_Glass_Transom",new Vector3(x,y,49.9f),new Vector3(2,.09f,.2f),"Bronze",details);
   }
   foreach(float x in new[]{-14f,-6f,6f,16f})
   {B("Corridor_Ceiling_Raft",new Vector3(x,3.1f,12),new Vector3(3.2f,.09f,2.8f),"Ceiling",details);B("Ceiling_Linear_Grille",new Vector3(x,3.04f,12),new Vector3(2.2f,.025f,.2f),"Trim",details);}
  }
  static void StudyDesk(Vector3 p,bool computer)
  {
   B("Study_Desk",p+Vector3.up*.76f,new Vector3(2.3f,.08f,.9f),"Oak",details);
   foreach(float x in new[]{-.95f,.95f})B("Desk_Frame",p+new Vector3(x,.36f,0),new Vector3(.05f,.72f,.7f),"Steel",details);
   foreach(float x in new[]{-.6f,.6f})
   {
    B("Student_Chair_Seat",p+new Vector3(x,.43f,.65f),new Vector3(.44f,.09f,.42f),"Blue",details);B("Student_Chair_Back",p+new Vector3(x,.69f,.83f),new Vector3(.44f,.46f,.07f),"Blue",details);
    foreach(float dx in new[]{-.17f,.17f})B("Chair_Leg",p+new Vector3(x+dx,.21f,.65f),new Vector3(.04f,.42f,.32f),"Steel",details);
   }
   if(computer)
   {B("Monitor_Stand",p+Vector3.up*.9f,new Vector3(.12f,.25f,.15f),"Steel",details);B("Monitor",p+new Vector3(0,1.14f,-.1f),new Vector3(.75f,.45f,.06f),"Black",details);B("Keyboard",p+new Vector3(0,.82f,.19f),new Vector3(.5f,.025f,.16f),"Black",details);}
   else B("Student_Notebook",p+new Vector3(.5f,.82f,.05f),new Vector3(.28f,.028f,.22f),"White",details);
  }
  static void CampusExterior()
  {
   B("Campus_Ground",new Vector3(35,-.25f,25),new Vector3(190,.18f,150),"CampusPaving",details);
   B("Emergency_Access_Road",new Vector3(90,-.14f,27),new Vector3(11,.04f,100),"Asphalt",details);
   for(float z=-18;z<78;z+=6)B("Road_Centre_Marking",new Vector3(90,-.112f,z),new Vector3(.12f,.009f,2.8f),"White",details);
   foreach(float x in new[]{84.5f,95.5f})B("Kerb",new Vector3(x,-.02f,27),new Vector3(.3f,.22f,100),"Concrete",details);
   BackgroundBuilding(new Vector3(112,0,35),new Vector3(26,13,54),"ENGINEERING FACULTY");
   BackgroundBuilding(new Vector3(39,0,72),new Vector3(56,10,18),"UNIVERSITY LIBRARY");
   BackgroundBuilding(new Vector3(32,0,-25),new Vector3(60,10,17),"SCIENCE & TECHNOLOGY");
   B("Entrance_Canopy",new Vector3(72.2f,4.15f,13),new Vector3(5,.25f,9),"Concrete",details);
   for(float z=9;z<18;z+=.65f)B("Bronze_Canopy_Fin",new Vector3(72.2f,4.45f,z),new Vector3(4.8f,.62f,.075f),"Bronze",details);
   foreach(float z in new[]{9f,17f})B("Canopy_Column",new Vector3(74,2,z),new Vector3(.16f,4,.16f),"Steel",details);
   B("University_Facade_Sign_Back",new Vector3(70.3f,5.1f,13),new Vector3(.2f,1.1f,9),"Trim",details);
   Sign("NORTHBRIDGE UNIVERSITY",new Vector3(70.42f,5.1f,13),Quaternion.Euler(0,-90,0),7.6f,details);
   foreach(var a in layout.areas.Where(x=>x.id=="seminar" || x.id=="learning" || x.id=="lab" || x.id=="solvent" || x.id=="office"))
    for(float x=a.x0+1;x<a.x1;x+=2)
    {B("External_Brick_Panel",new Vector3(x,1.65f,a.z0-.14f),new Vector3(2,3.3f,.15f),"CampusBrick",details);B("Facade_Parapet_Coping",new Vector3(x,3.4f,a.z0-.14f),new Vector3(2,.18f,.3f),"Concrete",details);}
   foreach(var p in new[]{new Vector3(81,0,20),new Vector3(81,0,29),new Vector3(73,0,46),new Vector3(24,0,59),new Vector3(50,0,60)})
   {
    B("Campus_Planter",p+Vector3.up*.35f,new Vector3(2.6f,.7f,1.3f),"Concrete",details,true);B("Planter_Soil",p+Vector3.up*.72f,new Vector3(2.35f,.05f,1.1f),"Char",details);
    for(int i=0;i<5;i++)Round("Shrub",p+new Vector3(-.9f+i*.45f,1,.04f),new Vector3(.6f,.65f,.9f),"Foliage",details);
   }
   foreach(var p in new[]{new Vector3(82,0,7),new Vector3(77,0,53),new Vector3(21,0,58),new Vector3(54,0,60)})
   {B("Tree_Trunk",p+Vector3.up*1.6f,new Vector3(.24f,3.2f,.24f),"Oak",details);Round("Tree_Crown",p+Vector3.up*4,new Vector3(3.8f,4.4f,3.5f),"Foliage",details);Round("Tree_Crown",p+new Vector3(1,3.8f,.2f),new Vector3(2.7f,3.4f,3),"Foliage",details);}
   Bench(new Vector3(81,0,25),2.5f);
   for(int i=0;i<5;i++){float z=18+i*.85f;B("Cycle_Rack_Top",new Vector3(72.5f,.8f,z),new Vector3(.8f,.055f,.055f),"Steel",details);foreach(float x in new[]{72.1f,72.9f})B("Cycle_Rack_Leg",new Vector3(x,.4f,z),new Vector3(.055f,.8f,.055f),"Steel",details);}
   var beacons=new List<Light>();FireTruck(new Vector3(89,0,36),beacons);FireTruck(new Vector3(89,0,49),beacons);campus.truckBeacons=beacons.ToArray();
   Firefighter(new Vector3(79,0,42.6f),Quaternion.Euler(0,190,0));Firefighter(new Vector3(81,0,39),Quaternion.Euler(0,250,0));Firefighter(new Vector3(85.5f,0,36),Quaternion.Euler(0,270,0));
   Sign("ASSEMBLY / NORTH COURT",new Vector3(79,2.7f,43),Quaternion.identity,3.6f,details,true);
  }
  static void BackgroundBuilding(Vector3 p,Vector3 size,string label)
  {
   B("Academic_Building_Mass",p+Vector3.up*size.y/2,size,"CampusBrick",details);
   for(int floor=0;floor<Mathf.FloorToInt(size.y/3.2f);floor++)
   {
    float y=1.8f+floor*3.2f;
    foreach(float side in new[]{-1f,1f})
    {
     for(float x=-size.x/2+2;x<size.x/2-1;x+=3)
     {B("Campus_Window_Frame",p+new Vector3(x,y,side*(size.z/2+.06f)),new Vector3(2.1f,1.75f,.14f),"Trim",details);B("Campus_Window_Glass",p+new Vector3(x,y,side*(size.z/2+.14f)),new Vector3(1.94f,1.57f,.02f),"Glass",details);}
     for(float z=-size.z/2+2;z<size.z/2-1;z+=3)
     {B("Campus_Window_Frame",p+new Vector3(side*(size.x/2+.06f),y,z),new Vector3(.14f,1.75f,2.1f),"Trim",details);B("Campus_Window_Glass",p+new Vector3(side*(size.x/2+.14f),y,z),new Vector3(.02f,1.57f,1.94f),"Glass",details);}
    }
    B("Facade_Concrete_Band",p+new Vector3(0,y+1.3f,0),new Vector3(size.x+.15f,.18f,size.z+.15f),"Concrete",details);
   }
   B("Roof_Parapet",p+Vector3.up*(size.y+.1f),new Vector3(size.x+.3f,.25f,size.z+.3f),"Concrete",details);
   Sign(label,p+new Vector3(0,3.2f,-size.z/2-.2f),Quaternion.identity,9,details);
  }
  static Transform Round(string name,Vector3 pos,Vector3 scale,string mat,Transform parent)
  {
   var t=New(name,parent);t.position=pos;t.localScale=scale;
   var profile=new List<Vector2>();for(int i=0;i<=12;i++){float a=-Mathf.PI/2+i*Mathf.PI/12;profile.Add(new Vector2(Mathf.Max(.001f,Mathf.Cos(a)*.5f),Mathf.Sin(a)*.5f));}
   Undo.AddComponent<MeshFilter>(t.gameObject).sharedMesh=meshes.Lathe("Campus_Rounded",profile.ToArray(),16);Undo.AddComponent<MeshRenderer>(t.gameObject).sharedMaterial=materials[mat];return t;
  }
  static void FireTruck(Vector3 p,List<Light> beacons)
  {
   B("Fire_Engine_Chassis",p+Vector3.up*.65f,new Vector3(2.5f,.38f,7.3f),"Black",details);
   B("Fire_Engine_Cab",p+new Vector3(0,1.65f,-2.5f),new Vector3(2.45f,2.1f,2.2f),"Red",details);
   B("Cab_Windscreen",p+new Vector3(0,2.12f,-3.62f),new Vector3(2.13f,.92f,.025f),"Glass",details);
   B("Front_Bumper",p+new Vector3(0,.7f,-3.72f),new Vector3(2.55f,.26f,.26f),"Steel",details);
   B("Front_Grille",p+new Vector3(0,1.15f,-3.63f),new Vector3(1.35f,.4f,.03f),"Trim",details);
   B("Equipment_Body",p+new Vector3(0,1.65f,1.35f),new Vector3(2.4f,2,4.5f),"Red",details);
   foreach(float side in new[]{-1f,1f})
   {
    B("Cab_Side_Window",p+new Vector3(side*1.235f,2.1f,-2.45f),new Vector3(.025f,.82f,1.5f),"Glass",details);
    B("High_Visibility_Stripe",p+new Vector3(side*1.23f,1.3f,.4f),new Vector3(.028f,.16f,6.1f),"ReflectiveStripe",details);
    foreach(float z in new[]{-.1f,1.4f,2.9f})
    {B("Roller_Shutter",p+new Vector3(side*1.215f,1.92f,z),new Vector3(.03f,1.3f,1.25f),"Steel",details);for(float y=1.35f;y<2.5f;y+=.12f)B("Shutter_Slat",p+new Vector3(side*1.24f,y,z),new Vector3(.025f,.016f,1.2f),"Trim",details);}
    foreach(float z in new[]{-2.5f,2.1f})
    {Round("Tyre",p+new Vector3(side*1.25f,.57f,z),new Vector3(.42f,1.05f,1.05f),"Black",details);Round("Wheel_Hub",p+new Vector3(side*1.47f,.57f,z),new Vector3(.05f,.57f,.57f),"Steel",details);}
    B("Roof_Ladder_Rail",p+new Vector3(side*.43f,2.78f,.9f),new Vector3(.07f,.1f,5),"Steel",details);
   }
   for(float z=-1.4f;z<3.4f;z+=.4f)B("Ladder_Rung",p+new Vector3(0,2.78f,z),new Vector3(.86f,.06f,.06f),"Steel",details);
   foreach(float x in new[]{-.8f,.8f})
   {
    B("Blue_Beacon_Lens",p+new Vector3(x,2.83f,-2.5f),new Vector3(.55f,.2f,.28f),"BlueLens",details);
    var l=Undo.AddComponent<Light>(New("Truck_Blue_Beacon",interactive).gameObject);l.transform.position=p+new Vector3(x,2.96f,-2.5f);l.type=LightType.Point;l.color=new Color(.08f,.27f,1);l.range=12;l.shadows=LightShadows.None;beacons.Add(l);
   }
   Sign("FIRE & RESCUE",p+new Vector3(-1.25f,1.65f,-2.5f),Quaternion.Euler(0,90,0),1.4f,details);
  }
  static void Firefighter(Vector3 p,Quaternion rotation)
  {
   var person=New("Stationary_Firefighter",details);person.position=p;
   Round("Protective_Jacket",p+Vector3.up*1.2f,new Vector3(.58f,.72f,.35f),"Uniform",person);
   foreach(float x in new[]{-.15f,.15f})
   {Round("Trouser_Leg",p+new Vector3(x,.52f,0),new Vector3(.23f,.85f,.25f),"Uniform",person);B("Protective_Boot",p+new Vector3(x,.12f,-.06f),new Vector3(.24f,.24f,.39f),"Black",person);B("Trouser_Reflective_Band",p+new Vector3(x,.35f,-.13f),new Vector3(.22f,.055f,.02f),"ReflectiveStripe",person);}
   foreach(float x in new[]{-.35f,.35f})
   {var arm=Round("Jacket_Sleeve",p+new Vector3(x,1.12f,0),new Vector3(.22f,.66f,.23f),"Uniform",person);arm.rotation=Quaternion.Euler(0,0,x>0?12:-12);Round("Glove",p+new Vector3(x*1.13f,.79f,0),new Vector3(.15f,.19f,.16f),"Black",person);}
   Round("Head",p+Vector3.up*1.69f,new Vector3(.25f,.3f,.26f),"Skin",person);
   Round("Helmet_Crown",p+Vector3.up*1.85f,new Vector3(.36f,.23f,.37f),"Helmet",person);B("Helmet_Brim",p+new Vector3(0,1.77f,-.025f),new Vector3(.39f,.04f,.43f),"Helmet",person);
   Round("Breathing_Mask",p+new Vector3(0,1.65f,-.15f),new Vector3(.23f,.2f,.1f),"Black",person);Round("Visor",p+new Vector3(0,1.73f,-.145f),new Vector3(.26f,.17f,.1f),"Glass",person);
   B("Jacket_Reflective_Band",p+new Vector3(0,1.04f,-.18f),new Vector3(.49f,.065f,.02f),"ReflectiveStripe",person);
   foreach(float x in new[]{-.15f,.15f})B("Jacket_Reflective_Strip",p+new Vector3(x,1.3f,-.17f),new Vector3(.055f,.35f,.02f),"ReflectiveStripe",person);
   Round("Air_Cylinder",p+new Vector3(0,1.25f,.26f),new Vector3(.28f,.62f,.25f),"Yellow",person);person.rotation=rotation;
  }
 }
}
