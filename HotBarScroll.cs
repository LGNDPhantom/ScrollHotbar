using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ScrollHotbar
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        const string pluginGUID = "com.kurophantom.scrollhotbar";
        const string pluginName = "HotbarScroll";
        const string pluginVersion = "1.0.0";

        private readonly Harmony HarmonyInstance = new Harmony(pluginGUID);
        public static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        private int currentIndex = 0;
        private float savedZoom;

        public void Awake()
        {
            HarmonyInstance.PatchAll();
            logger.LogInfo("HotbarScroll mod loaded!");
        }

        public void Update()
        {
            if (Player.m_localPlayer == null || GameCamera.instance == null) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            GameCamera cam = GameCamera.instance;

            if (Input.GetKey(KeyCode.LeftControl))
            {
                // Allow the player to zoom naturally, and store the current zoom
                savedZoom = GetCameraZoom(cam);
            }
            else if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) < 0.01f)
            {
                // Only restore zoom when NOT scrolling — this avoids jitter or interfering with input
                float currentZoom = GetCameraZoom(cam);
                if (!Mathf.Approximately(currentZoom, savedZoom))
                {
                    SetCameraZoom(cam, savedZoom);
                }
            }


            // Only handle hotbar scrolling if Ctrl is NOT held
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                if (scroll > 0)
                    currentIndex = (currentIndex + 1) % 9;
                else if (scroll < 0)
                    currentIndex = (currentIndex + 7) % 8;

                if (scroll != 0)
                {
                    Player.m_localPlayer.UseHotbarItem(currentIndex);
                    logger.LogInfo($"Switched to hotbar slot: {currentIndex + 1}");
                }
            }
        }

        private float GetCameraZoom(GameCamera cam)
        {
            return (float)typeof(GameCamera)
                .GetField("m_distance", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(cam);
        }

        private void SetCameraZoom(GameCamera cam, float value)
        {
            typeof(GameCamera)
                .GetField("m_distance", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(cam, value);
        }
    }
}
