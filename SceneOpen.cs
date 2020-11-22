#pragma warning disable IDE0062 // Make local function 'static'
#pragma warning disable IDE1006 // Naming Styles

using AdvancedSceneManager.Core;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.AsyncOperations;
using AdvancedSceneManager.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace AdvancedSceneManager.Support._Addressables
{

    internal static class SceneOpen
    {

        public static void Refresh(string[] addressableScenes)
        {

            SceneOpenAction.ClearOverrides();
            foreach (var scene in addressableScenes)
                SceneOpenAction.Override(scene, Open);

        }

        public static Dictionary<string, AsyncOperationHandle<SceneInstance>> scenes = new Dictionary<string, AsyncOperationHandle<SceneInstance>>();

        static IEnumerator Open(SceneManagerBase _sceneManager, SceneOpenAction action)
        {
            if (action.preload)
                yield return Preload(_sceneManager, action);
            else
                yield return Normal(_sceneManager, action);
        }

        static IEnumerator Normal(SceneManagerBase _, SceneOpenAction action)
        {

            var async = Addressables.LoadSceneAsync(AddressMappings.Current.Get(action.scene.path), loadMode: LoadSceneMode.Additive);
            
            while (!(async.IsDone))
            {
                yield return null;
                action.progress = async.PercentComplete;
            }

            if (async.OperationException != null)
                throw async.OperationException;

            action.openScene = new OpenSceneInfo(action.scene, async.Result.Scene);
            scenes.Set(action.scene.path, async);

        }

        static IEnumerator Preload(SceneManagerBase _sceneManager, SceneOpenAction action)
        {

            if (action.preloadedScene == null)
            {

                //Open scene

                var address = AddressMappings.Current.Get(action.scene.path);
                if (string.IsNullOrWhiteSpace(address))
                {
                    Debug.LogError("Could not find address for scene: " + action.scene.path);
                    yield break;
                }

                var async = Addressables.LoadSceneAsync(address, loadMode: LoadSceneMode.Additive, activateOnLoad: false);
                 
                while (!async.IsDone)
                {
                    yield return null;
                    action.progress = async.PercentComplete;
                }

                if (async.OperationException != null)
                    throw async.OperationException;

                action.openScene = new OpenSceneInfo(action.scene, async.Result.Scene, async) { isPreloadedOverride = true };
                scenes.Set(action.scene.path, async);

            }
            else
            {

                //Activate scene

                if (!_sceneManager.IsOpen(action.preloadedScene))
                {
                    action.Done();
                    yield break;
                }

                if (!typeof(AsyncOperationHandle<SceneInstance>).IsAssignableFrom(action.preloadedScene.asyncOperation?.GetType()))
                {
                    action.Done();
                    yield break;
                }

                var async = ((AsyncOperationHandle<SceneInstance>)action.preloadedScene.asyncOperation).Result.ActivateAsync();
                
                async.allowSceneActivation = true;
                while (!async.isDone)
                {
                    yield return null;
                    action.progress = async.progress;
                }

                action.openScene = action.preloadedScene;
                action.openScene.isPreloadedOverride = null;
                action.preloadedScene.asyncOperation = null;

            }

        }

    }

}
