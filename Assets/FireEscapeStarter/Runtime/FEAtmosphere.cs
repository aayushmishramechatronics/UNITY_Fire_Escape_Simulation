using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireEscape
{
    // Visual/audio presentation only. FEEmergency remains the owner of the run.
    [DefaultExecutionOrder(100)]
    public sealed class FEAtmosphere : MonoBehaviour
    {
        [Serializable] public sealed class Fixture
        {
            public Light light;
            public float normalIntensity;
            public bool damaged;
        }
        public FEEmergency emergency;
        public FEFirstPerson player;
        public Fixture[] fixtures = Array.Empty<Fixture>();
        public Light[] fireLights = Array.Empty<Light>();
        public Light[] alarmLights = Array.Empty<Light>();
        public AudioSource[] fireAudio = Array.Empty<AudioSource>();
        public AudioClip[] footsteps = Array.Empty<AudioClip>();
        public AudioClip structureCreak;
        public ReflectionProbe[] probes = Array.Empty<ReflectionProbe>();
        [Range(.35f, 1f)] public float emergencyBrightness = .72f;
        [Range(0, 1)] public float smokeHaze = .65f;
        public bool blackoutMode;
        public bool expandedUniversity;
        public Light courtyardSun;
        [HideInInspector] public string backupScene;

        private AudioSource stepAudio, roomAudio;
        private Vector3 lastPosition;
        private float distance, clock, nextCreak = 13;
        private bool begun, wasPaused, wasLive;
        private int stepIndex, nextProbe;
        private float probeDelay;
        private bool originalFog, capturedFog;
        private Color originalFogColor;
        private FogMode originalFogMode;
        private float originalDensity;

        private void OnEnable()
        {
            if (emergency != null) emergency.RunReset += ResetPresentation;
        }
        private void Start()
        {
            originalFog = RenderSettings.fog;
            originalFogColor = RenderSettings.fogColor;
            originalFogMode = RenderSettings.fogMode;
            originalDensity = RenderSettings.fogDensity;
            capturedFog = true;
            stepAudio = gameObject.AddComponent<AudioSource>();
            stepAudio.playOnAwake = false; stepAudio.volume = .24f;
            var roomSound = new GameObject("Structural_Sound_Position");
            roomSound.transform.SetParent(transform, false);
            roomAudio = roomSound.AddComponent<AudioSource>();
            roomAudio.playOnAwake = false; roomAudio.volume = .2f;
            roomAudio.spatialBlend = .7f; roomAudio.minDistance = 3; roomAudio.maxDistance = 14;
            // AddComponent can precede reference assignment in an editor builder.
            if (emergency != null)
            {
                emergency.RunReset -= ResetPresentation;
                emergency.RunReset += ResetPresentation;
            }
            ResetPresentation();
        }
        private void ResetPresentation()
        {
            begun = wasLive = wasPaused = false;
            clock = distance = 0; nextCreak = 13; stepIndex = 0;
            if (player != null) lastPosition = player.transform.position;
            if (stepAudio != null) stepAudio.Stop();
            if (roomAudio != null) roomAudio.Stop();
            foreach (var a in fireAudio) if (a != null) a.Stop();
            foreach (var f in fixtures) if (f.light != null) f.light.intensity = f.normalIntensity;
            foreach (var l in alarmLights) if (l != null) l.intensity = 0;
            RestoreFog();
            if(blackoutMode && courtyardSun!=null)courtyardSun.intensity=0;
            nextProbe = 0; probeDelay = .5f;
        }
        private void Update()
        {
            if (emergency == null || player == null) return;
            bool live = emergency.IsRunning || emergency.IsExploding;
            if (emergency.IsPaused)
            {
                if (!wasPaused)
                {
                    foreach (var a in fireAudio) if (a != null) a.Pause();
                    stepAudio.Pause(); roomAudio.Pause();
                }
                wasPaused = true; lastPosition = player.transform.position;
                return;
            }
            if (wasPaused)
            {
                if (live)
                {
                    foreach (var a in fireAudio) if (a != null && a.gameObject.activeInHierarchy) a.UnPause();
                    roomAudio.UnPause();
                }
                wasPaused = false;
            }
            if (live && !begun)
            {
                begun = true; clock = 0;
                nextProbe = 0; probeDelay = 2;
            }
            if (!live && wasLive)
            {
                foreach (var a in fireAudio) if (a != null) a.Stop();
                roomAudio.Stop(); stepAudio.Stop();
            }
            wasLive = live;
            // Capture a small number of reflection probes sequentially, never per frame.
            probeDelay -= Time.deltaTime;
            if (nextProbe < probes.Length && probeDelay <= 0)
            {
                if (probes[nextProbe] != null) probes[nextProbe].RenderProbe();
                nextProbe++; probeDelay = .45f;
            }
            if (!live) { lastPosition = player.transform.position; return; }
            clock += Time.deltaTime;
            for (int i = 0; i < fixtures.Length; i++)
            {
                var f = fixtures[i]; if (f.light == null) continue;
                float strength = blackoutMode ? 0 : emergencyBrightness;
                // Slow, restrained faulty-light variation, no rapid strobing.
                if (f.damaged) strength *= .62f + .2f * Mathf.PerlinNoise(clock * 1.1f, i * 3.7f);
                f.light.intensity = expandedUniversity ? f.normalIntensity*(1-Mathf.SmoothStep(0,1,(clock-6)/2)) : blackoutMode ? 0 : f.normalIntensity * Mathf.Lerp(1, strength, Mathf.Clamp01(clock / 3));
            }
            for (int i = 0; i < fireLights.Length; i++)
            {
                var l = fireLights[i]; if (l == null) continue;
                l.intensity = (blackoutMode ? 4.8f : 2.5f) + (blackoutMode ? 2.4f : 1.3f) * Mathf.PerlinNoise(clock * 3.2f, i * 2.3f);
            }
            for (int i = 0; i < alarmLights.Length; i++)
                if (alarmLights[i] != null) alarmLights[i].intensity = expandedUniversity ? 1.65f + .20f * Mathf.Sin(clock*2+i) : blackoutMode ? 3.0f + .45f * Mathf.Sin(clock * 2 + i) : .28f + .12f * Mathf.Sin(clock * 2 + i);
            foreach (var a in fireAudio)
                if (a != null && a.gameObject.activeInHierarchy && !a.isPlaying) a.Play();

            Vector3 p = player.transform.position;
            float outdoor = expandedUniversity ? Mathf.Clamp01((p.x-69)/5) : Mathf.Clamp01((p.x - 28) / 4) * Mathf.Clamp01((p.z - 32) / 3);
            RenderSettings.fog = smokeHaze > 0;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = expandedUniversity ? new Color(.055f,.052f,.047f) : blackoutMode ? new Color(.022f,.019f,.018f) : new Color(.24f, .265f, .28f);
            RenderSettings.fogDensity = smokeHaze * (expandedUniversity ? Mathf.Lerp(.033f,.054f,Mathf.Clamp01(clock/65)) : blackoutMode ? Mathf.Lerp(.070f,.095f,Mathf.Clamp01(clock/35)) : Mathf.Lerp(.012f,.024f,Mathf.Clamp01(clock/50))) * (1-outdoor);
            if(blackoutMode && courtyardSun!=null)courtyardSun.intensity=.65f*outdoor;

            Vector3 change = p - lastPosition; change.y = 0;
            if (emergency.IsRunning && player.controlsEnabled && change.magnitude < .75f)
                distance += change.magnitude;
            if (distance >= 1.5f && footsteps.Length > 0)
            {
                distance %= 1.5f;
                stepAudio.pitch = .96f + (stepIndex % 3) * .035f;
                stepAudio.PlayOneShot(footsteps[stepIndex++ % footsteps.Length]);
            }
            lastPosition = p;
            if (clock > nextCreak && structureCreak != null && outdoor < .5f)
            {
                roomAudio.transform.position = p + Vector3.up * 2.8f;
                roomAudio.PlayOneShot(structureCreak);
                nextCreak = clock + 19;
            }
        }
        private void RestoreFog()
        {
            if (!capturedFog) return;
            RenderSettings.fog = originalFog; RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogMode = originalFogMode; RenderSettings.fogDensity = originalDensity;
        }
        private void OnDisable()
        {
            if (emergency != null) emergency.RunReset -= ResetPresentation;
            foreach (var a in fireAudio) if (a != null) a.Stop();
            RestoreFog();
        }
    }
}
