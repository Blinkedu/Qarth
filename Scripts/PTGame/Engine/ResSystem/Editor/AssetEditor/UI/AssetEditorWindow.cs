﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace PTGame.Framework.Editor
{
    public class AssetEditorWindow : EditorWindow
    {
        [MenuItem("Assets/SCEngine/Res/导出配置")]
        public static void SavaConfig()
        {
            ABConfigInfo config = new ABConfigInfo();
            config.AddFolder("Assets/Res", null);
            config.ExportEditorConfig("abConfig.xml");
            Log.i("## Success Export Config");
        }

        [MenuItem("Assets/SCEngine/Res/读取配置")]
        public static void LoadConfig()
        {
            ABConfigInfo config = new ABConfigInfo();
            config.LoadFromEditorConfig("abConfig.xml");
            config.Dump();
        }

        [MenuItem("Assets/SCEngine/Res/AB编辑器")]
        public static void OpenABWindow()
        {
            AssetEditorWindow window = EditorWindow.GetWindow<AssetEditorWindow>();
            window.Show();
        }

        private Vector2 scrollPos;
        private GUIStyle m_Style = "Label";
        private Texture m_FolderIcon;
        private Texture m_TrangleDownIcon;
        private Texture m_TrangleRightIcon;

        private ABEditorMgr m_Mgr;

        private void Awake()
        {
            m_Style.normal.textColor = Color.white;

            m_Mgr = new ABEditorMgr();

            List<string> outResult = new List<string>();
            FilePath.GetFolderInFolder(Application.dataPath, "PTFramework", outResult);

            if (outResult.Count > 0)
            {
                string frameworkPath = EditorUtils.ABSPath2AssetsPath(outResult[0]);
                m_FolderIcon = AssetDatabase.LoadAssetAtPath<Texture>(frameworkPath + "/Editor/Res/folder_icon.png");
                m_TrangleDownIcon = AssetDatabase.LoadAssetAtPath<Texture>(frameworkPath + "/Editor/Res/triangle_down.png");
                m_TrangleRightIcon = AssetDatabase.LoadAssetAtPath<Texture>(frameworkPath + "/Editor/Res/triangle_right.png");
            }
        }

        private void OnDestroy()
        {
            m_Mgr.ExportConfig();
        }

        private void OnGUI()
        {
            using (var verView = new EditorGUILayout.VerticalScope())
            {
                ShowAddRootFolderUI();

                ShowControlUI();

                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos, false, true))
                {
                    scrollPos = scrollView.scrollPosition;
                    DrawFolder(m_Mgr.rootFolder);
                }
            }
        }

        private string m_AddRootFolderName = "";

        private void ShowAddRootFolderUI()
        {
            EditorGUILayout.BeginHorizontal();
            m_AddRootFolderName = GUILayout.TextField(m_AddRootFolderName);
            if (GUILayout.Button("Add"))
            {
                m_Mgr.AddFolder("Assets/" + m_AddRootFolderName);
                m_AddRootFolderName = "";
            }
            if (GUILayout.Button("Remove"))
            {
                m_Mgr.RemoveFolder("Assets/" + m_AddRootFolderName);
                m_AddRootFolderName = "";
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ShowControlUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export"))
            {
                m_Mgr.ExportConfig();
            }
            if (GUILayout.Button("Refresh"))
            {
                m_Mgr.RefreshState();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFolder(ABFolderInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(info.folderFullPath))
            {
                EditorGUI.indentLevel = info.level;
                DrawGUIData(info);
            }

            if (info.isOpen || info.level <= 0)
            {
                if (info.childFolderInfo != null)
                {
                    for (int i = 0; i < info.childFolderInfo.Length; ++i)
                    {
                        if (info.childFolderInfo[i] == null)
                        {
                            continue;
                        }
                        DrawFolder(info.childFolderInfo[i]);
                    }
                }
            }
        }

        private void DrawGUIData(ABFolderInfo info)
        {
            Rect rt = GUILayoutUtility.GetRect(20, 20, m_Style);
            rt.x += (16 * EditorGUI.indentLevel);

            using (var h = new EditorGUILayout.HorizontalScope())
            {
                if (info.childFolderInfo != null)
                {
                    rt.width = 20;
                    //EditorGUI.DrawRect(rt, Color.white);
                    if (info.isOpen)
                    {
                        if (GUI.Button(rt, m_TrangleDownIcon, m_Style))
                        {
                            info.isOpen = !info.isOpen;
                        }
                    }
                    else
                    {
                        if (GUI.Button(rt, m_TrangleRightIcon, m_Style))
                        {
                            info.isOpen = !info.isOpen;
                        }
                    }

                }

                rt.x += 20;

                GUI.Label(rt, m_FolderIcon, m_Style);
                rt.x += 20;
                rt.width = 120;
                m_Style.normal.textColor = Color.white;
                GUI.Label(rt, info.folderName);

                ABConfigUnit configUnit = m_Mgr.GetConfigUnit(info.folderFullPath);
                bool isFolderFlag = true;

                if (configUnit != null)
                {
                    isFolderFlag = configUnit.isFolderFlag;
                }

                if (configUnit != null)
                {
                    rt.x += 120;
                    if (configUnit.isFolderFlag)
                    {
                        configUnit.isFolderFlag = GUI.Toggle(rt, configUnit.isFolderFlag, "文件夹模式");
                    }
                    else
                    {
                        configUnit.isFolderFlag = GUI.Toggle(rt, configUnit.isFolderFlag, "文件模式");
                    }
                }


                ABStateUnit stateUnit = m_Mgr.GetStateUnit(info.folderFullPath);

                if (stateUnit != null)
                {
                    rt.x += 120;
                    rt.width = 160;

                    string stateMsg = null;
                    if (ABState2Msg(stateUnit.state, out stateMsg))
                    {
                        if (isFolderFlag != stateUnit.state.isFolderFlag && !stateUnit.state.isNoneFlag)
                        {
                            m_Style.normal.textColor = Color.red;
                        }
                        else
                        {
                            m_Style.normal.textColor = Color.gray;
                        }
                        
                        stateMsg = string.Format("当前状态:{0}", stateMsg);
                        GUI.Label(rt, stateMsg, m_Style);

                        rt.x += 180;
                        if (GUI.Button(rt, "重置"))
                        {
                            m_Mgr.FixedFolder(stateUnit.folderAssetPath);
                        }
                    }
                    else
                    {
                        m_Style.normal.textColor = Color.red;
                        stateMsg = string.Format("当前状态:{0}", stateMsg);
                        GUI.Label(rt, stateMsg, m_Style);

                        rt.x += 180;
                        if (GUI.Button(rt, "修复"))
                        {
                            m_Mgr.FixedFolder(stateUnit.folderAssetPath);
                        }
                    }
                }
            }
        }

        private bool ABState2Msg(ABState state, out string msg)
        {
            bool result = true;
            
            msg = "";
            if (state.isMixedFlag)
            {
                msg += "-混合模式";
                result = false;
            }
            else if (state.isFileFlag)
            {
                msg += "-文件模式";
            }
            else if (state.isFolderFlag)
            {
                msg += "-文件夹模式";
            }

            if (state.isLost)
            {
                msg += "-丢失";
                result = false;
            }

            if (string.IsNullOrEmpty(msg))
            {
                msg += "-正常";
            }

            return result;
        }

        void OnInspectorUpdate()
        {
            //Debug.Log("窗口面板的更新");
            //这里开启窗口的重绘，不然窗口信息不会刷新
            this.Repaint();
        }
    }
}
