#if UNITY_EDITOR

#pragma warning disable IDE0062 // Make local function 'static'
using AdvancedSceneManager.Editor.Utility;

namespace AdvancedSceneManager.Support._Addressables
{

    internal static class BuildSettings
    {

        public static void OnLoad()
        {
            BuildSettingsUtility.AddBuildSettingsCallback(Callback);
            void Callback() =>
                AddressablesSupport.Refresh(false);
        }

        public static void Refresh(string[] addressableScenes, bool notifyBuildSettings = false)
        {

            BuildSettingsUtility.ClearAddressableScenes();
            foreach (var scene in addressableScenes)
                BuildSettingsUtility.OverrideSceneEnabledState(scene, false, BuildSettingsUtility.Reason.IsAddressable);

            if (notifyBuildSettings)
                BuildSettingsUtility.UpdateBuildSettings();

        }

    }

}
#endif
