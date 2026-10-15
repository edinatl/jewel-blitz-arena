using UnityEngine;
using System.Collections.Generic;

// v0.5: every one of the 111 items gets its own multi-part 3D model, built
// at runtime from primitives in Power Stone item families — swords with
// guards and hilts, guns with barrels/drums/coils, bombs with fuses, food,
// glowing wands and shields — plus an idle animation per family (explosives
// pulse, specials glow). Replaces the old single-colored-primitive look.
// Unity has no Cone or Torus primitive, so both are tiny procedural meshes.
public static class ItemVisual
{
    private static Mesh coneMesh, torusMesh;
    private static List<Material> curGlowMats;
    private static List<Color> curGlowCols;

    static Color H(int hex)
    {
        return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, 1f);
    }

    static Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        return m;
    }

    static Material MatGlow(Color c, Color e)
    {
        var m = Mat(c);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", e);
        if (curGlowMats != null) { curGlowMats.Add(m); curGlowCols.Add(e); }
        return m;
    }

    // --- procedural meshes --------------------------------------------------

    static Mesh ConeMesh()
    {
        if (coneMesh != null) return coneMesh;
        int seg = 12;
        var verts = new List<Vector3>();
        var tris = new List<int>();
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

    static Mesh TorusMesh() // ring radius 0.38, tube 0.12, axis = Y
    {
        if (torusMesh != null) return torusMesh;
        int rings = 18, sides = 10;
        float R = 0.38f, r = 0.12f;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < rings; i++)
        {
            float a = i / (float)rings * Mathf.PI * 2f;
            for (int j = 0; j < sides; j++)
            {
                float b = j / (float)sides * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(b), Mathf.Sin(a) * Mathf.Cos(b));
                verts.Add(new Vector3(Mathf.Cos(a) * R, 0f, Mathf.Sin(a) * R) + dir * r);
            }
        }
        for (int i = 0; i < rings; i++)
        {
            int ni = (i + 1) % rings;
            for (int j = 0; j < sides; j++)
            {
                int nj = (j + 1) % sides;
                int A = i * sides + j, B = ni * sides + j, C = i * sides + nj, D = ni * sides + nj;
                tris.Add(A); tris.Add(D); tris.Add(B);
                tris.Add(A); tris.Add(C); tris.Add(D);
            }
        }
        var mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        torusMesh = mesh;
        return mesh;
    }

    // --- part helpers (sizes in item-local units, model ~1.1 tall) ----------

    static GameObject AddPart(GameObject parent, GameObject go, Vector3 pos, string name)
    {
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        return go;
    }

    static GameObject Box(GameObject p, float x, float y, float z, float w, float h, float d, Color c, string n)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = new Vector3(w, h, d);
        go.GetComponent<Renderer>().material = Mat(c);
        return AddPart(p, go, new Vector3(x, y, z), n);
    }

    static GameObject BoxG(GameObject p, float x, float y, float z, float w, float h, float d, Color c, Color e, string n)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.localScale = new Vector3(w, h, d);
        go.GetComponent<Renderer>().material = MatGlow(c, e);
        return AddPart(p, go, new Vector3(x, y, z), n);
    }

    static GameObject Sph(GameObject p, float x, float y, float z, float r, Color c, string n)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.localScale = Vector3.one * (r * 2f);
        go.GetComponent<Renderer>().material = Mat(c);
        return AddPart(p, go, new Vector3(x, y, z), n);
    }

    static GameObject SphG(GameObject p, float x, float y, float z, float r, Color c, Color e, string n)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.localScale = Vector3.one * (r * 2f);
        go.GetComponent<Renderer>().material = MatGlow(c, e);
        return AddPart(p, go, new Vector3(x, y, z), n);
    }

    static GameObject Cyl(GameObject p, float x, float y, float z, float r, float h, Color c, string n)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);
        go.GetComponent<Renderer>().material = Mat(c);
        return AddPart(p, go, new Vector3(x, y, z), n);
    }

    static GameObject CylG(GameObject p, float x, float y, float z, float r, float h, Color c, Color e, string n)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);
        go.GetComponent<Renderer>().material = MatGlow(c, e);
        return AddPart(p, go, new Vector3(x, y, z), n);
    }

    static GameObject Caps(GameObject p, float x, float y, float z, float r, float h, Color c, string n)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);
        go.GetComponent<Renderer>().material = Mat(c);
        return AddPart(p, go, new Vector3(x, y, z), n);
    }

    static GameObject Cone(GameObject p, float x, float y, float z, float r, float h, Color c, string n)
    {
        return ConeM(p, x, y, z, r, h, Mat(c), n);
    }

    static GameObject ConeG(GameObject p, float x, float y, float z, float r, float h, Color c, Color e, string n)
    {
        return ConeM(p, x, y, z, r, h, MatGlow(c, e), n);
    }

    static GameObject ConeM(GameObject p, float x, float y, float z, float r, float h, Material m, string n)
    {
        var go = new GameObject(n);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = ConeMesh();
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = m;
        go.transform.localScale = new Vector3(r * 2f, h, r * 2f);
        return AddPart(p, go, new Vector3(x, y, z), n);
    }

    static GameObject Tor(GameObject p, float x, float y, float z, float ringR, Color c, string n)
    {
        return TorM(p, x, y, z, ringR, Mat(c), n);
    }

    static GameObject TorG(GameObject p, float x, float y, float z, float ringR, Color c, Color e, string n)
    {
        return TorM(p, x, y, z, ringR, MatGlow(c, e), n);
    }

    static GameObject TorM(GameObject p, float x, float y, float z, float ringR, Material m, string n)
    {
        var go = new GameObject(n);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = TorusMesh();
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = m;
        float s = ringR / 0.38f;
        go.transform.localScale = new Vector3(s, s, s);
        return AddPart(p, go, new Vector3(x, y, z), n);
    }

    static GameObject Rot(GameObject go, float x, float y, float z)
    {
        go.transform.localRotation = Quaternion.Euler(x, y, z);
        return go;
    }

    // --- shared mini-builders ------------------------------------------------

    static void Sword(GameObject g, Color blade, Color guard)
    {
        Box(g, 0f, .62f, 0f, .07f, .8f, .03f, blade, "Blade");
        Box(g, 0f, .22f, 0f, .22f, .05f, .06f, guard, "Guard");
        Cyl(g, 0f, .08f, 0f, .035f, .22f, H(0x4a3220), "Hilt");
    }

    static void Hammer(GameObject g, Color head, float big)
    {
        Cyl(g, 0f, .5f, 0f, .04f, .9f, H(0x6b4a2a), "Shaft");
        Box(g, 0f, .92f, 0f, .5f * big, .24f, .24f, head, "Head");
    }

    static void Axe(GameObject g)
    {
        Cyl(g, 0f, .55f, 0f, .035f, 1.0f, H(0x6b4a2a), "Shaft");
        Box(g, .12f, .95f, 0f, .24f, .3f, .05f, H(0x8a8f9a), "Blade");
    }

    static void Barrel(GameObject g, Color wood, bool foam)
    {
        Cyl(g, 0f, .5f, 0f, .28f, .8f, wood, "Body");
        Tor(g, 0f, .7f, 0f, .29f, H(0x555560), "RingTop");
        Tor(g, 0f, .3f, 0f, .29f, H(0x555560), "RingLow");
        if (foam) Cyl(g, 0f, .93f, 0f, .24f, .08f, H(0xfff6dc), "Foam");
    }

    static void GunBody(GameObject g, float len)
    {
        Box(g, 0f, .5f, 0f, .16f, .2f, len, H(0x3a3a44), "Body");
        Rot(Cyl(g, 0f, .56f, len / 2f + .1f, .045f, .5f, H(0x555560), "Barrel"), 90f, 0f, 0f);
        Box(g, 0f, .32f, -.14f, .09f, .2f, .1f, H(0x2a2a32), "Grip");
    }

    // --- the build ------------------------------------------------------------

    public static GameObject Build(ItemDefinition def, string goName)
    {
        curGlowMats = new List<Material>();
        curGlowCols = new List<Color>();
        var g = new GameObject(goName);
        string n = def.Name;
        string anim = "none";

        if (def.Archetype == ItemArchetype.Melee)
        {
            if (n.Contains("Golden Club"))
            {
                Cyl(g, 0f, .45f, 0f, .04f, .85f, H(0x8a6a20), "Shaft");
                Box(g, .1f, .9f, 0f, .34f, .16f, .12f, H(0xffd94d), "Head");
                Box(g, -.07f, .88f, 0f, .1f, .1f, .1f, H(0xffd94d), "Neck");
            }
            else if (n.Contains("Katana"))
            {
                Box(g, 0f, .62f, 0f, .06f, .78f, .025f, H(0xd8dee8), "Blade");
                Box(g, 0f, .24f, 0f, .02f, .14f, .02f, H(0xd8dee8), "BladeBase");
                Cyl(g, 0f, .2f, 0f, .05f, .05f, H(0x8a6a20), "Guard");
                Cyl(g, 0f, .08f, 0f, .032f, .22f, H(0x222222), "Hilt");
            }
            else if (n.Contains("Broadsword")) { Sword(g, H(0xb8c0cc), H(0x8a6a20)); Box(g, 0f, .62f, 0f, .1f, .8f, .04f, H(0xb8c0cc), "BladeWide"); }
            else if (n.Contains("Laser Sword"))
            {
                BoxG(g, 0f, .62f, 0f, .06f, .8f, .03f, H(0x66eeff), H(0x2299cc) * 1.6f, "Blade");
                Box(g, 0f, .22f, 0f, .2f, .05f, .06f, H(0x333344), "Guard");
                Cyl(g, 0f, .08f, 0f, .035f, .22f, H(0x333344), "Hilt");
                anim = "glow";
            }
            else if (n.Contains("Rapier")) { Sword(g, H(0xd8dee8), H(0x8a6a20)); Rot(Tor(g, 0f, .22f, 0f, .09f, H(0x8a6a20), "Cup"), 90f, 0f, 0f); }
            else if (n.Contains("Battle Axe")) Axe(g);
            else if (n.Contains("War Mace"))
            {
                Cyl(g, 0f, .5f, 0f, .04f, .9f, H(0x6b4a2a), "Shaft");
                Sph(g, 0f, 1.0f, 0f, .16f, H(0x555560), "Head");
                for (int i = 0; i < 6; i++)
                {
                    float a = i / 6f * Mathf.PI * 2f;
                    Rot(Cone(g, Mathf.Cos(a) * .16f, 1.0f, Mathf.Sin(a) * .16f, .045f, .12f, H(0x8a8f9a), "Spike"), 0f, 0f, 0f);
                }
            }
            else if (n.Contains("Halberd")) { Axe(g); Cone(g, 0f, 1.28f, 0f, .05f, .2f, H(0xb8c0cc), "Tip"); }
            else if (n.Contains("Spear")) { Cyl(g, 0f, .6f, 0f, .03f, 1.2f, H(0x6b4a2a), "Shaft"); Cone(g, 0f, 1.3f, 0f, .06f, .22f, H(0xb8c0cc), "Tip"); }
            else if (n.Contains("Chainsaw"))
            {
                Box(g, 0f, .5f, 0f, .3f, .3f, .16f, H(0xcc4422), "Body");
                Box(g, 0f, .62f, .12f, .08f, .16f, .7f, H(0x9aa0aa), "Bar");
                Cyl(g, 0f, .32f, 0f, .03f, .18f, H(0x333333), "Grip");
            }
            else if (n.Contains("Spiked Club"))
            {
                Cyl(g, 0f, .55f, 0f, .085f, .95f, H(0x6b4a2a), "Club");
                Cyl(g, 0f, .05f, 0f, .035f, .2f, H(0x4a3220), "Grip");
                for (int i = 0; i < 5; i++) Cone(g, 0f, .45f + i * .14f, .09f, .035f, .1f, H(0x9aa0aa), "Spike");
            }
            else if (n.Contains("Golf Club"))
            {
                Cyl(g, 0f, .55f, 0f, .025f, 1.0f, H(0x888888), "Shaft");
                Rot(Box(g, .1f, .06f, 0f, .2f, .09f, .09f, H(0x555560), "Head"), 0f, 0f, 28f);
            }
            else if (n.Contains("Tennis Racket"))
            {
                Cyl(g, 0f, .35f, 0f, .03f, .6f, H(0x8a5a20), "Handle");
                Tor(g, 0f, .85f, 0f, .2f, H(0xcc4422), "Rim");
                g.transform.GetChild(g.transform.childCount - 1).localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else if (n.Contains("Broom")) { Cyl(g, 0f, .6f, 0f, .03f, 1.1f, H(0x8a6a20), "Stick"); Box(g, 0f, .08f, 0f, .16f, .22f, .12f, H(0xd8b45a), "Brush"); }
            else if (n.Contains("Shovel")) { Cyl(g, 0f, .6f, 0f, .03f, 1.0f, H(0x6b4a2a), "Shaft"); Box(g, 0f, .05f, 0f, .18f, .22f, .04f, H(0x8a8f9a), "Blade"); }
            else if (n.Contains("Torch"))
            {
                Cyl(g, 0f, .5f, 0f, .035f, .9f, H(0x6b4a2a), "Stick");
                SphG(g, 0f, 1.05f, 0f, .13f, H(0xff8c1e), H(0xff5500) * 1.8f, "Flame");
                anim = "glow";
            }
            else if (n.Contains("Umbrella")) { Cyl(g, 0f, .55f, 0f, .025f, 1.0f, H(0x333333), "Pole"); Cone(g, 0f, 1.02f, 0f, .42f, .25f, def.Color, "Canopy"); }
            else if (n.Contains("Wrench")) { Box(g, 0f, .55f, 0f, .09f, .9f, .05f, H(0x9aa0aa), "Shaft"); Box(g, 0f, 1.02f, 0f, .2f, .12f, .05f, H(0x9aa0aa), "Jaw"); }
            else if (n.Contains("Frying Pan")) { Cyl(g, 0f, .55f, 0f, .15f, .07f, H(0x3a3a44), "Pan"); Box(g, 0f, .85f, 0f, .06f, .5f, .05f, H(0x3a3a44), "Handle"); }
            else if (n.Contains("Mallet")) { Cyl(g, 0f, .5f, 0f, .04f, .85f, H(0x8a6a20), "Shaft"); Cyl(g, 0f, .92f, 0f, .15f, .34f, H(0xd8b45a), "Head"); }
            else if (n.Contains("Hammer")) Hammer(g, H(0x8a8f9a), 1f);
            else if (n.Contains("Pipe")) Cyl(g, 0f, .55f, 0f, .06f, 1.0f, H(0x8a8f9a), "Pipe");
            else if (n.Contains("Cricket")) { Box(g, 0f, .5f, 0f, .22f, .8f, .06f, H(0xd8b45a), "Blade"); Cyl(g, 0f, .02f, 0f, .035f, .22f, H(0x4a3220), "Grip"); }
            else { Cyl(g, 0f, .6f, 0f, .06f, 1.0f, H(0x9a6a38), "Bat"); Cyl(g, 0f, .02f, 0f, .035f, .16f, H(0x333333), "Knob"); }
        }
        else if (def.Archetype == ItemArchetype.Gun)
        {
            if (n.Contains("Mega Buster"))
            {
                Rot(Cyl(g, 0f, .5f, .1f, .15f, .62f, H(0x2a4bd7), "Cannon"), 90f, 0f, 0f);
                Cyl(g, 0f, .5f, .45f, .1f, .1f, H(0x9fe8ff), "Muzzle");
                Box(g, 0f, .34f, -.1f, .2f, .16f, .34f, H(0x2456a8), "Base");
            }
            else if (n.Contains("Rocket Launcher"))
            {
                Rot(Cyl(g, 0f, .55f, 0f, .14f, 1.1f, H(0x4a5a3a), "Tube"), 90f, 0f, 0f);
                Rot(Cone(g, 0f, .55f, -.62f, .14f, .2f, H(0x3a4a2a), "Back"), -90f, 0f, 0f);
                Box(g, 0f, .28f, .12f, .08f, .16f, .08f, H(0x2a2a32), "Grip");
            }
            else if (n.Contains("Grenade Launcher"))
            {
                Rot(Cyl(g, 0f, .55f, 0f, .1f, .95f, H(0x555560), "Tube"), 90f, 0f, 0f);
                Cyl(g, 0f, .42f, -.12f, .12f, .22f, H(0x777780), "Drum");
            }
            else if (n.Contains("Machine Gun")) { GunBody(g, .7f); Box(g, 0f, .34f, 0f, .09f, .22f, .12f, H(0x2a2a32), "Mag"); }
            else if (n.Contains("Shotgun")) { GunBody(g, .6f); Box(g, 0f, .5f, 0f, .2f, .08f, .7f, H(0x6b4a2a), "Pump"); }
            else if (n.Contains("Flame Thrower"))
            {
                Box(g, 0f, .5f, 0f, .16f, .2f, .6f, H(0x7a3a1a), "Body");
                Cyl(g, .12f, .5f, -.1f, .07f, .3f, H(0xaa3322), "TankL");
                Cyl(g, -.12f, .5f, -.1f, .07f, .3f, H(0xaa3322), "TankR");
                Rot(CylG(g, 0f, .56f, .38f, .05f, .12f, H(0xff8c1e), H(0xff5500) * 1.8f, "Nozzle"), 90f, 0f, 0f);
                anim = "glow";
            }
            else if (n.Contains("Freeze Ray")) { GunBody(g, .55f); Rot(ConeG(g, 0f, .56f, .42f, .07f, .18f, H(0x9fe8ff), H(0x33aaff) * 1.8f, "Crystal"), 90f, 0f, 0f); anim = "glow"; }
            else if (n.Contains("Laser Gun")) { GunBody(g, .5f); Rot(CylG(g, 0f, .56f, .36f, .03f, .16f, H(0xff4444), H(0xff2222) * 1.8f, "Emitter"), 90f, 0f, 0f); anim = "glow"; }
            else if (n.Contains("Plasma Rifle")) { GunBody(g, .6f); TorG(g, 0f, .56f, .28f, .06f, H(0x44ffcc), H(0x11cc88) * 1.6f, "CoilA"); TorG(g, 0f, .56f, .42f, .06f, H(0x44ffcc), H(0x11cc88) * 1.6f, "CoilB"); anim = "glow"; }
            else if (n.Contains("Railgun"))
            {
                Box(g, -.06f, .5f, 0f, .05f, .14f, .9f, H(0x3a3a44), "RailL");
                Box(g, .06f, .5f, 0f, .05f, .14f, .9f, H(0x3a3a44), "RailR");
                BoxG(g, 0f, .5f, .42f, .05f, .05f, .2f, H(0x9fe8ff), H(0x33aaff) * 1.6f, "Core");
                anim = "glow";
            }
            else if (n.Contains("Lightning Gun")) { GunBody(g, .5f); TorG(g, 0f, .56f, .2f, .045f, H(0xffee55), H(0xccaa00) * 1.7f, "CoilA"); TorG(g, 0f, .56f, .32f, .045f, H(0xffee55), H(0xccaa00) * 1.7f, "CoilB"); anim = "glow"; }
            else if (n.Contains("Crossbow"))
            {
                Box(g, 0f, .5f, 0f, .1f, .12f, .8f, H(0x6b4a2a), "Stock");
                Box(g, -.22f, .55f, .3f, .5f, .06f, .06f, H(0x8a6a20), "ArmL");
                Box(g, .22f, .55f, .3f, .5f, .06f, .06f, H(0x8a6a20), "ArmR");
            }
            else if (n.Contains("Bow"))
            {
                var bow = Tor(g, 0f, .55f, 0f, .35f, H(0x8a6a20), "Arc");
                bow.transform.localScale = new Vector3(bow.transform.localScale.x, bow.transform.localScale.y, bow.transform.localScale.z * 0.4f);
                Rot(bow, 90f, 0f, 0f);
            }
            else if (n.Contains("Slingshot"))
            {
                Rot(Cyl(g, -.08f, .62f, 0f, .03f, .4f, H(0x8a6a20), "ForkL"), 0f, 0f, -12f);
                Rot(Cyl(g, .08f, .62f, 0f, .03f, .4f, H(0x8a6a20), "ForkR"), 0f, 0f, 12f);
                Cyl(g, 0f, .38f, 0f, .035f, .5f, H(0x6b4a2a), "Grip");
            }
            else if (n.Contains("Water Cannon")) { GunBody(g, .5f); Cyl(g, 0f, .42f, -.14f, .09f, .24f, H(0x3388cc), "Tank"); }
            else if (n.Contains("Bubble Gun")) { GunBody(g, .4f); Sph(g, 0f, .62f, .4f, .12f, H(0xaad4ff), "Bubble"); }
            else if (n.Contains("Sonic Blaster")) { GunBody(g, .45f); Rot(Cone(g, 0f, .56f, .42f, .12f, .24f, H(0x777788), "Horn"), 90f, 0f, 0f); }
            else if (n.Contains("Revolver"))
            {
                Box(g, 0f, .5f, 0f, .13f, .16f, .5f, H(0x555560), "Body");
                Rot(Cyl(g, 0f, .5f, 0f, .08f, .18f, H(0x777780), "Drum"), 0f, 0f, 90f);
                Box(g, 0f, .32f, -.12f, .08f, .18f, .09f, H(0x6b4a2a), "Grip");
            }
            else GunBody(g, .45f); // Blaster Pistol + fallback
        }
        else if (def.Archetype == ItemArchetype.Throwable)
        {
            if (n.Contains("Chair"))
            {
                Box(g, 0f, .45f, 0f, .5f, .08f, .5f, H(0x8a5a20), "Seat");
                Box(g, 0f, .85f, -.22f, .5f, .8f, .08f, H(0x8a5a20), "Back");
                Cyl(g, -.2f, .2f, -.2f, .035f, .4f, H(0x6b4218), "Leg");
                Cyl(g, .2f, .2f, -.2f, .035f, .4f, H(0x6b4218), "Leg");
                Cyl(g, -.2f, .2f, .2f, .035f, .4f, H(0x6b4218), "Leg");
                Cyl(g, .2f, .2f, .2f, .035f, .4f, H(0x6b4218), "Leg");
            }
            else if (n.Contains("Stool"))
            {
                Cyl(g, 0f, .5f, 0f, .24f, .08f, H(0x8a5a20), "Seat");
                Cyl(g, -.15f, .22f, 0f, .03f, .45f, H(0x6b4218), "Leg");
                Cyl(g, .15f, .22f, 0f, .03f, .45f, H(0x6b4218), "Leg");
                Cyl(g, 0f, .22f, .15f, .03f, .45f, H(0x6b4218), "Leg");
            }
            else if (n.Contains("Bench"))
            {
                Box(g, 0f, .42f, 0f, 1.1f, .08f, .4f, H(0x8a5a20), "Seat");
                Box(g, -.45f, .18f, 0f, .08f, .36f, .36f, H(0x6b4218), "Leg");
                Box(g, .45f, .18f, 0f, .08f, .36f, .36f, H(0x6b4218), "Leg");
            }
            else if (n.Contains("Small Table"))
            {
                Box(g, 0f, .62f, 0f, .7f, .07f, .7f, H(0x9a6a30), "Top");
                Cyl(g, -.28f, .3f, -.28f, .03f, .6f, H(0x6b4218), "Leg");
                Cyl(g, .28f, .3f, -.28f, .03f, .6f, H(0x6b4218), "Leg");
                Cyl(g, -.28f, .3f, .28f, .03f, .6f, H(0x6b4218), "Leg");
                Cyl(g, .28f, .3f, .28f, .03f, .6f, H(0x6b4218), "Leg");
            }
            else if (n.Contains("Table Lamp"))
            {
                Cyl(g, 0f, .08f, 0f, .12f, .08f, H(0x555560), "Base");
                Cyl(g, 0f, .4f, 0f, .03f, .6f, H(0x555560), "Stem");
                ConeG(g, 0f, .75f, 0f, .2f, .22f, H(0xffe9a8), H(0xcc9900) * 1.2f, "Shade");
                anim = "glow";
            }
            else if (n.Contains("Ale Barrel")) Barrel(g, H(0x9a6a30), true);
            else if (n.Contains("Wooden Barrel")) Barrel(g, H(0x8a5a20), false);
            else if (n.Contains("Metal Barrel"))
            {
                Cyl(g, 0f, .5f, 0f, .3f, .85f, H(0x5a6a7a), "Body");
                Tor(g, 0f, .7f, 0f, .3f, H(0x8a8f9a), "Ring");
            }
            else if (n.Contains("Crate"))
            {
                Box(g, 0f, .5f, 0f, .62f, .62f, .62f, H(0xa0762e), "Body");
                Box(g, 0f, .5f, .32f, .66f, .1f, .06f, H(0x7a5a1e), "BraceF");
                Box(g, 0f, .5f, -.32f, .66f, .1f, .06f, H(0x7a5a1e), "BraceB");
            }
            else if (n.Contains("Cactus"))
            {
                Cyl(g, 0f, .15f, 0f, .2f, .3f, H(0xb5652a), "Pot");
                Caps(g, 0f, .62f, 0f, .16f, .75f, H(0x3f9e3f), "Trunk");
                Rot(Caps(g, -.24f, .62f, 0f, .07f, .34f, H(0x3f9e3f), "ArmL"), 0f, 0f, 90f);
                Rot(Caps(g, .24f, .74f, 0f, .07f, .34f, H(0x3f9e3f), "ArmR"), 0f, 0f, 90f);
                Sph(g, 0f, 1.02f, 0f, .06f, H(0xe86ab0), "Flower");
                anim = "wobble";
            }
            else if (n.Contains("Rock")) { var r = Sph(g, 0f, .4f, 0f, .38f, H(0x8a8a8e), "Rock"); r.transform.localScale = new Vector3(.76f, .6f, .7f); }
            else if (n.Contains("Brick")) Box(g, 0f, .3f, 0f, .5f, .2f, .24f, H(0xb0402a), "Brick");
            else if (n.Contains("Anvil"))
            {
                Box(g, 0f, .15f, 0f, .5f, .2f, .3f, H(0x3a3a44), "Base");
                Box(g, 0f, .42f, 0f, .44f, .22f, .28f, H(0x4a4a54), "Top");
                Rot(Cone(g, .36f, .42f, 0f, .09f, .3f, H(0x4a4a54), "Horn"), 0f, 0f, -90f);
            }
            else if (n.Contains("Safe"))
            {
                Box(g, 0f, .45f, 0f, .6f, .7f, .55f, H(0x3a4a3a), "Body");
                Rot(Cyl(g, 0f, .45f, .29f, .09f, .06f, H(0x8a8f9a), "Dial"), 90f, 0f, 0f);
            }
            else if (n.Contains("Book")) { Box(g, 0f, .35f, 0f, .44f, .12f, .3f, H(0xa02020), "Cover"); Box(g, .02f, .35f, 0f, .38f, .09f, .26f, H(0xf5f0dc), "Pages"); }
            else if (n.Contains("Vase")) { Cyl(g, 0f, .35f, 0f, .19f, .5f, H(0x4a7a9a), "Body"); Cyl(g, 0f, .66f, 0f, .09f, .16f, H(0x4a7a9a), "Neck"); }
            else if (n.Contains("Plant Pot")) { Cyl(g, 0f, .2f, 0f, .21f, .3f, H(0xb5652a), "Pot"); Sph(g, 0f, .55f, 0f, .22f, H(0x3f9e3f), "Bush"); }
            else if (n.Contains("Globe"))
            {
                Sph(g, 0f, .55f, 0f, .24f, H(0x3a7acc), "Ocean");
                Sph(g, -.08f, .6f, .1f, .1f, H(0x3f9e3f), "Land");
                Cyl(g, 0f, .12f, 0f, .05f, .2f, H(0x8a6a20), "Stand");
            }
            else if (n.Contains("Television"))
            {
                Box(g, 0f, .5f, 0f, .6f, .5f, .4f, H(0x3a3a44), "Body");
                BoxG(g, 0f, .52f, .21f, .46f, .34f, .02f, H(0x9fe8ff), H(0x3388cc) * 1.1f, "Screen");
                anim = "glow";
            }
            else if (n.Contains("Microwave")) { Box(g, 0f, .45f, 0f, .55f, .4f, .4f, H(0x8a8f9a), "Body"); Box(g, 0f, .45f, .21f, .34f, .24f, .02f, H(0x2a2a32), "Door"); }
            else if (n.Contains("Toolbox")) { Box(g, 0f, .4f, 0f, .6f, .36f, .34f, H(0xcc2222), "Body"); Box(g, 0f, .66f, 0f, .4f, .08f, .08f, H(0x8a8f9a), "Handle"); }
            else if (n.Contains("Backpack")) { Box(g, 0f, .5f, 0f, .44f, .55f, .3f, H(0x7a3a9a), "Body"); Box(g, 0f, .62f, .17f, .3f, .2f, .06f, H(0x5a2a7a), "Pocket"); }
            else if (n.Contains("Tire")) Rot(Tor(g, 0f, .45f, 0f, .3f, H(0x2a2a2e), "Tire"), 0f, 0f, 0f);
            else if (n.Contains("Bowling Ball")) Sph(g, 0f, .4f, 0f, .3f, H(0x22228a), "Ball");
            else if (n.Contains("Dumbbell"))
            {
                Rot(Cyl(g, 0f, .4f, 0f, .045f, .7f, H(0x555560), "Bar"), 0f, 0f, 90f);
                Rot(Cyl(g, -.3f, .4f, 0f, .14f, .12f, H(0x3a3a44), "PlateL"), 0f, 0f, 90f);
                Rot(Cyl(g, .3f, .4f, 0f, .14f, .12f, H(0x3a3a44), "PlateR"), 0f, 0f, 90f);
            }
            else if (n.Contains("Kettle"))
            {
                Cyl(g, 0f, .4f, 0f, .2f, .4f, H(0xb8c0cc), "Body");
                Rot(Cone(g, .26f, .5f, 0f, .06f, .24f, H(0xb8c0cc), "Spout"), 0f, 0f, -40f);
            }
            else if (n.Contains("Bucket")) Cyl(g, 0f, .35f, 0f, .21f, .45f, H(0x8a8f9a), "Body");
            else if (n.Contains("Treasure Chest"))
            {
                Box(g, 0f, .32f, 0f, .6f, .32f, .4f, H(0x8a5a20), "Body");
                Box(g, 0f, .52f, 0f, .6f, .16f, .4f, H(0x9a6a30), "Lid");
                Box(g, 0f, .4f, .21f, .08f, .12f, .02f, H(0xffd94d), "Lock");
            }
            else if (n.Contains("Statue Head"))
            {
                Box(g, 0f, .12f, 0f, .4f, .24f, .4f, H(0x9a9a9e), "Base");
                Sph(g, 0f, .55f, 0f, .26f, H(0xb8b8bc), "Head");
                Box(g, 0f, .55f, .24f, .3f, .12f, .06f, H(0x8a8a8e), "Brow");
            }
            else Box(g, 0f, .45f, 0f, .5f, .5f, .5f, def.Color, "Prop");
        }
        else if (def.Archetype == ItemArchetype.Explosive)
        {
            if (n.Contains("Big Bomb")) BombBall(g, .42f, H(0x1a1a20));
            else if (n.Contains("Dynamite"))
            {
                for (int i = -1; i <= 1; i++) Cyl(g, i * .11f, .5f, 0f, .07f, .6f, H(0xcc2222), "Stick");
                Cyl(g, 0f, .85f, 0f, .02f, .14f, H(0x6b4a2a), "Fuse");
                SphG(g, 0f, .95f, 0f, .04f, H(0xffd23a), H(0xff5500) * 2f, "Spark");
            }
            else if (n.Contains("Grenade")) BombBall(g, .24f, H(0x3a5a3a));
            else if (n.Contains("Fire Bomb")) { Sph(g, 0f, .5f, 0f, .3f, H(0xcc5522), "Body"); SphG(g, 0f, .72f, 0f, .14f, H(0xff8c1e), H(0xff5500) * 1.8f, "Flame"); }
            else if (n.Contains("Ice Bomb")) SphG(g, 0f, .5f, 0f, .3f, H(0x9fd4ff), H(0x3388cc) * 0.9f, "Body");
            else if (n.Contains("Shock Bomb")) SphG(g, 0f, .5f, 0f, .3f, H(0xd8b400), H(0xaa8800) * 1.2f, "Body");
            else if (n.Contains("Poison Bomb")) SphG(g, 0f, .5f, 0f, .3f, H(0x6a2a9a), H(0x3a105a) * 1.2f, "Body");
            else if (n.Contains("Holy Bomb"))
            {
                SphG(g, 0f, .5f, 0f, .3f, H(0xf5f0dc), H(0xffd94d) * 0.9f, "Body");
                Box(g, 0f, .82f, 0f, .05f, .2f, .05f, H(0xffd94d), "CrossV");
                Box(g, 0f, .82f, 0f, .14f, .05f, .05f, H(0xffd94d), "CrossH");
            }
            else if (n.Contains("Powder Keg")) { Cyl(g, 0f, .42f, 0f, .22f, .55f, H(0x8a5a20), "Body"); Tor(g, 0f, .58f, 0f, .23f, H(0x555560), "Ring"); }
            else if (n.Contains("Oil Drum")) { Cyl(g, 0f, .45f, 0f, .26f, .7f, H(0x3a3a44), "Body"); Tor(g, 0f, .6f, 0f, .26f, H(0x8a8f9a), "Ring"); }
            else if (n.Contains("Gas Can")) { Box(g, 0f, .4f, 0f, .4f, .45f, .24f, H(0xcc2222), "Body"); Rot(Cyl(g, .22f, .68f, 0f, .05f, .24f, H(0x555560), "Spout"), 0f, 0f, -28f); }
            else if (n.Contains("Firework"))
            {
                Box(g, 0f, .25f, 0f, .5f, .4f, .4f, H(0x8a5a20), "Crate");
                Cyl(g, -.14f, .62f, 0f, .05f, .45f, H(0xcc2222), "Rocket");
                Cyl(g, 0f, .62f, 0f, .05f, .45f, H(0x2266cc), "Rocket");
                Cyl(g, .14f, .62f, 0f, .05f, .45f, H(0x22aa44), "Rocket");
            }
            else if (n.Contains("Land Mine"))
            {
                Cyl(g, 0f, .22f, 0f, .32f, .16f, H(0x4a4a44), "Body");
                CylG(g, 0f, .33f, 0f, .08f, .06f, H(0xcc2222), H(0xff2222) * 1.4f, "Button");
            }
            else if (n.Contains("Sticky Bomb"))
            {
                Sph(g, 0f, .5f, 0f, .28f, H(0x4a9a3a), "Body");
                for (int i = 0; i < 5; i++)
                {
                    float a = i / 5f * Mathf.PI * 2f;
                    Sph(g, Mathf.Cos(a) * .28f, .5f, Mathf.Sin(a) * .28f, .07f, H(0x6ac04a), "Glob");
                }
            }
            else if (n.Contains("Cluster Bomb"))
            {
                Sph(g, -.12f, .45f, 0f, .18f, H(0x1a1a20), "Bomblet");
                Sph(g, .12f, .45f, .08f, .18f, H(0x1a1a20), "Bomblet");
                Sph(g, 0f, .68f, -.06f, .18f, H(0x1a1a20), "Bomblet");
            }
            else BombBall(g, .3f, H(0x1a1a20));
            anim = "pulse";
        }
        else if (def.Archetype == ItemArchetype.Food)
        {
            if (n.Contains("Cake"))
            {
                Cyl(g, 0f, .3f, 0f, .3f, .22f, H(0xf5d0e0), "Base");
                Cyl(g, 0f, .5f, 0f, .22f, .18f, H(0xe8a0c0), "Top");
                Sph(g, 0f, .64f, 0f, .05f, H(0xcc2222), "Cherry");
            }
            else if (n.Contains("Roast Chicken"))
            {
                Sph(g, 0f, .45f, 0f, .28f, H(0xd88a3a), "Body");
                Rot(Cyl(g, .3f, .6f, 0f, .04f, .3f, H(0xf5e8d0), "Bone"), 0f, 0f, -35f);
                Sph(g, .42f, .72f, 0f, .07f, H(0xf5e8d0), "BoneEnd");
            }
            else if (n.Contains("Pizza"))
            {
                Cyl(g, 0f, .35f, 0f, .32f, .08f, H(0xe8b45a), "Base");
                for (int i = 0; i < 4; i++)
                {
                    float a = i / 4f * Mathf.PI * 2f + .4f;
                    Sph(g, Mathf.Cos(a) * .14f, .4f, Mathf.Sin(a) * .14f, .045f, H(0xcc2222), "Pepperoni");
                }
            }
            else if (n.Contains("Burger"))
            {
                var top = Sph(g, 0f, .44f, 0f, .3f, H(0xe8b45a), "BunTop"); top.transform.localScale = new Vector3(.6f, .36f, .6f);
                Cyl(g, 0f, .3f, 0f, .31f, .08f, H(0x6b4a2a), "Patty");
                var bot = Sph(g, 0f, .18f, 0f, .3f, H(0xe8d08a), "BunBase"); bot.transform.localScale = new Vector3(.6f, .24f, .6f);
            }
            else if (n.Contains("Sushi"))
            {
                Box(g, 0f, .28f, 0f, .7f, .08f, .4f, H(0x8a6a20), "Tray");
                for (int i = -1; i <= 1; i++)
                {
                    Box(g, i * .2f, .4f, 0f, .14f, .12f, .2f, H(0xf5f0dc), "Rice");
                    Box(g, i * .2f, .44f, 0f, .16f, .05f, .22f, H(0x2a4a2a), "Nori");
                }
            }
            else if (n.Contains("Apple")) { Sph(g, 0f, .45f, 0f, .24f, H(0xcc2222), "Fruit"); Cyl(g, 0f, .68f, 0f, .02f, .14f, H(0x6b4a2a), "Stem"); }
            else if (n.Contains("Banana"))
            {
                Rot(Box(g, -.12f, .35f, 0f, .24f, .12f, .12f, H(0xffe94d), "Half"), 0f, 0f, 23f);
                Rot(Box(g, .1f, .42f, 0f, .24f, .12f, .12f, H(0xffe94d), "Half"), 0f, 0f, -23f);
            }
            else if (n.Contains("Watermelon"))
            {
                var w = Sph(g, 0f, .42f, 0f, .3f, H(0x2a8a3a), "Rind"); w.transform.localScale = new Vector3(.6f, .48f, .6f);
                Sph(g, 0f, .5f, 0f, .2f, H(0xcc4a5a), "Flesh");
            }
            else if (n.Contains("Cheese")) Cyl(g, 0f, .3f, 0f, .3f, .24f, H(0xffd94d), "Wheel");
            else if (n.Contains("Honey")) { CylG(g, 0f, .35f, 0f, .16f, .4f, H(0xd88a1a), H(0xaa5500) * 0.6f, "Jar"); Cyl(g, 0f, .58f, 0f, .12f, .08f, H(0x8a5a20), "Lid"); }
            else if (n.Contains("Energy Drink")) { CylG(g, 0f, .4f, 0f, .12f, .45f, H(0x22ccaa), H(0x00aa88) * 0.7f, "Can"); Cyl(g, 0f, .65f, 0f, .11f, .04f, H(0x8a8f9a), "Top"); }
            else if (n.Contains("Magic Potion")) { SphG(g, 0f, .35f, 0f, .2f, H(0x9a4ae8), H(0x5a1a9a) * 1.4f, "Flask"); Cyl(g, 0f, .58f, 0f, .07f, .18f, H(0xb8c0cc), "Neck"); }
            else Sph(g, 0f, .45f, 0f, .26f, def.Color, "Food");
            anim = "sparkle";
        }
        else // Special
        {
            if (n.Contains("Spring Punch")) { Box(g, 0f, .64f, 0f, .34f, .3f, .3f, H(0xcc2222), "Glove"); Box(g, 0f, .64f, .19f, .36f, .12f, .1f, H(0xcc2222), "Thumb"); Tor(g, 0f, .4f, 0f, .11f, H(0xb8c0cc), "Coil1"); Tor(g, 0f, .29f, 0f, .11f, H(0xb8c0cc), "Coil2"); Tor(g, 0f, .18f, 0f, .11f, H(0xb8c0cc), "Coil3"); Box(g, 0f, .08f, 0f, .3f, .12f, .3f, H(0x555560), "Base"); }
            else if (n.Contains("Fire Wand")) { Cyl(g, 0f, .5f, 0f, .035f, .9f, H(0x6b4a2a), "Rod"); SphG(g, 0f, 1.05f, 0f, .12f, H(0xff6a2a), H(0xcc3300) * 1.8f, "Orb"); }
            else if (n.Contains("Ice Wand")) { Cyl(g, 0f, .5f, 0f, .035f, .9f, H(0x6b4a2a), "Rod"); ConeG(g, 0f, 1.08f, 0f, .09f, .2f, H(0x9fe8ff), H(0x33aaff) * 1.8f, "Crystal"); }
            else if (n.Contains("Lightning Rod")) { Cyl(g, 0f, .5f, 0f, .035f, .95f, H(0x555560), "Rod"); SphG(g, 0f, 1.06f, 0f, .1f, H(0xffee55), H(0xccaa00) * 1.8f, "Orb"); }
            else if (n.Contains("Wind Fan"))
            {
                Cyl(g, 0f, .3f, 0f, .035f, .5f, H(0x8a6a20), "Handle");
                Rot(Box(g, 0f, .62f, 0f, .5f, .06f, .06f, H(0xd8b45a), "Blade"), 0f, 0f, 28f);
                Rot(Box(g, 0f, .62f, 0f, .5f, .06f, .06f, H(0xd8b45a), "Blade"), 0f, 0f, -28f);
            }
            else if (n.Contains("Poison Dagger"))
            {
                BoxG(g, 0f, .55f, 0f, .07f, .6f, .03f, H(0x4ae86a), H(0x1a8a3a) * 1.2f, "Blade");
                Cyl(g, 0f, .18f, 0f, .04f, .05f, H(0x8a6a20), "Guard");
                Cyl(g, 0f, .06f, 0f, .03f, .2f, H(0x6b4a2a), "Grip");
            }
            else if (n.Contains("Vampire Fang")) Rot(Cone(g, 0f, .55f, 0f, .1f, .5f, H(0xf5f0dc), "Fang"), 180f, 0f, 0f);
            else if (n.Contains("Golden Hammer")) { Cyl(g, 0f, .5f, 0f, .045f, .9f, H(0x8a6a20), "Shaft"); BoxG(g, 0f, .95f, 0f, .55f, .26f, .26f, H(0xffd94d), H(0xaa7700) * 1.1f, "Head"); }
            else if (n.Contains("Giant Glove"))
            {
                Box(g, 0f, .5f, 0f, .4f, .4f, .34f, H(0xcc5a3a), "Fist");
                Box(g, 0f, .5f, .2f, .42f, .16f, .36f, H(0xcc5a3a), "Thumb");
                Cyl(g, 0f, .18f, 0f, .14f, .2f, H(0x8a4a2a), "Cuff");
            }
            else if (n.Contains("Shock Glove"))
            {
                Box(g, 0f, .5f, 0f, .36f, .36f, .3f, H(0x4a5a6a), "Glove");
                SphG(g, 0f, .5f, .2f, .06f, H(0xffee55), H(0xccaa00) * 2f, "Spark");
                SphG(g, .12f, .55f, 0f, .05f, H(0xffee55), H(0xccaa00) * 2f, "Spark");
            }
            else if (n.Contains("Magnet Coil"))
            {
                Cyl(g, 0f, .5f, 0f, .05f, .8f, H(0x555560), "Core");
                Tor(g, 0f, .35f, 0f, .16f, H(0xcc2222), "Coil");
                Tor(g, 0f, .5f, 0f, .16f, H(0xf5f0dc), "Coil");
                Tor(g, 0f, .65f, 0f, .16f, H(0xcc2222), "Coil");
            }
            else if (n.Contains("Gravity Orb"))
            {
                SphG(g, 0f, .55f, 0f, .24f, H(0x6a2a9a), H(0x3a105a) * 1.6f, "Orb");
                Rot(TorG(g, 0f, .55f, 0f, .36f, H(0xb08ad8), H(0x6a2a9a) * 1f, "Ring"), 65f, 0f, 0f);
            }
            else if (n.Contains("Mirror Shield"))
            {
                Rot(CylG(g, 0f, .55f, 0f, .3f, .05f, H(0xd8dee8), H(0x88aacc) * 0.8f, "Disc"), 90f, 0f, 0f);
                Rot(Tor(g, 0f, .55f, 0f, .3f, H(0x8a6a20), "Rim"), 90f, 0f, 0f);
                Box(g, 0f, .55f, -.1f, .08f, .12f, .08f, H(0x8a6a20), "Grip");
            }
            else Caps(g, 0f, .5f, 0f, .18f, .8f, def.Color, "Special");
            anim = "glow";
        }

        g.transform.localScale = Vector3.one * (0.3f + def.Scale);
        var animator = g.AddComponent<ItemIdleAnimator>();
        animator.AnimType = anim;
        animator.GlowMaterials = curGlowMats.ToArray();
        animator.GlowBase = curGlowCols.ToArray();
        return g;
    }

    static void BombBall(GameObject g, float r, Color c)
    {
        Sph(g, 0f, .5f, 0f, r, c, "Body");
        Cyl(g, .1f, .5f + r * .8f, 0f, .025f, .18f, H(0x6b4a2a), "Fuse");
        SphG(g, .1f, .5f + r * .8f + .1f, 0f, .045f, H(0xffd23a), H(0xff5500) * 2f, "Spark");
    }

    public static void StripCollider(GameObject go)
    {
        var cols = go.GetComponentsInChildren<Collider>();
        for (int i = 0; i < cols.Length; i++) Object.Destroy(cols[i]);
    }
}

// Idle animation for item models: explosives pulse (with a live fuse spark),
// glow-family items breathe their emissive, everything else is left to the
// pickup spin/bob. Wobble (cactus) sways gently.
public class ItemIdleAnimator : MonoBehaviour
{
    public string AnimType = "none";
    public Material[] GlowMaterials;
    public Color[] GlowBase;

    private Vector3 baseScale;
    private float phase;

    void Start()
    {
        baseScale = transform.localScale;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (AnimType == "pulse")
        {
            float s = 1f + Mathf.Sin(Time.time * 6f + phase) * 0.07f;
            transform.localScale = baseScale * s;
        }
        else if (AnimType == "glow" && GlowMaterials != null)
        {
            float e = 0.85f + Mathf.Sin(Time.time * 4f + phase) * 0.45f;
            for (int i = 0; i < GlowMaterials.Length; i++)
                if (GlowMaterials[i] != null)
                    GlowMaterials[i].SetColor("_EmissionColor", GlowBase[i] * e);
        }
        else if (AnimType == "wobble")
        {
            transform.localRotation = Quaternion.Euler(0f, transform.localRotation.eulerAngles.y, Mathf.Sin(Time.time * 2f + phase) * 4f);
        }
    }
}
