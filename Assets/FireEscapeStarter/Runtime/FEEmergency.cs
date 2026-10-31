using System;
using System.Globalization;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FireEscape
{
    [DefaultExecutionOrder(-100)]
    public sealed class FEEmergency : MonoBehaviour
    {
        public FEFirstPerson player;
        public GameObject emergencyRoot;
        public GameObject route1Closure;
        public Transform[] fallingDebris;
        public Light[] emergencyLights;
        public Bounds collapseApproach = new Bounds(new Vector3(3.1f, 1, 21), new Vector3(2.2f, 3, 1.84f));
        public Bounds assemblyArea = new Bounds(new Vector3(29, 1, 31), new Vector3(5.4f, 3, 5.4f));
        public Bounds[] hazardAreas;
        public float exposurePerSecond = 35;
        public Bounds route1VisitArea = new Bounds(new Vector3(4, 1, 17), new Vector3(8, 3, 10));
        public Bounds route2VisitArea = new Bounds(new Vector3(-10, 1, 15), new Vector3(16, 3, 10));
        public AudioClip explosionClip;
        public AudioClip collapseClip;
        public event Action RunReset;
        public bool IsRunning => state == RunState.Running;
        public bool IsExploding => state == RunState.Exploding;
        public bool IsPaused => state == RunState.Paused;
        public bool inspectionActive;
        public bool compactHUD;
        public bool universityPresentation;
        public bool campusMode;
        public int ExitUsed { get; private set; }
        public bool VisitedExit4 { get; private set; }
        private Vector3 previousPosition;
        public float[] hazardStartSeconds = Array.Empty<float>();
        public bool MapStudied { get; private set; }
        public bool IsBriefing => state == RunState.Briefing;
        public bool HasEnded => state == RunState.Escaped || state == RunState.Failed;
        public bool Escaped => state == RunState.Escaped;
        public bool Collapsed => collapsed;
        public bool VisitedExit1 => visited1;
        public bool VisitedExit2 => visited2;
        public int UnsafeEntries => unsafeEntries;
        public float Elapsed => elapsed;
        public string FailureReason => failureReason;
        public float ExplosionFlash => IsExploding && explosionPlayed ? Mathf.Clamp01(1-(explosionTime-.18f)/.7f)*.55f : 0;
        public void RecordMapInspection() { MapStudied = true; }
        public void BeginEvacuation() { if(IsBriefing) StartRun(); }
        public void ResumeEvacuation() { if(IsPaused) ResumeRun(); }
        public void RestartEvacuation() { ResetRun(); }
        public float Exposure => exposure;
        public bool IsTakingDamage => IsRunning && insideHazard;

        private enum RunState { Briefing, Running, Paused, Escaped, Failed, Exploding }
        private RunState state;
        private RunState stateBeforePause;
        private ParticleSystem activeExplosion;
        private AudioSource effectsAudio;
        private float explosionTime;
        private bool explosionPlayed;
        private string failureReason = "", failureCode = "failed_exposure";
        private Vector3[] debrisRest;
        private AudioSource alarm;
        private AudioClip alarmClip;
        private ParticleSystem[] particles;
        private float elapsed, exposure, collapseTime;
        private bool collapsed, insideHazard, visited1, visited2, resultWritten;
        private int unsafeEntries;
        private string notice, logStatus = "";
        private float noticeUntil;
        private string runId;
        private GUIStyle titleStyle, bodyStyle, buttonStyle;

        private void Start()
        {
            if (player == null || emergencyRoot == null || route1Closure == null)
            {
                Debug.LogError("Emergency setup is incomplete. Stop Play mode and check Phase 2 references.");
                enabled = false;
                return;
            }
            player.showWalkthroughHUD = false;
            player.restartRequested = ResetRun;
            debrisRest = new Vector3[fallingDebris.Length];
            for (int i = 0; i < fallingDebris.Length; i++) debrisRest[i] = fallingDebris[i].localPosition;
            particles = emergencyRoot.GetComponentsInChildren<ParticleSystem>(true);
            alarm = gameObject.AddComponent<AudioSource>();
            effectsAudio = gameObject.AddComponent<AudioSource>();
            effectsAudio.playOnAwake = false;
            effectsAudio.volume = .4f;
            alarm.loop = true;
            alarm.volume = universityPresentation ? 0 : .09f; // Expanded scene uses spatial alarm emitters.
            alarm.spatialBlend = 0;
            const int sampleRate = 22050;
            float[] data = new float[sampleRate];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / sampleRate;
                float frequency = t < .5f ? 660 : 880;
                float envelope = Mathf.Clamp01(Mathf.Min(t % .5f, .5f - t % .5f) * 100);
                data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * .5f;
            }
            alarmClip = AudioClip.Create("PrototypeAlarm", sampleRate, 1, sampleRate, false);
            alarmClip.SetData(data, 0);
            alarm.clip = alarmClip;
            ResetRun();
        }

        private void ResetRun()
        {
            if (state == RunState.Running || state == RunState.Paused || state == RunState.Exploding) WriteResult("aborted");
            state = RunState.Briefing;
            player.controlsEnabled = false;
            player.ResetToSpawn();
            Unlock();
            alarm.Stop();
            effectsAudio.Stop();
            if (activeExplosion != null) activeExplosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            activeExplosion = null;
            explosionTime = 0;
            explosionPlayed = inspectionActive = false;
            failureReason = "Hazard exposure reached 100%. Avoid direct fire areas on your next attempt.";
            failureCode = "failed_exposure";
            foreach (var ps in particles) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            emergencyRoot.SetActive(false);
            route1Closure.SetActive(false);
            for (int i = 0; i < fallingDebris.Length; i++) fallingDebris[i].localPosition = debrisRest[i];
            elapsed = exposure = collapseTime = 0;
            MapStudied = false; ExitUsed = 0; VisitedExit4 = false; previousPosition = player.transform.position + Vector3.up*.9f;
            collapsed = insideHazard = visited1 = visited2 = resultWritten = false;
            unsafeEntries = 0;
            notice = "";
            logStatus = "";
            RunReset?.Invoke();
        }

        private void StartRun()
        {
            runId = Guid.NewGuid().ToString("N");
            state = RunState.Running;
            emergencyRoot.SetActive(true);
            route1Closure.SetActive(false);
            foreach (var ps in particles)
                if (ps.gameObject.activeInHierarchy) ps.Play();
            player.controlsEnabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            alarm.Play();
            ShowNotice("Fire in the laboratory. Check the wall map and head toward the closest exit. Reassess if it is blocked.");
        }

        private void Update()
        {
            if (player == null) return;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (state == RunState.Briefing && keyboard.enterKey.wasPressedThisFrame) { StartRun(); return; }
                if (keyboard.rKey.wasPressedThisFrame && state != RunState.Briefing) { ResetRun(); return; }
                if (keyboard.escapeKey.wasPressedThisFrame && (state == RunState.Running || state == RunState.Exploding)) { PauseRun(); return; }
            }
#endif
            if (state == RunState.Exploding)
            {
                explosionTime += Time.deltaTime;
                if (!explosionPlayed && explosionTime >= .18f)
                {
                    explosionPlayed = true;
                    if (activeExplosion != null) activeExplosion.Play(true);
                    if (explosionClip != null) effectsAudio.PlayOneShot(explosionClip);
                }
                if (explosionTime >= 1.1f) Finish(false);
                return;
            }
            if (state != RunState.Running) return;
            if (Cursor.lockState != CursorLockMode.Locked) { PauseRun(); return; }
            float dt = Time.deltaTime;
            elapsed += dt;
            Vector3 position = player.transform.position + Vector3.up * .9f;
            if(campusMode)
            {
                if(position.x>27 && position.x<70 && position.z>10 && position.z<16)VisitedExit4=true;
                if(previousPosition.x<70 && position.x>=70)
                {
                    if(position.z>38 && position.z<42)ExitUsed=3;
                    else if(position.z>10 && position.z<16)ExitUsed=4;
                }
                previousPosition=position;
            }
            if (route1VisitArea.Contains(position)) visited1 = true;
            if (route2VisitArea.Contains(position)) visited2 = true;
            if (!collapsed && collapseApproach.Contains(position))
            {
                collapsed = true;
                if (collapseClip != null) effectsAudio.PlayOneShot(collapseClip, .6f);
                route1Closure.SetActive(true); // Barrier blocks passage immediately, ahead of the player.
                for (int i = 0; i < fallingDebris.Length; i++) fallingDebris[i].localPosition = debrisRest[i] + Vector3.up * 4;
                foreach (var ps in route1Closure.GetComponentsInChildren<ParticleSystem>()) ps.Play();
                ShowNotice("Exit 1 is blocked by falling debris. Turn back and find another route.");
            }
            if (collapsed)
            {
                collapseTime += dt;
                float t = Mathf.Clamp01(collapseTime / .65f);
                for (int i = 0; i < fallingDebris.Length; i++)
                    fallingDebris[i].localPosition = debrisRest[i] + Vector3.up * (4 * (1 - t * t));
            }
            bool hazardous = false;
            for (int i = 0; i < hazardAreas.Length; i++)
                if ((i != hazardAreas.Length - 1 || collapsed) && (i >= hazardStartSeconds.Length || elapsed >= hazardStartSeconds[i]) && hazardAreas[i].Contains(position)) hazardous = true;
            if (hazardous && !insideHazard) unsafeEntries++;
            insideHazard = hazardous;
            if (hazardous) exposure = Mathf.Min(100, exposure + exposurePerSecond * dt);
            foreach (var light in emergencyLights)
                if (light != null) light.intensity = 1.5f + Mathf.PingPong(elapsed * 3, 2);
            if (exposure >= 100) { Finish(false); return; }
            if (assemblyArea.Contains(position) && (!campusMode || ExitUsed>0)) { if(!campusMode)ExitUsed=3; Finish(true); }
        }

        private void PauseRun()
        {
            if (state != RunState.Running && state != RunState.Exploding) return;
            stateBeforePause = state;
            state = RunState.Paused;
            player.controlsEnabled = false;
            Unlock();
            alarm.Pause();
            effectsAudio.Pause();
            if (activeExplosion != null) activeExplosion.Pause(true);
            foreach (var ps in particles) if (ps.gameObject.activeInHierarchy) ps.Pause();
        }
        private void ResumeRun()
        {
            state = stateBeforePause;
            player.controlsEnabled = state == RunState.Running;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            alarm.UnPause();
            effectsAudio.UnPause();
            if (activeExplosion != null && explosionPlayed) activeExplosion.Play(true);
            foreach (var ps in particles) if (ps.gameObject.activeInHierarchy) ps.Play();
        }
        private void Finish(bool success)
        {
            state = success ? RunState.Escaped : RunState.Failed;
            player.controlsEnabled = false;
            Unlock();
            alarm.Stop();
            foreach (var ps in particles) if (ps.gameObject.activeInHierarchy) ps.Pause();
            if (activeExplosion != null) activeExplosion.Pause(true);
            WriteResult(success ? "escaped" : failureCode);
        }
        public void TriggerExplosion(string roomName, ParticleSystem effect)
        {
            if (state != RunState.Running) return;
            state = RunState.Exploding;
            inspectionActive = false;
            player.controlsEnabled = false;
            explosionTime = 0;
            explosionPlayed = false;
            activeExplosion = effect;
            failureCode = "failed_explosion";
            failureReason = "Opening the " + roomName + " door triggered an explosion. The attempt ended.";
            alarm.Stop();
        }
        private void WriteResult(string outcome)
        {
            if (resultWritten || string.IsNullOrEmpty(runId)) return;
            resultWritten = true;
            string path = Path.Combine(Application.persistentDataPath, "FireEscape_Runs.csv");
            try
            {
                if (!File.Exists(path)) File.WriteAllText(path, "run_id,utc_time,outcome,elapsed_seconds,exposure_percent,unsafe_entries,visited_route_1,visited_route_2,collapse_triggered,exit_used\n");
                string row = string.Join(",", runId, DateTime.UtcNow.ToString("o"), outcome,
                    elapsed.ToString("F2", CultureInfo.InvariantCulture), exposure.ToString("F1", CultureInfo.InvariantCulture),
                    unsafeEntries.ToString(CultureInfo.InvariantCulture), visited1 ? "1" : "0", visited2 ? "1" : "0", collapsed ? "1" : "0", outcome == "escaped" ? ExitUsed.ToString(CultureInfo.InvariantCulture) : "none");
                File.AppendAllText(path, row + "\n");
                logStatus = "Run saved to CSV.";
                Debug.Log("Evacuation result saved: " + path);
            }
            catch (Exception ex)
            {
                logStatus = "CSV could not be saved. See Console.";
                Debug.LogWarning("Could not save evacuation result: " + ex.Message);
            }
        }
        private void ShowNotice(string message) { notice = message; noticeUntil = elapsed + 6; }
        private static void Unlock() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        private void OnApplicationFocus(bool focused) { if (!focused) PauseRun(); }
        private void OnDestroy()
        {
            if (player != null) { player.restartRequested = null; player.controlsEnabled = true; player.showWalkthroughHUD = true; }
            if (alarmClip != null) Destroy(alarmClip);
            Unlock();
        }

        private void OnGUI()
        {
            if (universityPresentation || (inspectionActive && state == RunState.Running)) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, wordWrap = true };
                bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
                titleStyle.normal.textColor = bodyStyle.normal.textColor = Color.white;
            }
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960 * scale) / 2, (Screen.height - 540 * scale) / 2), Quaternion.identity, Vector3.one * scale);
            if (state == RunState.Exploding)
            {
                float alpha = explosionPlayed ? Mathf.Clamp01(1 - (explosionTime - .18f) / .7f) * .55f : 0;
                Color previousColor = GUI.color;
                GUI.color = new Color(1, .3f, .06f, alpha);
                GUI.DrawTexture(new Rect(0, 0, 960, 540), Texture2D.whiteTexture);
                GUI.color = previousColor;
            }
            else if (state == RunState.Running)
            {
                if(compactHUD)
                {
                    GUI.Label(new Rect(24,20,520,28), "EVACUATE TO THE ASSEMBLY AREA", bodyStyle);
                    GUI.Label(new Rect(24,49,520,28), "Heat exposure: " + exposure.ToString("F0") + "%", bodyStyle);
                }
                else {
                GUI.Box(new Rect(16, 16, 430, 102), "");
                GUI.Label(new Rect(28, 23, 406, 90), "EVACUATE TO THE ASSEMBLY AREA\nTime: " + elapsed.ToString("F1") + " s   |   Exposure: " + exposure.ToString("F0") + "%\nWASD: move   Esc: pause   R: restart", bodyStyle);
                }
                GUI.Label(new Rect(475, 257, 20, 25), "+", bodyStyle);
                if (insideHazard) GUI.Label(new Rect(250, 390, 500, 40), "HAZARD EXPOSURE - MOVE AWAY FROM THE FIRE", bodyStyle);
                if (elapsed < noticeUntil) { GUI.Box(new Rect(100, 448, 760, 70), ""); GUI.Label(new Rect(116, 458, 728, 55), notice, bodyStyle); }
            }
            else
            {
                GUI.Box(new Rect(180, 60, 600, 420), "");
                string title = state == RunState.Briefing ? "CHEMISTRY LAB | FIRE ESCAPE" : state == RunState.Paused ? "PAUSED" : state == RunState.Escaped ? "ASSEMBLY AREA REACHED" : "ATTEMPT ENDED";
                GUI.Label(new Rect(204, 82, 552, 65), title, titleStyle);
                string text;
                if (state == RunState.Briefing)
                    text = "A fire will start when you begin. Find an available exit and reach the assembly area.\n\nStudy the evacuation plan on the laboratory wall. Look at the plan or a door and press E to interact.\n\nWASD: walk | Mouse: look | E: interact\nEsc: pause | R: restart\n\nRead warning signs and observe conditions before entering a room.";
                else if (state == RunState.Paused) text = "The timer, hazard exposure and collapse animation are paused.\n\nResume to continue, or restart to return to the lab.";
                else text = (state == RunState.Escaped ? "You escaped using Exit 3." : failureReason) + "\n\nElapsed time: " + elapsed.ToString("F1") + " seconds\nExposure: " + exposure.ToString("F0") + "%\nUnsafe-area entries: " + unsafeEntries + "\nExit 1 collapse triggered: " + (collapsed ? "Yes" : "No") + "\n\n" + logStatus;
                GUI.Label(new Rect(204, 150, 552, 235), text, bodyStyle);
                if (state == RunState.Briefing && GUI.Button(new Rect(204, 410, 552, 46), "START EVACUATION (Enter)", buttonStyle)) StartRun();
                else if (state == RunState.Paused)
                {
                    if (GUI.Button(new Rect(204, 410, 265, 46), "RESUME", buttonStyle)) ResumeRun();
                    if (GUI.Button(new Rect(487, 410, 269, 46), "RESTART", buttonStyle)) ResetRun();
                }
                else if (state != RunState.Briefing && GUI.Button(new Rect(204, 410, 552, 46), "TRY AGAIN", buttonStyle)) ResetRun();
            }
            GUI.matrix = previous;
        }
    }
}
