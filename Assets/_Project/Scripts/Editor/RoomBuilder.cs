using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Roguelike.EditorTools
{
    public static class RoomBuilder
    {
        private const string Root = "Assets/_Project";
        private const string SceneFolder = Root + "/Scenes";
        private const string MaterialFolder = Root + "/Materials";
        private const string ResourcesFolder = Root + "/Resources";

        private const string CellScene = "Floor1_Cell_A";
        private const string LobbyScene = "Floor2_Lobby_A";
        private const string BossScene = "Floor3_Boss_A";

        private const float WallThickness = 0.5f;
        private const float NearWallHeight = 0.6f;
        private const float CellDoorTop = 2.4f;

        private static Dictionary<string, Material> materials;

        [MenuItem("Roguelike/Build All Rooms")]
        public static void BuildAllRooms()
        {
            if (!ConfirmOverwrite())
            {
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolders();
            CreateMaterials();

            var scenePaths = new List<string>
            {
                BuildScene(CellScene, BuildCellBlock),
                BuildScene(LobbyScene, BuildLobby),
                BuildScene(BossScene, BuildBossRoom)
            };

            AddScenesToBuildSettings(scenePaths);
            CreateRunSequence();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(scenePaths[0]);
            EditorUtility.DisplayDialog(
                "Rooms built",
                "Created 3 isometric scenes in Assets/_Project/Scenes and added them to Build Settings.\n\n" +
                "Open them from that folder to look around in the Scene view. The green box at the end of each room is the exit to the next floor.",
                "OK");
        }

        private static void BuildCellBlock(Transform root)
        {
            SetAmbient(new Color(0.13f, 0.13f, 0.17f));
            KeyLight(root, new Color(0.55f, 0.62f, 0.9f), 0.35f, 55f, 20f);

            const float width = 15f;
            const float depth = 28f;
            const float wallHeight = 3.5f;
            const float corridorHalf = 3.5f;
            const float cellSize = 4.5f;
            const int cellsPerSide = 5;
            const float dividerHeight = 2.2f;
            const float exitWidth = 2.4f;

            Shell("Shell", root, 0f, width, depth, wallHeight, 0f, 0f, 0f, exitWidth, 2.8f, "DarkStone", "Stone");

            float cellRowEnd = cellSize * cellsPerSide;
            Transform cells = Group("Cells", root);
            for (int side = -1; side <= 1; side += 2)
            {
                string sideName = side < 0 ? "West" : "East";
                float barX = side * corridorHalf;
                float outerX = side * (width / 2f);
                float xMin = Mathf.Min(barX, outerX);
                float xMax = Mathf.Max(barX, outerX);

                for (int i = 0; i < cellsPerSide; i++)
                {
                    float zStart = i * cellSize;
                    float zEnd = zStart + cellSize;
                    bool isPlayerCell = side < 0 && i == 0;
                    string cellName = "Cell_" + sideName + "_" + (i + 1) + (isPlayerCell ? " (Player)" : "");
                    Transform cell = Group(cellName, cells);

                    bool open = IsOpenCell(side, i);
                    float gapStart = open ? zStart + 1.65f : -1f;
                    float gapEnd = open ? zStart + 2.85f : -1f;

                    Bars("Bars", cell, barX, zStart, zEnd, wallHeight, gapStart, gapEnd, "Metal");
                    if (open)
                    {
                        OpenCellDoor(cell, barX, gapStart, side);
                    }
                    if (i > 0)
                    {
                        WallX("Divider", cell, zStart, xMin, xMax, dividerHeight, 0f, 0f, 0f, "Stone", 0.3f);
                    }

                    Box("Cot", cell, new Vector3(side * 6.9f, 0.225f, zStart + 2.25f), new Vector3(0.9f, 0.45f, 2f), "Wood");
                    Cylinder("Bucket", cell, new Vector3(side * 6.9f, 0.2f, zStart + 3.9f), 0.2f, 0.4f, "Metal");
                }

                WallX("CellRowEnd_" + sideName, cells, cellRowEnd, xMin, xMax, dividerHeight, 0f, 0f, 0f, "Stone", WallThickness);
            }

            Transform post = Group("GuardPost", root);
            Box("Desk", post, new Vector3(4.5f, 0.5f, 25.5f), new Vector3(2.4f, 1f, 1f), "Wood");
            Box("Chair", post, new Vector3(4.5f, 0.45f, 26.4f), new Vector3(0.6f, 0.9f, 0.6f), "Wood");
            Box("KeyRack", post, new Vector3(7.4f, 1.6f, 24.5f), new Vector3(0.1f, 0.6f, 1f), "Metal");
            Cylinder("Barrel_1", post, new Vector3(-6.6f, 0.5f, 27.2f), 0.4f, 1f, "Wood");
            Cylinder("Barrel_2", post, new Vector3(-5.7f, 0.5f, 27.3f), 0.4f, 1f, "Wood");
            Box("Crate", post, new Vector3(-6.5f, 0.4f, 24f), new Vector3(0.8f, 0.8f, 0.8f), "Wood");

            Transform lights = Group("Lights", root);
            Color lamp = new Color(1f, 0.62f, 0.3f);
            PointLight("Lamp_1", lights, new Vector3(0f, 3f, 4f), lamp, 9f, 2.5f, true);
            PointLight("Lamp_2", lights, new Vector3(0f, 3f, 12f), lamp, 9f, 2f, false);
            PointLight("Lamp_3", lights, new Vector3(0f, 3f, 19f), lamp, 9f, 2.5f, false);
            PointLight("GuardPost", lights, new Vector3(3f, 3f, 25.5f), lamp, 9f, 3f, true);
            PointLight("PlayerCell_Moonlight", lights, new Vector3(-6.5f, 2.8f, 2.25f), new Color(0.55f, 0.65f, 1f), 5f, 2f, false);

            Transform spawns = Group("Spawns", root);
            Spawn(SpawnType.Player, spawns, new Vector3(-5.5f, 0f, 2.25f), 90f);
            Spawn(SpawnType.Prisoner, spawns, new Vector3(5.5f, 0f, 6.75f), -90f);
            Spawn(SpawnType.Prisoner, spawns, new Vector3(0f, 0f, 10f), 180f);
            Spawn(SpawnType.Prisoner, spawns, new Vector3(-5.5f, 0f, 15.75f), 90f);
            Spawn(SpawnType.Prisoner, spawns, new Vector3(5.5f, 0f, 20.25f), -90f);
            Spawn(SpawnType.Prisoner, spawns, new Vector3(1f, 0f, 21f), 180f);
            Spawn(SpawnType.Prisoner, spawns, new Vector3(-3f, 0f, 26f), 180f);

            ExitAlcove(root, 0f, depth + WallThickness, exitWidth, wallHeight, "Stone", "DarkStone");
        }

        private static bool IsOpenCell(int side, int index)
        {
            return (side < 0 && index == 0)
                || (side > 0 && index == 1)
                || (side < 0 && index == 3)
                || (side > 0 && index == 4);
        }

        private static void Bars(string name, Transform parent, float x, float zStart, float zEnd, float height,
            float gapStart, float gapEnd, string mat)
        {
            Transform group = Group(name, parent);
            for (float z = zStart + 0.15f; z < zEnd; z += 0.3f)
            {
                bool inGap = z > gapStart && z < gapEnd;
                if (inGap)
                {
                    float topHeight = height - CellDoorTop;
                    Cylinder("Bar_AboveDoor", group, new Vector3(x, CellDoorTop + topHeight / 2f, z), 0.04f, topHeight, mat);
                }
                else
                {
                    Cylinder("Bar", group, new Vector3(x, height / 2f, z), 0.04f, height, mat);
                }
            }
            Box("Rail_Top", group, new Vector3(x, height - 0.1f, (zStart + zEnd) / 2f), new Vector3(0.08f, 0.08f, zEnd - zStart), mat);
            if (gapEnd > gapStart)
            {
                Box("Rail_DoorTop", group, new Vector3(x, CellDoorTop, (gapStart + gapEnd) / 2f), new Vector3(0.08f, 0.08f, gapEnd - gapStart), mat);
            }
        }

        private static void OpenCellDoor(Transform parent, float x, float hingeZ, int side)
        {
            Transform door = Group("Door_Open", parent);
            door.localPosition = new Vector3(x, 0f, hingeZ);
            door.localRotation = Quaternion.Euler(0f, -side * 70f, 0f);

            for (int k = 0; k < 4; k++)
            {
                Cylinder("Bar", door, new Vector3(0f, CellDoorTop / 2f, 0.15f + 0.3f * k), 0.04f, CellDoorTop - 0.05f, "Metal");
            }
            Box("Rail_Top", door, new Vector3(0f, CellDoorTop - 0.1f, 0.6f), new Vector3(0.08f, 0.08f, 1.2f), "Metal");
            Box("Rail_Bottom", door, new Vector3(0f, 0.1f, 0.6f), new Vector3(0.08f, 0.08f, 1.2f), "Metal");
        }

        private static void BuildLobby(Transform root)
        {
            SetAmbient(new Color(0.22f, 0.2f, 0.18f));
            KeyLight(root, new Color(1f, 0.9f, 0.75f), 0.5f, 55f, 30f);

            const float width = 28f;
            const float depth = 30f;
            const float wallHeight = 6f;
            const float exitX = 12.5f;
            const float exitWidth = 2.5f;

            Shell("Shell", root, 0f, width, depth, wallHeight, 0f, 0f, exitX, exitWidth, 3f, "Marble", "Stone");

            Box("Carpet", root, new Vector3(0f, 0.01f, 11f), new Vector3(3f, 0.02f, 20f), "Carpet");

            Transform pillars = Group("Pillars", root);
            float[] pillarZ = { 5f, 11f, 17f };
            foreach (float z in pillarZ)
            {
                Cylinder("Pillar_W_" + z, pillars, new Vector3(-8f, 2f, z), 0.6f, 4f, "Stone");
                Cylinder("Pillar_E_" + z, pillars, new Vector3(8f, 2f, z), 0.6f, 4f, "Stone");
            }

            Transform desk = Group("ReceptionDesk", root);
            Box("Front", desk, new Vector3(0f, 0.55f, 12f), new Vector3(6f, 1.1f, 0.8f), "Wood");
            Box("Side_W", desk, new Vector3(-2.6f, 0.55f, 13.9f), new Vector3(0.8f, 1.1f, 3f), "Wood");
            Box("Side_E", desk, new Vector3(2.6f, 0.55f, 13.9f), new Vector3(0.8f, 1.1f, 3f), "Wood");

            Transform cover = Group("Cover", root);
            Box("Bench_W", cover, new Vector3(-11.5f, 0.25f, 8f), new Vector3(0.8f, 0.5f, 3f), "Wood");
            Box("Bench_E", cover, new Vector3(11.5f, 0.25f, 8f), new Vector3(0.8f, 0.5f, 3f), "Wood");
            Box("Bench_NW", cover, new Vector3(-5f, 0.25f, 18.5f), new Vector3(3f, 0.5f, 0.8f), "Wood");
            Box("Bench_NE", cover, new Vector3(5f, 0.25f, 18.5f), new Vector3(3f, 0.5f, 0.8f), "Wood");
            Cylinder("Planter_SW", cover, new Vector3(-12.5f, 0.5f, 1.5f), 0.6f, 1f, "Stone");
            Cylinder("Planter_SE", cover, new Vector3(12.5f, 0.5f, 1.5f), 0.6f, 1f, "Stone");

            Transform gallery = Group("Gallery", root);
            const float galleryTop = 1.5f;
            const float galleryFront = 23f;
            Box("Platform", gallery, new Vector3(0f, galleryTop / 2f, (galleryFront + depth) / 2f), new Vector3(22f, galleryTop, depth - galleryFront), "Marble");
            Box("Railing_W", gallery, new Vector3(-7.5f, galleryTop + 0.45f, galleryFront + 0.1f), new Vector3(7f, 0.9f, 0.15f), "Wood");
            Box("Railing_E", gallery, new Vector3(7.5f, galleryTop + 0.45f, galleryFront + 0.1f), new Vector3(7f, 0.9f, 0.15f), "Wood");
            for (int i = 0; i < 6; i++)
            {
                float top = 0.25f * (i + 1);
                float z = galleryFront - 3f + 0.5f * i + 0.25f;
                Box("Step_" + i, gallery, new Vector3(0f, top / 2f, z), new Vector3(8f, top, 0.5f), "Marble");
            }

            Transform lights = Group("Lights", root);
            Color warm = new Color(1f, 0.85f, 0.6f);
            PointLight("Chandelier_South", lights, new Vector3(0f, 5f, 7f), warm, 16f, 4f, true);
            PointLight("Chandelier_North", lights, new Vector3(0f, 5f, 17f), warm, 16f, 4f, true);
            PointLight("Sconce_E", lights, new Vector3(13f, 4f, 12f), warm, 9f, 2f, false);
            PointLight("Gallery", lights, new Vector3(0f, 4.5f, 27f), warm, 12f, 3f, false);

            Transform spawns = Group("Spawns", root);
            Spawn(SpawnType.Player, spawns, new Vector3(0f, 0f, 2f), 0f);
            Spawn(SpawnType.Guard, spawns, new Vector3(-6f, 0f, 9f), 180f);
            Spawn(SpawnType.Guard, spawns, new Vector3(6f, 0f, 9f), 180f);
            Spawn(SpawnType.Guard, spawns, new Vector3(0f, 0f, 14f), 180f);
            Spawn(SpawnType.Guard, spawns, new Vector3(-10f, 0f, 19f), 180f);
            Spawn(SpawnType.Guard, spawns, new Vector3(10f, 0f, 19f), 180f);
            Spawn(SpawnType.Guard, spawns, new Vector3(-6f, galleryTop, 27f), 180f);
            Spawn(SpawnType.Guard, spawns, new Vector3(0f, galleryTop, 28f), 180f);
            Spawn(SpawnType.Guard, spawns, new Vector3(6f, galleryTop, 27f), 180f);

            ExitAlcove(root, exitX, depth + WallThickness, exitWidth, wallHeight, "Stone", "Marble");
        }

        private static void BuildBossRoom(Transform root)
        {
            SetAmbient(new Color(0.14f, 0.05f, 0.05f));
            KeyLight(root, new Color(1f, 0.5f, 0.4f), 0.3f, 60f, 35f);

            const float hallWidth = 5f;
            const float hallDepth = 8f;
            const float hallHeight = 4f;
            const float arenaStart = hallDepth + WallThickness;
            const float arenaWidth = 36f;
            const float arenaDepth = 36f;
            const float arenaHeight = 8f;
            const float exitX = 12f;
            const float exitWidth = 3f;

            Shell("EntranceHall", root, 0f, hallWidth, hallDepth, hallHeight, 0f, 0f, 0f, hallWidth, hallHeight, "DarkStone", "DarkStone");
            Shell("Arena", root, arenaStart, arenaWidth, arenaDepth, arenaHeight, 0f, hallWidth, exitX, exitWidth, 4f, "DarkStone", "DarkStone");

            float arenaEnd = arenaStart + arenaDepth;

            Box("Carpet", root, new Vector3(0f, 0.01f, (arenaStart + 35f) / 2f), new Vector3(3f, 0.02f, 35f - arenaStart), "Carpet");

            Transform pillars = Group("Pillars", root);
            Cylinder("Pillar_SW", pillars, new Vector3(-9f, 2.5f, 18f), 1.5f, 5f, "Stone");
            Cylinder("Pillar_SE", pillars, new Vector3(9f, 2.5f, 18f), 1.5f, 5f, "Stone");
            Cylinder("Pillar_NW", pillars, new Vector3(-9f, 2.5f, 31f), 1.5f, 5f, "Stone");
            Cylinder("Pillar_NE", pillars, new Vector3(9f, 2.5f, 31f), 1.5f, 5f, "Stone");

            Transform dais = Group("ThroneDais", root);
            Box("Tier1", dais, new Vector3(0f, 0.5f, 40f), new Vector3(14f, 1f, 6f), "Stone");
            Box("Tier2", dais, new Vector3(0f, 1.5f, 41.5f), new Vector3(8f, 1f, 3f), "Stone");
            for (int i = 0; i < 4; i++)
            {
                float top = 0.25f * (i + 1);
                Box("Step1_" + i, dais, new Vector3(0f, top / 2f, 35f + 0.5f * i + 0.25f), new Vector3(6f, top, 0.5f), "Stone");
                float top2 = 1f + 0.25f * (i + 1);
                Box("Step2_" + i, dais, new Vector3(0f, top2 / 2f, 38f + 0.5f * i + 0.25f), new Vector3(4f, top2, 0.5f), "Stone");
            }
            Box("Throne_Seat", dais, new Vector3(0f, 2.5f, 42.2f), new Vector3(2f, 1f, 1.5f), "Gold");
            Box("Throne_Back", dais, new Vector3(0f, 4f, 43f), new Vector3(2f, 4f, 0.4f), "Gold");

            Transform banners = Group("Banners", root);
            float[] bannerZ = { 16f, 26f, 36f };
            foreach (float z in bannerZ)
            {
                Box("Banner_E_" + z, banners, new Vector3(arenaWidth / 2f - 0.06f, 5f, z), new Vector3(0.1f, 4f, 2f), "Carpet");
            }
            Box("Banner_N_W", banners, new Vector3(-6f, 5f, arenaEnd - 0.06f), new Vector3(2f, 4f, 0.1f), "Carpet");
            Box("Banner_N_E", banners, new Vector3(6f, 5f, arenaEnd - 0.06f), new Vector3(2f, 4f, 0.1f), "Carpet");

            Transform lights = Group("Lights", root);
            Color fire = new Color(1f, 0.45f, 0.15f);
            Vector3[] braziers =
            {
                new Vector3(-5f, 0f, 35.5f),
                new Vector3(5f, 0f, 35.5f),
                new Vector3(-14f, 0f, 14f),
                new Vector3(14f, 0f, 14f),
                new Vector3(-14f, 0f, 30f),
                new Vector3(14f, 0f, 30f)
            };
            for (int i = 0; i < braziers.Length; i++)
            {
                Vector3 p = braziers[i];
                Cylinder("Brazier_" + i, lights, new Vector3(p.x, 0.6f, p.z), 0.4f, 1.2f, "Metal");
                PointLight("BrazierLight_" + i, lights, new Vector3(p.x, 1.8f, p.z), fire, 12f, 3f, i < 2);
            }
            PointLight("ThroneGlow", lights, new Vector3(0f, 5f, 42.5f), new Color(1f, 0.15f, 0.1f), 14f, 3f, false);
            PointLight("HallLight", lights, new Vector3(0f, 3f, 4f), fire, 7f, 2f, false);

            Transform spawns = Group("Spawns", root);
            Spawn(SpawnType.Player, spawns, new Vector3(0f, 0f, 1.5f), 0f);
            Spawn(SpawnType.Boss, spawns, new Vector3(0f, 1f, 37.5f), 180f);

            ExitAlcove(root, exitX, arenaEnd + WallThickness, exitWidth, arenaHeight, "DarkStone", "DarkStone");
        }

        private static Transform Shell(string name, Transform parent, float zStart, float width, float depth, float height,
            float southDoorX, float southDoorWidth, float northDoorX, float northDoorWidth, float doorHeight,
            string floorMat, string wallMat)
        {
            Transform group = Group(name, parent);
            float t = WallThickness;
            float half = width / 2f;
            float zMid = zStart + depth / 2f;

            Box("Floor", group, new Vector3(0f, -0.25f, zMid), new Vector3(width + t * 2f, 0.5f, depth + t * 2f), floorMat);

            WallZ("Wall_West_Low", group, -half - t / 2f, zStart - t, zStart + depth + t, NearWallHeight, wallMat);
            WallZ("Wall_East", group, half + t / 2f, zStart - t, zStart + depth + t, height, wallMat);
            WallX("Wall_South_Low", group, zStart - t / 2f, -half, half, NearWallHeight, southDoorX, southDoorWidth, doorHeight, wallMat, t);
            WallX("Wall_North", group, zStart + depth + t / 2f, -half, half, height, northDoorX, northDoorWidth, doorHeight, wallMat, t);
            return group;
        }

        private static void WallX(string name, Transform parent, float z, float xMin, float xMax, float height,
            float doorX, float doorWidth, float doorHeight, string mat, float thickness)
        {
            Transform group = Group(name, parent);
            if (doorWidth <= 0f)
            {
                Box("Wall", group, new Vector3((xMin + xMax) / 2f, height / 2f, z), new Vector3(xMax - xMin, height, thickness), mat);
                return;
            }

            float left = doorX - doorWidth / 2f;
            float right = doorX + doorWidth / 2f;
            if (left > xMin)
            {
                Box("Wall_Left", group, new Vector3((xMin + left) / 2f, height / 2f, z), new Vector3(left - xMin, height, thickness), mat);
            }
            if (right < xMax)
            {
                Box("Wall_Right", group, new Vector3((right + xMax) / 2f, height / 2f, z), new Vector3(xMax - right, height, thickness), mat);
            }
            if (doorHeight < height)
            {
                Box("Lintel", group, new Vector3(doorX, (doorHeight + height) / 2f, z), new Vector3(doorWidth, height - doorHeight, thickness), mat);
            }
        }

        private static void WallZ(string name, Transform parent, float x, float zMin, float zMax, float height, string mat)
        {
            Box(name, parent, new Vector3(x, height / 2f, (zMin + zMax) / 2f), new Vector3(WallThickness, height, zMax - zMin), mat);
        }

        private static void ExitAlcove(Transform parent, float x, float zStart, float width, float wallHeight,
            string wallMat, string floorMat)
        {
            Transform group = Group("ExitAlcove", parent);
            const float length = 6f;
            float zEnd = zStart + length;
            float zMid = zStart + length / 2f;

            Box("Floor", group, new Vector3(x, -0.25f, zMid), new Vector3(width + 1f, 0.5f, length), floorMat);
            Box("Wall_West_Low", group, new Vector3(x - width / 2f - 0.25f, NearWallHeight / 2f, zMid), new Vector3(0.5f, NearWallHeight, length), wallMat);
            Box("Wall_East", group, new Vector3(x + width / 2f + 0.25f, wallHeight / 2f, zMid), new Vector3(0.5f, wallHeight, length), wallMat);
            Box("Wall_End", group, new Vector3(x, wallHeight / 2f, zEnd + 0.25f), new Vector3(width + 1f, wallHeight, 0.5f), wallMat);

            const int stepCount = 6;
            const float stepDepth = 0.4f;
            const float stepRise = 0.2f;
            float stepsStart = zStart + 0.25f;
            for (int i = 0; i < stepCount; i++)
            {
                float top = stepRise * (i + 1);
                Box("Step_" + i, group, new Vector3(x, top / 2f, stepsStart + stepDepth * i + stepDepth / 2f), new Vector3(width, top, stepDepth), wallMat);
            }
            float landingStart = stepsStart + stepDepth * stepCount;
            float landingTop = stepRise * stepCount;
            Box("Landing", group, new Vector3(x, landingTop / 2f, (landingStart + zEnd) / 2f), new Vector3(width, landingTop, zEnd - landingStart), wallMat);

            GameObject trigger = Box("ExitTrigger", group, new Vector3(x, landingTop + 1.25f, zEnd - 0.75f), new Vector3(width, 2.5f, 1f), "Exit");
            trigger.GetComponent<BoxCollider>().isTrigger = true;
            trigger.AddComponent<RoomExit>();

            PointLight("ExitLight", group, new Vector3(x, 3f, zEnd - 1.5f), new Color(0.7f, 1f, 0.75f), 6f, 2.5f, false);
        }

        private static void Spawn(SpawnType type, Transform parent, Vector3 position, float yaw)
        {
            var go = new GameObject("Spawn_" + type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.AddComponent<SpawnPoint>().type = type;
        }

        private static void PointLight(string name, Transform parent, Vector3 position, Color color, float range, float intensity, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        private static void KeyLight(Transform parent, Color color, float intensity, float pitch, float yaw)
        {
            var go = new GameObject("KeyLight");
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
        }

        private static void SetAmbient(Color ambient)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = false;
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, string mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Place(go, name, parent, center, size, mat);
            return go;
        }

        private static GameObject Cylinder(string name, Transform parent, Vector3 center, float radius, float height, string mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Place(go, name, parent, center, new Vector3(radius * 2f, height / 2f, radius * 2f), mat);
            return go;
        }

        private static void Place(GameObject go, string name, Transform parent, Vector3 position, Vector3 scale, string mat)
        {
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = materials[mat];
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        private static bool ConfirmOverwrite()
        {
            bool anyExist = File.Exists(ScenePath(CellScene)) || File.Exists(ScenePath(LobbyScene)) || File.Exists(ScenePath(BossScene));
            if (!anyExist)
            {
                return true;
            }
            return EditorUtility.DisplayDialog(
                "Rebuild rooms?",
                "This overwrites the variant A scenes and any changes you made to them by hand. Other scenes are not touched.",
                "Rebuild",
                "Cancel");
        }

        private static string ScenePath(string sceneName)
        {
            return SceneFolder + "/" + sceneName + ".unity";
        }

        private static string BuildScene(string sceneName, System.Action<Transform> build)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform root = new GameObject(sceneName).transform;
            build(root);
            string path = ScenePath(sceneName);
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Project");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Resources");
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void CreateMaterials()
        {
            materials = new Dictionary<string, Material>
            {
                { "Stone", GetOrCreateMaterial("Stone", new Color(0.42f, 0.42f, 0.44f)) },
                { "DarkStone", GetOrCreateMaterial("DarkStone", new Color(0.22f, 0.21f, 0.22f)) },
                { "Metal", GetOrCreateMaterial("Metal", new Color(0.12f, 0.12f, 0.13f)) },
                { "Wood", GetOrCreateMaterial("Wood", new Color(0.4f, 0.27f, 0.16f)) },
                { "Marble", GetOrCreateMaterial("Marble", new Color(0.78f, 0.75f, 0.7f)) },
                { "Carpet", GetOrCreateMaterial("Carpet", new Color(0.45f, 0.06f, 0.07f)) },
                { "Gold", GetOrCreateMaterial("Gold", new Color(0.8f, 0.63f, 0.2f)) },
                { "Exit", GetOrCreateMaterial("Exit", new Color(0.2f, 0.85f, 0.35f)) }
            };
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                mat = new Material(shader);
                mat.color = color;
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void AddScenesToBuildSettings(List<string> paths)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in paths)
            {
                if (!scenes.Exists(s => s.path == path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void CreateRunSequence()
        {
            string path = ResourcesFolder + "/RunSequence.asset";
            if (AssetDatabase.LoadAssetAtPath<RunSequence>(path) != null)
            {
                return;
            }

            var sequence = ScriptableObject.CreateInstance<RunSequence>();
            sequence.floors = new[]
            {
                new RunSequence.Floor { floorName = "Prison Cells", variantScenes = new[] { CellScene } },
                new RunSequence.Floor { floorName = "Lobby", variantScenes = new[] { LobbyScene } },
                new RunSequence.Floor { floorName = "Throne Room", variantScenes = new[] { BossScene } }
            };
            AssetDatabase.CreateAsset(sequence, path);
        }
    }
}
