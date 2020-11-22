#pragma warning disable IDE0062 // Make local function 'static'
#pragma warning disable IDE1006 // Naming Styles

using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models.AsyncOperations;
using System.Collections;
using UnityEngine.AddressableAssets;

namespace AdvancedSceneManager.Support._Addressables
{

    internal static class SceneClose
    {

        public static void Refresh(string[] addressableScenes)
        {

            SceneCloseAction.ClearOverrides();
            foreach (var scene in addressableScenes)
                SceneCloseAction.Override(scene, Close);

        }

        static IEnumerator Close(SceneManagerBase _sceneManager, SceneCloseAction action)
        {

            var path = action.openScene.scene.path;
            if (SceneOpen.scenes.TryGetValue(path, out var handle))
            {

                var _async = Addressables.UnloadSceneAsync(handle);

                while (!(_async.IsDone))
                {
                    action.progress = _async.PercentComplete;
                    yield return null;
                }

                SceneOpen.scenes.Remove(path);

            }

        }

    }

}
