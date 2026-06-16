using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StarForce.Editor.MyAsset
{
    public sealed class MyAssetCollectionWindow : EditorWindow
    {
        private MyAssetCollection m_Collection;
        private Vector2 m_BundleScrollPosition;
        private Vector2 m_AssetScrollPosition;
        private int m_SelectedBundleIndex = -1;

        [MenuItem("Tools/MyAsset/Collection")]
        private static void OpenWindow()
        {
            var window = GetWindow<MyAssetCollectionWindow>("MyAsset Collection");
            window.minSize = new Vector2(640f, 360f);
            window.LoadCollection();
        }

        private void OnEnable()
        {
            if (m_Collection == null)
            {
                LoadCollection();
            }
        }

        private void OnGUI()
        {
            if (m_Collection == null)
            {
                EditorGUILayout.HelpBox("Failed to load collection config.", MessageType.Error);
                if (GUILayout.Button("Reload"))
                {
                    LoadCollection();
                }

                return;
            }

            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawBundleList();
            DrawAssetList();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
            {
                LoadCollection();
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                SaveCollection();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Add Bundle", EditorStyles.toolbarButton))
            {
                AddBundle();
            }

            using (new EditorGUI.DisabledScope(m_SelectedBundleIndex < 0))
            {
                if (GUILayout.Button("Delete Bundle", EditorStyles.toolbarButton))
                {
                    DeleteSelectedBundle();
                }

                if (GUILayout.Button("Add Selected Assets", EditorStyles.toolbarButton))
                {
                    AddSelectedAssetsToBundle();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBundleList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(220f));
            EditorGUILayout.LabelField("Bundles", EditorStyles.boldLabel);

            m_BundleScrollPosition = EditorGUILayout.BeginScrollView(m_BundleScrollPosition, GUILayout.ExpandHeight(true));
            for (int i = 0; i < m_Collection.bundles.Count; i++)
            {
                MyBundleConfig bundle = m_Collection.bundles[i];
                bool selected = i == m_SelectedBundleIndex;
                if (GUILayout.Toggle(selected, GetBundleLabel(bundle), "Button"))
                {
                    m_SelectedBundleIndex = i;
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawAssetList()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);

            if (m_SelectedBundleIndex < 0 || m_SelectedBundleIndex >= m_Collection.bundles.Count)
            {
                EditorGUILayout.HelpBox("Select a bundle to view or edit its assets.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            MyBundleConfig selectedBundle = m_Collection.bundles[m_SelectedBundleIndex];
            selectedBundle.bundleName = EditorGUILayout.TextField("Bundle Name", selectedBundle.bundleName);

            m_AssetScrollPosition = EditorGUILayout.BeginScrollView(m_AssetScrollPosition, GUILayout.ExpandHeight(true));
            for (int i = 0; i < selectedBundle.assetGuids.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                string guid = selectedBundle.assetGuids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                EditorGUILayout.LabelField(assetPath, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Remove", GUILayout.Width(72f)))
                {
                    selectedBundle.assetGuids.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static string GetBundleLabel(MyBundleConfig bundle)
        {
            string bundleName = string.IsNullOrEmpty(bundle.bundleName) ? "<unnamed>" : bundle.bundleName;
            return string.Format("{0} ({1})", bundleName, bundle.assetGuids.Count);
        }

        private void LoadCollection()
        {
            string path = MyAssetPaths.CollectionRelativePath;
            if (!File.Exists(path))
            {
                m_Collection = new MyAssetCollection();
                SaveCollection();
                return;
            }

            string json = File.ReadAllText(path);
            m_Collection = JsonUtility.FromJson<MyAssetCollection>(json) ?? new MyAssetCollection();
            m_SelectedBundleIndex = m_Collection.bundles.Count > 0 ? 0 : -1;
        }

        private void SaveCollection()
        {
            string directory = Path.GetDirectoryName(MyAssetPaths.CollectionRelativePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(m_Collection, true);
            File.WriteAllText(MyAssetPaths.CollectionRelativePath, json);
            AssetDatabase.Refresh();
            Debug.LogFormat("MyAsset collection saved to '{0}'.", MyAssetPaths.CollectionRelativePath);
        }

        private void AddBundle()
        {
            m_Collection.bundles.Add(new MyBundleConfig
            {
                bundleName = string.Format("bundle_{0}", m_Collection.bundles.Count + 1)
            });
            m_SelectedBundleIndex = m_Collection.bundles.Count - 1;
        }

        private void DeleteSelectedBundle()
        {
            if (m_SelectedBundleIndex < 0 || m_SelectedBundleIndex >= m_Collection.bundles.Count)
            {
                return;
            }

            m_Collection.bundles.RemoveAt(m_SelectedBundleIndex);
            if (m_Collection.bundles.Count == 0)
            {
                m_SelectedBundleIndex = -1;
            }
            else if (m_SelectedBundleIndex >= m_Collection.bundles.Count)
            {
                m_SelectedBundleIndex = m_Collection.bundles.Count - 1;
            }
        }

        private void AddSelectedAssetsToBundle()
        {
            if (m_SelectedBundleIndex < 0 || m_SelectedBundleIndex >= m_Collection.bundles.Count)
            {
                return;
            }

            MyBundleConfig bundle = m_Collection.bundles[m_SelectedBundleIndex];
            Object[] selectedObjects = Selection.objects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.LogWarning("No assets selected in Project window.");
                return;
            }

            HashSet<string> existingGuids = new HashSet<string>(bundle.assetGuids);
            int addedCount = 0;

            foreach (Object selectedObject in selectedObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid) || existingGuids.Contains(guid))
                {
                    continue;
                }

                bundle.assetGuids.Add(guid);
                existingGuids.Add(guid);
                addedCount++;
            }

            Debug.LogFormat("Added {0} asset(s) to bundle '{1}'.", addedCount, bundle.bundleName);
        }
    }
}
