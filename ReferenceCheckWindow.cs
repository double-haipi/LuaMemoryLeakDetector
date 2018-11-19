using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

namespace com.tencent.pandora.tools
{
    public class ReferenceCheckWindow : EditorWindow
    {
        enum InfoType
        {
            Info,
            Warn,
            Error,
        }

        #region 参数
        private int _defaultPadding = 2;
        private int _buttonHeight = 30;
        private int _headerHeight = 120;
        private int _titleHeight = 20;
        private int _infoLineHeight = 30;

        private bool _needInitScrollViewArea = false;

        private Vector2 _briefInfoScrollPosition = Vector2.zero;
        private Vector2 _detailInfoScrollPosition = Vector2.zero;
        private float _briefInfoScrollViewHeight;
        private float _detailInfoScrollViewHeight;

        private int _selectedInfoIndex = -1;

        private GUIStyle _detailInfoStyle;
        private int _detailInfoFontSize = 12;
        private string _detailInfo = "";
        private GUIStyle _briefInfoStyle;
        private int _briefInfoFontSize = 12;
        GUIStyle instructionStyle;

        private Rect _segmentingLineRect;
        private bool _segmentingLineMove = false;
        private float _segmentingLinePositonToWindowHeightRatio;
        private string _segmentingLineRatioKey = "SEGMENTING_LINE_RATIO";

        private Rect _cursorRect;
        private int _cursorRectWidth = 10;
        private float _lastWindowHeight;
        private bool _windowResize = false;

        private Texture2D _segmentingLineTexture;
        private Texture2D _oddLineBackgroundTexture;
        private Texture2D _evenLineBackgroundTexture;
        private Texture2D _selectedLineBackoundTexture;

        private Dictionary<int, string> _referenceDescriptionMap = new Dictionary<int, string>();
        private List<string> _referenceInfo = new List<string>();

        private bool _isDisplayingFirstSnap = true;
        private string _isDisplayingFirstSnapKey = "IS_DISPLAYING_FIRST_SNAP";

        private string _content = "说明：\r\n1.打开待检测活动面板，对面板做全面交互操作。\r\n 2.点击'打开活动面板后快照',对引用关系做第一次快照。\r\n 3.关闭活动面板，但保持工程处于运行中，点击'关闭活动面板后快照'，显示区显示的即为未释放对象。";
        private Rect _instructionPosition;

        private string _title;

        private Dictionary<IntPtr, string> _lastLuaObjectInfo;
        private Dictionary<IntPtr, string> _currentLuaObjectInfo;
        private Dictionary<IntPtr, string> _leakedLuaObjectInfo = new Dictionary<IntPtr, string>();
        #endregion

        [MenuItem("PandoraTools/ReferenceChecker")]
        public static void ShowWindow()
        {
            GetWindow<ReferenceCheckWindow>(false, "ReferenceChecker", true);
        }

        private void OnEnable()
        {
            //每次获取到焦点时初始化一次
            _needInitScrollViewArea = true;

            _isDisplayingFirstSnap = EditorPrefs.GetBool(_isDisplayingFirstSnapKey, true);

            _detailInfoStyle = new GUIStyle();
            _detailInfoStyle.alignment = TextAnchor.UpperLeft;
            _detailInfoStyle.padding = new RectOffset(2, 2, 2, 2);
            _detailInfoStyle.wordWrap = true;
            _detailInfoStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            _detailInfoStyle.fontSize = _detailInfoFontSize;

            _instructionPosition = new Rect(0, 0, 320, _headerHeight);
        }

        private void OnLostFocus()
        {
            //记录segmentingLineRatio
            EditorPrefs.SetFloat(_segmentingLineRatioKey, _segmentingLinePositonToWindowHeightRatio);
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            InitInfoScrollViewArea();
            DrawHeader();
            GUILayout.Space(_headerHeight - _buttonHeight * 2);
            DrawTitle();
            DrawSegmentingLine();
            OnMouseEvent();
            OnScreenHeightResize();
            RefreshInfoScrollViewArea();
            DrawBriefInfoArea();
            DrawDetailInfoArea();
            OnInfoClick();
            EditorGUILayout.EndVertical();
        }

        private void InitInfoScrollViewArea()
        {
            if (_needInitScrollViewArea == false)
            {
                return;
            }
            else
            {
                _segmentingLinePositonToWindowHeightRatio = EditorPrefs.GetFloat(_segmentingLineRatioKey, 0.5f);
                SetInfoScrollViewArea();
                _needInitScrollViewArea = false;
            }
        }

        private void SetInfoScrollViewArea()
        {
            float screenHeight = Screen.height;
            float ySegmentingLinePosition = screenHeight * _segmentingLinePositonToWindowHeightRatio;

            _briefInfoScrollViewHeight = ySegmentingLinePosition - _headerHeight - _titleHeight - _defaultPadding;
            _detailInfoScrollViewHeight = screenHeight - ySegmentingLinePosition - _defaultPadding;

            if (_segmentingLineRect == null)
            {
                _segmentingLineRect = new Rect(0, ySegmentingLinePosition, Screen.width, 1f);
            }
            else
            {
                _segmentingLineRect.Set(0, ySegmentingLinePosition, Screen.width, 1f);
            }

            if (_cursorRect == null)
            {
                _cursorRect = new Rect(0, _segmentingLineRect.y - _cursorRectWidth / 2, Screen.width, _cursorRectWidth);
            }
            else
            {
                _cursorRect.Set(0, _segmentingLineRect.y - _cursorRectWidth / 2, Screen.width, _cursorRectWidth);
            }
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            ShowIntroduction();
            GUILayout.Space(330f);
            DrawButtons();
            GUILayout.EndHorizontal();
        }

        private void DrawButtons()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("打开活动面板后快照", GUILayout.Height(_buttonHeight)))
            {
                _isDisplayingFirstSnap = true;
                EditorPrefs.SetBool(_isDisplayingFirstSnapKey, true);
                ReferenceChecker.Instance.GetReferenceDataWhenPanelOpened();
                _referenceDescriptionMap = ReferenceChecker.Instance.ReferenceDescription;
                FillReferenceInfo();
                _title = "以下是lua引用的c#对象：";
            }

            if (GUILayout.Button("关闭活动面板后快照", GUILayout.Height(_buttonHeight)))
            {
                _isDisplayingFirstSnap = false;
                EditorPrefs.SetBool(_isDisplayingFirstSnapKey, false);
                ReferenceChecker.Instance.GetReferenceDataWhenPanelClosed();
                _referenceDescriptionMap = ReferenceChecker.Instance.ReferenceDescription;
                FillReferenceInfo();
                _title = "以下是lua引用的c#对象泄漏项：";
            }

            if (GUILayout.Button("清空显示", GUILayout.Width(80f), GUILayout.Height(_buttonHeight)))
            {
                _referenceDescriptionMap.Clear();
                _referenceInfo.Clear();
                _title = "";
                _detailInfo = "";
                Repaint();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("打开活动面板前lua 对象快照", GUILayout.Height(_buttonHeight)))
            {
                _lastLuaObjectInfo = LuaObjectSnapShot.Snapshot();
            }

            if (GUILayout.Button("关闭活动面板后lua 对象快照", GUILayout.Height(_buttonHeight)))
            {
                _isDisplayingFirstSnap = false;
                EditorPrefs.SetBool(_isDisplayingFirstSnapKey, false);
                _currentLuaObjectInfo = LuaObjectSnapShot.Snapshot();
                if (_lastLuaObjectInfo != null)
                {
                    _leakedLuaObjectInfo.Clear();
                    foreach (var item in _currentLuaObjectInfo)
                    {
                        if (_lastLuaObjectInfo.ContainsKey(item.Key) == false)
                        {
                            _leakedLuaObjectInfo.Add(item.Key, item.Value);
                        }
                    }
                    //处理数据
                    FillReferenceInfoByLuaObjectSnapshot();
                    _title = "以下是 lua 对象泄漏项：";
                    _lastLuaObjectInfo.Clear();
                    _lastLuaObjectInfo = _currentLuaObjectInfo;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void FillReferenceInfoByLuaObjectSnapshot()
        {
            _referenceInfo.Clear();
            foreach (var item in _leakedLuaObjectInfo)
            {
                string[] objInfo = GetSplitInfo(item.Value);
                StringBuilder sb = new StringBuilder(512);
                int depth = 0;
                GetReferenceChain(item.Key.ToString(), ref sb, ref depth);
                string referenceChain = "";
                if (sb.Length > 2)
                {
                    referenceChain = sb.ToString(2, sb.Length - 2);
                }
                sb = null;
                _referenceInfo.Add(string.Format("ObjInfo：\t{0}({1})\nReference Chain:\t{2}", objInfo[2], objInfo[0], referenceChain));
            }
            Repaint();
        }

        //返回内容： typeName,parentPtr,desc,""
        private string[] GetSplitInfo(string origin)
        {
            return origin.Split(new char[] { '\n', ':' });
        }

        private void GetReferenceChain(string parentPointer, ref StringBuilder sb, ref int depth)
        {
            //控制迭代层数，不需太多链，太多会导致内存占用过大把unity卡死
            if (depth > 6)
            {
                return;
            }
            IntPtr pointer = (IntPtr)(Convert.ToInt32(parentPointer));
            if (pointer == IntPtr.Zero)
            {
                return;
            }
            if (_currentLuaObjectInfo.ContainsKey(pointer) == false)
            {
                return;
            }
            string[] infos = GetSplitInfo(_currentLuaObjectInfo[pointer]);
            string insertContent = string.Format("{0}({1})", infos[2], infos[0]);
            sb.Insert(0, insertContent);
            sb.Insert(0, "->");
            depth++;
            GetReferenceChain(infos[1], ref sb, ref depth);
        }

        private void ShowIntroduction()
        {
            instructionStyle = new GUIStyle("flow node 0");
            instructionStyle.alignment = TextAnchor.UpperLeft;
            instructionStyle.wordWrap = true;
            instructionStyle.padding = new RectOffset(10, 10, 30, 0);
            instructionStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            instructionStyle.fontSize = _detailInfoFontSize;
            GUI.Box(_instructionPosition, _content, instructionStyle);
        }

        private void DrawTitle()
        {
            Color originalColor = _detailInfoStyle.normal.textColor;
            _detailInfoStyle.normal.textColor = new Color(0f, 1f, 0f, 1f);
            GUILayout.Label(_title, _detailInfoStyle, GUILayout.Height(_titleHeight));
            _detailInfoStyle.normal.textColor = originalColor;
        }

        private void FillReferenceInfo()
        {
            _referenceInfo.Clear();
            foreach (var item in _referenceDescriptionMap)
            {
                _referenceInfo.Add(item.Value);
            }
            Repaint();
        }

        private void DrawSegmentingLine()
        {
            if (_segmentingLineTexture == null)
            {
                _segmentingLineTexture = CreateTexture(Vector2.one, Color.black);
            }

            GUI.DrawTexture(_segmentingLineRect, _segmentingLineTexture);
            EditorGUIUtility.AddCursorRect(_cursorRect, MouseCursor.ResizeVertical);
        }

        private void OnMouseEvent()
        {
            if (Event.current.type == EventType.MouseDrag && _cursorRect.Contains(Event.current.mousePosition))
            {
                _segmentingLineMove = true;
            }

            if (Event.current.rawType == EventType.MouseUp)
            {
                _segmentingLineMove = false;
            }
        }


        private void OnScreenHeightResize()
        {
            if (_lastWindowHeight != Screen.height)
            {
                _lastWindowHeight = Screen.height;
                _windowResize = true;
            }
            else
            {
                _windowResize = false;
            }
        }

        private void RefreshInfoScrollViewArea()
        {
            //只有分割线移动时计算radio,窗口变动不计算
            if (_segmentingLineMove == true)
            {
                _segmentingLinePositonToWindowHeightRatio = Event.current.mousePosition.y / Screen.height;

            }
            if (_segmentingLineMove == true || _windowResize == true)
            {
                SetInfoScrollViewArea();
                Repaint();
            }
        }

        private void DrawBriefInfoArea()
        {
            _briefInfoScrollPosition = EditorGUILayout.BeginScrollView(_briefInfoScrollPosition, GUILayout.Height(_briefInfoScrollViewHeight));

            InfoType type = (_isDisplayingFirstSnap == true ? InfoType.Info : InfoType.Warn);

            int length = _referenceInfo.Count;
            for (int i = 0; i < length; i++)
            {
                DrawInfo(i, _referenceInfo[i].Substring(0, _referenceInfo[i].IndexOf("\n")), type);
            }
            GUILayout.Space(length * _infoLineHeight);
            EditorGUILayout.EndScrollView();
        }

        private void DrawInfo(int index, string message, InfoType messageType, float topPadding = 30f)
        {
            string guiStyleName = "";
            switch (messageType)
            {
                case InfoType.Info:
                    guiStyleName = "CN EntryInfo";
                    break;
                case InfoType.Warn:
                    guiStyleName = "CN EntryWarn";
                    break;
                case InfoType.Error:
                    guiStyleName = "CN EntryError";
                    break;
                default:
                    break;
            }

            Rect rect = new Rect(0, index * _infoLineHeight, Screen.width - 18f, _infoLineHeight);
            DrawInfoBackground(index, rect);
            _briefInfoStyle = GetBriefInfoStyle(guiStyleName);
            GUI.Label(rect, message, _briefInfoStyle);
            //添加cursorRect
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Text);
        }

        private void DrawInfoBackground(int index, Rect rect, float topPadding = 30f)
        {
            if (_oddLineBackgroundTexture == null)
            {
                _oddLineBackgroundTexture = CreateTexture(Vector2.one, new Color(0.15f, 0.15f, 0.15f, 0.5f));
            }
            if (_evenLineBackgroundTexture == null)
            {
                _evenLineBackgroundTexture = CreateTexture(Vector2.one, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            }
            if (_selectedLineBackoundTexture == null)
            {
                _selectedLineBackoundTexture = CreateTexture(Vector2.one, new Color(62.0f / 255.0f, 95.0f / 255.0f, 149.0f / 255.0f, 1.0f));
            }

            if (index == _selectedInfoIndex)
            {
                GUI.DrawTexture(rect, _selectedLineBackoundTexture);
            }
            else if (index % 2 == 0)
            {
                GUI.DrawTexture(rect, _evenLineBackgroundTexture);
            }
            else
            {
                GUI.DrawTexture(rect, _oddLineBackgroundTexture);
            }
        }

        private GUIStyle GetBriefInfoStyle(string styleName)
        {
            GUIStyle style = new GUIStyle(styleName);
            style.alignment = TextAnchor.UpperLeft;
            style.clipping = TextClipping.Clip;
            style.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            style.fontSize = _briefInfoFontSize;
            return style;
        }

        private void DrawDetailInfoArea()
        {
            _detailInfoScrollPosition = EditorGUILayout.BeginScrollView(_detailInfoScrollPosition, GUILayout.Height(_detailInfoScrollViewHeight));

            _detailInfo = "";
            if (-1 < _selectedInfoIndex && _selectedInfoIndex < _referenceInfo.Count)
            {
                _detailInfo = _referenceInfo[_selectedInfoIndex];
            }

            EditorGUILayout.TextArea(_detailInfo, _detailInfoStyle);

            //加两个空行
            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("");
            EditorGUILayout.EndScrollView();
        }

        private void OnInfoClick()
        {
            int index = GetSeletedInfoIndex();
            if (_selectedInfoIndex != index && index != -1)
            {
                _selectedInfoIndex = index;
                //如果选中了详情区的TextArea，需要移除焦点，否则更新不了TextArea
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = 0;
                //如果需要立马重绘界面，就调用Repaint
                Repaint();
            }
        }

        private int GetSeletedInfoIndex()
        {
            if (Event.current.type != EventType.MouseUp)
            {
                return -1;
            }

            //鼠标弹起代表一次点击
            Vector2 currentMousePosition = Event.current.mousePosition + _briefInfoScrollPosition;
            float height = _segmentingLinePositonToWindowHeightRatio * Screen.height + _briefInfoScrollPosition.y;

            if (currentMousePosition.y < _headerHeight + _titleHeight + _defaultPadding + _briefInfoScrollPosition.y || currentMousePosition.y > height)
            {
                return -1;
            }
            int index = -1;
            Rect currentRect;
            for (int y = _headerHeight + _titleHeight + _defaultPadding; y < height; y += _infoLineHeight)
            {
                index++;
                currentRect = new Rect(0, y, Screen.width - 18, _infoLineHeight);
                if (currentRect.Contains(currentMousePosition) == true)
                {
                    return index;
                }
            }

            return -1;
        }

        private Texture2D CreateTexture(Vector2 size, Color color)
        {
            Texture2D tex = new Texture2D((int)size.x, (int)size.y);
            tex.hideFlags = HideFlags.DontSave;
            for (int i = 0; i < size.x; i++)
            {
                for (int j = 0; j < size.y; j++)
                {
                    tex.SetPixel(i, j, color);
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return tex;
        }
    }
}