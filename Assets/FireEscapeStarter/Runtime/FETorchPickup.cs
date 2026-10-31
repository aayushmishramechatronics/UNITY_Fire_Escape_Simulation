using UnityEngine;
namespace FireEscape
{
 public sealed class FETorchPickup : MonoBehaviour
 {
  public FECampusRun campus;
  public void Collect(){if(campus!=null)campus.CollectTorch();}
 }
}
