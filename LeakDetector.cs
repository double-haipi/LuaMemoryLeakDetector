using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace com.tencent.pandora.tools
{
    public class LeakDetector
    {
        private static LeakDetector _instance;
        private Dictionary<object, int> _objectMapWhenPanelOpened;
        private Dictionary<object, int> _objectMapWhenPanelClosed;
        private Dictionary<int, string> _targetObjectsDescriptionMap = new Dictionary<int, string>();
        private List<string> _leakInfoList = new List<string>();

        public static LeakDetector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LeakDetector();
                }
                return _instance;
            }
        }

        public List<string> LeakInfoList
        {
            get
            {
                return _leakInfoList;
            }
        }

        public void RecordWhenPanelOpened()
        {
            _objectMapWhenPanelOpened = GetObjMap();
            FillTargetOjectsDescriptionDict();
        }

        public void CheckLeakWhenPanelClosed()
        {

            if (_objectMapWhenPanelOpened.Count == 0)
            {
                DisplayWarningDialog("请先执行'打开活动面板后-记录',再做此操作");
                return;
            }
            LuaGC();
            UnityEngine.Resources.UnloadUnusedAssets();
            _objectMapWhenPanelClosed = GetObjMap();
            CheckLeak();
        }

        private Dictionary<object, int> GetObjMap()
        {
            IntPtr luaStatePointer = GetLuaStatePointer();
            if (luaStatePointer == IntPtr.Zero)
            {
                return new Dictionary<object, int>();
            }
            ObjectCache objectCache = ObjectCache.get(luaStatePointer);
            Type objectCacheType = Type.GetType("com.tencent.pandora.ObjectCache");
            FieldInfo objMapField = objectCacheType.GetField("objMap", BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<object, int> objMap = objMapField.GetValue(objectCache) as Dictionary<object, int>;
            return objMap;
        }

        private IntPtr GetLuaStatePointer()
        {
            GameObject sluaSvrGameObject = GameObject.Find("LuaStateProxy_0");
            if (sluaSvrGameObject == null)
            {
                DisplayWarningDialog("lua 虚拟机未在运行中，请运行游戏工程后做此操作！");
                return IntPtr.Zero;
            }
            return sluaSvrGameObject.GetComponent<LuaSvrGameObject>().state.L;
        }

        private void FillTargetOjectsDescriptionDict()
        {
            _targetObjectsDescriptionMap.Clear();
            GameObject go = null;
            Component component = null;
            LuaSentry sentry = null;
            string description = "";
            try
            {
                foreach (var item in _objectMapWhenPanelOpened)
                {
                    description = "";
                    if (item.Key == null)
                    {
                        continue;
                    }
                    if (item.Key is GameObject)
                    {
                        go = item.Key as GameObject;
                        description = string.Format("[C# GameObject]: {0}\nPath In Hierarchy: {1}", item.Key, GetTransformPath(go.transform));
                    }
                    else if (item.Key is Component)
                    {
                        component = item.Key as Component;
                        description = string.Format("[C# Component]: {0}\nPath In Hierarchy: {1}", item.Key, GetTransformPath(component.transform));
                    }
                    else if (item.Key is LuaSentry)
                    {
                        sentry = item.Key as LuaSentry;
                        description = string.Format("{0}\n", sentry.ToString());
                    }

                    if (string.IsNullOrEmpty(description) == false)
                    {

                        _targetObjectsDescriptionMap[item.Value] = description;
                    }
                }
            }
            catch (Exception e)
            {
                //当C#层的GameObject/Component 对象被销毁,而lua层未释放对它的引用时,点击'打开活动面板后-记录'按钮会触发此异常.
                //请在正确的时机点击'打开活动面板后-记录'按钮,以记录正确的C# 对象信息
                Debug.LogWarning(e.Message + "\nStackTrace:\n" + e.StackTrace);
            }
        }

        private void LuaGC()
        {
            IntPtr luaStatePointer = GetLuaStatePointer();
            if (luaStatePointer == IntPtr.Zero)
            {
                return;
            }
            LuaDLL.pua_gc((IntPtr)luaStatePointer, LuaGCOptions.LUA_GCCOLLECT, 0);

        }

        //一般情况下，关闭面板后，objMap中还存在GameObject，Component，LuaSentry类型的对象，就是泄漏的对象
        //泄漏对象的描述信息在_targetObjectsDescriptionMap中查询
        private void CheckLeak()
        {
            _leakInfoList.Clear();
            string description = "";
            foreach (var item in _objectMapWhenPanelClosed)
            {
                if (_targetObjectsDescriptionMap.TryGetValue(item.Value, out description))
                {
                    _leakInfoList.Add(description);
                }
            }
        }

        //path 是相对于活动面板的，把UI Root，Canvas 头去掉。
        private string GetTransformPath(Transform trans)
        {
            if (trans == null)
            {
                return "";
            }
            Transform parentTrans = trans;
            StringBuilder sb = new StringBuilder();
            while (parentTrans != null)
            {
                sb.Insert(0, parentTrans.name);
                sb.Insert(0, "/");
                parentTrans = parentTrans.parent;
            }
            string path = sb.ToString(1, sb.Length - 1);

            string rootNodeName = "";
            if (path.Contains("UI Root") == true)
            {
                rootNodeName = "UI Root";
            }
            else if (path.Contains("Canvas") == true)
            {
                rootNodeName = "Canvas";
            }

            int rootNodeNameIndex = path.IndexOf(rootNodeName);
            int subIndex = rootNodeNameIndex + rootNodeName.Length + 1;

            if (rootNodeNameIndex != -1 && subIndex < path.Length)
            {
                return path.Substring(subIndex);
            }
            else
            {
                return path;
            }
        }

        public static void DisplayWarningDialog(string message, string title = "")
        {
            EditorUtility.DisplayDialog(title, message, "我知道了");
        }
    }
}