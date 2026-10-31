using System;
using System.Collections.Generic;
using UnityEngine;
namespace FireEscape
{
    [DefaultExecutionOrder(160)]
    public sealed class FEUniversityExperience : MonoBehaviour
    {
        [Serializable] public class Stage { public GameObject target; public float atSeconds; }
        public FEEmergency emergency;
        public FECampusRun campus;
        public FEFirstPerson player;
        public Texture2D debriefPlan;
        public Stage[] stages=Array.Empty<Stage>();
        public AudioSource[] spatialSounds=Array.Empty<AudioSource>();
        public AudioSource[] roomSounds=Array.Empty<AudioSource>();
        public Transform hangingPanel;
        public Vector3 panelRest;
        public Quaternion panelRotation;
        public string backupScene;
        Vector2 timelineScroll,feedbackScroll;
        AudioLowPassFilter[] filters;
        GUIStyle title,body,small,button;
        float occlusionClock;
        int soundIndex;
        bool shortPassage,northWing,ended,wasPaused;
        bool[] occluded;
        private void OnEnable(){if(emergency!=null)emergency.RunReset+=ResetRun;}
        private void OnDisable(){if(emergency!=null)emergency.RunReset-=ResetRun;foreach(var s in roomSounds)if(s!=null)s.Stop();}
        private void Start()
        {
            if(emergency!=null){emergency.RunReset-=ResetRun;emergency.RunReset+=ResetRun;}
            filters=new AudioLowPassFilter[spatialSounds.Length];occluded=new bool[spatialSounds.Length];
            for(int i=0;i<spatialSounds.Length;i++)if(spatialSounds[i]!=null)
            {
                filters[i]=spatialSounds[i].GetComponent<AudioLowPassFilter>();
                if(filters[i]==null)filters[i]=spatialSounds[i].gameObject.AddComponent<AudioLowPassFilter>();
                filters[i].cutoffFrequency=18000;
            }
            ResetRun();
        }
        private void ResetRun()
        {
            timelineScroll=feedbackScroll=Vector2.zero;shortPassage=northWing=ended=wasPaused=false;
            foreach(var stage in stages)if(stage.target!=null)stage.target.SetActive(false);
            foreach(var s in roomSounds)if(s!=null)s.Stop();
            if(hangingPanel!=null){hangingPanel.localPosition=panelRest;hangingPanel.localRotation=panelRotation;}
        }
        private void Update()
        {
            if(emergency==null || player==null)return;
            if(emergency.IsPaused)
            {if(!wasPaused)foreach(var s in roomSounds)if(s!=null)s.Pause();wasPaused=true;return;}
            if(wasPaused){foreach(var s in roomSounds)if(s!=null)s.UnPause();wasPaused=false;}
            if(emergency.HasEnded)
            {
                if(!ended){ended=true;foreach(var s in roomSounds)if(s!=null)s.Stop();}
                return;
            }
            if(!emergency.IsRunning && !emergency.IsExploding)return;
            float t=emergency.Elapsed;
            foreach(var stage in stages)if(stage.target!=null && !stage.target.activeSelf && t>=stage.atSeconds)
            {
                stage.target.SetActive(true);
                foreach(var p in stage.target.GetComponentsInChildren<ParticleSystem>())p.Play();
            }
            if(hangingPanel!=null && t>22)
            {
                float amount=Mathf.SmoothStep(0,1,(t-22)/1.2f);
                hangingPanel.localPosition=panelRest+Vector3.down*(amount*.45f);
                hangingPanel.localRotation=panelRotation*Quaternion.Euler(amount*23,0,amount*7);
            }
            foreach(var s in roomSounds)if(s!=null && s.gameObject.activeInHierarchy && !s.isPlaying)s.Play();
            Vector3 pos=player.transform.position;
            if(pos.x>50 && pos.x<62 && pos.z>38 && pos.z<42)shortPassage=true;
            if(pos.x>50 && pos.x<62 && pos.z>52 && pos.z<56)northWing=true;
            // Spread obstruction queries across frames; walls muffle spatial sound.
            occlusionClock-=Time.deltaTime;
            if(occlusionClock<=0 && spatialSounds.Length>0 && player.viewCamera!=null)
            {
                occlusionClock=.035f;int i=soundIndex++%spatialSounds.Length;var s=spatialSounds[i];
                if(s!=null && filters[i]!=null)
                {
                    Vector3 from=player.viewCamera.transform.position,to=s.transform.position;
                    Vector3 offset=to-from;float distance=offset.magnitude;
                    bool hit=distance>.5f && Physics.Raycast(from,offset/distance,out RaycastHit obstruction,distance-.35f,~0,QueryTriggerInteraction.Ignore);
                    occluded[i]=hit;filters[i].cutoffFrequency=hit?1100:18000;
                }
            }
        }
        private static void Panel(Rect rect,Color color)
        {Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        private void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){fontSize=27,fontStyle=FontStyle.Bold,wordWrap=true};
            body=new GUIStyle(GUI.skin.label){fontSize=20,wordWrap=true};small=new GUIStyle(body){fontSize=16};
            button=new GUIStyle(GUI.skin.button){fontSize=21,fontStyle=FontStyle.Bold};
            title.normal.textColor=body.normal.textColor=small.normal.textColor=Color.white;
        }
        private string Zone()
        {
            if(campus!=null)return campus.Zone().ToUpperInvariant();
            Vector3 p=player.transform.position;
            if(p.x>=70)return "ASSEMBLY COURTYARD";
            if(p.x>=66)return "STAIR LOBBY";
            if(p.x>=46)return p.z>42?"NORTH TEACHING WING":"SERVICE PASSAGE";
            if(p.x>=30 && p.z>=30)return "SCIENCE ATRIUM";
            if(p.z<2)return "CHEMISTRY LABORATORY";
            return "CHEMISTRY / TEACHING WING";
        }
        private void OnGUI()
        {
            if(emergency==null || player==null || !emergency.universityPresentation || emergency.inspectionActive)return;
            Styles();Matrix4x4 old=GUI.matrix;int depth=GUI.depth;GUI.depth=0;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
            Color charcoal=new Color(.025f,.033f,.043f,1);
            if(emergency.IsRunning)
            {
                Panel(new Rect(22,20,470,139),charcoal);Panel(new Rect(22,20,5,139),new Color(.15f,.7f,.43f));
                GUI.Label(new Rect(40,30,438,30),campus!=null?campus.Objective:"EVACUATE TO THE ASSEMBLY AREA",body);
                GUI.Label(new Rect(40,67,435,25),Zone(),small);
                GUI.Label(new Rect(40,96,300,26),"Heat exposure: "+emergency.Exposure.ToString("F0")+"%",small);
                Panel(new Rect(40,132,428,7),new Color(.16f,.19f,.22f));
                Panel(new Rect(40,132,428*emergency.Exposure/100,7),new Color(.9f,.26f,.08f));
                GUI.Label(new Rect(629,347,22,26),"+",body);
                if(campus!=null && campus.HasTorch)
                {
                    Panel(new Rect(988,20,270,76),charcoal);
                    GUI.Label(new Rect(1003,30,245,27),"TORCH " + (campus.TorchOn?"ON":"OFF")+"  [F]",small);
                    GUI.Label(new Rect(1003,57,245,26),"Battery: "+Mathf.CeilToInt(campus.Battery)+" seconds",small);
                }
                string notice=campus!=null?campus.Notice:"";
                if(string.IsNullOrEmpty(notice))
                {
                    if(campus!=null && emergency.ExitUsed>0 && player.transform.position.x>=70)notice="You are outside. Follow assembly signs to the firefighters in the north courtyard.";
                    else if(!emergency.MapStudied && player.transform.position.z<10)notice="CHECK THE WALL MAP / Press E to inspect it";
                    else if(emergency.Collapsed || emergency.VisitedExit2)notice="A route is blocked. Check signs and assess another exit.";
                    else if(emergency.Elapsed<18)notice="Head toward the closest exit. Check conditions before entering a route.";
                }
                if(!string.IsNullOrEmpty(notice))
                {Panel(new Rect(195,578,890,62),charcoal);GUI.Label(new Rect(213,590,854,44),notice,body);}
                if(emergency.IsTakingDamage){Panel(new Rect(335,515,610,44),new Color(.35f,.025f,.02f,1));GUI.Label(new Rect(351,523,590,33),"HEAT EXPOSURE — MOVE AWAY FROM THE FIRE",body);}
            }
            else if(emergency.IsExploding)
                Panel(new Rect(0,0,1280,720),new Color(1,.28f,.04f,emergency.ExplosionFlash));
            else if(emergency.HasEnded) Debrief(charcoal);
            else
            {
                Panel(new Rect(270,80,740,558),charcoal);Panel(new Rect(270,80,740,5),new Color(.12f,.7f,.43f));
                GUI.Label(new Rect(306,111,668,75),emergency.IsPaused?"EVACUATION PAUSED":"FIRE EVACUATION / SCIENCE BUILDING",title);
                string instructions=emergency.IsPaused?"The emergency, exposure and timer are paused.\n\nResume to continue, or restart from the laboratory.":
                    "1. CHECK THE EVACUATION MAP\nFind your location on the laboratory wall plan.\n\n2. TAKE THE CLOSEST EXIT\nIf fire or debris blocks the route, turn back and use an alternative. Observe warning signs before opening doors.\n\n3. REACH THE ASSEMBLY POINT\nFollow the green signs to an available exit and the outdoor courtyard.";
                GUI.Label(new Rect(307,204,660,306),instructions,body);
                GUI.Label(new Rect(307,517,660,48),"WASD: move  •  Mouse: look  •  E: inspect / interact\nF: torch  •  Esc: pause  •  R: restart",small);
                if(emergency.IsPaused)
                {
                    if(GUI.Button(new Rect(306,576,320,43),"RESUME",button))emergency.ResumeEvacuation();
                    if(GUI.Button(new Rect(645,576,320,43),"RESTART",button))emergency.RestartEvacuation();
                }
                else if(GUI.Button(new Rect(306,576,660,43),"BEGIN EVACUATION  [ENTER]",button))emergency.BeginEvacuation();
            }
            GUI.matrix=old;GUI.depth=depth;
        }
        private void Debrief(Color charcoal)
        {
            Panel(new Rect(12,12,1256,696),charcoal);
            GUI.Label(new Rect(34,25,1190,48),emergency.Escaped?"ASSEMBLY REACHED / EVACUATION DEBRIEF":"ATTEMPT ENDED / EVACUATION DEBRIEF",title);
            string exit=emergency.ExitUsed>0?"Exit "+emergency.ExitUsed:"Not reached";
            GUI.Label(new Rect(34,85,365,36),"YOUR RUN",body);
            string summary="Exit used: "+exit+"\nTime: "+emergency.Elapsed.ToString("F1")+" s\nHeat exposure: "+emergency.Exposure.ToString("F0")+"%\nFire-zone entries: "+emergency.UnsafeEntries+"\nMap inspected: "+(emergency.MapStudied?"Yes":"No");
            if(campus!=null)summary+="\nTorch collected: "+(campus.HasTorch?"Yes":"No")+"\nTorch used: "+(campus.TorchUsed?"Yes":"No")+"\nEvents encountered: "+campus.EventsSeen+" / 3";
            GUI.Label(new Rect(34,129,344,255),summary,body);
            GUI.Label(new Rect(34,402,344,30),"AREAS VISITED",body);
            string visits=campus!=null?string.Join("\n",campus.visitedAreas):shortPassage&&northWing?"Service passage\nNorth teaching wing":shortPassage?"Service passage":northWing?"North teaching wing":"Chemistry wing";
            GUI.Label(new Rect(34,440,344,208),visits,small);
            Panel(new Rect(391,91,1,549),new Color(.25f,.3f,.32f));
            GUI.Label(new Rect(415,85,380,36),"DECISION TIMELINE",body);
            if(campus!=null)
            {
                timelineScroll=GUI.BeginScrollView(new Rect(414,128,385,482),timelineScroll,new Rect(0,0,359,Mathf.Max(480,campus.timeline.Count*52)));
                for(int i=0;i<campus.timeline.Count;i++)GUI.Label(new Rect(0,i*52,350,50),campus.timeline[i],small);
                GUI.EndScrollView();GUI.Label(new Rect(415,620,380,25),"Observed actions only. Scroll to review.",small);
            }
            else GUI.Label(new Rect(415,130,365,120),"Apply Phase 6 to enable the decision timeline and fourth exit.",body);
            Panel(new Rect(820,91,1,549),new Color(.25f,.3f,.32f));
            GUI.Label(new Rect(845,85,385,36),"EXIT COMPARISON",body);
            string exits="1   CLOSEST / blocked by collapse\n\n2   NEARBY / blocked by fire and debris\n\n3   VIABLE / longer, safer route\n\n4   VIABLE / shorter, harder route";
            GUI.Label(new Rect(845,130,385,218),exits,small);
            GUI.Label(new Rect(845,356,385,30),"FEEDBACK",body);
            var advice=new List<string>();
            if(!emergency.Escaped)advice.Add(emergency.FailureReason);
            if(!emergency.MapStudied)advice.Add("The wall map was not inspected. Use it to locate alternatives before choosing a route.");
            if(campus!=null && !campus.HasTorch)advice.Add("A torch was available in the labelled TOOLS cabinet beside the laboratory exit.");
            if(emergency.UnsafeEntries>0)advice.Add("You entered fire damage zones. Leave more clearance around flames and debris.");
            if(emergency.Escaped && emergency.Exposure==0)advice.Add("You reached assembly without taking fire damage.");
            if(emergency.Escaped && (emergency.Collapsed || emergency.VisitedExit2))advice.Add("You successfully found an alternative after encountering a blocked route.");
            if(emergency.Escaped && emergency.ExitUsed==3)advice.Add("Exit 4 offers a shorter but more hazardous alternative. Choosing Exit 3 is not an error.");
            if(emergency.Escaped && emergency.ExitUsed==4)advice.Add("Exit 3 offers a longer route with less hazardous conditions.");
            if(advice.Count==0)advice.Add("Compare the available routes before your next attempt.");
            var feedbackStyle=new GUIStyle(small){fontSize=15};string feedback=string.Join("\n\n",advice);
            float h=Mathf.Max(225,feedbackStyle.CalcHeight(new GUIContent(feedback),355));
            feedbackScroll=GUI.BeginScrollView(new Rect(845,395,385,235),feedbackScroll,new Rect(0,0,359,h));
            GUI.Label(new Rect(0,0,355,h),feedback,feedbackStyle);GUI.EndScrollView();
            if(GUI.Button(new Rect(845,646,385,42),"RESTART FROM THE LAB",button))emergency.RestartEvacuation();
        }
    }
}
