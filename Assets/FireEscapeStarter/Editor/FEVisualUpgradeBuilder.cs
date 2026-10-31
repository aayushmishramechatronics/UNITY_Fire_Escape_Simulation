using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace FireEscape
{
    public static partial class FEVisualUpgradeBuilder
    {
        private const string Source = "Assets/FireEscapeStarter/VisualAssets/";
        private static Transform department, geometry, art;
        private static FEEmergency emergency;
        private static FEAtmosphere atmosphere;
        private static FEVisualMeshes meshes;
        private static string output;
        private static Font font;
        private static readonly Dictionary<string,Material> mats = new Dictionary<string,Material>();

        [MenuItem("Fire Escape/Apply Phase 4 - Visual Upgrade")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) { Tell("Stop Play mode before applying the upgrade."); return; }
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var root=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Department_Interior_V3");
            if(root==null || string.IsNullOrEmpty(scene.path))
            { Tell("Open your saved department scene first. In your upload it is Assets/_Project/Phase1Generated/MainLevel_Phase1.unity."); return; }
            if(root.GetComponentInChildren<FEAtmosphere>(true)!=null)
            { Tell("Phase 4 is already applied to this scene. Do not apply it twice. Use the saved backup if you need the previous appearance."); return; }
            department=root.transform;geometry=department.Find("Level_Geometry");
            emergency=root.GetComponentInChildren<FEEmergency>();
            if(geometry==null || emergency==null || emergency.player==null || emergency.player.viewCamera==null)
            { Tell("The current scene is missing a level, emergency manager or player camera. No changes made."); return; }
            if(GraphicsSettings.currentRenderPipeline==null || Shader.Find("Universal Render Pipeline/Lit")==null)
            { Tell("This update requires your existing Universal Render Pipeline project."); return; }
            foreach(string required in new[]{"Porcelain_Base.png","Plaster_Base.png","Acoustic_Base.png","Oak_Base.png","Epoxy_Base.png","Steel_Base.png","SmokeAtlas.png","FlameAtlas.png","Soot.png","SafetyPoster.png","ResearchPoster.png","FireLoop.wav"})
                if(AssetDatabase.LoadMainAssetAtPath(Source+required)==null)
                { Tell("Missing visual asset: "+required+". Copy the complete FireEscapeStarter folder from the update first.");return; }
            EnsureFolder("Assets/_Project/Backups");
            string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Backups/Before_Phase4_Visuals.unity");
            if(!EditorSceneManager.SaveScene(scene,backup,true)) { Tell("Could not save the backup. No changes made.");return; }
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Apply university visual upgrade");
            Undo.RegisterFullObjectHierarchyUndo(root,"Upgrade existing department");
            Undo.RegisterFullObjectHierarchyUndo(emergency.player.gameObject,"Configure player camera");
            try
            {
                ImportTextures();
                output=AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Phase4Visuals");
                AssetDatabase.CreateFolder("Assets/_Project",Path.GetFileName(output));
                AssetDatabase.CreateFolder(output,"Meshes");meshes=new FEVisualMeshes(output+"/Meshes");
                font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");MakeMaterials();
                art=Child("University_Visuals_Phase4",department);
                atmosphere=art.gameObject.AddComponent<FEAtmosphere>();
                atmosphere.emergency=emergency;atmosphere.player=emergency.player;atmosphere.backupScene=backup;
                UpgradeSurfaces();BuildLab();BuildCorridor();UpgradeDoors();
                SetupLighting();UpgradeEffects();SetupAudio();
                CombineStaticDetails();
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(group);
                Selection.activeGameObject=art.gameObject;
                Debug.Log("PHASE 4 APPLIED. Save with Ctrl+S, then Play. Backup scene: "+backup+". Assets: "+output);
                Tell("Visual upgrade applied to the current scene. Press Ctrl+S, then Play.\n\nBackup: "+backup+"\n\nUse Fire Escape > Check Phase 4 Setup if needed.");
            }
            catch(Exception ex)
            {
                Undo.RevertAllDownToGroup(group);
                Debug.LogException(ex);
                Tell("The upgrade stopped. Open "+backup+" to restore the saved scene completely. Send the FIRST red Console error.");
            }
        }

        private static void ImportTextures()
        {
            foreach(string file in Directory.GetFiles(Source,"*.png"))
            {
                string path=file.Replace('\\','/'); var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null) continue;
                string name=Path.GetFileNameWithoutExtension(file);
                importer.textureType=name.EndsWith("_Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=!name.EndsWith("_MetalSmooth") && !name.EndsWith("_Normal");
                importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.anisoLevel=4;
                bool decal=name.Contains("Atlas") || name.Contains("Poster") || name=="Soot" || name=="Spark";
                importer.wrapMode=decal?TextureWrapMode.Clamp:TextureWrapMode.Repeat;
                importer.alphaIsTransparency=decal; importer.textureCompression=TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }
        private static void MakeMaterials()
        {
            mats.Clear();
            Pbr("Floor","Porcelain",Color.white);Pbr("Wall","Plaster",Color.white);
            Pbr("Ceiling","Acoustic",Color.white);Pbr("Oak","Oak",Color.white);
            Pbr("Counter","Epoxy",Color.white);Pbr("Steel","Steel",Color.white);
            Solid("Blue",new Color(.10f,.19f,.25f),.34f);
            Solid("White",new Color(.77f,.81f,.79f),.35f);
            Solid("Black",new Color(.025f,.035f,.039f),.3f);
            Solid("Rubber",new Color(.025f,.026f,.027f),.08f);
            Solid("Red",new Color(.52f,.025f,.012f),.32f);
            Solid("Yellow",new Color(.73f,.5f,.065f),.3f);
            Solid("Paper",new Color(.83f,.83f,.76f),.1f);
            Solid("Cork",new Color(.29f,.17f,.08f),.05f);
            Solid("Bottle",new Color(.23f,.095f,.018f),.65f);
            Solid("Liquid",new Color(.025f,.28f,.33f),.68f);
            Solid("Concrete",new Color(.31f,.30f,.28f),.08f);
            Pbr("Char","Plaster",new Color(.13f,.12f,.11f));
            var led=Solid("LED",new Color(.82f,.87f,.86f),.4f);
            led.EnableKeyword("_EMISSION");led.SetColor("_EmissionColor",new Color(1.35f,1.48f,1.5f));
            var screen=Solid("Screen",new Color(.025f,.1f,.12f),.55f);
            screen.EnableKeyword("_EMISSION");screen.SetColor("_EmissionColor",new Color(.035f,.22f,.24f));
            var glass=Solid("Glass",new Color(.64f,.85f,.87f,.14f),.9f);
            Transparent(glass,false);glass.SetFloat("_Cull",(float)CullMode.Back);
            TexturedUnlit("Smoke","SmokeAtlas.png",Color.white,true,false);
            TexturedUnlit("Flame","FlameAtlas.png",new Color(2.0f,1.55f,1.1f,1),true,true);
            TexturedUnlit("Spark","Spark.png",new Color(3,1.4f,.25f,1),true,true);
            TexturedUnlit("Soot","Soot.png",Color.white,true,false);
            TexturedUnlit("SafetyPoster","SafetyPoster.png",Color.white,false,false);
            TexturedUnlit("ResearchPoster","ResearchPoster.png",Color.white,false,false);
        }
        private static Material Solid(string key,Color color,float smooth)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="P4_"+key,enableInstancing=true};
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);
            AssetDatabase.CreateAsset(m,output+"/"+key+".mat");mats[key]=m;return m;
        }
        private static Material Pbr(string key,string texture,Color tint)
        {
            var m=Solid(key,tint,1);
            m.SetTexture("_BaseMap",Tex(texture+"_Base.png"));m.SetTexture("_BumpMap",Tex(texture+"_Normal.png"));
            m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.55f);
            m.SetTexture("_MetallicGlossMap",Tex(texture+"_MetalSmooth.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");
            return m;
        }
        private static void TexturedUnlit(string key,string texture,Color color,bool transparent,bool additive)
        {
            bool particle=key=="Smoke" || key=="Flame" || key=="Spark";
            var m=new Material(Shader.Find(particle?"Universal Render Pipeline/Particles/Unlit":"Universal Render Pipeline/Unlit")){name="P4_"+key,enableInstancing=true};
            m.SetColor("_BaseColor",color);m.SetTexture("_BaseMap",Tex(texture));
            if(transparent) Transparent(m,additive);
            AssetDatabase.CreateAsset(m,output+"/"+key+".mat");mats[key]=m;
        }
        private static void Transparent(Material m,bool additive)
        {
            m.SetFloat("_Surface",1);m.SetFloat("_Blend",additive?2:0);
            m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend",(float)(additive?BlendMode.One:BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",(float)CullMode.Off);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;
        }
        private static Texture2D Tex(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Source+name);

        private static void UpgradeSurfaces()
        {
            var mapping=new Dictionary<string,string>{
                {"Wall_Paint","Wall"},{"Floor_Tiles","Floor"},{"Ceiling","Ceiling"},{"Door_And_Desk","Oak"},
                {"Metal","Steel"},{"Department_Teal","Blue"},{"Skirting","Black"},{"Equipment_White","White"},
                {"Dark_Surface","Counter"},{"Concrete","Concrete"},{"Extinguisher_Red","Red"},{"Glass","Glass"}};
            foreach(var r in geometry.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(r.GetComponent<TextMesh>()!=null || r.sharedMaterial==null) continue;
                if(mapping.TryGetValue(r.sharedMaterial.name,out string key))r.sharedMaterial=mats[key];
            }
            // Replace only the four prototype lab benches visually. Original bench collision stays.
            var furniture=geometry.Find("Furniture_And_Lab_Equipment");
            if(furniture!=null)foreach(var r in furniture.GetComponentsInChildren<MeshRenderer>())
            {
                Vector3 p=r.transform.position;
                if(p.x>-7 && p.x<7 && p.z>-8 && p.z<-1.9f && !r.name.StartsWith("Sign"))
                {
                    r.enabled=false;
                    if(r.name=="Instrument" || r.name.StartsWith("Reagent") || r.name=="Bottle_Cap")
                    {var c=r.GetComponent<Collider>();if(c!=null)c.enabled=false;}
                }
            }
            // The lab's ceiling grid adds a physical edge below the textured panels.
            for(int x=-8;x<=8;x+=2) Box("Ceiling_T_Bar",new Vector3(x,3.285f,-4),new Vector3(.025f,.025f,12),"Steel",art,.003f);
            for(int z=-10;z<=2;z+=2) Box("Ceiling_T_Bar",new Vector3(0,3.285f,z),new Vector3(16,.025f,.025f),"Steel",art,.003f);
        }

        private static void SetupLighting()
        {
            var fixtures=new List<FEAtmosphere.Fixture>();
            foreach(var t in geometry.GetComponentsInChildren<Transform>().Where(x=>x.name=="Ceiling_Light").ToArray())
            {
                var light=t.GetComponent<Light>();if(light==null)continue;
                var old=t.GetComponent<MeshRenderer>();if(old!=null)old.enabled=false;
                light.color=new Color(.86f,.92f,1);light.intensity=2.4f;light.range=7.2f;
                bool lab=t.position.x>-8 && t.position.x<8 && t.position.z<2;
                light.shadows=(lab && t.position.x>=0) || Vector3.Distance(t.position,new Vector3(2,3,12))<3?LightShadows.Soft:LightShadows.None;
                light.shadowBias=.035f;light.shadowNormalBias=.15f;light.shadowNearPlane=.15f;
                var fixture=Child("Recessed_LED_Panel",art);fixture.position=new Vector3(t.position.x,3.265f,t.position.z);
                Box("Housing",Vector3.zero,new Vector3(1.38f,.065f,.48f),"Steel",fixture,.012f);
                Box("Diffuser",new Vector3(0,-.037f,0),new Vector3(1.28f,.018f,.38f),"LED",fixture,.006f);
                fixtures.Add(new FEAtmosphere.Fixture{light=light,normalIntensity=light.intensity,damaged=lab && t.position.x>2});
            }
            atmosphere.fixtures=fixtures.ToArray();
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.27f,.30f,.34f);
            RenderSettings.ambientEquatorColor=new Color(.20f,.225f,.25f);
            RenderSettings.ambientGroundColor=new Color(.11f,.12f,.13f);
            RenderSettings.reflectionIntensity=.7f;RenderSettings.fog=false;
            var sun=department.Find("Courtyard_Daylight");
            if(sun!=null){var l=sun.GetComponent<Light>();l.intensity=.85f;l.color=new Color(1,.94f,.83f);}
            var camera=emergency.player.viewCamera;camera.allowHDR=true;camera.nearClipPlane=.07f;
            var data=camera.GetComponent<UniversalAdditionalCameraData>();
            if(data==null)data=Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
            data.renderPostProcessing=true;data.renderShadows=true;
            data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality=AntialiasingQuality.Medium;data.dithering=true;
            data.volumeLayerMask|=1;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="University_Colour_Grade";
            AssetDatabase.CreateAsset(profile,output+"/University_Colour_Grade.asset");
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(.25f);color.contrast.Override(9);color.saturation.Override(-7);
            var bloom=profile.Add<Bloom>(true);bloom.threshold.Override(1.1f);bloom.intensity.Override(.18f);bloom.scatter.Override(.55f);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.13f);vignette.smoothness.Override(.5f);
            foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);
            var volume=Child("University_Colour_Grade",art).gameObject.AddComponent<Volume>();
            volume.isGlobal=true;volume.priority=30;volume.sharedProfile=profile;
            var probes=new List<ReflectionProbe>();
            Probe(new Vector3(0,1.6f,-4),new Vector3(16,3.2f,12),probes);
            Probe(new Vector3(0,1.6f,12),new Vector3(40,3.2f,4),probes);
            atmosphere.probes=probes.ToArray();
            var signals=new List<Light>();
            foreach(var p in new[]{new Vector3(-1.82f,2.55f,6),new Vector3(6,2.55f,13.82f),new Vector3(-15,2.55f,13.82f)})
            {
                var signal=Child("Alarm_Beacon",art);signal.position=p;
                Box("Beacon_Base",Vector3.zero,new Vector3(.23f,.3f,.1f),"White",signal,.02f);
                Box("Red_Lens",new Vector3(0,0,-.055f),new Vector3(.16f,.22f,.07f),"Red",signal,.03f);
                var light=Child("Alarm_Glow",signal).gameObject.AddComponent<Light>();light.transform.localPosition=new Vector3(0,0,-.2f);
                light.color=new Color(1,.045f,.015f);light.range=2.7f;light.intensity=0;light.shadows=LightShadows.None;signals.Add(light);
            }
            atmosphere.alarmLights=signals.ToArray();
        }
        private static void Probe(Vector3 position,Vector3 size,List<ReflectionProbe> list)
        {
            var probe=Child("Interior_Reflection",art).gameObject.AddComponent<ReflectionProbe>();probe.transform.position=position;
            probe.size=size;probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;probe.resolution=128;
            probe.boxProjection=true;probe.blendDistance=1;probe.intensity=.75f;probe.nearClipPlane=.15f;probe.farClipPlane=35;
            probe.shadowDistance=20;list.Add(probe);
        }

        private static void UpgradeDoors()
        {
            foreach(var door in department.GetComponentsInChildren<FEDoor>(true))
            {
                Transform hinge=door.hinge;if(hinge==null)continue;
                // All accessories are on the existing moving hinge; interaction collider stays untouched.
                foreach(string n in new[]{"Handle","Kickplate"})
                {var t=hinge.Find(n);if(t!=null && t.GetComponent<Renderer>()!=null)t.GetComponent<Renderer>().enabled=false;}
                Box("Brushed_Kick_Plate",new Vector3(.88f,.26f,-.064f),new Vector3(1.65f,.39f,.018f),"Steel",hinge,.008f);
                Box("Lever_Backplate",new Vector3(1.57f,1.1f,-.065f),new Vector3(.065f,.22f,.018f),"Steel",hinge,.01f);
                Rod("Lever",new Vector3(1.57f,1.13f,-.12f),new Vector3(1.38f,1.13f,-.12f),.018f,"Steel",hinge);
                for(int i=0;i<3;i++) Rod("Hinge_Knuckle",new Vector3(.025f,.35f+i*.9f,-.04f),new Vector3(.025f,.49f+i*.9f,-.04f),.022f,"Steel",hinge);
                Box("Door_Closer",new Vector3(1.1f,2.43f,-.09f),new Vector3(.28f,.09f,.08f),"Steel",hinge,.015f);
                Rod("Closer_Arm",new Vector3(1.1f,2.45f,-.13f),new Vector3(.75f,2.57f,-.13f),.009f,"Steel",hinge);
                if(door.kind!=FEDoor.DoorKind.Fatal)
                {
                    Box("Inset_Panel",new Vector3(.88f,1.65f,-.057f),new Vector3(1.22f,.73f,.012f),"Blue",hinge,.025f);
                    Label(door.kind==FEDoor.DoorKind.Safe?"FACULTY OFFICE":"STAFF ACCESS",new Vector3(.88f,1.66f,-.069f),Quaternion.identity,.95f,.13f,Color.white,hinge);
                }
            }
        }

        private static void CombineStaticDetails()
        {
            // Batch opaque static detail by material and 8 m region. Moving doors,
            // transparent glass, text, particles and collapse pieces are excluded.
            var groups=new Dictionary<string,List<MeshFilter>>();
            foreach(var f in art.GetComponentsInChildren<MeshFilter>(true))
            {
                var r=f.GetComponent<MeshRenderer>();
                if(r==null || !r.enabled || r.sharedMaterial==null || r.sharedMaterial.renderQueue>=2500 || f.sharedMesh==null)continue;
                Vector3 p=f.transform.position;
                string key=r.sharedMaterial.name+"_"+Mathf.FloorToInt(p.x/8)+"_"+Mathf.FloorToInt(p.z/8);
                if(!groups.ContainsKey(key))groups[key]=new List<MeshFilter>();groups[key].Add(f);
            }
            var batchRoot=Child("Batched_Static_Details",art);int count=0;
            foreach(var pair in groups)
            {
                if(pair.Value.Count<4)continue;
                var combine=pair.Value.Select(f=>new CombineInstance{mesh=f.sharedMesh,subMeshIndex=0,transform=art.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();
                var mesh=new Mesh{name="Scenery_Batch_"+count,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(combine,true,true);mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,output+"/Meshes/Scenery_Batch_"+count+".asset");count++;
                var t=Child(pair.Key,batchRoot);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=pair.Value[0].GetComponent<MeshRenderer>().sharedMaterial;
                foreach(var f in pair.Value)f.GetComponent<MeshRenderer>().enabled=false;
            }
        }

        [MenuItem("Fire Escape/Check Phase 4 Setup")]
        public static void Check()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var a=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<FEAtmosphere>(true)).FirstOrDefault();
            if(a==null){Tell("Phase 4 has not been applied in this scene.");return;}
            var missing=new List<string>();
            if(a.emergency==null)missing.Add("emergency manager");if(a.player==null)missing.Add("player");
            int pink=a.GetComponentsInChildren<Renderer>(true).Count(r=>r.sharedMaterials.Any(m=>m==null || m.shader==null || !m.shader.isSupported));
            int fatal=a.transform.parent.GetComponentsInChildren<FEDoor>(true).Count(d=>d.kind==FEDoor.DoorKind.Fatal);
            string message="Missing references: "+(missing.Count==0?"none":string.Join(", ",missing))+"\nUnsupported or missing materials: "+pink+"\nFatal doors: "+fatal+" (expected 2)\nFixtures: "+a.fixtures.Length+"\nBackup: "+a.backupScene;
            Debug.Log(message);Tell(message);
        }
        private static Transform Child(string name,Transform parent)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);Undo.RegisterCreatedObjectUndo(go,"Add visual detail");return go.transform;}
        private static Transform Box(string name,Vector3 position,Vector3 size,string material,Transform parent,float bevel=.008f)
        {
            var t=Child(name,parent);t.localPosition=position;
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=meshes.Box(size,bevel);
            var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mats[material];
            r.shadowCastingMode=material=="Glass"?ShadowCastingMode.Off:ShadowCastingMode.On;
            return t;
        }
        private static void Rod(string name,Vector3 start,Vector3 end,float radius,string material,Transform parent)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);
            Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.localPosition=(start+end)*.5f;
            go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,end-start);
            go.transform.localScale=new Vector3(radius*2,Vector3.Distance(start,end)*.5f,radius*2);
            go.GetComponent<Renderer>().sharedMaterial=mats[material];
            Undo.RegisterCreatedObjectUndo(go,"Add visual detail");
        }
        private static void Label(string text,Vector3 position,Quaternion rotation,float width,float height,Color color,Transform parent)
        {
            var t=Child("Label_"+text,parent);t.localPosition=position;t.localRotation=rotation;
            var tm=t.gameObject.AddComponent<TextMesh>();tm.text=text;tm.font=font;tm.fontSize=48;
            tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=color;
            font.RequestCharactersInTexture(text,48,FontStyle.Normal);
            float longest=1,line=0;int lines=1;
            foreach(char c in text)
            {if(c=='\n'){longest=Mathf.Max(longest,line);line=0;lines++;}else if(font.GetCharacterInfo(c,out CharacterInfo info,48))line+=info.advance;}
            longest=Mathf.Max(longest,line);tm.characterSize=10*Mathf.Min(width/longest,height/(48*lines*1.2f));
            var r=t.GetComponent<MeshRenderer>();r.sharedMaterial=font.material;r.shadowCastingMode=ShadowCastingMode.Off;
        }
        private static void Quad(string name,Vector3 position,Quaternion rotation,Vector2 size,string material,Transform parent)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name=name;go.transform.SetParent(parent,false);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localPosition=position;go.transform.localRotation=rotation;go.transform.localScale=new Vector3(size.x,size.y,1);
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=mats[material];r.shadowCastingMode=ShadowCastingMode.Off;
            Undo.RegisterCreatedObjectUndo(go,"Add visual detail");
        }
        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        private static void Tell(string message)=>EditorUtility.DisplayDialog("University visual upgrade",message,"OK");
    }
}
