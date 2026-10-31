using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace FireEscape
{
    public static class FEInteriorBuilder
    {
        private const string Source = "Assets/FireEscapeStarter/InteriorAssets/";
        [Serializable] private sealed class Area { public string id, label, kind; public int x0, x1, z0, z1; }
        [Serializable] private sealed class Opening { public string id, label, axis, mode, warning; public float x, z, yaw; }
        [Serializable] private sealed class WindowSpec { public string axis; public float x, z; }
        [Serializable] private sealed class Layout { public Area[] areas; public Opening[] doors; public WindowSpec[] windows; public float mapX, mapZ; }
        private static Material wall, trim, floor, ceiling, wood, metal, teal, glass, white, red, yellow, green, black, concrete, particle;
        private static Transform geometry;
        private static string output;
        private static Font font;
        private static FEEmergency emergency;
        private static readonly List<Light> fireLights = new List<Light>();

        [MenuItem("Fire Escape/Upgrade Current Level - Department Interior")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlaying) { Message("Stop Play mode before upgrading the scene."); return; }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path)) { Message("Save your working scene with Ctrl+S first."); return; }
            var oldLevel = GameObject.Find("Level_Geometry");
            var oldEmergency = GameObject.Find("Emergency_Scenario");
            var playerObject = GameObject.Find("Player");
            if (oldLevel == null || oldEmergency == null || playerObject == null || playerObject.GetComponent<FEFirstPerson>() == null)
            { Message("Open your working Phase 2 scene before applying this update."); return; }
            if (GameObject.Find("Department_Interior_V3") != null) { Message("The department interior is already installed in this scene."); return; }
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(Source + "DepartmentLayout.json");
            Texture2D plan = AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "EvacuationPlan.png");
            if (source == null || plan == null || Shader.Find("Universal Render Pipeline/Lit") == null)
            { Message("Interior assets or URP shaders are missing. Copy the full update folder into Assets first."); return; }
            Layout layout = JsonUtility.FromJson<Layout>(source.text);
            var cells = new Dictionary<Vector2Int, Area>();
            foreach (var area in layout.areas)
                for (int x = area.x0 / 2; x < area.x1 / 2; x++)
                    for (int z = area.z0 / 2; z < area.z1 / 2; z++)
                    {
                        var key = new Vector2Int(x, z);
                        if (cells.ContainsKey(key)) { Message("Layout contains overlapping rooms. No scene changes made."); return; }
                        cells.Add(key, area);
                    }
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Backups");
            string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Backups/Before_Department_Interior.unity");
            if (!EditorSceneManager.SaveScene(scene, backup, true)) { Message("Could not save the scene backup. No scene changes made."); return; }
            output = AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/DepartmentMaterials");
            AssetDatabase.CreateFolder("Assets/_Project", Path.GetFileName(output));
            MakeMaterials();
            ConfigurePlanTexture();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Upgrade department interior");
            try
            {
                // Keep the original geometry and emergency setup as inactive objects.
                Archive(oldLevel, "Phase2_Level_Backup");
                Archive(oldEmergency, "Phase2_Emergency_Backup");
                var oldDaylight = GameObject.Find("Daylight");
                if (oldDaylight != null) Archive(oldDaylight, "Phase2_Daylight_Backup");
                var root = new GameObject("Department_Interior_V3");
                Undo.RegisterCreatedObjectUndo(root, "Create department interior");
                geometry = Child("Level_Geometry", root.transform).transform;
                var scenario = Child("Emergency_Scenario", root.transform);
                emergency = scenario.AddComponent<FEEmergency>();
                emergency.player = playerObject.GetComponent<FEFirstPerson>();
                var oldManager = oldEmergency.GetComponent<FEEmergency>();
                if (oldManager != null) emergency.exposurePerSecond = oldManager.exposurePerSecond;

                BuildShell(layout, cells);
                Furnish(layout);
                BuildMap(layout, plan);
                BuildSigns();
                BuildEmergency();
                var doorList = new List<FEDoor>();
                foreach (var door in layout.doors)
                {
                    FEDoor built = BuildDoor(door);
                    if (built != null) doorList.Add(built);
                }
                var interaction = root.AddComponent<FEInteriorInteraction>();
                interaction.player = emergency.player;
                interaction.emergency = emergency;
                interaction.doors = doorList.ToArray();
                Undo.RecordObject(playerObject.transform, "Position player in new laboratory");
                playerObject.transform.SetPositionAndRotation(new Vector3(0, .06f, -6), Quaternion.identity);
                if (emergency.player.viewCamera != null)
                {
                    Undo.RecordObject(emergency.player.viewCamera.transform, "Reset camera view");
                    emergency.player.viewCamera.transform.localRotation = Quaternion.identity;
                }
                var sun = Child("Courtyard_Daylight", root.transform).AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(48, -35, 0);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.43f, .46f, .5f);
                RenderSettings.fog = false;
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeGameObject = root;
                Debug.Log("Department interior installed in the CURRENT scene. Ctrl+S to save. Backup: " + backup);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception);
                Message("The upgrade stopped and scene changes were rolled back. Send the first Console error. A scene backup was saved at " + backup);
            }
        }

        private static void BuildShell(Layout layout, Dictionary<Vector2Int, Area> cells)
        {
            var doorEdges = new HashSet<string>();
            var windowEdges = new HashSet<string>();
            foreach (var door in layout.doors) doorEdges.Add(Edge(door.axis, door.x, door.z));
            foreach (var window in layout.windows) windowEdges.Add(Edge(window.axis, window.x, window.z));
            var built = new HashSet<string>();
            Vector2Int[] directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            Transform shell = Child("Architecture", geometry).transform;
            foreach (var cell in cells)
            {
                Vector3 center = new Vector3(cell.Key.x * 2 + 1, 0, cell.Key.y * 2 + 1);
                Box("Floor_" + cell.Key, center + Vector3.down * .1f, new Vector3(2, .2f, 2), cell.Value.kind == "outdoor" ? concrete : floor, shell);
                if (cell.Value.kind != "outdoor")
                    Box("Ceiling", center + Vector3.up * 3.4f, new Vector3(2, .2f, 2), ceiling, shell);
                foreach (var direction in directions)
                {
                    cells.TryGetValue(cell.Key + direction, out Area neighbor);
                    bool circulation = cell.Value.kind != "room" && neighbor != null && neighbor.kind != "room";
                    if (neighbor != null && (neighbor.id == cell.Value.id || circulation)) continue;
                    float x = center.x + direction.x, z = center.z + direction.y;
                    string axis = direction.x == 0 ? "z" : "x";
                    string key = Edge(axis, x, z);
                    if (!built.Add(key)) continue;
                    if (doorEdges.Contains(key)) continue;
                    bool low = cell.Value.kind == "outdoor" && neighbor == null;
                    BuildWall(x, z, axis, windowEdges.Contains(key), low, shell);
                }
            }
            foreach (var area in layout.areas)
            {
                if (area.kind == "outdoor") continue;
                for (int x = area.x0 + 2; x < area.x1; x += 6)
                    for (int z = area.z0 + 2; z < area.z1; z += 6) Fixture(new Vector3(x, 3.22f, z));
            }
        }

        private static void BuildWall(float x, float z, string axis, bool window, bool low, Transform parent)
        {
            Vector3 Size(float length, float height, float depth) => axis == "z" ? new Vector3(length, height, depth) : new Vector3(depth, height, length);
            if (low) { Box("Courtyard_Parapet", new Vector3(x, .45f, z), Size(2.16f, .9f, .18f), concrete, parent); return; }
            if (window)
            {
                Box("Window_Lower_Wall", new Vector3(x, .55f, z), Size(2.16f, 1.1f, .18f), wall, parent);
                Box("Window_Upper_Wall", new Vector3(x, 3.1f, z), Size(2.16f, .4f, .18f), wall, parent);
                Box("Glass_Partition", new Vector3(x, 2, z), Size(1.9f, 1.8f, .05f), glass, parent);
                Box("Window_Sill", new Vector3(x, 1.12f, z), Size(2, .09f, .26f), metal, parent);
                Box("Window_Mullion", new Vector3(x, 2, z), Size(.06f, 1.8f, .1f), metal, parent);
            }
            else Box("Wall", new Vector3(x, 1.65f, z), Size(2.16f, 3.3f, .18f), wall, parent);
            Box("Skirting", new Vector3(x, .09f, z), Size(2.16f, .18f, .21f), trim, parent);
            if (!window) Box("Wall_Band", new Vector3(x, 1.08f, z), Size(2.16f, .11f, .205f), teal, parent);
        }

        private static FEDoor BuildDoor(Opening spec)
        {
            var root = Child("Door_" + spec.id, geometry);
            root.transform.SetPositionAndRotation(new Vector3(spec.x, 0, spec.z), Quaternion.Euler(0, spec.yaw, 0));
            LocalBox("Frame_Left", new Vector3(-.95f, 1.32f, 0), new Vector3(.1f, 2.64f, .26f), metal, root.transform);
            LocalBox("Frame_Right", new Vector3(.95f, 1.32f, 0), new Vector3(.1f, 2.64f, .26f), metal, root.transform);
            LocalBox("Frame_Header", new Vector3(0, 2.65f, 0), new Vector3(2, .12f, .26f), metal, root.transform);
            LocalBox("Above_Door", new Vector3(0, 3, 0), new Vector3(2.1f, .6f, .18f), wall, root.transform);
            Plate(spec.label, new Vector3(spec.x, 2.99f, spec.z) + root.transform.forward * -.16f, root.transform.rotation, 1.85f, .35f, teal, root.transform, 31);
            if (spec.mode == "passage") return null;
            var door = root.AddComponent<FEDoor>();
            door.roomName = spec.label.ToLowerInvariant();
            door.kind = spec.mode == "fatal" ? FEDoor.DoorKind.Fatal : spec.mode == "safe" ? FEDoor.DoorKind.Safe : FEDoor.DoorKind.Locked;
            door.emergency = emergency;
            Transform hinge = Child("Hinge", root.transform).transform;
            hinge.localPosition = new Vector3(-.88f, 0, 0);
            var leaf = LocalBox("Door_Leaf", new Vector3(.88f, 1.29f, 0), new Vector3(1.76f, 2.58f, .1f), wood, hinge);
            door.hinge = hinge;
            door.leafCollider = leaf.GetComponent<Collider>();
            LocalBox("Handle", new Vector3(1.55f, 1.1f, -.09f), new Vector3(.18f, .045f, .07f), metal, hinge, false);
            LocalBox("Kickplate", new Vector3(.88f, .3f, -.057f), new Vector3(1.55f, .35f, .015f), metal, hinge, false);
            if (spec.mode == "fatal")
            {
                var warning = Child("Warning_Label", hinge);
                warning.transform.localPosition = new Vector3(.88f, 1.8f, -.075f);
                LocalBox("Warning_Back", Vector3.zero, new Vector3(1.53f, .55f, .025f), yellow, warning.transform, false);
                Text(spec.warning.Replace(" - ", "\n"), warning.transform.position + root.transform.forward * -.018f, root.transform.rotation, 1.42f, .46f, Color.black, warning.transform, 40);
                Transform effects = emergency.emergencyRoot.transform;
                Vector3 smokePosition = root.transform.position - root.transform.forward * .16f + Vector3.up * .05f;
                Particle("Door_Seep_" + spec.id, smokePosition, effects, true, .28f, false);
                // Visible, one-shot burst just behind the leaf as it swings open.
                door.explosion = Particle("Explosion_" + spec.id, root.transform.position + root.transform.forward * .5f + Vector3.up * 1.2f, root.transform, false, 2.4f, true);
            }
            return door;
        }

        private static void BuildMap(Layout layout, Texture2D texture)
        {
            var board = Child("Wall_Evacuation_Plan", geometry);
            board.transform.position = new Vector3(layout.mapX, 1.8f, layout.mapZ - .09f);
            var component = board.AddComponent<FEWallPlan>(); component.plan = texture;
            LocalBox("Frame", Vector3.zero, new Vector3(3.3f, 2.68f, .09f), metal, board.transform);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Plan_Surface";
            quad.transform.SetParent(board.transform, false);
            quad.transform.localPosition = new Vector3(0, 0, -.051f);
            quad.transform.localScale = new Vector3(3.2f, 2.56f, 1);
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetTexture("_BaseMap", texture);
            AssetDatabase.CreateAsset(material, output + "/EvacuationPlan.mat");
            quad.GetComponent<Renderer>().sharedMaterial = material;
            Plate("EVACUATION PLAN - PRESS E TO INSPECT", board.transform.position + new Vector3(0, -1.48f, -.03f), Quaternion.identity, 3.3f, .22f, teal, geometry, 30);
        }

        private static void BuildSigns()
        {
            Plate("< EXIT 2     EXIT 1 ^     EXIT 3 >", new Vector3(0, 2.92f, 13.65f), Quaternion.identity, 5.2f, .38f, green, geometry, 42);
            Plate("EXIT 1  ^", new Vector3(0, 2.9f, 15.8f), Quaternion.identity, 2.2f, .38f, green, geometry, 42);
            Plate("EXIT 2  ^", new Vector3(-5, 2.9f, 12), Quaternion.Euler(0, -90, 0), 2.1f, .38f, green, geometry, 42);
            Plate("EXIT 3  ^", new Vector3(5, 2.9f, 12), Quaternion.Euler(0, 90, 0), 2.1f, .38f, green, geometry, 42);
            Plate("EXIT 3  ^", new Vector3(24, 2.9f, 16), Quaternion.identity, 2.2f, .38f, green, geometry, 42);
            Plate("EXIT 3  <", new Vector3(24, 2.85f, 33.75f), Quaternion.identity, 2.2f, .38f, green, geometry, 42);
            Plate("EXIT 3  >", new Vector3(6.23f, 2.85f, 32), Quaternion.Euler(0, -90, 0), 2.2f, .38f, green, geometry, 42);
            Plate("EXIT 3  >", new Vector3(8, 2.85f, 41.75f), Quaternion.identity, 2.2f, .38f, green, geometry, 42);
            Plate("EXIT 3 - ASSEMBLY", new Vector3(29.8f, 2.85f, 40), Quaternion.Euler(0, 90, 0), 3.4f, .45f, green, geometry, 42);
            FakeExit(new Vector3(9.85f, 0, 20), Quaternion.Euler(0, 90, 0), "EXIT 1");
            FakeExit(new Vector3(-25.85f, 0, 24), Quaternion.Euler(0, -90, 0), "EXIT 2");
            Plate("ASSEMBLY POINT", new Vector3(37, 2, 44), Quaternion.identity, 2.6f, .8f, green, geometry, 42);
            Box("Assembly_Sign_Post", new Vector3(37, .8f, 44.08f), new Vector3(.1f, 1.7f, .1f), metal, geometry);
        }

        private static void BuildEmergency()
        {
            fireLights.Clear();
            Transform parent = Child("Emergency_Objects", emergency.transform).transform;
            emergency.emergencyRoot = parent.gameObject;
            Fire("Lab_Fire", new Vector3(4.8f, 1.02f, -3), parent, 1);
            Box("Exit2_Debris_Blockage", new Vector3(-24, 1.6f, 24), new Vector3(.8f, 3.2f, 3.88f), concrete, parent);
            Fire("Exit2_Fire", new Vector3(-23.35f, .05f, 24), parent, 1.2f);
            Transform closure = Child("Exit1_Triggered_Collapse", parent).transform;
            emergency.route1Closure = closure.gameObject;
            var barrier = Child("Collision_Barrier", closure);
            barrier.transform.position = new Vector3(8, 1.6f, 20);
            barrier.AddComponent<BoxCollider>().size = new Vector3(.8f, 3.2f, 3.88f);
            var debris = new List<Transform>();
            for (int i = 0; i < 4; i++)
            {
                var chunk = Box("Falling_Rubble_" + i, new Vector3(8, .38f + i * .72f, 20), new Vector3(.95f, .74f, 3.8f), i % 2 == 0 ? concrete : black, closure, false);
                chunk.transform.rotation = Quaternion.Euler(i % 2 == 0 ? 5 : -5, 0, 0);
                debris.Add(chunk.transform);
            }
            Fire("Exit1_Fire", new Vector3(7.35f, .05f, 20), closure, 1.1f);
            emergency.fallingDebris = debris.ToArray();
            emergency.collapseApproach = new Bounds(new Vector3(4.2f, 1, 20), new Vector3(2.6f, 3, 3.85f));
            emergency.assemblyArea = new Bounds(new Vector3(35, 1, 40), new Vector3(7, 3, 8));
            emergency.route1VisitArea = new Bounds(new Vector3(0, 1, 18), new Vector3(3.8f, 3, 7.5f));
            emergency.route2VisitArea = new Bounds(new Vector3(-12, 1, 12), new Vector3(15, 3, 3.8f));
            Box("Corridor_Debris_A", new Vector3(12, .5f, 13.25f), new Vector3(1.4f, 1, 1.1f), concrete, parent);
            Box("Corridor_Debris_B", new Vector3(25.3f, .5f, 23), new Vector3(1.2f, 1, 1.5f), black, parent);
            Box("Corridor_Debris_C", new Vector3(15, .45f, 33.25f), new Vector3(1.8f, .9f, 1.1f), concrete, parent);
            Fire("Side_Fire", new Vector3(25.4f, 1, 23), parent, .6f);
            emergency.hazardAreas = new[]
            {
                new Bounds(new Vector3(4.8f, 1, -3), new Vector3(2.6f, 3, 2.6f)),
                new Bounds(new Vector3(-23.15f, 1, 24), new Vector3(1.2f, 3, 3.8f)),
                new Bounds(new Vector3(25.4f, 1, 23), new Vector3(1.3f, 3, 2)),
                new Bounds(new Vector3(7.25f, 1, 20), new Vector3(1.2f, 3, 3.8f))
            };
            emergency.emergencyLights = fireLights.ToArray();
            emergency.explosionClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Source + "Explosion.wav");
            emergency.collapseClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Source + "Collapse.wav");
            closure.gameObject.SetActive(false);
            parent.gameObject.SetActive(false);
        }

        private static void Furnish(Layout layout)
        {
            Transform props = Child("Furniture_And_Lab_Equipment", geometry).transform;
            foreach (float x in new[] { -4.5f, 4.5f })
                foreach (float z in new[] { -6.5f, -3f }) Bench(new Vector3(x, 0, z), props, true);
            Bench(new Vector3(14, 0, 5), props, true);
            Bench(new Vector3(10, 0, 25), props, true);
            Bench(new Vector3(15, 0, 27), props, true);
            Desk(new Vector3(-11, 0, 5), props);
            Desk(new Vector3(30, 0, 23), props);
            Cabinet(new Vector3(-15.4f, 0, 5), props);
            Cabinet(new Vector3(-12, 0, 18.5f), props);
            Cabinet(new Vector3(-6, 0, 18.5f), props);
            Cabinet(new Vector3(18.8f, 0, 5), props);
            for (int i = 0; i < 3; i++)
            {
                Cylinder("Storage_Drum", new Vector3(-12 + i * 2.5f, .55f, 16.5f), new Vector3(.7f, .55f, .7f), i == 1 ? yellow : teal, props);
                Box("Drum_Label", new Vector3(-12 + i * 2.5f, .6f, 16.12f), new Vector3(.25f, .3f, .02f), yellow, props, false);
            }
            foreach (Vector3 p in new[] { new Vector3(-4, .75f, 10.22f), new Vector3(22.24f, .75f, 18), new Vector3(12, .75f, 38.23f) })
            {
                Cylinder("Fire_Extinguisher", p, new Vector3(.2f, .35f, .2f), red, props);
                Box("Extinguisher_Handle", p + Vector3.up * .38f, new Vector3(.16f, .08f, .07f), black, props, false);
            }
            Box("Office_Rug", new Vector3(-11, .012f, 6), new Vector3(5, .02f, 4), teal, props, false);
            Bench(new Vector3(34, 0, 44), props, false);
            Bench(new Vector3(38, 0, 36), props, false);
            Plate("DEPARTMENT OF CHEMISTRY", new Vector3(5.3f, 2.2f, 1.77f), Quaternion.identity, 4.3f, .6f, teal, props, 40);
        }

        private static void Bench(Vector3 position, Transform parent, bool equipment)
        {
            Box("Bench_Top", position + Vector3.up * .93f, new Vector3(2.8f, .14f, 1.2f), black, parent);
            foreach (float x in new[] { -1.18f, 1.18f })
                foreach (float z in new[] { -.43f, .43f }) Box("Bench_Leg", position + new Vector3(x, .44f, z), new Vector3(.09f, .88f, .09f), metal, parent);
            if (!equipment) return;
            Box("Bench_Cabinet", position + new Vector3(-.7f, .44f, 0), new Vector3(1, .8f, 1), white, parent);
            Box("Instrument", position + new Vector3(.8f, 1.18f, .1f), new Vector3(.48f, .4f, .42f), white, parent);
            Box("Instrument_Display", position + new Vector3(.8f, 1.23f, -.117f), new Vector3(.28f, .15f, .018f), teal, parent, false);
            for (int i = 0; i < 3; i++)
            {
                Cylinder("Reagent_Bottle", position + new Vector3(-.7f + i * .32f, 1.15f, .12f), new Vector3(.14f, .15f, .14f), i == 1 ? green : white, parent);
                Cylinder("Bottle_Cap", position + new Vector3(-.7f + i * .32f, 1.32f, .12f), new Vector3(.085f, .025f, .085f), black, parent, false);
            }
        }
        private static void Desk(Vector3 position, Transform parent)
        {
            Box("Desk", position + Vector3.up * .72f, new Vector3(2.7f, .1f, 1.25f), wood, parent);
            Box("Desk_Drawers", position + new Vector3(-.85f, .35f, 0), new Vector3(.7f, .7f, 1), metal, parent);
            Box("Desk_Leg", position + new Vector3(1.1f, .35f, 0), new Vector3(.1f, .7f, 1), metal, parent);
            Box("Monitor", position + new Vector3(.3f, 1.12f, -.3f), new Vector3(.8f, .48f, .05f), black, parent);
            Box("Screen", position + new Vector3(.3f, 1.12f, -.265f), new Vector3(.72f, .4f, .01f), teal, parent, false);
            Box("Chair_Seat", position + new Vector3(.2f, .45f, 1.1f), new Vector3(.6f, .1f, .6f), teal, parent);
            Box("Chair_Back", position + new Vector3(.2f, .78f, 1.4f), new Vector3(.6f, .7f, .08f), teal, parent);
            Cylinder("Chair_Base", position + new Vector3(.2f, .22f, 1.1f), new Vector3(.12f, .22f, .12f), metal, parent);
        }
        private static void Cabinet(Vector3 position, Transform parent)
        {
            Box("Cabinet", position + Vector3.up, new Vector3(1.2f, 2, .7f), white, parent);
            for (int i = 0; i < 3; i++) Box("Cabinet_Seam", position + new Vector3(0, .5f + .5f * i, -.359f), new Vector3(1.1f, .025f, .02f), metal, parent, false);
        }
        private static void Fixture(Vector3 position)
        {
            var frame = Box("Ceiling_Light", position, new Vector3(1.35f, .09f, .45f), white, geometry, false);
            var light = frame.AddComponent<Light>(); light.type = LightType.Point; light.range = 7; light.intensity = 2;
            light.color = new Color(.91f, .95f, 1); light.shadows = LightShadows.None;
            light.transform.position = position + Vector3.down * .1f;
        }
        private static void FakeExit(Vector3 position, Quaternion rotation, string title)
        {
            var root = Child(title + "_Door", geometry).transform;
            root.SetPositionAndRotation(position, rotation);
            LocalBox("Exit_Double_Doors", new Vector3(0, 1.3f, 0), new Vector3(3.4f, 2.6f, .08f), teal, root);
            LocalBox("Panic_Bar", new Vector3(0, 1.1f, -.06f), new Vector3(2.9f, .07f, .07f), metal, root, false);
            Plate(title, position + Vector3.up * 2.95f + rotation * Vector3.back * .05f, rotation, 2.2f, .38f, green, geometry, 42);
        }

        private static void MakeMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            wall = Mat("Wall_Paint", new Color(.79f, .8f, .76f), shader);
            trim = Mat("Skirting", new Color(.14f, .18f, .2f), shader);
            floor = Mat("Floor_Tiles", Color.white, shader);
            floor.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "FloorTile.png"));
            ceiling = Mat("Ceiling", new Color(.82f, .83f, .8f), shader);
            wood = Mat("Door_And_Desk", new Color(.4f, .28f, .17f), shader);
            metal = Mat("Metal", new Color(.23f, .28f, .3f), shader);
            teal = Mat("Department_Teal", new Color(.08f, .27f, .29f), shader);
            white = Mat("Equipment_White", new Color(.85f, .87f, .85f), shader);
            red = Mat("Extinguisher_Red", new Color(.67f, .045f, .025f), shader);
            yellow = Mat("Warning_Yellow", new Color(.97f, .73f, .07f), shader);
            green = Mat("Exit_Green", new Color(.025f, .33f, .15f), Shader.Find("Universal Render Pipeline/Unlit"));
            black = Mat("Dark_Surface", new Color(.07f, .09f, .09f), shader);
            concrete = Mat("Concrete", new Color(.44f, .43f, .39f), shader);
            glass = Mat("Glass", new Color(.43f, .67f, .7f, .24f), shader);
            Transparent(glass);
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null) throw new InvalidOperationException("URP particle shader missing.");
            particle = Mat("Fire_Smoke", Color.white, particleShader);
            Texture2D soft = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var colors = new Color[4096];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float radius = new Vector2((x + .5f) / 32 - 1, (y + .5f) / 32 - 1).magnitude;
                colors[y * 64 + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - radius), 1.5f));
            }
            soft.SetPixels(colors); soft.Apply();
            soft.wrapMode = TextureWrapMode.Clamp;
            AssetDatabase.CreateAsset(soft, output + "/SoftParticle.asset");
            particle.SetTexture("_BaseMap", soft); Transparent(particle);
        }
        private static Material Mat(string name, Color color, Shader shader)
        {
            if (shader == null) throw new InvalidOperationException("Required shader missing: " + name);
            var result = new Material(shader) { name = name };
            result.SetColor("_BaseColor", color);
            if (result.HasProperty("_Smoothness")) result.SetFloat("_Smoothness", .2f);
            AssetDatabase.CreateAsset(result, output + "/" + name + ".mat");
            return result;
        }
        private static void Transparent(Material material)
        {
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
        }
        private static void Fire(string name, Vector3 position, Transform parent, float scale)
        {
            Transform root = Child(name, parent).transform; root.position = position;
            Particle("Flames", position, root, false, scale, false);
            Particle("Smoke", position, root, true, scale, false);
            Light light = Child("Fire_Light", root).AddComponent<Light>();
            light.transform.localPosition = Vector3.up; light.type = LightType.Point; light.range = 5 * scale;
            light.color = new Color(1, .25f, .04f); light.intensity = 2; light.shadows = LightShadows.None;
            fireLights.Add(light);
        }
        private static ParticleSystem Particle(string name, Vector3 position, Transform parent, bool smoke, float scale, bool burst)
        {
            GameObject go = Child(name, parent); go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(-90, 0, 0);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.duration = 3; main.loop = !burst; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = burst ? 180 : 100;
            main.startLifetime = burst ? .65f : smoke ? 1.8f : .9f;
            main.startSpeed = new ParticleSystem.MinMaxCurve((burst ? 1.5f : .6f) * scale, (burst ? 3 : 1.1f) * scale);
            main.startSize = new ParticleSystem.MinMaxCurve((smoke ? .7f : .3f) * scale, (smoke ? 1.1f : .7f) * scale);
            var emission = ps.emission; emission.rateOverTime = burst ? 0 : smoke ? 12 : 60;
            if (burst) emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)150) });
            var shape = ps.shape; shape.shapeType = burst ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Cone;
            shape.radius = .32f * scale; if (!burst) shape.angle = 10;
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, smoke || burst ? 2 : .2f));
            var color = ps.colorOverLifetime; color.enabled = true; var gradient = new Gradient();
            gradient.SetKeys(smoke ? new[] { new GradientColorKey(new Color(.16f, .17f, .18f), 0), new GradientColorKey(new Color(.35f, .36f, .37f), 1) }
                : new[] { new GradientColorKey(new Color(1, .85f, .12f), 0), new GradientColorKey(new Color(1, .2f, .015f), .5f), new GradientColorKey(new Color(.25f, .06f, .01f), 1) },
                new[] { new GradientAlphaKey(burst ? 1 : 0, 0), new GradientAlphaKey(smoke ? .5f : 1, .12f), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = particle;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return ps;
        }

        private static void Plate(string text, Vector3 position, Quaternion rotation, float width, float height, Material material, Transform parent, int fontSize)
        {
            var board = Child("Sign_" + text, parent);
            board.transform.SetPositionAndRotation(position, rotation);
            LocalBox("Sign_Back", Vector3.zero, new Vector3(width, height, .035f), material, board.transform, false);
            Text(text, position + rotation * Vector3.back * .025f, rotation, width * .92f, height * .8f, Color.white, board.transform, fontSize);
        }
        private static void Text(string text, Vector3 position, Quaternion rotation, float width, float height, Color color, Transform parent, int fontSize)
        {
            var go = Child("Lettering", parent); go.transform.SetPositionAndRotation(position, rotation);
            var mesh = go.AddComponent<TextMesh>(); mesh.text = text; mesh.font = font; mesh.fontSize = fontSize; mesh.characterSize = .1f;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center; mesh.color = color;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = font.material;
            // Fit by the generated local mesh bounds, independent of sign rotation.
            Vector3 extent = renderer.localBounds.size;
            if (extent.x > .001f && extent.y > .001f)
            {
                float factor = Mathf.Min(width / extent.x, height / extent.y);
                go.transform.localScale = Vector3.one * factor;
            }
            else
            {
                // TextMesh has no MeshFilter; obtain glyph advances if bounds
                // have not refreshed yet in the Editor.
                font.RequestCharactersInTexture(text, fontSize, FontStyle.Normal);
                float longest = 1, line = 0; int lines = 1;
                foreach (char letter in text)
                {
                    if (letter == '\n') { longest = Mathf.Max(longest, line); line = 0; lines++; continue; }
                    if (font.GetCharacterInfo(letter, out CharacterInfo info, fontSize, FontStyle.Normal)) line += info.advance;
                }
                longest = Mathf.Max(longest, line);
                mesh.characterSize = 10 * Mathf.Min(width / longest, height / (fontSize * lines * 1.2f));
            }
        }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private static GameObject LocalBox(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool collision = true)
        {
            var go = Box(name, parent.TransformPoint(position), scale, material, parent, collision);
            go.transform.localRotation = Quaternion.identity; return go;
        }
        private static GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name;
            go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private static GameObject Child(string name, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go;
        }
        private static string Edge(string axis, float x, float z) => axis + ":" + Mathf.RoundToInt(x) + ":" + Mathf.RoundToInt(z);
        private static void Archive(GameObject go, string name) { Undo.RecordObject(go, "Archive previous setup"); go.name = name; go.SetActive(false); }
        private static void Message(string text) { EditorUtility.DisplayDialog("Department interior", text, "OK"); }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static void ConfigurePlanTexture()
        {
            var importer = AssetImporter.GetAtPath(Source + "EvacuationPlan.png") as TextureImporter;
            if (importer == null) return;
            importer.maxTextureSize = 4096; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        }
    }
}
