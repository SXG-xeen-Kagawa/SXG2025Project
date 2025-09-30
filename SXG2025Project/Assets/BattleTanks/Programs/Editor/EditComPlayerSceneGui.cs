﻿using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;

namespace SXG2025
{

    [InitializeOnLoad]
    public static class EditComPlayerSceneGui
    {
        // 砲塔プレハブのパス 
        const string TurretPrefabPath = "Assets/BattleTanks/Prefabs/TurretPart.prefab";

        // 回転パーツプレハブのパス 
        const string RotatorPrefabPath = "Assets/BattleTanks/Prefabs/RotJointPart.prefab";

        // ComPlayerBase 側の配列フィールド名
        const string TurretArrayFieldName = "m_turrets";

        // ComPlayerBase 側の配列フィールド名 
        const string RotatorArrayFieldName = "m_rotJoints";


        const string DataFormatTankPath = "Assets/BattleTanks/Data/Resources/DataTank.asset";


        static int m_lastHash = 0;
        static int m_lastCost = -1;
        static double m_lastRecalcTime = 0;
        static int m_countOfTurrets = 0;
        static int m_countOfRotators = 0;
        static int m_countOfArmors = 0;
        static float m_tankMass = 0;
        static DataFormatTank m_editorDataTankCache = null;
        static bool m_showCollision = false;
        static GameObject m_collisionPrefab;

        static GUIStyle m_valueStyle;
        static GUIStyle m_warningValueStyle;


        static EditComPlayerSceneGui()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.hierarchyChanged += OnEditorChangeEvent;
            Undo.undoRedoPerformed += OnEditorChangeEvent;
            EditorApplication.projectChanged += OnEditorChangeEvent;

        }

        static void OnEditorChangeEvent()
        {
            // 直接再計算を呼ぶ
            RecalculateCostIfNeeded(Selection.activeGameObject, force: true);
        }

        static void RemakeStyles()
        {
            // スタイル 
            m_valueStyle = new GUIStyle(EditorStyles.label);

            m_warningValueStyle = new GUIStyle(EditorStyles.label);
            m_warningValueStyle.normal.textColor = Color.red;

        }


        static void OnSceneGUI(SceneView sv)
        {
            // Prefab編集画面でなければ何もしない 
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return;

            // 選択オブジェクトが ComPlayerBase を持っているか確認 
            GameObject obj = stage.prefabContentsRoot;
            //GameObject obj = Selection.activeGameObject;
            if (obj == null) return;
            var comPlayer = obj.GetComponent<ComPlayerBase>();
            if (comPlayer == null) return;

            // スタイル生成 
            RemakeStyles();

            // 定期的に必要があればコスト計算 
            if (0.2f < EditorApplication.timeSinceStartup - m_lastRecalcTime)
            {
                RecalculateCostIfNeeded(obj);
                m_lastRecalcTime = EditorApplication.timeSinceStartup;
            }

            // 見出し 
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(50, 10, 400, 128), GUI.skin.box);
            GUILayout.Label("＜戦車Prefab編集モード＞", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // コスト 
            bool isCostOver = false;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Cost:", GUILayout.Width(40));
            if (m_lastCost <= GameConstants.DEFAULT_PLAYER_ENERGY)
            {
                GUILayout.Label(m_lastCost.ToString(), m_valueStyle, GUILayout.Width(40));
            } else
            {
                GUILayout.Label(m_lastCost.ToString(), m_warningValueStyle, GUILayout.Width(40));
                isCostOver = true;
            }
            GUILayout.Label("出撃可能回数:");
            if (0 < m_lastCost)
            {
                GUILayout.Label((GameConstants.DEFAULT_PLAYER_ENERGY/m_lastCost).ToString(), GUILayout.Width(40));
            }
            if (isCostOver)
            {
                GUILayout.Label("  COST OVER!!!", m_warningValueStyle);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // 追加情報 
            GUILayout.Label(string.Format("砲塔数:{0}基 / 回転部位:{1}基 / 装備数:{2}基 / 戦車質量:{3}Kg",
                m_countOfTurrets, m_countOfRotators, m_countOfArmors, m_tankMass));

            // 砲塔追加ボタン 
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("砲塔追加", GUILayout.Width(100)))
            {
                CompactArray(comPlayer);
                TryAddTurretToTankInPrefabStage(comPlayer, stage);
            }
            if (GUILayout.Button("回転部位追加", GUILayout.Width(100)))
            {
                CompactArray(comPlayer);
                TryAddRotatorToTankInPrefabStage(comPlayer, stage);
            }
            GUILayout.EndHorizontal();

            // チェック 
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("要素チェック", GUILayout.Width(100)))
            {
                bool isUpdate = false;
                if (comPlayer.transform.localPosition != Vector3.zero || comPlayer.transform.localRotation != Quaternion.identity)
                {
                    comPlayer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    EditorUtility.SetDirty(comPlayer);
                    isUpdate = true;
                }
                if (CompactArray(comPlayer))
                {
                    isUpdate = true;
                }
                if (isUpdate)
                {
                    // Prefab Stage のシーンをDirtyにする 
                    EditorSceneManager.MarkSceneDirty(stage.scene);
                }
            }
            if (GUILayout.Button("コリジョン表示", GUILayout.Width(100)))
            {
                m_showCollision = !m_showCollision;
                SceneView.RepaintAll();
            }
            GUILayout.EndHorizontal();

            // 終了 
            GUILayout.EndArea();
            Handles.EndGUI();


            // 砲塔、回転部位にテキスト表示 
            Handles.BeginGUI();
            try
            {
                TryDrawFromSerializedProperty(comPlayer, sv);
            }
            finally
            {
                Handles.EndGUI();
            }
        }

        static void TryAddTurretToTankInPrefabStage(ComPlayerBase comPlayer, PrefabStage stage)
        {
            Vector3 DefTurretLocalPosition = new Vector3(0, 1.42f, 0);

            // 砲塔プレハブロード 
            GameObject turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurretPrefabPath);
            if (turretPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", string.Format("砲塔プレハブが見つかりません：{0}", TurretPrefabPath), "OK");
                return;
            }

            // Prefab Stage が有効か再確認 
            if (stage == null)
            {
                EditorUtility.DisplayDialog("Error", "Prefab 編集画面が開かれていません。", "OK");
                return;
            }

            // Prefab Stage のシーン上でインスタンス化 (Undo 登録)
            GameObject newTurret = null;
            try
            {
                newTurret = PrefabUtility.InstantiatePrefab(turretPrefab, stage.scene) as GameObject;
                if (newTurret == null)
                {
                    EditorUtility.DisplayDialog("Error", "砲塔のインスタンス化に失敗しました。", "OK");
                    return;
                }
            }
            catch(Exception ex)
            {
                Debug.LogError("砲塔のインスタンス化に失敗：" + ex);
                return;
            }

            // Undo登録
            Undo.RegisterCreatedObjectUndo(newTurret, "Create Turret");

            // 親をタンクにする
            newTurret.transform.SetParent(comPlayer.transform, false);
            newTurret.transform.SetLocalPositionAndRotation(DefTurretLocalPosition, Quaternion.identity);

            // ComPlayerBaseの配列に追加
            SerializedObject so = new SerializedObject(comPlayer);
            SerializedProperty arrayProperty = so.FindProperty(TurretArrayFieldName);
            if (arrayProperty == null)
            {
                EditorUtility.DisplayDialog("Error", string.Format("シリアライズされたフィールド '{0}' が見つかりません。", TurretArrayFieldName), "OK");
                return;
            }

            // 配列への追加前に Undo登録 
            Undo.RecordObject(comPlayer, "Add Turret Reference");

            int newIndex = arrayProperty.arraySize;
            arrayProperty.InsertArrayElementAtIndex(newIndex);
            var element = arrayProperty.GetArrayElementAtIndex(newIndex);
            element.objectReferenceValue = newTurret;
            so.ApplyModifiedProperties();

            // Prefab Stage のシーンをDirtyにする 
            EditorSceneManager.MarkSceneDirty(stage.scene);

            // Inspector更新とログ 
            EditorUtility.SetDirty(comPlayer);
            Debug.Log("砲塔を追加しました。Prefab編集画面のSaveを押して保存してください。");
        }

        /// <summary>
        /// 回転パーツを追加する 
        /// </summary>
        /// <param name="comPlayer"></param>
        /// <param name="stage"></param>
        static void TryAddRotatorToTankInPrefabStage(ComPlayerBase comPlayer, PrefabStage stage)
        {
            // 回転部プレハブロード 
            GameObject rotatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RotatorPrefabPath);
            if (rotatorPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", string.Format("回転部プレハブが見つかりません：{0}", RotatorPrefabPath), "OK");
                return;
            }

            // Prefab Stage が有効か再確認 
            if (stage == null)
            {
                EditorUtility.DisplayDialog("Error", "Prefab 編集画面が開かれていません。", "OK");
                return;
            }

            // Prefab Stage のシーン上でインスタンス化 (Undo 登録)
            GameObject newRotator = null;
            try
            {
                newRotator = PrefabUtility.InstantiatePrefab(rotatorPrefab, stage.scene) as GameObject;
                if (newRotator == null)
                {
                    EditorUtility.DisplayDialog("Error", "回転部位のインスタンス化に失敗しました。", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("回転部位のインスタンス化に失敗：" + ex);
                return;
            }

            // Undo登録
            Undo.RegisterCreatedObjectUndo(newRotator, "Create Rotator");

            // 親をタンクにする
            newRotator.transform.SetParent(comPlayer.transform, false);
            newRotator.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            // ComPlayerBaseの配列に追加
            SerializedObject so = new SerializedObject(comPlayer);
            SerializedProperty arrayProperty = so.FindProperty(RotatorArrayFieldName);
            if (arrayProperty == null)
            {
                EditorUtility.DisplayDialog("Error", string.Format("シリアライズされたフィールド '{0}' が見つかりません。", RotatorArrayFieldName), "OK");
                return;
            }

            // 配列への追加前に Undo登録 
            Undo.RecordObject(comPlayer, "Add Rotator Reference");

            int newIndex = arrayProperty.arraySize;
            arrayProperty.InsertArrayElementAtIndex(newIndex);
            var element = arrayProperty.GetArrayElementAtIndex(newIndex);
            element.objectReferenceValue = newRotator;
            so.ApplyModifiedProperties();

            // Prefab Stage のシーンをDirtyにする 
            EditorSceneManager.MarkSceneDirty(stage.scene);

            // Inspector更新とログ 
            EditorUtility.SetDirty(comPlayer);
            Debug.Log("回転部位を追加しました。Prefab編集画面のSaveを押して保存してください。");
        }


        /// <summary>
        /// 必要なら配列を作り直す 
        /// </summary>
        static bool CompactArray(ComPlayerBase comPlayer)
        {
            if (comPlayer == null) return false;

            bool result = false;
            result |= CompactArrayCore(comPlayer, TurretArrayFieldName, "Compact Turrets Array");
            result |= CompactArrayCore(comPlayer, RotatorArrayFieldName, "Compact Rotators Array");

            return result;
        }

        static bool CompactArrayCore(ComPlayerBase comPlayer, string arrayFieldName, string comments)
        {
            if (comPlayer == null) return false;

            SerializedObject so = new SerializedObject(comPlayer);
            if (so == null) return false;
            SerializedProperty arrayProperty = so.FindProperty(arrayFieldName);
            if (arrayProperty == null) return false;

            // Collect non-null references
            List<UnityEngine.Object> keep = new();
            int originalSize = arrayProperty.arraySize;
            for (int i = 0; i < originalSize; ++i)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);
                if (element != null && element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
                {
                    if (!keep.Contains(element.objectReferenceValue))
                    {
                        keep.Add(element.objectReferenceValue);
                    }
                }
            }

            // 数を比較 
            if (originalSize == keep.Count)
            {
                return false;
            }

            // Undo
            Undo.RecordObject(so.targetObject, comments);

            arrayProperty.ClearArray();
            foreach (var obj in keep)
            {
                int newIndex = arrayProperty.arraySize;
                arrayProperty.InsertArrayElementAtIndex(newIndex);
                var element = arrayProperty.GetArrayElementAtIndex(newIndex);
                element.objectReferenceValue = obj;
            }
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(comPlayer);

            return true;

        }



        static DataFormatTank LoadEditorDataTankCache()
        {
            if (m_editorDataTankCache != null) return m_editorDataTankCache;

            var loaded = AssetDatabase.LoadAssetAtPath<DataFormatTank>(DataFormatTankPath);
            if (loaded == null)
            {
                Debug.LogWarning("Asset is not found : " + DataFormatTankPath);
                return null;
            }

            m_editorDataTankCache = loaded;
            return m_editorDataTankCache;
        }

        /// <summary>
        /// 必要ならコストを再計算 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="tankWrapper"></param>
        /// <param name="force"></param>
        static void RecalculateCostIfNeeded(GameObject root, bool force=false)
        {
            if (root == null) return;
            ComPlayerBase comPlayer = root.GetComponent<ComPlayerBase>();
            if (comPlayer == null) return;

            // 簡易ハッシュを比較して更新があった時だけ再計算する 
            int h = ComputeSimpleHash(root);
            if (force || h != m_lastHash)
            {
                // コストデータをロード
                var dataTank = LoadEditorDataTankCache();


                // コスト再計算 
                m_lastHash = h;
                m_lastCost = BaseTank.SystemCalculateTankCost(comPlayer, 
                    out m_countOfTurrets, out m_countOfRotators, out m_countOfArmors, out m_tankMass,
                    dataTank);
                //m_lastCost = ComputeCost(root);
                m_lastRecalcTime = EditorApplication.timeSinceStartup;
                // SceneViewの再描画
                SceneView.RepaintAll();
            }

        }

        /// <summary>
        /// プレハブの簡易ハッシュ 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="tank"></param>
        /// <returns></returns>
        static int ComputeSimpleHash(GameObject root)
        {
            int hash = 17;

            unchecked
            {
                // 子の数 
                hash = hash * 31 + root.GetComponentsInChildren<Transform>(true).Length;
                // MeshFilter / MeshRenderer の参照IDと頂点数 
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var m = mf.sharedMesh;
                    int id = (m != null)? m.GetInstanceID() : 0;
                    int vc = (m != null) ? m.vertexCount : 0;
                    hash = hash * 31 + id;
                    hash = hash * 31 + vc;
                }
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mats = mr.sharedMaterials;
                    hash = hash * 31 + ((mats != null) ? mats.Length : 0);
                    foreach (var mat in mats)
                    {
                        hash = hash * 31 + ((mat != null) ? mat.GetInstanceID() : 0);
                    }
                }
                // 子のスケール変更 
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    int sx = Mathf.RoundToInt(t.localScale.x * 1000.0f);
                    int sy = Mathf.RoundToInt(t.localScale.y * 1000.0f);
                    int sz = Mathf.RoundToInt(t.localScale.z * 1000.0f);
                    hash = hash * 397 ^ sx;
                    hash = hash * 397 ^ sy;
                    hash = hash * 397 ^ sz;
                }
            }
            return hash;
        }



        #region 3D空間に砲塔、回転部位の番号を表示

        static void TryDrawFromSerializedProperty(ComPlayerBase comPlayer, SceneView sv)
        {
            var so = new SerializedObject(comPlayer);

            // 砲塔 
            TryDrawParts(so, TurretArrayFieldName, "  Turret[{0}]", sv);

            // 回転部位 
            TryDrawParts(so, RotatorArrayFieldName, "  Rotator[{0}]", sv);
        }

        static void TryDrawParts(SerializedObject so, string propertyName, string message, SceneView sv)
        {
            var array = so.FindProperty(propertyName);
            if (array == null || !array.isArray) return;

            for (int i=0; i < array.arraySize; ++i)
            {
                var element = array.GetArrayElementAtIndex(i);
                if (element != null)
                {
                    var objRef = element.objectReferenceValue;
                    if (objRef != null)
                    {
                        Transform t = GetTransformFromObjectReference(objRef);
                        if (t != null)
                        {
                            DrawLabelAt(t.position, string.Format(message, i), sv);
                        }
                    }
                }
            }
        }

        static void DrawLabelAt(Vector3 worldPos, string text, SceneView sv)
        {
            Handles.Label(worldPos, text);

            // 小さなドット 
            Handles.color = Color.cyan;
            Handles.DrawSolidDisc(worldPos, sv.camera.transform.forward, 0.02f);
            Handles.color = Color.white;
        }

        static Transform GetTransformFromObjectReference(UnityEngine.Object obj)
        {
            if (obj is GameObject go) return go.transform;
            if (obj is Component comp) return comp.transform;
            return null;
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        static void DrawCollisionGizmo(ComPlayerBase src, GizmoType gizmoType)
        {
            if (!m_showCollision) return;

            if (m_collisionPrefab == null)
            {
                m_collisionPrefab = Resources.Load<GameObject>("collision");
                if (m_collisionPrefab == null) return;
            }

            MeshFilter mf = m_collisionPrefab.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
                Gizmos.DrawMesh(mf.sharedMesh, src.transform.position, src.transform.rotation, src.transform.lossyScale);
            }
        }

        #endregion

    }

}

