using UnityEngine;

// A comic-style 3D fighter built entirely from primitives at runtime — no
// model files needed. The scene builder drops this component on a player,
// sets `character`, and at play time it constructs a themed humanoid
// (body, head, swinging arms/legs, plus signature gear per character).
// FighterAnimator drives the part pivots to animate it, Power Stone style:
// run cycles, punches, throws, flinches, KO falls, transform glow.
public class CharacterVisual : MonoBehaviour
{
    public string character = "Soldier";

    // Part pivots the animator drives.
    public Transform Body;      // whole-figure container (fall/lean/squash)
    public Transform ArmL, ArmR, LegL, LegR, Torso, Head;

    private Renderer[] allRenderers;
    private Color[] baseColors;
    private bool gold;

    public struct Palette
    {
        public Color Main, Secondary, Skin, Accent, Dark;
        public Palette(Color m, Color s, Color sk, Color a, Color d)
        { Main = m; Secondary = s; Skin = sk; Accent = a; Dark = d; }
    }

    public static Palette PaletteFor(string name)
    {
        Color skin = new Color(0.96f, 0.76f, 0.60f);
        switch (name)
        {
            case "Samurai":   return new Palette(new Color(0.16f, 0.30f, 0.78f), new Color(0.10f, 0.18f, 0.50f), skin, new Color(1f, 0.78f, 0.25f), new Color(0.15f, 0.13f, 0.12f));
            case "Warrior":   return new Palette(new Color(0.48f, 0.33f, 0.20f), new Color(0.35f, 0.24f, 0.15f), skin, new Color(0.75f, 0.72f, 0.68f), new Color(0.25f, 0.18f, 0.12f));
            case "Gladiator": return new Palette(new Color(0.74f, 0.52f, 0.22f), new Color(0.55f, 0.36f, 0.16f), skin, new Color(0.75f, 0.14f, 0.12f), new Color(0.30f, 0.22f, 0.16f));
            case "Knight":    return new Palette(new Color(0.76f, 0.79f, 0.84f), new Color(0.55f, 0.58f, 0.64f), skin, new Color(0.18f, 0.30f, 0.75f), new Color(0.35f, 0.36f, 0.40f));
            case "Ninja":     return new Palette(new Color(0.09f, 0.09f, 0.11f), new Color(0.16f, 0.16f, 0.20f), skin, new Color(0.85f, 0.16f, 0.16f), new Color(0.05f, 0.05f, 0.06f));
            case "Pirate":    return new Palette(new Color(0.52f, 0.17f, 0.13f), new Color(0.32f, 0.20f, 0.13f), skin, new Color(0.95f, 0.75f, 0.25f), new Color(0.10f, 0.08f, 0.08f));
            case "Robot":     return new Palette(new Color(0.96f, 0.66f, 0.16f), new Color(0.45f, 0.47f, 0.52f), new Color(0.96f, 0.66f, 0.16f), new Color(0.90f, 0.20f, 0.16f), new Color(0.30f, 0.31f, 0.35f));
            case "Jester":    return new Palette(new Color(0.58f, 0.20f, 0.72f), new Color(0.85f, 0.22f, 0.28f), skin, new Color(1f, 0.80f, 0.25f), new Color(0.22f, 0.10f, 0.28f));
            case "Pyra":      return new Palette(new Color(0.72f, 0.16f, 0.30f), new Color(0.45f, 0.10f, 0.35f), skin, new Color(1f, 0.55f, 0.15f), new Color(0.25f, 0.08f, 0.12f));
            case "Gusto":     return new Palette(new Color(0.92f, 0.92f, 0.90f), new Color(0.35f, 0.35f, 0.38f), skin, new Color(0.85f, 0.20f, 0.18f), new Color(0.30f, 0.25f, 0.22f));
            case "Dusty":     return new Palette(new Color(0.45f, 0.32f, 0.20f), new Color(0.30f, 0.22f, 0.14f), skin, new Color(0.85f, 0.65f, 0.25f), new Color(0.15f, 0.12f, 0.10f));
            case "Ignis":     return new Palette(new Color(0.60f, 0.48f, 0.29f), new Color(0.54f, 0.48f, 0.35f), skin, new Color(0.42f, 0.44f, 0.46f), new Color(0.29f, 0.22f, 0.15f));
            case "Kira":      return new Palette(new Color(0.17f, 0.21f, 0.31f), new Color(0.72f, 0.69f, 0.75f), skin, new Color(0.85f, 0.70f, 0.29f), new Color(0.14f, 0.13f, 0.18f));
            default: // Soldier
                return new Palette(new Color(0.33f, 0.43f, 0.26f), new Color(0.24f, 0.32f, 0.19f), skin, new Color(0.20f, 0.22f, 0.16f), new Color(0.16f, 0.15f, 0.12f));
        }
    }

    void Awake()
    {
        Build();
    }

    // --- construction ------------------------------------------------------

    Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        return m;
    }

    GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color, string name)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col); // visuals never collide
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.material = Mat(color);
        return go;
    }

    // Unity has no Cone primitive — build one tiny mesh and reuse it.
    private static Mesh coneMesh;
    static Mesh ConeMesh()
    {
        if (coneMesh != null) return coneMesh;
        int seg = 12;
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();
        verts.Add(new Vector3(0f, 0.5f, 0f));
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f));
        }
        int center = verts.Count;
        verts.Add(new Vector3(0f, -0.5f, 0f));
        for (int i = 0; i < seg; i++)
        {
            int cur = 1 + i, nxt = 1 + (i + 1) % seg;
            tris.Add(0); tris.Add(nxt); tris.Add(cur);
            tris.Add(center); tris.Add(cur); tris.Add(nxt);
        }
        var mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        coneMesh = mesh;
        return mesh;
    }

    GameObject PartCone(Transform parent, Vector3 pos, Vector3 scale, Color color, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = ConeMesh();
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = Mat(color);
        return go;
    }

    Transform Pivot(Transform parent, Vector3 pos, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    void Build()
    {
        var pal = PaletteFor(character);

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(transform, false);
        bodyGo.transform.localPosition = new Vector3(0f, -1.02f, 0f); // root sits at capsule center; feet at Body y=0
        Body = bodyGo.transform;

        // Legs (pivot at hip, y .98)
        LegL = Pivot(Body, new Vector3(-0.17f, 0.98f, 0f), "LegL");
        LegR = Pivot(Body, new Vector3(0.17f, 0.98f, 0f), "LegR");
        BuildLeg(LegL, pal);
        BuildLeg(LegR, pal);

        // Hips + torso
        Part(PrimitiveType.Cube, Body, new Vector3(0f, 1.02f, 0f), new Vector3(0.52f, 0.22f, 0.32f), pal.Secondary, "Hips");
        var torsoGo = Part(PrimitiveType.Cube, Body, new Vector3(0f, 1.48f, 0f), new Vector3(0.64f, 0.72f, 0.36f),
                           character == "Warrior" || character == "Gladiator" ? pal.Skin : pal.Main, "Torso");
        Torso = torsoGo.transform;
        if (character == "Soldier") // chest rig
            Part(PrimitiveType.Cube, Torso, new Vector3(0f, 0.02f, 0.22f), new Vector3(0.5f, 0.45f, 0.12f), pal.Secondary, "Vest");
        if (character == "Gladiator" || character == "Warrior") // belt + strap
            Part(PrimitiveType.Cube, Torso, new Vector3(0f, -0.30f, 0.20f), new Vector3(0.55f, 0.14f, 0.06f), pal.Dark, "Belt");

        // Arms (pivot at shoulder, y 1.76)
        ArmL = Pivot(Body, new Vector3(-0.44f, 1.76f, 0f), "ArmL");
        ArmR = Pivot(Body, new Vector3(0.44f, 1.76f, 0f), "ArmR");
        BuildArm(ArmL, pal, false);
        BuildArm(ArmR, pal, true);

        // Head
        var headGo = new GameObject("Head");
        headGo.transform.SetParent(Body, false);
        headGo.transform.localPosition = new Vector3(0f, 2.06f, 0f);
        Head = headGo.transform;
        if (character == "Robot")
            Part(PrimitiveType.Cube, Head, Vector3.zero, new Vector3(0.52f, 0.46f, 0.46f), pal.Main, "HeadBox");
        else
            Part(PrimitiveType.Sphere, Head, Vector3.zero, new Vector3(0.46f, 0.50f, 0.46f), pal.Skin, "Face");
        // Eyes look toward +Z (the fighter's forward)
        Part(PrimitiveType.Cube, Head, new Vector3(-0.10f, 0.03f, 0.22f), new Vector3(0.07f, 0.07f, 0.03f), pal.Dark, "EyeL");
        Part(PrimitiveType.Cube, Head, new Vector3(0.10f, 0.03f, 0.22f), new Vector3(0.07f, 0.07f, 0.03f), character == "Robot" ? new Color(0.3f, 0.9f, 1f) : pal.Dark, "EyeR");

        BuildGear(pal);

        allRenderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++) baseColors[i] = allRenderers[i].material.color;
    }

    void BuildLeg(Transform pivot, Palette pal)
    {
        Part(PrimitiveType.Cube, pivot, new Vector3(0f, -0.42f, 0f), new Vector3(0.21f, 0.80f, 0.23f), pal.Secondary, "Leg");
        Part(PrimitiveType.Cube, pivot, new Vector3(0f, -0.86f, 0.05f), new Vector3(0.23f, 0.14f, 0.36f), pal.Dark, "Boot");
    }

    void BuildArm(Transform pivot, Palette pal, bool right)
    {
        Part(PrimitiveType.Cube, pivot, new Vector3(0f, -0.30f, 0f), new Vector3(0.17f, 0.58f, 0.18f), pal.Main, "Arm");
        Color hand = character == "Robot" ? pal.Accent : pal.Skin;
        float fistScale = character == "Robot" ? 0.34f : 0.20f; // robot gets boxing gloves
        Part(PrimitiveType.Sphere, pivot, new Vector3(0f, -0.66f, 0f), new Vector3(fistScale, fistScale, fistScale), hand, "Fist");
        // Shoulder pad
        Part(PrimitiveType.Sphere, pivot, new Vector3(0f, -0.02f, 0f), new Vector3(0.26f, 0.20f, 0.26f),
             character == "Warrior" ? pal.Main : pal.Secondary, "Shoulder");
    }

    // Signature gear per character — simple shapes, big silhouettes.
    void BuildGear(Palette pal)
    {
        switch (character)
        {
            case "Ignis": {
                // From Ben's concept art: spiky auburn hair, goggles,
                // red shirt, furnace backpack, oversized metal gauntlets.
                var s1 = PartCone(Head, new Vector3(-0.16f, 0.30f, -0.02f), new Vector3(0.13f, 0.34f, 0.13f), new Color(0.66f, 0.35f, 0.16f), "SpikeL");
                s1.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
                PartCone(Head, new Vector3(0.02f, 0.34f, -0.02f), new Vector3(0.15f, 0.42f, 0.15f), new Color(0.66f, 0.35f, 0.16f), "SpikeC");
                var s3 = PartCone(Head, new Vector3(0.18f, 0.30f, -0.02f), new Vector3(0.13f, 0.34f, 0.13f), new Color(0.66f, 0.35f, 0.16f), "SpikeR");
                s3.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
                Part(PrimitiveType.Cube, Head, new Vector3(0f, 0.16f, 0.20f), new Vector3(0.34f, 0.07f, 0.07f), pal.Dark, "GoggleStrap");
                Part(PrimitiveType.Sphere, Head, new Vector3(-0.09f, 0.16f, 0.24f), new Vector3(0.11f, 0.11f, 0.06f), new Color(0.49f, 0.84f, 0.94f), "LensL");
                Part(PrimitiveType.Sphere, Head, new Vector3(0.09f, 0.16f, 0.24f), new Vector3(0.11f, 0.11f, 0.06f), new Color(0.49f, 0.84f, 0.94f), "LensR");
                Part(PrimitiveType.Cube, Torso, new Vector3(0f, 0f, 0.20f), new Vector3(0.42f, 0.50f, 0.05f), new Color(0.69f, 0.19f, 0.16f), "Shirt");
                Part(PrimitiveType.Cube, Body, new Vector3(0f, 1.55f, -0.30f), new Vector3(0.40f, 0.50f, 0.16f), pal.Accent, "FurnacePack");
                Part(PrimitiveType.Cube, Body, new Vector3(0.14f, 1.95f, -0.30f), new Vector3(0.09f, 0.30f, 0.09f), pal.Dark, "Chimney");
                Part(PrimitiveType.Cube, ArmL, new Vector3(0f, -0.66f, 0f), new Vector3(0.30f, 0.32f, 0.30f), pal.Accent, "GauntletL");
                Part(PrimitiveType.Cube, ArmR, new Vector3(0f, -0.68f, 0f), new Vector3(0.38f, 0.40f, 0.38f), pal.Accent, "GauntletR");
                Part(PrimitiveType.Cube, ArmR, new Vector3(0f, -0.68f, 0.20f), new Vector3(0.20f, 0.12f, 0.04f), new Color(1f, 0.48f, 0.16f), "FurnaceGlow");
                break; }
            case "Kira": {
                // From Ben's concept art: long purple ponytail, gold
                // hair tie, orange sash, chain-sickle across the back.
                Part(PrimitiveType.Sphere, Head, new Vector3(0f, 0.12f, -0.02f), new Vector3(0.55f, 0.38f, 0.55f), new Color(0.42f, 0.23f, 0.60f), "HairCap");
                var tail = PartCone(Head, new Vector3(0.05f, 0.52f, -0.18f), new Vector3(0.20f, 0.85f, 0.20f), new Color(0.42f, 0.23f, 0.60f), "Ponytail");
                tail.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
                Part(PrimitiveType.Sphere, Head, new Vector3(0.05f, 0.34f, -0.06f), new Vector3(0.12f, 0.12f, 0.12f), pal.Accent, "HairTie");
                Part(PrimitiveType.Cube, Body, new Vector3(0f, 1.14f, 0f), new Vector3(0.56f, 0.14f, 0.40f), new Color(0.85f, 0.44f, 0.16f), "Sash");
                Part(PrimitiveType.Cube, Body, new Vector3(0.10f, 0.92f, 0.18f), new Vector3(0.16f, 0.40f, 0.05f), new Color(0.85f, 0.44f, 0.16f), "SashFlap");
                var sickle = Part(PrimitiveType.Cube, Body, new Vector3(-0.20f, 1.55f, -0.34f), new Vector3(0.05f, 0.50f, 0.05f), pal.Dark, "SickleHandle");
                sickle.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
                Part(PrimitiveType.Cube, Body, new Vector3(-0.38f, 1.78f, -0.36f), new Vector3(0.34f, 0.09f, 0.04f), new Color(0.82f, 0.83f, 0.85f), "SickleBlade");
                break; }
            case "Soldier": {
                Part(PrimitiveType.Sphere, Head, new Vector3(0f, 0.10f, 0f), new Vector3(0.54f, 0.34f, 0.54f), pal.Main, "Helmet");
                Part(PrimitiveType.Cube, Body, new Vector3(0f, 1.48f, -0.30f), new Vector3(0.46f, 0.55f, 0.18f), pal.Secondary, "Backpack");
                var rifle = Part(PrimitiveType.Cube, Body, new Vector3(0.18f, 1.55f, -0.34f), new Vector3(0.07f, 0.62f, 0.07f), pal.Dark, "Rifle");
                rifle.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
                break; }
            case "Samurai": {
                Part(PrimitiveType.Sphere, Head, new Vector3(0f, 0.10f, 0f), new Vector3(0.55f, 0.36f, 0.55f), pal.Main, "Kabuto");
                var hornL = PartCone(Head, new Vector3(-0.20f, 0.34f, 0.10f), new Vector3(0.09f, 0.34f, 0.09f), pal.Accent, "HornL");
                hornL.transform.localRotation = Quaternion.Euler(18f, 0f, 24f);
                var hornR = PartCone(Head, new Vector3(0.20f, 0.34f, 0.10f), new Vector3(0.09f, 0.34f, 0.09f), pal.Accent, "HornR");
                hornR.transform.localRotation = Quaternion.Euler(18f, 0f, -24f);
                var katana = Part(PrimitiveType.Cube, Body, new Vector3(-0.10f, 1.50f, -0.32f), new Vector3(0.05f, 0.95f, 0.05f), new Color(0.80f, 0.82f, 0.86f), "Katana");
                katana.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
                Part(PrimitiveType.Cube, Body, new Vector3(-0.30f, 1.80f, -0.20f), new Vector3(0.16f, 0.10f, 0.16f), pal.Dark, "KatanaGrip");
                break; }
            case "Gladiator": {
                Part(PrimitiveType.Sphere, Head, new Vector3(0f, 0.08f, 0f), new Vector3(0.54f, 0.44f, 0.54f), pal.Main, "Galea");
                Part(PrimitiveType.Cube, Head, new Vector3(0f, 0.34f, 0f), new Vector3(0.08f, 0.26f, 0.44f), pal.Accent, "Crest");
                var shield = Part(PrimitiveType.Cylinder, ArmL, new Vector3(-0.16f, -0.34f, 0.06f), new Vector3(0.78f, 0.06f, 0.78f), pal.Main, "Shield");
                shield.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var sword = Part(PrimitiveType.Cube, ArmR, new Vector3(0f, -0.86f, 0.10f), new Vector3(0.06f, 0.55f, 0.12f), new Color(0.82f, 0.84f, 0.88f), "Gladius");
                sword.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
                break; }
            case "Warrior": {
                var axeH = Part(PrimitiveType.Cube, ArmR, new Vector3(0f, -0.80f, 0.06f), new Vector3(0.06f, 0.72f, 0.06f), pal.Dark, "AxeHandle");
                axeH.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
                Part(PrimitiveType.Cube, ArmR, new Vector3(0f, -1.02f, 0.28f), new Vector3(0.07f, 0.34f, 0.42f), new Color(0.78f, 0.80f, 0.84f), "AxeHead");
                Part(PrimitiveType.Sphere, Head, new Vector3(0f, 0.16f, -0.02f), new Vector3(0.52f, 0.26f, 0.52f), pal.Main, "FurCap");
                break; }
            case "Knight": {
                Part(PrimitiveType.Cylinder, Head, new Vector3(0f, 0.08f, 0f), new Vector3(0.52f, 0.44f, 0.52f), pal.Main, "Helm");
                Part(PrimitiveType.Cube, Head, new Vector3(0f, 0.02f, 0.24f), new Vector3(0.30f, 0.06f, 0.04f), pal.Dark, "Visor");
                var shield = Part(PrimitiveType.Cube, ArmL, new Vector3(-0.16f, -0.36f, 0.06f), new Vector3(0.08f, 0.72f, 0.55f), pal.Secondary, "Shield");
                var sword = Part(PrimitiveType.Cube, ArmR, new Vector3(0f, -0.92f, 0.10f), new Vector3(0.05f, 0.72f, 0.10f), new Color(0.85f, 0.87f, 0.90f), "Sword");
                sword.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
                Part(PrimitiveType.Cube, Body, new Vector3(0f, 1.42f, -0.26f), new Vector3(0.58f, 0.80f, 0.05f), pal.Accent, "Cape");
                break; }
            case "Ninja": {
                Part(PrimitiveType.Cube, Head, new Vector3(0f, 0.05f, 0f), new Vector3(0.48f, 0.20f, 0.48f), pal.Main, "Mask");
                Part(PrimitiveType.Cube, Head, new Vector3(0f, 0.24f, 0f), new Vector3(0.50f, 0.10f, 0.50f), pal.Accent, "Headband");
                Part(PrimitiveType.Cube, ArmR, new Vector3(0f, -0.78f, 0.08f), new Vector3(0.05f, 0.34f, 0.05f), pal.Dark, "NunchuckA");
                Part(PrimitiveType.Cube, ArmR, new Vector3(0.12f, -0.92f, 0.08f), new Vector3(0.05f, 0.34f, 0.05f), pal.Dark, "NunchuckB");
                break; }
            case "Pirate": {
                Part(PrimitiveType.Cylinder, Head, new Vector3(0f, 0.20f, 0f), new Vector3(0.62f, 0.05f, 0.62f), pal.Dark, "HatBrim");
                Part(PrimitiveType.Cylinder, Head, new Vector3(0f, 0.32f, 0f), new Vector3(0.40f, 0.22f, 0.40f), pal.Dark, "HatTop");
                Part(PrimitiveType.Cube, Head, new Vector3(-0.11f, 0.02f, 0.23f), new Vector3(0.13f, 0.13f, 0.03f), pal.Dark, "Eyepatch");
                var cutlass = Part(PrimitiveType.Cube, ArmR, new Vector3(0f, -0.88f, 0.10f), new Vector3(0.05f, 0.62f, 0.11f), new Color(0.83f, 0.85f, 0.89f), "Cutlass");
                cutlass.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
                break; }
            case "Robot": {
                Part(PrimitiveType.Cylinder, Head, new Vector3(0f, 0.38f, 0f), new Vector3(0.05f, 0.30f, 0.05f), pal.Secondary, "Antenna");
                Part(PrimitiveType.Sphere, Head, new Vector3(0f, 0.56f, 0f), new Vector3(0.13f, 0.13f, 0.13f), pal.Accent, "AntennaTip");
                Part(PrimitiveType.Cube, Torso, new Vector3(0f, 0.05f, 0.20f), new Vector3(0.40f, 0.30f, 0.05f), new Color(0.30f, 0.95f, 1f), "ChestScreen");
                break; }
            case "Jester": {
                // Three-pronged fool's cap with gold bells, ruff collar, chest star.
                var j1 = PartCone(Head, new Vector3(-0.20f, 0.30f, 0f), new Vector3(0.14f, 0.36f, 0.14f), pal.Main, "FoolL");
                j1.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
                PartCone(Head, new Vector3(0f, 0.36f, 0f), new Vector3(0.14f, 0.40f, 0.14f), pal.Secondary, "FoolC");
                var j3 = PartCone(Head, new Vector3(0.20f, 0.30f, 0f), new Vector3(0.14f, 0.36f, 0.14f), pal.Main, "FoolR");
                j3.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);
                Part(PrimitiveType.Sphere, Head, new Vector3(-0.32f, 0.44f, 0f), new Vector3(0.10f, 0.10f, 0.10f), pal.Accent, "BellL");
                Part(PrimitiveType.Sphere, Head, new Vector3(0f, 0.60f, 0f), new Vector3(0.10f, 0.10f, 0.10f), pal.Accent, "BellC");
                Part(PrimitiveType.Sphere, Head, new Vector3(0.32f, 0.44f, 0f), new Vector3(0.10f, 0.10f, 0.10f), pal.Accent, "BellR");
                Part(PrimitiveType.Cylinder, Body, new Vector3(0f, 1.86f, 0f), new Vector3(0.56f, 0.12f, 0.56f), Color.white, "Ruff");
                var star = Part(PrimitiveType.Cube, Torso, new Vector3(0f, 0.05f, 0.21f), new Vector3(0.20f, 0.20f, 0.04f), pal.Accent, "Star");
                star.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                break; }
            case "Pyra": {
                // Flame hair (three cones) and a fire staff with a glowing orb.
                var f1 = PartCone(Head, new Vector3(-0.14f, 0.30f, -0.04f), new Vector3(0.16f, 0.38f, 0.16f), new Color(1f, 0.45f, 0.10f), "FlameL");
                f1.transform.localRotation = Quaternion.Euler(-10f, 0f, 14f);
                PartCone(Head, new Vector3(0.05f, 0.36f, -0.02f), new Vector3(0.18f, 0.48f, 0.18f), new Color(1f, 0.62f, 0.12f), "FlameC");
                var f3 = PartCone(Head, new Vector3(0.20f, 0.28f, -0.04f), new Vector3(0.14f, 0.32f, 0.14f), new Color(0.95f, 0.30f, 0.08f), "FlameR");
                f3.transform.localRotation = Quaternion.Euler(-8f, 0f, -16f);
                Part(PrimitiveType.Cylinder, ArmR, new Vector3(0f, -0.55f, 0.06f), new Vector3(0.06f, 1.15f, 0.06f), pal.Dark, "Staff");
                Part(PrimitiveType.Sphere, ArmR, new Vector3(0f, -1.18f, 0.06f), new Vector3(0.20f, 0.20f, 0.20f), pal.Accent, "FireOrb");
                break; }
            case "Gusto": {
                // Tall chef hat, neckerchief, apron, and a trusty frying pan.
                Part(PrimitiveType.Cylinder, Head, new Vector3(0f, 0.30f, 0f), new Vector3(0.40f, 0.30f, 0.40f), Color.white, "Toque");
                Part(PrimitiveType.Sphere, Head, new Vector3(0f, 0.50f, 0f), new Vector3(0.46f, 0.26f, 0.46f), Color.white, "ToqueTop");
                Part(PrimitiveType.Cube, Body, new Vector3(0f, 1.80f, 0.16f), new Vector3(0.30f, 0.16f, 0.10f), pal.Accent, "Scarf");
                Part(PrimitiveType.Cube, Torso, new Vector3(0f, -0.05f, 0.20f), new Vector3(0.50f, 0.55f, 0.04f), Color.white, "Apron");
                Part(PrimitiveType.Cylinder, ArmR, new Vector3(0f, -0.80f, 0.08f), new Vector3(0.34f, 0.05f, 0.34f), pal.Dark, "Pan");
                Part(PrimitiveType.Cube, ArmR, new Vector3(0f, -0.66f, 0.02f), new Vector3(0.05f, 0.22f, 0.05f), pal.Dark, "PanHandle");
                break; }
            case "Dusty": {
                // Cowboy hat and band, bandana, poncho, six-shooter.
                Part(PrimitiveType.Cylinder, Head, new Vector3(0f, 0.22f, 0f), new Vector3(0.68f, 0.05f, 0.68f), pal.Main, "Brim");
                Part(PrimitiveType.Cylinder, Head, new Vector3(0f, 0.34f, 0f), new Vector3(0.40f, 0.24f, 0.40f), pal.Main, "Crown");
                Part(PrimitiveType.Cube, Head, new Vector3(0f, 0.24f, 0f), new Vector3(0.42f, 0.07f, 0.42f), pal.Accent, "HatBand");
                Part(PrimitiveType.Cube, Body, new Vector3(0f, 1.78f, 0.14f), new Vector3(0.34f, 0.20f, 0.12f), pal.Accent, "Bandana");
                Part(PrimitiveType.Cube, Torso, new Vector3(0f, 0.10f, 0f), new Vector3(0.70f, 0.30f, 0.44f), pal.Secondary, "Poncho");
                Part(PrimitiveType.Cube, ArmR, new Vector3(0f, -0.74f, 0.10f), new Vector3(0.07f, 0.16f, 0.30f), pal.Dark, "SixShooter");
                break; }
        }
    }

    // Transform glow: tint every part gold (Power Stone transformation look).
    public void SetGold(bool on)
    {
        if (gold == on || allRenderers == null) return;
        gold = on;
        for (int i = 0; i < allRenderers.Length; i++)
            allRenderers[i].material.color = on ? new Color(1f, 0.80f, 0.15f) : baseColors[i];
    }
}
