using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireEscape
{
    public static class FEEmergencyBuilder
    {
        [MenuItem("Fire Escape/Add Phase 2 Emergency to Current Level")]
        public static void AddEmergency()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Stop Play mode", "Stop Play mode before applying Phase 2.", "OK");
                return;
            }
            var level = GameObject.Find("Level_Geometry");
            var playerObject = GameObject.Find("Player");
            if (level == null || playerObject == null || playerObject.GetComponent<FEFirstPerson>() == null)
            {
                EditorUtility.DisplayDialog("Open your level", "Open the working Phase 1 level first.", "OK");
                return;
            }
            if (GameObject.Find("Emergency_Scenario") != null)
            {
                EditorUtility.DisplayDialog("Already installed", "Phase 2 already exists in this scene. Press Play to test it.", "OK");
                return;
            }
            if (level.transform.position != Vector3.zero || level.transform.rotation != Quaternion.identity || level.transform.lossyScale != Vector3.one)
            {
                EditorUtility.DisplayDialog("Layout changed", "The level transform has changed. Send a screenshot before applying this update.", "OK");
                return;
            }
            bool turn1 = false, turn2 = false;
            foreach (Transform item in level.GetComponentsInChildren<Transform>())
            {
                if (!item.name.StartsWith("Floor_")) continue;
                if (Vector3.Distance(item.position, new Vector3(7, -.1f, 21)) < .01f) turn1 = true;
                if (Vector3.Distance(item.position, new Vector3(-17, -.1f, 19)) < .01f) turn2 = true;
            }
            if (!turn1 || !turn2)
            {
                EditorUtility.DisplayDialog("Turns required", "Apply Add Turns to Current Level before Phase 2.", "OK");
                return;
            }
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (lit == null || particleShader == null)
            {
                EditorUtility.DisplayDialog("URP required", "URP shaders were not found. Send the Console error before continuing.", "OK");
                return;
            }
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Phase2Assets");
            AssetDatabase.CreateFolder("Assets/_Project", Path.GetFileName(folder));
            Material debris = Material(folder, "Debris", new Color(.25f, .20f, .17f), lit);
            Material black = Material(folder, "BurntDebris", new Color(.08f, .065f, .055f), lit);
            Material fireMaterial = CreateParticleMaterial(folder, particleShader);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add Phase 2 emergency");
            var root = new GameObject("Emergency_Scenario");
            var manager = root.AddComponent<FEEmergency>();
            manager.player = playerObject.GetComponent<FEFirstPerson>();
            var activeRoot = new GameObject("Emergency_Objects");
            activeRoot.transform.SetParent(root.transform, false);
            manager.emergencyRoot = activeRoot;
            Transform parent = activeRoot.transform;
            var lights = new List<Light>();

            Fire("Lab_Fire", new Vector3(3, .96f, 0), parent, fireMaterial, lights);
            Box("Lab_Burning_Object", new Vector3(3, 1.12f, 0), new Vector3(.5f, .32f, .5f), black, parent);

            // Route 2: a permanent blockage beyond its bend, visible on approach.
            Box("Exit2_Blockage", new Vector3(-17, 1.6f, 16.5f), new Vector3(1.9f, 3.2f, .65f), debris, parent);
            var tilted = Box("Exit2_Fallen_Beam", new Vector3(-17, 1.2f, 15.9f), new Vector3(1.7f, .25f, .4f), black, parent);
            tilted.transform.rotation = Quaternion.Euler(0, 0, 28);
            Fire("Exit2_Fire", new Vector3(-17, .1f, 15.6f), parent, fireMaterial, lights);

            // Route 1: hidden until the player reaches the final approach.
            var closure = new GameObject("Exit1_Triggered_Collapse");
            closure.transform.SetParent(parent, false);
            manager.route1Closure = closure;
            var barrier = new GameObject("Exit1_Collision_Barrier");
            barrier.transform.SetParent(closure.transform, false);
            barrier.transform.position = new Vector3(6, 1.6f, 21);
            barrier.AddComponent<BoxCollider>().size = new Vector3(.8f, 3.2f, 1.9f);
            var falling = new List<Transform>();
            for (int i = 0; i < 4; i++)
            {
                var chunk = Box("Falling_Debris_" + i, new Vector3(6, .38f + i * .72f, 21), new Vector3(.9f, .74f, 1.86f), i % 2 == 0 ? debris : black, closure.transform);
                Object.DestroyImmediate(chunk.GetComponent<BoxCollider>()); // Static barrier handles collision.
                chunk.transform.rotation = Quaternion.Euler((i % 2 == 0 ? 1 : -1) * 8, 0, 0);
                falling.Add(chunk.transform);
            }
            manager.fallingDebris = falling.ToArray();
            Fire("Exit1_Fire", new Vector3(5.5f, .1f, 21), closure.transform, fireMaterial, lights);

            // Each obstacle leaves at least 1.16 m of clear corridor width.
            Box("Route3_Obstacle_A", new Vector3(8, .45f, 11.6f), new Vector3(1.1f, .9f, .55f), debris, parent);
            Box("Route3_Obstacle_B", new Vector3(17.6f, .45f, 21), new Vector3(.55f, .9f, 1.1f), debris, parent);
            Box("Route3_Obstacle_C", new Vector3(23, .45f, 31.6f), new Vector3(1.1f, .9f, .55f), debris, parent);
            Fire("Route3_Side_Fire", new Vector3(17.65f, .95f, 21), parent, fireMaterial, lights, .55f);
            manager.emergencyLights = lights.ToArray();
            manager.hazardAreas = new[]
            {
                new Bounds(new Vector3(3, 1, 0), new Vector3(2.7f, 3, 2.7f)),
                new Bounds(new Vector3(-17, 1, 15.3f), new Vector3(1.85f, 3, 1.25f)),
                new Bounds(new Vector3(17.65f, 1, 21), new Vector3(.9f, 3, 1.6f)),
                // Last entry belongs to the conditional Exit 1 collapse.
                new Bounds(new Vector3(5.25f, 1, 21), new Vector3(1.1f, 3, 1.85f))
            };
            closure.SetActive(false);
            activeRoot.SetActive(false);
            Undo.RegisterCreatedObjectUndo(root, "Create emergency scenario");
            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(level.scene);
            Selection.activeGameObject = root;
            Debug.Log("Phase 2 added to current scene. Ctrl+S to save, then Play and Start Evacuation. Start with low speaker volume.");
        }

        private static Material Material(string folder, string name, Color color, Shader shader)
        {
            var result = new Material(shader) { name = name };
            result.SetColor("_BaseColor", color);
            result.SetFloat("_Smoothness", .1f);
            AssetDatabase.CreateAsset(result, folder + "/" + name + ".mat");
            return result;
        }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.position = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            return result;
        }
        private static Material CreateParticleMaterial(string folder, Shader shader)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float radius = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1).magnitude;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1 - radius), 1.5f);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            string path = folder + "/SoftParticle.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            var material = new Material(shader) { name = "FireAndSmoke", renderQueue = 3000 };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            AssetDatabase.CreateAsset(material, folder + "/FireAndSmoke.mat");
            return material;
        }
        private static void Fire(string name, Vector3 position, Transform parent, Material material, List<Light> lights, float scale = 1)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.position = position;
            Emitter("Flames", group.transform, material, false, scale);
            Emitter("Smoke", group.transform, material, true, scale);
            var light = new GameObject("Fire_Light").AddComponent<Light>();
            light.transform.SetParent(group.transform, false);
            light.transform.localPosition = Vector3.up;
            light.type = LightType.Point;
            light.color = new Color(1, .24f, .035f);
            light.range = 5 * scale;
            light.intensity = 2;
            light.shadows = LightShadows.None;
            lights.Add(light);
        }
        private static void Emitter(string name, Transform parent, Material material, bool smoke, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(-90, 0, 0); // Cone +Z points upward.
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 3;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = smoke ? 70 : 100;
            main.startLifetime = smoke ? 3 : .9f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(.7f * scale, 1.4f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve((smoke ? .8f : .3f) * scale, (smoke ? 1.2f : .7f) * scale);
            main.startColor = Color.white;
            var emission = ps.emission;
            emission.rateOverTime = smoke ? 12 : 65;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = smoke ? 12 : 8;
            shape.radius = .35f * scale;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, smoke ? 2 : .2f));
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(smoke ? new[] { new GradientColorKey(new Color(.2f, .2f, .2f), 0), new GradientColorKey(new Color(.36f, .36f, .36f), 1) }
                : new[] { new GradientColorKey(new Color(1, .9f, .15f), 0), new GradientColorKey(new Color(1, .25f, .02f), .55f), new GradientColorKey(new Color(.45f, .05f, .01f), 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(smoke ? .5f : 1, .12f), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
