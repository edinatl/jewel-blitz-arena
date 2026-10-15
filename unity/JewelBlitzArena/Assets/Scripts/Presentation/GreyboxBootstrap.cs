using System.Reflection;
using UnityEngine;

namespace JewelBlitzArena.Presentation
{
    /// <summary>
    /// Spawns a playable greybox when opening an empty project (no scene asset required).
    /// </summary>
    public static class GreyboxBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindFirstObjectByType<MatchDriver>() != null)
            {
                return;
            }

            var root = new GameObject("GreyboxMatch");
            var driver = root.AddComponent<MatchDriver>();
            var view = root.AddComponent<GreyboxArenaView>();
            var hud = root.AddComponent<MatchHud>();

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }

            var camRig = cam.gameObject.GetComponent<MatchCamera>();
            if (camRig == null)
            {
                camRig = cam.gameObject.AddComponent<MatchCamera>();
            }

            AssignDriver(view, driver);
            AssignDriver(hud, driver);
            AssignDriver(camRig, driver);
        }

        private static void AssignDriver(Object behaviour, MatchDriver driver)
        {
            FieldInfo? field = behaviour.GetType().GetField(
                "driver",
                BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(behaviour, driver);
        }
    }
}
