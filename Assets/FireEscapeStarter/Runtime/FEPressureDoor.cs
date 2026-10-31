using UnityEngine;
namespace FireEscape
{
 public sealed class FEPressureDoor : MonoBehaviour
 {
  public FECampusRun campus;public Transform hinge;public Collider leaf;public Bounds swingArea;
  float angle=-90,target=-90;bool pressureClosed;
  public string Prompt=>target<0?"Fire door open":"E - Reopen fire door";
  public bool SafeToClose()=>campus!=null && !swingArea.Contains(campus.player.transform.position+Vector3.up*.9f);
  public void ResetDoor(){angle=target=-90;pressureClosed=false;if(hinge!=null)hinge.localRotation=Quaternion.Euler(0,angle,0);if(leaf!=null)leaf.enabled=false;}
  public void CloseFromPressure(){if(!SafeToClose())return;target=0;pressureClosed=true;if(leaf!=null)leaf.enabled=false;}
  public void Interact()
  {
   if(campus==null || !campus.emergency.IsRunning || target<0)return;target=-90;if(leaf!=null)leaf.enabled=false;
   if(pressureClosed){campus.Add("Reopened the pressure-closed fire door");campus.Notify("Door reopened. Check the passage ahead.");pressureClosed=false;}
  }
  void Update()
  {
   if(campus==null || !campus.emergency.IsRunning)return;
   if(target==0 && angle<0 && !SafeToClose()){target=-90;campus.Notify("Fire door obstructed; it remains open.");}
   angle=Mathf.MoveTowards(angle,target,Time.deltaTime*180);hinge.localRotation=Quaternion.Euler(0,angle,0);
   if(leaf!=null)leaf.enabled=target==0 && Mathf.Abs(angle)<.1f;
  }
 }
}
