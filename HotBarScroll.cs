using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ScrollHotbar
{
    [BepInPlugin("com.kurophantom.scrollhotbar", "HotbarScroll", "1.2.4")]
    public class Main : BaseUnityPlugin
    {
        private readonly Harmony HarmonyInstance = new Harmony("com.kurophantom.scrollhotbar");
        private ManualLogSource logger;

        private ConfigEntry<KeyCode> keybindPreview;
        private ConfigEntry<bool> invertScroll;

        private int currentIndex = 0;
        private float savedZoom;

        private float scrollTimer = 0f;
        private float scrollDelay = 0.1f;
        private bool pendingEquip = false;

        private float lastScrollValue = 0f;
        private bool scrollJustEnded = false;

        public void Awake()
        {
            HarmonyInstance.PatchAll();
            logger = Logger;
            logger.LogInfo("HotbarScroll mod loaded!");

            keybindPreview = Config.Bind(
                "Hotbar Scroll Settings",
                "Preview Key",
                KeyCode.LeftControl,
                "Key used to activate hotbar preview scrolling."
            );

            invertScroll = Config.Bind(
                "Hotbar Scroll Settings",
                "Invert Scroll Direction",
                false,
                "If true, scrolling up selects lower hotbar slots and vice versa."
            );
        }

        public void Update()
        {
            if (Player.m_localPlayer == null || GameCamera.instance == null) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            bool isPreviewing = Input.GetKey(keybindPreview.Value);
            int direction = scroll > 0f ? 1 : scroll < 0f ? -1 : 0;
            if (invertScroll.Value) direction *= -1;

            GameCamera cam = GameCamera.instance;

            // Save zoom when entering preview
            if (isPreviewing)
            {
                savedZoom = GetCameraZoom(cam);
            }

            // Detect scroll end
            scrollJustEnded = (lastScrollValue != 0f && Mathf.Approximately(scroll, 0f));
            lastScrollValue = scroll;

            if (!isPreviewing && scrollJustEnded)
            {
                float currentZoom = GetCameraZoom(cam);
                if (Mathf.Abs(currentZoom - savedZoom) > 0.0005f)
                    SetCameraZoom(cam, savedZoom);
            }

            if (!isPreviewing && direction != 0)
            {
                if (direction > 0)
                    currentIndex = (currentIndex + 1) % 9;
                else if (direction < 0)
                    currentIndex = (currentIndex + 7) % 8;

                scrollTimer = scrollDelay;
                pendingEquip = true;

                logger.LogInfo($"Queued slot: {currentIndex + 1}");
            }

            if (pendingEquip)
            {
                scrollTimer -= Time.deltaTime;
                if (scrollTimer <= 0f)
                {
                    Player.m_localPlayer.UseHotbarItem(currentIndex);
                    logger.LogInfo($"Equipped slot: {currentIndex + 1}");
                    pendingEquip = false;
                }
            }
        }

        private float GetCameraZoom(GameCamera cam)
        {
            return (float)typeof(GameCamera)
                .GetField("m_distance", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(cam);
        }

        private void SetCameraZoom(GameCamera cam, float value)
        {
            typeof(GameCamera)
                .GetField("m_distance", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(cam, value);
        }
    }
}