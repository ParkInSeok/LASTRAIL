namespace LastRail.Editor
{
    // Preserve the current initial/auth/download UI before session-authorized catalog admission.
    public static class OfflineBootstrapResources
    {
        public static readonly string[] ResourceKeys =
        {
            "EntryPopup/control-backplate",
            "EntryPopup/control-frame-v03",
            "EntryPopup/popup-backplate",
            "EntryPopup/popup-frame-v03",



            "CombatSecondFinal/title_emblem-v01",
            "StartupVideo/landscape-first-frame-v01",
            "StartupVideo/TitleMetal",
            "StartupVideo/OpeningMetalTitle",









            // TMP creates default controls before callers assign the bootstrap font.
            "TMP Settings",
            "Fonts & Materials/LiberationSans SDF",
            "Fonts & Materials/LiberationSans SDF - Fallback",
            "Sprite Assets/EmojiOne",
            "Style Sheets/Default Style Sheet",
            "LineBreaking Leading Characters",
            "LineBreaking Following Characters"
        };

        public static readonly string[] DirectAssetPaths =
        {
            "Assets/_LASTRAIL/Scenes/ResourceBootstrap.unity",
            "Assets/_LASTRAIL/Prefabs/Screens/StartupScreen.prefab",
            "Assets/_LASTRAIL/UI/StartupResources/BootstrapFont.asset",
            "Assets/_LASTRAIL/UI/Fonts/Pretendard/Pretendard-Regular.otf",
            "Assets/_LASTRAIL/UI/StartupResources/download_surface_accent.png",
            "Assets/_LASTRAIL/UI/Icons/ic_warning.png",
            "Assets/_LASTRAIL/UI/Icons/ic_retry.png",
            "Assets/_LASTRAIL/UI/Icons/ic_lock.png",
            "Assets/_LASTRAIL/UI/Icons/ic_info.png",
            "Assets/_LASTRAIL/UI/Common/Skins/ui_slider_track_9slice.png",
            "Assets/_LASTRAIL/UI/Common/Skins/ui_slider_fill_9slice.png",
            "Assets/_LASTRAIL/UI/Common/Skins/ui_button_primary_9slice.png",
            "Assets/_LASTRAIL/UI/Common/Skins/ui_button_secondary_9slice.png",
            "Assets/StreamingAssets/Startup/lastrail-opening-landscape-4s-v01.mp4",
            "Assets/TextMesh Pro/Fonts/LiberationSans.ttf",
            "Assets/TextMesh Pro/Sprites/EmojiOne.png",
            "Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader",
            "Assets/TextMesh Pro/Shaders/TMP_Sprite.shader",
            "Assets/TextMesh Pro/Shaders/TMPro_Properties.cginc",
            "Assets/TextMesh Pro/Shaders/TMPro.cginc"
        };
    }
}
