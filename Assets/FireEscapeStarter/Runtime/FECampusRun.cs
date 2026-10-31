using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace FireEscape
{
 [DefaultExecutionOrder(150)]
 public sealed class FECampusRun : MonoBehaviour
 {
  [Serializable] public sealed class Cue
  {
   public string description,warning; public float warningSeconds=.8f; public AudioSource warningSound; public Light arcLight; public Bounds approach; public Transform movingPart; public Vector3 shift,tilt;
   public ParticleSystem sparks; public Light failedLight; public FEPressureDoor pressureDoor; public AudioSource sound;
   [NonSerialized] public bool armed,fired; [NonSerialized] public float delay,progress,effectTime;
   [NonSerialized] public Vector3 rest; [NonSerialized] public Quaternion rotation;
  }
  public FEEmergency emergency; public FEFirstPerson player;
  public GameObject torchPickup,heldTorch; public Light torchLight; public float batterySeconds=85;
  public int refinementVersion;
  public Cue[] cues=Array.Empty<Cue>(); public Light[] truckBeacons=Array.Empty<Light>();
  public bool HasTorch {get;private set;} public bool TorchUsed {get;private set;} public bool TorchOn {get;private set;}
  public float Battery {get;private set;}
  public readonly List<string> timeline=new List<string>(); public readonly List<string> visitedAreas=new List<string>();
  bool wasMap,wasCollapse,wasExit2,wasExit4,wasDamage,wasEnded,wasPaused;int lastExit;
  float noticeUntil;string notice="";
  public string Notice=>emergency!=null && emergency.Elapsed<noticeUntil?notice:"";
  public string Objective
  {
   get {
    if(emergency.ExitUsed>0 && player.transform.position.x>=70)return "REACH ASSEMBLY";
    if(emergency.Collapsed || emergency.VisitedExit2)return "FIND AN ALTERNATIVE";
    if(emergency.MapStudied || player.transform.position.z>10)return "ASSESS AN EXIT";
    return "INSPECT THE MAP";
   }
  }
  public int EventsSeen {get {int n=0;foreach(var c in cues)if(c.fired)n++;return n;}}
  void Start()
  {
   foreach(var c in cues)if(c.movingPart!=null){c.rest=c.movingPart.localPosition;c.rotation=c.movingPart.localRotation;}
   emergency.RunReset+=ResetRun;ResetRun();
  }
  void OnDestroy(){if(emergency!=null)emergency.RunReset-=ResetRun;}
  void ResetRun()
  {
   HasTorch=TorchUsed=TorchOn=false;Battery=batterySeconds;timeline.Clear();visitedAreas.Clear();
   wasMap=wasCollapse=wasExit2=wasExit4=wasDamage=wasEnded=wasPaused=false;lastExit=0;notice="";noticeUntil=0;
   if(torchPickup!=null)torchPickup.SetActive(true);if(heldTorch!=null)heldTorch.SetActive(false);if(torchLight!=null)torchLight.enabled=false;
   foreach(var c in cues)
   {
    c.armed=c.fired=false;c.progress=c.effectTime=0;c.delay=string.IsNullOrEmpty(c.warning)?UnityEngine.Random.Range(.15f,.8f):c.warningSeconds;
    if(c.movingPart!=null){c.movingPart.localPosition=c.rest;c.movingPart.localRotation=c.rotation;}
    if(c.sparks!=null)c.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
    if(c.failedLight!=null)c.failedLight.enabled=true;if(c.arcLight!=null)c.arcLight.enabled=false;
    if(c.warningSound!=null)c.warningSound.Stop();
    if(c.pressureDoor!=null)c.pressureDoor.ResetDoor();if(c.sound!=null)c.sound.Stop();
   }
  }
  public void CollectTorch()
  {
   if(!emergency.IsRunning || HasTorch)return;HasTorch=true;
   if(torchPickup!=null)torchPickup.SetActive(false);if(heldTorch!=null)heldTorch.SetActive(true);
   Add("Collected the laboratory torch");Notify("Torch equipped. Press F to switch it on. Battery drains only while lit.");
  }
  public void Add(string text){if(timeline.Count<40)timeline.Add(emergency.Elapsed.ToString("F0")+"s  "+text);}
  public void Notify(string text){notice=text;noticeUntil=emergency.Elapsed+5;}
  public string Zone()
  {
   Vector3 p=player.transform.position;
   if(p.x>=70)return p.z<32?"Campus promenade":"Assembly courtyard";
   if(p.x>=26 && p.z<18)return "East teaching gallery";
   if(p.x>=66)return "Stair lobby";
   if(p.x>=46)return p.z>42?"North teaching wing":"Service passage";
   if(p.x>=30 && p.z>=30)return "Science atrium";
   return p.z<2?"Chemistry laboratory":"Chemistry wing";
  }
  void Update()
  {
   if(emergency==null || player==null)return;float clock=emergency.Elapsed;
   for(int i=0;i<truckBeacons.Length;i++)if(truckBeacons[i]!=null)truckBeacons[i].intensity=Mathf.Repeat(clock*2.7f+i*.5f,1)<.22f?3.5f:.12f;
   if(emergency.IsPaused)
   {
    if(!wasPaused)foreach(var c in cues){if(c.sound!=null)c.sound.Pause();if(c.warningSound!=null)c.warningSound.Pause();if(c.sparks!=null && c.sparks.isPlaying)c.sparks.Pause(true);}wasPaused=true;return;
   }
   if(wasPaused){foreach(var c in cues){if(c.sound!=null)c.sound.UnPause();if(c.warningSound!=null)c.warningSound.UnPause();if(c.sparks!=null && c.sparks.isPaused)c.sparks.Play(true);}wasPaused=false;}
   if(emergency.HasEnded)
   {
    if(!wasEnded){Add(emergency.Escaped?"Reached assembly via Exit "+emergency.ExitUsed:"Attempt ended");wasEnded=true;foreach(var c in cues){if(c.sound!=null)c.sound.Stop();if(c.warningSound!=null)c.warningSound.Stop();if(c.arcLight!=null)c.arcLight.enabled=false;if(c.sparks!=null)c.sparks.Stop(true,ParticleSystemStopBehavior.StopEmitting);}}
    if(torchLight!=null)torchLight.enabled=false;return;
   }
   if(!emergency.IsRunning)return;
   if(emergency.MapStudied && !wasMap){Add("Inspected an evacuation map");wasMap=true;}
   if(emergency.Collapsed && !wasCollapse){Add("Exit 1 collapsed; route blocked");wasCollapse=true;Notify("Exit 1 is blocked. Reassess your route on a wall map.");}
   if(emergency.VisitedExit2 && !wasExit2){Add("Reached the blocked Exit 2 approach");wasExit2=true;Notify("Exit 2 is blocked. Find another exit.");}
   if(emergency.VisitedExit4 && !wasExit4){Add("Entered the East Gallery / Exit 4 route");wasExit4=true;}
   if(emergency.ExitUsed!=lastExit){lastExit=emergency.ExitUsed;if(lastExit>0)Add("Left the building through Exit "+lastExit);}
   if(emergency.IsTakingDamage && !wasDamage)Add("Entered a fire damage zone");wasDamage=emergency.IsTakingDamage;
   string zone=Zone();if(!visitedAreas.Contains(zone))visitedAreas.Add(zone);
#if ENABLE_INPUT_SYSTEM
   if(HasTorch && !emergency.inspectionActive && Keyboard.current!=null && Keyboard.current.fKey.wasPressedThisFrame && Battery>0)
   {TorchOn=!TorchOn;if(TorchOn && !TorchUsed){TorchUsed=true;Add("Used the torch");}}
#endif
   if(TorchOn){Battery=Mathf.Max(0,Battery-Time.deltaTime);if(Battery<=0){TorchOn=false;Add("Torch battery depleted");Notify("Torch battery empty. Follow the illuminated exit signs.");}}
   if(torchLight!=null){torchLight.enabled=TorchOn;torchLight.intensity=Battery<12?Mathf.Lerp(1.4f,3.4f,Battery/12):3.4f;}
   Vector3 pos=player.transform.position+Vector3.up*.9f;
   foreach(var c in cues)
   {
    if(!c.armed && c.approach.Contains(pos))
    {c.armed=true;if(!string.IsNullOrEmpty(c.warning))Notify(c.warning);if(c.warningSound!=null)c.warningSound.Play();}
    if(c.armed && !c.fired)
    {
     c.delay-=Time.deltaTime;if(c.delay>0 || (c.pressureDoor!=null && !c.pressureDoor.SafeToClose()))continue;
     c.fired=true;c.effectTime=0;Add(c.description);Notify(c.description);
     if(c.sound!=null)c.sound.Play();if(c.sparks!=null)c.sparks.Play(true);
     if(c.pressureDoor!=null)c.pressureDoor.CloseFromPressure();
    }
    if(c.fired)
    {
     c.effectTime+=Time.deltaTime;
     // Two separated arc bursts, then the nearby supply light stays off.
     bool pulse=c.effectTime<.32f || (c.effectTime>.95f && c.effectTime<1.3f);
     if(c.arcLight!=null)c.arcLight.enabled=pulse;
     if(c.failedLight!=null)c.failedLight.enabled=c.effectTime<1.5f && !pulse;
    }
    if(c.fired && c.movingPart!=null)
    {c.progress=Mathf.Min(1,c.progress+Time.deltaTime/.95f);float t=Mathf.SmoothStep(0,1,c.progress);c.movingPart.localPosition=c.rest+c.shift*t;c.movingPart.localRotation=c.rotation*Quaternion.Euler(c.tilt*t);}
   }
  }
 }
}
