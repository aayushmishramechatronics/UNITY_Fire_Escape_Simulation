using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace FireEscape
{
    public static class FEFireSmokeOverhaul
    {
        const string Source="Assets/FireEscapeStarter/VisualAssets/";
        static string output;
        static Material flame,smoke,spark,soot;
        // Keep added hazards along corridor edges, leaving a continuous escape lane.
        static readonly Vector3[] firePositions={
            new Vector3(-5,.12f,13.35f),new Vector3(12,.12f,13.35f),
            new Vector3(24.95f,.12f,28),new Vector3(15,.12f,33.25f),
            new Vector3(8.95f,.12f,36.5f),new Vector3(22,.12f,41.25f)};
        static readonly Vector3[] fireBounds={
            new Vector3(2.8f,3,1.8f),new Vector3(3.2f,3,1.8f),
            new Vector3(1.8f,3,2.8f),new Vector3(3.2f,3,1.8f),
            new Vector3(1.8f,3,2.8f),new Vector3(3.2f,3,1.8f)};

        [MenuItem("Fire Escape/Apply Fire and Smoke Overhaul")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){Tell("Stop Play mode first.");return;}
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var department=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Department_Interior_V3");
            var a=department==null?null:department.GetComponentInChildren<FEAtmosphere>(true);
            if(a==null || a.emergency==null || a.player==null || string.IsNullOrEmpty(scene.path))
            {Tell("Open the saved working department scene with Phase 4 already applied. Do not use SampleScene.");return;}
            if(a.blackoutMode){Tell("The fire and smoke overhaul is already applied to this scene.");return;}
            var manager=a.emergency;
            if(manager.hazardAreas==null || manager.hazardAreas.Length!=4)
            {Tell("Expected the four original hazard areas. Send the FEEmergency Inspector so custom hazards can be preserved.");return;}
            var shader=Shader.Find("FireEscape/SmokeAndFire");var textShader=Shader.Find("FireEscape/WorldTextDepth");
            if(shader==null || textShader==null || !shader.isSupported || !textShader.isSupported || ShaderUtil.ShaderHasError(shader) || ShaderUtil.ShaderHasError(textShader))
            {Tell("The new shaders are missing or unsupported. Copy the entire update, wait for import, and check Console.");return;}
            Folder("Assets/_Project/Backups");
            string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Backups/Before_FireSmoke_Overhaul.unity");
            if(!EditorSceneManager.SaveScene(scene,backup,true)){Tell("Could not save backup. No scene changes made.");return;}
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Fire and smoke overhaul");
            Undo.RegisterFullObjectHierarchyUndo(department,"Overhaul department");
            Undo.RegisterFullObjectHierarchyUndo(a.player.gameObject,"Add damage feedback");
            try
            {
                RepairImports();
                output=AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/FireSmokeOverhaul");
                Folder(output);
                flame=ParticleMaterial("Flame","FlameAtlas.png",new Color(2.6f,1.7f,1.0f,1),false);
                smoke=ParticleMaterial("Smoke","SmokeAtlas.png",Color.white,false);
                spark=ParticleMaterial("Ember","Spark.png",new Color(3.5f,1.1f,.12f,1),true);
                soot=ParticleMaterial("Soot","Soot.png",Color.white,false);soot.SetFloat("_Softness",.001f);
                var systems=manager.emergencyRoot.GetComponentsInChildren<ParticleSystem>(true);
                foreach(var ps in systems)
                {
                    if(ps.name=="Ceiling_Smoke") {Object.DestroyImmediate(ps.gameObject);continue;}
                    bool isSmoke=ps.name=="Smoke" || ps.name.StartsWith("Door_Seep");
                    if(ps.name=="Rising_Embers") {ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=spark;Streams(ps);continue;}
                    Configure(ps,isSmoke,ps.name.StartsWith("Door_Seep")?.65f:1,ps.name.StartsWith("Door_Seep"));
                }
                foreach(var door in department.GetComponentsInChildren<FEDoor>(true))
                    if(door.explosion!=null)
                    {
                        var p=door.explosion;p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                        p.GetComponent<ParticleSystemRenderer>().sharedMaterial=flame;Streams(p);Sheet(p);
                        var m=p.main;m.startSize=new ParticleSystem.MinMaxCurve(1.1f,2);m.maxParticles=100;
                    }
                var effects=New("FireSmoke_Overhaul",manager.emergencyRoot.transform);
                for(int i=0;i<firePositions.Length;i++) AddFire(effects,firePositions[i],i);
                CeilingLayer(effects,new Vector3(4,2.60f,-3),new Vector3(5,.10f,4));
                CeilingLayer(effects,new Vector3(0,2.60f,6),new Vector3(2.6f,.10f,5));
                foreach(float x in new[]{-17f,-10f,-3f,4f,11f,18f})
                    CeilingLayer(effects,new Vector3(x,2.58f,12),new Vector3(5,.1f,2.4f));
                foreach(float z in new[]{17f,24f,30f})CeilingLayer(effects,new Vector3(24,2.58f,z),new Vector3(2.4f,.1f,4));
                foreach(float x in new[]{10f,17f})CeilingLayer(effects,new Vector3(x,2.60f,32),new Vector3(4,.1f,2.4f));
                CeilingLayer(effects,new Vector3(8,2.6f,37),new Vector3(2.4f,.1f,4));
                CeilingLayer(effects,new Vector3(19,2.6f,40),new Vector3(10,.1f,2.4f));
                var hazards=new List<Bounds>{
                    new Bounds(new Vector3(4.8f,1,-3),new Vector3(3.8f,3,3.8f)),
                    new Bounds(new Vector3(-23.15f,1,24),new Vector3(2.4f,3,3.8f)),
                    new Bounds(new Vector3(25.4f,1,23),new Vector3(2.4f,3,3.2f))};
                for(int i=0;i<firePositions.Length;i++)
                    hazards.Add(new Bounds(new Vector3(firePositions[i].x,1,firePositions[i].z),fireBounds[i]));
                // FEEmergency activates only the LAST hazard when Exit 1 collapses.
                hazards.Add(new Bounds(new Vector3(7.25f,1,20),new Vector3(2.4f,3,3.8f)));
                manager.hazardAreas=hazards.ToArray();manager.compactHUD=true;
                var fireLights=manager.emergencyRoot.GetComponentsInChildren<Light>(true);
                foreach(var light in fireLights)
                {
                    light.color=new Color(1,.29f,.055f);light.range=6.5f;
                    light.shadows=light.transform.parent.name=="Lab_Fire" || light.transform.parent.name=="Extra_Fire_1"?LightShadows.Soft:LightShadows.None;
                }
                manager.emergencyLights=fireLights;a.fireLights=fireLights;
                a.fireAudio=manager.emergencyRoot.GetComponentsInChildren<AudioSource>(true).Where(x=>x.loop && x.clip!=null && x.clip.name=="FireLoop").ToArray();
                Blackout(department,a);
                FixText(department,textShader);
                var feedback=Undo.AddComponent<FEDamageFeedback>(a.player.gameObject);feedback.emergency=manager;feedback.view=a.player.viewCamera;
                var data=a.player.viewCamera.GetUniversalAdditionalCameraData();data.requiresDepthTexture=true;
                a.blackoutMode=true;a.smokeHaze=1;a.backupScene=backup;
                EditorUtility.SetDirty(manager);EditorUtility.SetDirty(a);
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
                Selection.activeGameObject=a.gameObject;
                Tell("Fire and smoke overhaul applied. Press Ctrl+S, then Play and START EVACUATION.\n\nBackup: "+backup);
            }
            catch(Exception ex)
            {
                Undo.RevertAllDownToGroup(undo);Debug.LogException(ex);
                Tell("Upgrade stopped. Open the backup to restore fully:\n"+backup+"\nSend the first red Console error.");
            }
        }
        static void RepairImports()
        {
            var paths=new List<string>();
            foreach(string file in Directory.GetFiles(Source,"*.png"))
            {
                string path=file.Replace('\\','/');var t=AssetImporter.GetAtPath(path) as TextureImporter;if(t==null)continue;
                string n=Path.GetFileNameWithoutExtension(path);bool sprite=n.Contains("Atlas") || n=="Spark" || n=="Soot";
                t.textureType=n.EndsWith("_Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                t.sRGBTexture=!n.EndsWith("_Normal") && !n.EndsWith("_MetalSmooth");
                t.alphaSource=TextureImporterAlphaSource.FromInput;t.alphaIsTransparency=sprite;
                t.wrapMode=sprite || n.Contains("Poster")?TextureWrapMode.Clamp:TextureWrapMode.Repeat;
                t.mipmapEnabled=!sprite;t.maxTextureSize=1024;
                t.textureCompression=sprite?TextureImporterCompression.Uncompressed:TextureImporterCompression.Compressed;
                if(sprite){t.ClearPlatformTextureSettings("Standalone");t.ClearPlatformTextureSettings("WebGL");}
                t.SaveAndReimport();paths.Add(path);
            }
            paths.AddRange(Directory.GetFiles(Source,"*.wav").Select(x=>x.Replace('\\','/')));
            AssetDatabase.ForceReserializeAssets(paths,ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
        }
        static Material ParticleMaterial(string name,string file,Color tint,bool additive)
        {
            var m=new Material(Shader.Find("FireEscape/SmokeAndFire")){name="FS_"+name};
            m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+file));m.SetColor("_Tint",tint);
            m.SetFloat("_DstBlend",(float)(additive?BlendMode.One:BlendMode.OneMinusSrcAlpha));
            AssetDatabase.CreateAsset(m,output+"/"+name+".mat");return m;
        }
        static Transform New(string name,Transform parent)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(go,"Fire smoke detail");return go.transform;
        }
        static void Streams(ParticleSystem p)
        {
            var r=p.GetComponent<ParticleSystemRenderer>();r.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
            r.renderMode=ParticleSystemRenderMode.Billboard;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            r.sortMode=ParticleSystemSortMode.Distance;r.maxParticleSize=.65f;r.minParticleSize=0;
        }
        static void Sheet(ParticleSystem p)
        {
            var t=p.textureSheetAnimation;t.enabled=true;t.mode=ParticleSystemAnimationMode.Grid;
            t.numTilesX=4;t.numTilesY=4;t.animation=ParticleSystemAnimationType.WholeSheet;
            t.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));t.startFrame=new ParticleSystem.MinMaxCurve(0,1);t.cycleCount=1;
        }
        static void Configure(ParticleSystem p,bool isSmoke,float scale,bool seep=false)
        {
            p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            p.transform.localRotation=Quaternion.identity;
            if(!isSmoke)p.transform.localPosition=Vector3.up*.55f;
            var m=p.main;m.playOnAwake=false;m.loop=true;m.prewarm=true;
            m.simulationSpace=ParticleSystemSimulationSpace.World;m.maxParticles=isSmoke?45:65;
            m.startColor=Color.white;m.startSize3D=!isSmoke;m.startSpeed=0;
            m.startLifetime=new ParticleSystem.MinMaxCurve(isSmoke?2f:.48f,isSmoke?3.3f:1.05f);
            if(isSmoke){m.startSize=new ParticleSystem.MinMaxCurve(.95f*scale,1.55f*scale);m.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);}
            else {m.startSizeX=new ParticleSystem.MinMaxCurve(.7f*scale,1.4f*scale);m.startSizeY=new ParticleSystem.MinMaxCurve(1.5f*scale,2.45f*scale);m.startSizeZ=1;m.startRotation=new ParticleSystem.MinMaxCurve(-.14f,.14f);}
            var e=p.emission;e.rateOverTime=seep?6:isSmoke?12:40;
            var shape=p.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(.85f*scale,.12f,.7f*scale);
            var v=p.velocityOverLifetime;v.enabled=true;v.space=ParticleSystemSimulationSpace.World;v.x=0;v.z=0;v.y=isSmoke?.16f:.48f;
            var size=p.sizeOverLifetime;size.enabled=true;size.separateAxes=false;
            size.size=new ParticleSystem.MinMaxCurve(1,isSmoke?AnimationCurve.Linear(0,.8f,1,1.5f):new AnimationCurve(new Keyframe(0,.45f),new Keyframe(.25f,1),new Keyframe(1,.08f)));
            var rotation=p.rotationOverLifetime;rotation.enabled=isSmoke;rotation.z=new ParticleSystem.MinMaxCurve(-.12f,.12f);
            var color=p.colorOverLifetime;color.enabled=true;var g=new Gradient();
            g.SetKeys(isSmoke?new[]{new GradientColorKey(new Color(.018f,.015f,.014f),0),new GradientColorKey(new Color(.08f,.065f,.057f),1)}:
                new[]{new GradientColorKey(new Color(1,.98f,.72f),0),new GradientColorKey(new Color(1,.48f,.12f),.55f),new GradientColorKey(new Color(.22f,.07f,.018f),1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(isSmoke?.84f:1,.12f),new GradientAlphaKey(isSmoke?.72f:.8f,.67f),new GradientAlphaKey(0,1)});
            color.color=g;Streams(p);Sheet(p);p.GetComponent<ParticleSystemRenderer>().sharedMaterial=isSmoke?smoke:flame;
            if(isSmoke && !seep)p.transform.position=new Vector3(p.transform.position.x,2.15f,p.transform.position.z);
        }
        static void AddFire(Transform parent,Vector3 position,int index)
        {
            var f=New("Extra_Fire_"+index,parent);f.position=position;
            var p=New("Flames",f).gameObject.AddComponent<ParticleSystem>();p.transform.localPosition=Vector3.up*.55f;Configure(p,false,.90f);
            var s=New("Smoke",f).gameObject.AddComponent<ParticleSystem>();Configure(s,true,1);
            var light=New("Fire_Light",f).gameObject.AddComponent<Light>();light.transform.localPosition=Vector3.up*.9f;light.type=LightType.Point;light.intensity=5;
            var audio=New("Local_Fire_Audio",f).gameObject.AddComponent<AudioSource>();audio.playOnAwake=false;audio.loop=true;
            audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Source+"FireLoop.wav");audio.spatialBlend=1;audio.minDistance=1.6f;audio.maxDistance=11;audio.volume=.42f;audio.dopplerLevel=0;
            var q=GameObject.CreatePrimitive(PrimitiveType.Quad);q.name="Burnt_Floor";q.transform.SetParent(f,false);
            Object.DestroyImmediate(q.GetComponent<Collider>());q.transform.position=new Vector3(position.x,.014f,position.z);
            q.transform.rotation=Quaternion.Euler(90,0,0);q.transform.localScale=new Vector3(3.4f,2.6f,1);q.GetComponent<Renderer>().sharedMaterial=soot;
            Undo.RegisterCreatedObjectUndo(q,"Soot patch");
        }
        static void CeilingLayer(Transform parent,Vector3 position,Vector3 size)
        {
            var p=New("Dense_Ceiling_Smoke",parent).gameObject.AddComponent<ParticleSystem>();p.transform.position=position;Configure(p,true,1);
            p.transform.position=position;var m=p.main;m.startSize=new ParticleSystem.MinMaxCurve(1.5f,2.2f);m.startLifetime=new ParticleSystem.MinMaxCurve(4,6);m.maxParticles=38;
            var shape=p.shape;shape.scale=size;var e=p.emission;e.rateOverTime=6;
            var v=p.velocityOverLifetime;v.y=0;v.x=.05f;v.z=.025f;
            var s=p.sizeOverLifetime;s.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.85f,1,1.05f));
        }
        static void Blackout(GameObject root,FEAtmosphere a)
        {
            foreach(var f in a.fixtures)if(f.light!=null){f.normalIntensity=0;f.light.intensity=0;}
            // Disable every regular ceiling fixture, including any original not in the presentation array.
            foreach(var l in root.GetComponentsInChildren<Light>(true))
                if(l.name=="Ceiling_Light" || l.type==LightType.Directional)l.intensity=0;
            var sun=root.transform.Find("Courtyard_Daylight");if(sun!=null)a.courtyardSun=sun.GetComponent<Light>();
            var changed=new Dictionary<Material,Material>();
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials=r.sharedMaterials;bool dirty=false;
                for(int i=0;i<materials.Length;i++)
                {
                    var old=materials[i];if(old==null || (old.name!="P4_LED" && old.name!="P4_Screen"))continue;
                    if(!changed.TryGetValue(old,out var replacement))
                    {
                        replacement=new Material(old){name="Blackout_"+old.name};
                        replacement.SetColor("_EmissionColor",Color.black);replacement.DisableKeyword("_EMISSION");
                        replacement.SetColor("_BaseColor",new Color(.11f,.12f,.12f));
                        AssetDatabase.CreateAsset(replacement,output+"/"+replacement.name+".mat");changed.Add(old,replacement);
                    }
                    materials[i]=replacement;dirty=true;
                }
                if(dirty)r.sharedMaterials=materials;
            }
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.035f,.028f,.025f);
            RenderSettings.ambientEquatorColor=new Color(.018f,.015f,.014f);
            RenderSettings.ambientGroundColor=new Color(.01f,.009f,.008f);
            RenderSettings.reflectionIntensity=.28f;
            var alarms=new List<Light>(a.alarmLights.Where(x=>x!=null));
            var beaconMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Emergency_Red_Lens"};
            beaconMaterial.SetColor("_BaseColor",new Color(.6f,.012f,.003f));beaconMaterial.EnableKeyword("_EMISSION");
            beaconMaterial.SetColor("_EmissionColor",new Color(4,.018f,.003f));AssetDatabase.CreateAsset(beaconMaterial,output+"/Emergency_Red_Lens.mat");
            // All beacons are inside corridors rather than inside a wall.
            Vector3[] positions={new Vector3(-7.65f,2.6f,-3),new Vector3(7.65f,2.6f,-7),
                new Vector3(-19.7f,2.6f,19),new Vector3(23.65f,2.6f,17),new Vector3(23.65f,2.6f,30),
                new Vector3(12,2.6f,31.65f),new Vector3(7.65f,2.6f,36),new Vector3(25,2.6f,39.65f)};
            foreach(var pos in positions)
            {
                var t=New("Red_Emergency_Beacon",a.transform);t.position=pos;
                var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.015f,.005f);l.intensity=0;l.range=6.2f;l.shadows=LightShadows.None;alarms.Add(l);
                var lens=GameObject.CreatePrimitive(PrimitiveType.Sphere);lens.name="Red_Lens";lens.transform.SetParent(t,false);lens.transform.localScale=Vector3.one*.15f;
                Object.DestroyImmediate(lens.GetComponent<Collider>());lens.GetComponent<Renderer>().sharedMaterial=beaconMaterial;Undo.RegisterCreatedObjectUndo(lens,"Emergency lens");
            }
            foreach(var l in alarms){l.range=6.2f;l.color=new Color(1,.012f,.004f);l.intensity=0;}
            foreach(var r in a.GetComponentsInChildren<MeshRenderer>(true))
                if(r.name=="Red_Lens")r.sharedMaterial=beaconMaterial;
            a.alarmLights=alarms.ToArray();
            // A cloned profile preserves the pre-upgrade backup's colour grade.
            foreach(var volume in a.GetComponentsInChildren<Volume>(true))
            {
                if(volume.sharedProfile==null)continue;
                var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Blackout_Grade";
                string path=AssetDatabase.GenerateUniqueAssetPath(output+"/Blackout_Grade.asset");AssetDatabase.CreateAsset(profile,path);
                foreach(var component in volume.sharedProfile.components)
                {var copy=Object.Instantiate(component);profile.components.Add(copy);AssetDatabase.AddObjectToAsset(copy,profile);}
                if(profile.TryGet<ColorAdjustments>(out var color)){color.postExposure.Override(-.35f);color.contrast.Override(18);color.saturation.Override(-12);}
                if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.27f);bloom.threshold.Override(1.25f);}
                if(profile.TryGet<Vignette>(out var vignette)){vignette.intensity.Override(.22f);vignette.smoothness.Override(.62f);}
                volume.sharedProfile=profile;
            }
        }
        static void FixText(GameObject root,Shader shader)
        {
            var fonts=new Dictionary<Font,Material>();
            foreach(var tm in root.GetComponentsInChildren<TextMesh>(true))
            {
                if(tm.font==null)continue;
                if(!fonts.TryGetValue(tm.font,out var material))
                {
                    material=new Material(shader){name="DepthTested_Font_"+fonts.Count};material.mainTexture=tm.font.material.mainTexture;
                    AssetDatabase.CreateAsset(material,output+"/"+material.name+".mat");fonts.Add(tm.font,material);
                    var sync=New("Font_Atlas_Sync",root.transform).gameObject.AddComponent<FEWorldText>();sync.font=tm.font;sync.depthMaterial=material;
                }
                var r=tm.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=0;
            }
        }
        static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        static void Tell(string text)=>EditorUtility.DisplayDialog("Fire and smoke overhaul",text,"OK");
    }
}
