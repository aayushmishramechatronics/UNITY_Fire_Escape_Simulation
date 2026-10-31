using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FireEscape
{
    public static class FEStarterBuilder
    {
        private const float CellSize = 2f;

        [MenuItem("Fire Escape/Add Turns to Current Level")]
        public static void AddTurnsToCurrentLevel()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Stop Play mode", "Stop Play mode before editing the level.", "OK");
                return;
            }
            var level = GameObject.Find("Level_Geometry");
            if (level == null || level.transform.Find("RouteTurns_Additions") != null)
            {
                EditorUtility.DisplayDialog("Level check", "Open the original Phase 1 level. This update can only be applied once.", "OK");
                return;
            }
            if (level.transform.position != Vector3.zero || level.transform.rotation != Quaternion.identity || level.transform.lossyScale != Vector3.one)
            {
                EditorUtility.DisplayDialog("Level transform changed", "This update expects the original Level_Geometry transform. Send a screenshot of its Inspector before continuing.", "OK");
                return;
            }
            var cells = new HashSet<Vector2Int>();
            Material route1 = null, route2 = null, wallMaterial = null;
            GameObject opening1 = null, opening2 = null;
            Transform sign1 = null, sign2 = null;
            foreach (Transform item in level.transform)
            {
                Vector3 p = item.position;
                if (item.name.StartsWith("Floor_"))
                {
                    var key = new Vector2Int(Mathf.RoundToInt((p.x - 1) / 2), Mathf.RoundToInt((p.z - 1) / 2));
                    cells.Add(key);
                    if (key == new Vector2Int(0, 10)) route1 = item.GetComponent<Renderer>().sharedMaterial;
                    if (key == new Vector2Int(-9, 5)) route2 = item.GetComponent<Renderer>().sharedMaterial;
                }
                if (item.name == "Wall")
                {
                    wallMaterial = item.GetComponent<Renderer>().sharedMaterial;
                    if (Vector3.Distance(p, new Vector3(2, 1.6f, 21)) < .01f) opening1 = item.gameObject;
                    if (Vector3.Distance(p, new Vector3(-17, 1.6f, 12)) < .01f) opening2 = item.gameObject;
                }
                if (item.name.StartsWith("Sign_EXIT 1")) sign1 = item;
                if (item.name.StartsWith("Sign_EXIT 2")) sign2 = item;
            }
            var additions = new Dictionary<Vector2Int, Material>();
            for (int x = 1; x <= 3; x++) additions[new Vector2Int(x, 10)] = route1;
            for (int z = 6; z <= 9; z++) additions[new Vector2Int(-9, z)] = route2;
            foreach (var key in additions.Keys)
            {
                if (cells.Contains(key))
                {
                    EditorUtility.DisplayDialog("Layout check", "These turns already exist or this layout has changed. No changes were made.", "OK");
                    return;
                }
            }
            if (opening1 == null || opening2 == null || route1 == null || route2 == null || wallMaterial == null || sign1 == null || sign2 == null)
            {
                EditorUtility.DisplayDialog("Layout check", "Could not identify the original route walls, floors, or signs. No changes were made. Send a screenshot of the Hierarchy.", "OK");
                return;
            }
            foreach (var key in additions.Keys) cells.Add(key);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add turns to Exits 1 and 2");
            Undo.DestroyObjectImmediate(opening1);
            Undo.DestroyObjectImmediate(opening2);
            var container = new GameObject("RouteTurns_Additions");
            container.transform.SetParent(level.transform, false);
            Vector2Int[] directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            foreach (var cell in additions)
            {
                Vector3 center = new Vector3(cell.Key.x * 2 + 1, 0, cell.Key.y * 2 + 1);
                Box("Floor_" + cell.Key, center + Vector3.down * .1f, new Vector3(2, .2f, 2), cell.Value, container.transform);
                foreach (var direction in directions)
                {
                    if (cells.Contains(cell.Key + direction)) continue;
                    Vector3 edge = center + new Vector3(direction.x, 1.6f, direction.y);
                    Vector3 size = direction.x == 0 ? new Vector3(2.16f, 3.2f, .16f) : new Vector3(.16f, 3.2f, 2.16f);
                    Box("Wall", edge, size, wallMaterial, container.transform);
                }
            }
            Undo.RegisterCreatedObjectUndo(container, "Create route extensions");
            Undo.RecordObjects(new Object[] { sign1, sign2 }, "Move exit signs");
            sign1.SetPositionAndRotation(new Vector3(7.85f, 2, 21), Quaternion.Euler(0, 90, 0));
            sign2.SetPositionAndRotation(new Vector3(-17, 2, 19.85f), Quaternion.identity);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(level.scene);
            Debug.Log("Turns added to your CURRENT scene. Press Ctrl+S to save, then Play. Ctrl+Z undoes this edit before saving if needed.");
        }

        [MenuItem("Fire Escape/Create Phase 1 Walkthrough")]
        public static void Create()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Stop Play mode", "Stop Play mode before creating the level.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                EditorUtility.DisplayDialog("URP required", "Use the Universal 3D project template for this starter.", "OK");
                return;
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Project")) AssetDatabase.CreateFolder("Assets", "_Project");
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Phase1Generated");
            AssetDatabase.CreateFolder("Assets/_Project", System.IO.Path.GetFileName(folder));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Level_Geometry").transform;
            Material wall = MakeMaterial(folder, "Walls", new Color(.65f, .71f, .75f), shader);
            Material lab = MakeMaterial(folder, "LabFloor", new Color(.36f, .40f, .44f), shader);
            Material corridor = MakeMaterial(folder, "Corridor", new Color(.52f, .55f, .58f), shader);
            Material nearest = MakeMaterial(folder, "Route1_Yellow", new Color(.80f, .65f, .20f), shader);
            Material middle = MakeMaterial(folder, "Route2_Orange", new Color(.75f, .38f, .18f), shader);
            Material farthest = MakeMaterial(folder, "Route3_Blue", new Color(.15f, .43f, .65f), shader);
            Material safe = MakeMaterial(folder, "Assembly_Green", new Color(.18f, .57f, .32f), shader);
            Material bench = MakeMaterial(folder, "Benches", new Color(.19f, .25f, .29f), shader);

            // A union of 2 m floor cells. Walls are created only on its perimeter,
            // leaving every shared edge open, including all corridor junctions.
            var cells = new Dictionary<Vector2Int, Material>();
            for (int x = -3; x <= 2; x++)
                for (int z = -3; z <= 2; z++) cells[new Vector2Int(x, z)] = lab;
            for (int z = 3; z <= 5; z++) cells[new Vector2Int(0, z)] = corridor;
            for (int z = 6; z <= 10; z++) cells[new Vector2Int(0, z)] = nearest;
            // Exit 1 turns right (east) for a short final approach.
            for (int x = 1; x <= 3; x++) cells[new Vector2Int(x, 10)] = nearest;
            for (int x = -1; x >= -9; x--) cells[new Vector2Int(x, 5)] = middle;
            // Exit 2 turns right (north) after its westbound corridor.
            for (int z = 6; z <= 9; z++) cells[new Vector2Int(-9, z)] = middle;
            for (int x = 1; x <= 8; x++) cells[new Vector2Int(x, 5)] = farthest;
            for (int z = 6; z <= 15; z++) cells[new Vector2Int(8, z)] = farthest;
            for (int x = 9; x <= 12; x++) cells[new Vector2Int(x, 15)] = farthest;
            for (int x = 13; x <= 15; x++)
                for (int z = 14; z <= 16; z++) cells[new Vector2Int(x, z)] = safe;

            Vector2Int[] directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            foreach (var cell in cells)
            {
                Vector3 center = new Vector3(cell.Key.x * CellSize + 1, 0, cell.Key.y * CellSize + 1);
                Box("Floor_" + cell.Key, center + Vector3.down * .1f, new Vector3(2, .2f, 2), cell.Value, root);
                foreach (var direction in directions)
                {
                    if (cells.ContainsKey(cell.Key + direction)) continue;
                    Vector3 edge = center + new Vector3(direction.x, 1.6f, direction.y);
                    Vector3 size = direction.x == 0 ? new Vector3(2.16f, 3.2f, .16f) : new Vector3(.16f, 3.2f, 2.16f);
                    Box("Wall", edge, size, wall, root);
                }
            }
            foreach (float x in new[] { -3f, 3f })
                foreach (float z in new[] { -3f, 0f, 3f })
                    Box("Lab_Bench", new Vector3(x, .45f, z), new Vector3(2f, .9f, 1.1f), bench, root);

            Label("CHEMISTRY LAB", new Vector3(0, 2.3f, 5.85f), Quaternion.identity, root);
            Label("EXIT 1\nNearest route", new Vector3(7.85f, 2, 21), Quaternion.Euler(0, 90, 0), root);
            Label("EXIT 2\nIntermediate route", new Vector3(-17, 2, 19.85f), Quaternion.identity, root);
            Label("ASSEMBLY AREA", new Vector3(29, 2.3f, 33.85f), Quaternion.identity, root);

            var player = new GameObject("Player");
            player.transform.position = new Vector3(1, .06f, -4);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = new Vector3(0, .9f, 0);
            controller.radius = .3f;
            controller.stepOffset = .25f;
            controller.skinWidth = .03f;
            controller.minMoveDistance = 0;
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.65f, 0);
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 150;
            camera.fieldOfView = 75;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.34f, .43f, .53f);
            cameraObject.AddComponent<AudioListener>();
            player.AddComponent<FEFirstPerson>().viewCamera = camera;
            var sun = new GameObject("Daylight").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.5f;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.65f, .68f, .72f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            AssetDatabase.SaveAssets();
            string path = folder + "/MainLevel_Phase1.unity";
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                Debug.LogError("Could not save scene. Use File > Save As before continuing.");
                return;
            }
            Selection.activeGameObject = player;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            Debug.Log("Phase 1 created and saved: " + path + ". Press Play, open Game, then click inside it.");
        }

        private static Material MakeMaterial(string folder, string name, Color color, Shader shader)
        {
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .15f);
            AssetDatabase.CreateAsset(material, folder + "/" + name + ".mat");
            return material;
        }

        private static void Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.position = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void Label(string text, Vector3 position, Quaternion rotation, Transform parent)
        {
            var item = new GameObject("Sign_" + text.Replace('\n', '_'));
            item.transform.SetParent(parent, false);
            item.transform.SetPositionAndRotation(position, rotation);
            var label = item.AddComponent<TextMesh>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48;
            label.characterSize = .035f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.black;
            item.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
        }
    }
}
