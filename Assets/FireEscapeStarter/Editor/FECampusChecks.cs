using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace FireEscape
{
 public static class FECampusChecks
 {
  [MenuItem("Fire Escape/Check Phase 6 Setup")]
  public static void Check()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   var c=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FECampusRun>(true)).FirstOrDefault();
   if(c==null){EditorUtility.DisplayDialog("Phase 6 check","Open the upgraded scene and apply Phase 6 first.","OK");return;}
   var errors=new List<string>();var e=c.emergency;
   if(e==null || c.player==null)errors.Add("Missing emergency manager or player.");
   else
   {
    if(!e.campusMode)errors.Add("Campus mode is off.");
    int expected=c.refinementVersion>=1?20:16;
    if(e.hazardAreas.Length!=expected || e.hazardStartSeconds.Length!=expected)errors.Add("Expected "+expected+" matching hazards and start times.");
    if(c.player.viewCamera==null)errors.Add("Missing view camera.");
   }
   if(c.torchPickup==null || c.heldTorch==null || c.torchLight==null)errors.Add("Torch references are incomplete.");
   if(c.cues==null || c.cues.Length!=3)errors.Add("Expected three event cues.");
   else foreach(var q in c.cues)
   {if(q.sound==null || q.sound.clip==null)errors.Add("Missing event audio: "+q.description);}
   if(c.refinementVersion>=1)
   {
    if(c.transform.Find("Interactive_Props/Lab_Tools_Cabinet")==null)errors.Add("Missing lab tools cabinet.");
    if(c.cues.Length==3 && (c.cues[0].warningSound==null || c.cues[1].arcLight==null))errors.Add("Missing ceiling warning or electrical arc light.");
   }
   if(c.truckBeacons==null || c.truckBeacons.Length!=4)errors.Add("Expected four truck beacons.");
   var plans=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FEWallPlan>(true)).ToArray();
   if(plans.Length<4 || plans.Any(p=>p.plan==null || !p.plan.name.StartsWith("CampusPlan_")))errors.Add("An evacuation plan was not updated.");
   foreach(string shaderName in new[]{"Universal Render Pipeline/Lit","FireEscape/SurfaceDecal","FireEscape/SmokeAndFire"})
   {var shader=Shader.Find(shaderName);if(shader==null || ShaderUtil.ShaderHasError(shader))errors.Add("Missing or invalid shader: "+shaderName);}
   Physics.SyncTransforms();
   if(Physics.Linecast(new Vector3(25,1,13),new Vector3(27,1,13),out RaycastHit opening,~0,QueryTriggerInteraction.Ignore))errors.Add("Gallery opening is obstructed by "+opening.collider.name);
   if(Physics.Linecast(new Vector3(77,1,31),new Vector3(77,1,33),out RaycastHit court,~0,QueryTriggerInteraction.Ignore))errors.Add("Promenade connection is obstructed by "+court.collider.name);
   string result=errors.Count==0?"Scene references and both new openings passed.\n\nNext: Play through each exit, test torch pickup, pause/restart, and inspect the debrief. This check does not measure FPS or visual quality.":string.Join("\n",errors);
   if(errors.Count==0)Debug.Log("Phase 6 setup check passed.");else Debug.LogError(result);
   EditorUtility.DisplayDialog("Phase 6 check",result,"OK");
  }
 }
}
