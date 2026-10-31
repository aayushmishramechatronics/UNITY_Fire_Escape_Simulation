using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace FireEscape
{
 public static partial class FEUniversityBuilder
 {
  [MenuItem("Fire Escape/Apply Phase 6.1 - Fire Equipment and Events")]
  public static void RefineCampus()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Stop Play mode first.");return;}
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   var department=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Department_Interior_V3");
   campus=department==null?null:department.GetComponentInChildren<FECampusRun>(true);
   if(campus==null || string.IsNullOrEmpty(scene.path)){Tell("Open your working Phase 6 scene first. This command upgrades it in place.");return;}
   if(campus.refinementVersion>=1){Tell("Phase 6.1 is already applied. Press Play to test it.");return;}
   experience=department.GetComponentInChildren<FEUniversityExperience>(true);atmosphere=department.GetComponentInChildren<FEAtmosphere>(true);emergency=campus.emergency;
   if(experience==null || atmosphere==null || emergency==null || emergency.hazardAreas.Length!=16 || emergency.hazardStartSeconds.Length!=16 || campus.cues.Length!=3)
   {Tell("Expected the working Phase 6 scene with 16 hazards and three events. No changes made.");return;}
   try{PreflightCampusAssets();layout=JsonUtility.FromJson<Layout>(TexData("CampusLayout.json"));ValidateLayout();}
   catch(Exception ex){Debug.LogException(ex);Tell("No scene changes made: "+ex.Message);return;}
   Folder("Assets/_Project/Backups");string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Backups/Before_Campus_Phase61.unity");
   if(!EditorSceneManager.SaveScene(scene,backup,true)){Tell("Could not save backup. No changes made.");return;}
   Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Refine campus fire and event cues");
   Undo.RegisterFullObjectHierarchyUndo(department,"Campus refinement");Undo.RegisterFullObjectHierarchyUndo(emergency.player.gameObject,"Campus equipment refinement");
   try
   {
    root=department.transform;expansion=campus.transform;interactive=expansion.Find("Interactive_Props");details=expansion.Find("Campus_Details");architecture=expansion.Find("Architecture");
    if(interactive==null)throw new InvalidOperationException("Missing Interactive_Props folder.");
    output=AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/CampusRefinement61");Folder(output);Folder(output+"/Meshes");meshes=new FEVisualMeshes(output+"/Meshes");materials.Clear();MakeMaterials();
    if(campus.torchPickup!=null)Undo.DestroyObjectImmediate(campus.torchPickup);
    foreach(var t in interactive.GetComponentsInChildren<Transform>(true).ToArray())
     if(t!=null && (t.name=="Printed_TORCH / E TO COLLECT" || (t.name=="Plaque_Back" && Vector3.Distance(t.position,new Vector3(-4.5f,1.05f,-5.96f))<.1f)))Undo.DestroyObjectImmediate(t.gameObject);
    BuildTorchCabinet();
    // Preserve the existing gallery door; replace the two branch-specific cues.
    var doorCue=campus.cues[2];
    foreach(var cue in campus.cues.Take(2))
    {
     if(cue.sound!=null)Undo.DestroyObjectImmediate(cue.sound.gameObject);
     if(cue.sparks!=null)Undo.DestroyObjectImmediate(cue.sparks.gameObject);
     if(cue.failedLight!=null)Undo.DestroyObjectImmediate(cue.failedLight.gameObject);
    }
    var cues=new List<FECampusRun.Cue>();CommonApproachEvents(cues);cues.Add(doorCue);campus.cues=cues.ToArray();
    var remove=experience.stages.Where(s=>s.target!=null && (s.target.name=="East_Gallery_Fire" || s.target.name=="East_Gallery_Dense_Smoke")).ToArray();
    experience.stages=experience.stages.Except(remove).ToArray();foreach(var stage in remove)Undo.DestroyObjectImmediate(stage.target);
    // The first 12 hazards belong to Phase 5. Collapse always remains LAST.
    emergency.hazardAreas=emergency.hazardAreas.Take(12).Concat(new[]{emergency.hazardAreas.Last()}).ToArray();
    emergency.hazardStartSeconds=emergency.hazardStartSeconds.Take(12).Concat(new[]{0f}).ToArray();CampusFires();
    foreach(var plan in root.GetComponentsInChildren<FEWallPlan>(true))if(plan.plan!=null)EditorUtility.SetDirty(plan);
    campus.refinementVersion=1;experience.backupScene=backup;AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
    if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save upgraded scene.");
    Undo.CollapseUndoOperations(group);Tell("Phase 6.1 installed.\n\nSeven Exit 4 fires, a lab TOOLS cabinet, shared-approach events and building-only wall maps.\n\nBackup: "+backup);
   }
   catch(Exception ex){Undo.RevertAllDownToGroup(group);Debug.LogException(ex);Tell("Refinement stopped: "+ex.Message+"\n\nReopen the saved backup before retrying:\n"+backup);}
  }
  static string TexData(string file)=>AssetDatabase.LoadAssetAtPath<TextAsset>(Assets+file).text;
 }
}
