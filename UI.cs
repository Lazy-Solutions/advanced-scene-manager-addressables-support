#if UNITY_EDITOR

#pragma warning disable IDE0062 // Make local function 'static'
#pragma warning disable IDE1006 // Naming Styles

using AdvancedSceneManager.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

using Scene = AdvancedSceneManager.Models.Scene;

using static AdvancedSceneManager.Support._Addressables.AddressablesSupport;
using AdvancedSceneManager.Utility;

using AdvancedSceneManager.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;

namespace AdvancedSceneManager.Support._Addressables
{

    internal static class UI
    {

        internal static void OnLoad()
        {
            SceneManagerWindow.OnGUIEvent -= OnGUI;
            SceneManagerWindow.OnGUIEvent += OnGUI;
            ScenesTab.AddExtraButton(GetCollectionAddressablesButton);
            ScenesTab.AddExtraButton(GetSceneAddressablesButton);

        }

        static Vector2 mousePos;
        static void OnGUI() =>
            mousePos = Event.current.mousePosition;

        static bool IsEnabled(string path)
        {
            var group = settings ? settings.groups?.FirstOrDefault(g => g.entries.Any(e => e.AssetPath == path)) : null;
            var entry = group ? group.entries?.FirstOrDefault(e => e.AssetPath == path) : null;
            return entry != null;
        }

        static bool IsEnabled(string[] paths)
        {
            var entries = settings.groups.SelectMany(g => g.entries?.Where(e => paths.Contains(e.AssetPath)));
            return paths.All(path => entries.Any(e => e.AssetPath == path));
        }

        static AddressableAssetGroup GetGroup()
        {
            var g = settings.FindGroup(Profile.current.name);
            return g ? g : settings.CreateGroup(Profile.current.name, setAsDefaultGroup: false, readOnly: false, postEvent: false, schemasToCopy: null);
        }

        static VisualElement GetCollectionAddressablesButton(SceneCollection collection)
        {

            if (!collection || collection.scenes == null)
                return null;

            var paths = collection.scenes.Where(s => s).Select(s => s.path).ToArray();
            var button = Button(collection, "Addressable", 100, IsEnabled(paths));

            button.RegisterValueChangedCallback(value =>
            {

                if (value.newValue)
                {

                    var pathsToAdd = paths.Where(p => !IsEnabled(p));
                    var group = GetGroup();

                    foreach (var path in pathsToAdd)
                        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group, postEvent: false);
                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryCreated, null, postEvent: true, settingsModified: true);

                }
                else
                {
                    foreach (var group in settings.groups)
                        foreach (var entry in group.entries.ToArray())
                            if (paths.Contains(entry.AssetPath))
                                group.RemoveAssetEntry(entry, postEvent: false);

                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, null, postEvent: true, settingsModified: true);

                }

                RefreshButtons();

            });

            return button;

        }

        static VisualElement GetSceneAddressablesButton(Scene scene)
        {

            if (!scene)
                return null;

            var button = Button(scene, "Addr.", 56, IsEnabled(scene.path));

            button.RegisterValueChangedCallback(value =>
            {

                if (value.newValue)
                {

                    var group = GetGroup();
                    var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(scene.path), group, postEvent: false);
                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryCreated, entry, postEvent: true, settingsModified: true);

                }
                else
                {

                    var group = settings.groups.FirstOrDefault(g => g.entries.Any(e => e.AssetPath == scene.path));
                    var entry = group?.entries?.FirstOrDefault(e => e?.AssetPath == scene.path);
                    group.RemoveAssetEntry(entry, postEvent: false);

                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, entry, postEvent: true, settingsModified: true);

                }

                RefreshButtons();

            });

            return button;

        }

        static readonly Color hoverBackground = new Color(0, 0, 0, 0.3f);
        static readonly Color checkedColor = new Color32(85, 246, 98, 255);
        static readonly Color uncheckedColor = Color.white;

        static readonly Dictionary<ISceneObject, ToolbarToggle> buttons = new Dictionary<ISceneObject, ToolbarToggle>();
        static ToolbarToggle Button(ISceneObject obj, string text, float width, bool value)
        {

            var button = new ToolbarToggle();
            button.style.alignSelf = Align.Center;
            button.style.marginLeft = 2;
            button.style.SetBorderWidth(0);
            button.style.width = width;
            button.text = text;

            button.AddToClassList("StandardButton");
            button.AddToClassList("no-checkedBackground");
            button.style.backgroundColor = Color.clear;
            button.SetValueWithoutNotify(value);

            RefreshButton(button, value);
            button.RegisterValueChangedCallback(e => RefreshButton(button, e.newValue));

            button.RegisterCallback<MouseEnterEvent>(e => { button.style.backgroundColor = hoverBackground; });
            button.RegisterCallback<MouseLeaveEvent>(e => { button.style.backgroundColor = Color.clear; });

            button.RegisterCallback<GeometryChangedEvent>(e =>
            {

                var pos = mousePos;
                if (button.worldBound.Contains(pos))
                    button.style.backgroundColor = new Color(0, 0, 0, 0.3f);

            });

            buttons.Set(obj, button);

            return button;

        }

        static void RefreshButtons()
        {
            foreach (var button in buttons)
            {
                if (button.Key is SceneCollection collection)
                    RefreshButton(collection);
                else if (button.Key is Scene scene)
                    RefreshButton(scene);
            }
        }

        static void RefreshButton(SceneCollection collection) =>
            RefreshButton(buttons.GetValue(collection), IsEnabled(collection.scenes.Select(s => s.path).ToArray()));

        static void RefreshButton(Scene scene) =>
            RefreshButton(buttons.GetValue(scene), scene && IsEnabled(scene.path));

        static void RefreshButton(ToolbarToggle button, bool value)
        {

            button.style.opacity = value ? 1 : 0.4f;

            button.Q<Label>().style.color = value ? checkedColor : uncheckedColor;
            button.SetValueWithoutNotify(value);

        }

    }

}
#endif
