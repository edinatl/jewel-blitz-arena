#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

// One-click scene builder. In Unity: menu  Prototype > Build Arena Scene
// It creates the whole playable scene (arena, 2 fighters, camera, spawner,
// hazard, game manager) and saves it as Assets/Scenes/Arena.unity.
public static class PrototypeBuilder
{
    [MenuItem("Prototype/Build Arena Scene")]
    public static void BuildArenaScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Lighting --------------------------------------------------------
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);

        // --- Arena floor (18 x 18 square) ------------------------------------
        var floor = GameObject.CreatePrimitive(PrimitiveType.Box);
        floor.name = "ArenaFloor";
        floor.transform.position = new Vector3(0f, -0.25f, 0f);
        floor.transform.localScale = new Vector3(18f, 0.5f, 18f);
        SetColor(floor, new Color(0.85f, 0.62f, 0.30f)); // desert sand fallback
        // v0.3: comic desert art as the floor texture when the art asset is imported
        var desertTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/desert_arena.png");
        if (desertTex != null)
        {
            var fm = floor.GetComponent<Renderer>().sharedMaterial;
            fm.mainTexture = desertTex;
            fm.color = Color.white;
        }

        // Low visible walls so fighters stay in the ring.
        MakeWall("Wall_North", new Vector3(0f, 0.6f, 9.2f), new Vector3(19f, 1.2f, 0.4f));
        MakeWall("Wall_South", new Vector3(0f, 0.6f, -9.2f), new Vector3(19f, 1.2f, 0.4f));
        MakeWall("Wall_East", new Vector3(9.2f, 0.6f, 0f), new Vector3(0.4f, 1.2f, 19f));
        MakeWall("Wall_West", new Vector3(-9.2f, 0.6f, 0f), new Vector3(0.4f, 1.2f, 19f));

        // Center marker so movement reads well.
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "CenterMarker";
        marker.transform.position = new Vector3(0f, 0.02f, 0f);
        marker.transform.localScale = new Vector3(2.2f, 0.03f, 2.2f);
        var markerCol = marker.GetComponent<Collider>();
        if (markerCol != null) Object.DestroyImmediate(markerCol);
        SetColor(marker, new Color(0.85f, 0.7f, 0.25f));

        // --- Fighters ---------------------------------------------------------
        var p1 = MakePlayer("Player1_Blue", new Vector3(-4f, 1.05f, 0f),
                            new Color(0.2f, 0.45f, 1f), 0, "BLUE");
        ConfigureP1(p1);

        var p2 = MakePlayer("Player2_Red", new Vector3(4f, 1.05f, 0f),
                            new Color(1f, 0.3f, 0.25f), 1, "RED");
        ConfigureP2(p2);

        // --- Camera -------------------------------------------------------------
        var camGo = new GameObject("ArenaCamera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 13f, -12f);
        camGo.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
        camGo.AddComponent<ArenaCamera>();
        camGo.AddComponent<AudioListener>();

        // --- Systems ------------------------------------------------------------
        var gmGo = new GameObject("GameManager");
        gmGo.AddComponent<GameManager>();
        gmGo.AddComponent<Spawner>();

        StageHazard.Create(new Vector3(0f, 1f, 5f));

        // --- Save -----------------------------------------------------------------
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Arena.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Arena.unity", true) };
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Prototype",
            "Arena scene built and saved.\n\nPress Play to fight!\n\nP1: WASD + Space/F/G\nP2 starts as AI — press any P2 key to take over.", "Play!");
    }

    static void MakeWall(string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Box);
        wall.name = name;
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        SetColor(wall, new Color(0.5f, 0.55f, 0.65f));
    }

    static GameObject MakePlayer(string name, Vector3 pos, Color color, int index, string display)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.tag = "Player";
        go.transform.position = pos;
        go.transform.localScale = new Vector3(1f, 1.1f, 1f);
        SetColor(go, color);

        var rb = go.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        go.AddComponent<Health>();
        go.AddComponent<PlayerCombat>();
        var ctrl = go.AddComponent<PlayerController>();
        ctrl.playerIndex = index;
        ctrl.displayName = display;
        return go;
    }

    static void ConfigureP1(GameObject go)
    {
        var ctrl = go.GetComponent<PlayerController>();
        ctrl.upKey = KeyCode.W;
        ctrl.downKey = KeyCode.S;
        ctrl.leftKey = KeyCode.A;
        ctrl.rightKey = KeyCode.D;
        ctrl.jumpKey = KeyCode.Space;
        ctrl.attackKey = KeyCode.F;
        ctrl.heavyKey = KeyCode.G;
        ctrl.aiEnabled = false;
    }

    static void ConfigureP2(GameObject go)
    {
        var ctrl = go.GetComponent<PlayerController>();
        ctrl.upKey = KeyCode.UpArrow;
        ctrl.downKey = KeyCode.DownArrow;
        ctrl.leftKey = KeyCode.LeftArrow;
        ctrl.rightKey = KeyCode.RightArrow;
        ctrl.jumpKey = KeyCode.RightShift;
        ctrl.attackKey = KeyCode.Period;
        ctrl.heavyKey = KeyCode.Slash;
        ctrl.aiEnabled = true; // solo play vs bot until a human presses a P2 key
    }

    static void SetColor(GameObject go, Color color)
    {
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        // Materials must be saved as assets, or they'd vanish when the
        // scene is reopened. One material asset per object name.
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        string path = "Assets/Materials/" + go.name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        rend.sharedMaterial = mat;
    }
}
#endif
