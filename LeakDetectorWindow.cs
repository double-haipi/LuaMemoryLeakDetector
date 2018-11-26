using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace com.tencent.pandora.tools
{
    public class LeakDetectorWindow : EditorWindow
    {
        #region 参数
        private const int INSTRUCTION_HEIGHT = 80;
        private const int DEFAULT_PADDING = 4;
        private const int BUTTON_HEIGHT = 30;
        private const int TITLE_HEIGHT = 20;
        private const int INFO_LINE_HEIGHT = 30;

        private Vector2 _windowSize = Vector2.zero;
        private bool _windowResize = false;
        private Vector2 _csharpAreaScrollPosition = Vector2.zero;
        private Vector2 _luaAreaScrollPosition = Vector2.zero;
        private Vector2 _detailAreaScrollPosition = Vector2.zero;

        private float _csharpAreaScrollViewHeight;
        private float _luaAreaScrollViewHeight;
        private float _detailAreaScrollViewHeight;

        private int _selectedArea = -1;
        private int _selectedInfoIndex = -1;
        private int _newSelectedArea = -1;
        private int _newSelectedInfoIndex = -1;

        GUIStyle instructionStyle;
        private GUIStyle _briefInfoStyle;
        private const int BRIEF_INFO_FONT_SIZE = 12;
        private GUIStyle _detailInfoStyle;
        private const int DETAIL_INFO_FONT_SIZE = 12;
        private string _detailInfo = "";

        private Rect _segmentLineRect1 = new Rect();
        private Rect _segmentLineRect2 = new Rect();
        float yPostionOfSegmentLine1;
        float yPostionOfSegmentLine2;

        private Texture2D _segmentingLineTexture;
        private Texture2D _oddLineBackgroundTexture;
        private Texture2D _evenLineBackgroundTexture;
        private Texture2D _selectedLineBackoundTexture;

        private List<string> _totalLeakInfoList = new List<string>();
        private List<string> _csharpObjectLeakInfoList = new List<string>();
        private List<string> _luaObjectLeakInfoList = new List<string>();

        private const string INSTRUCTION = "说明：\n1.打开待检测活动面板，对面板做全面交互操作。\n2.点击'打开活动面板后-记录',对内存中对象做第一次快照。\n3.关闭活动面板，但保持工程处于运行中，点击'关闭活动面板后-检查'，显示区显示的即为未释放对象。";
        private const string CSHARP_LEAK_TITLE = "以下是c#泄漏项：";
        private const string LUA_LEAK_TITLE = "以下是lua泄漏项：";
        #endregion

        [MenuItem("PandoraTools/MemoryLeakDetector")]
        public static void ShowWindow()
        {
            GetWindow<LeakDetectorWindow>(false, "LeakDetector", true);
        }

        private void OnEnable()
        {
            InitTextures();
        }
        private void InitTextures()
        {
            _oddLineBackgroundTexture = CreateTexture(Vector2.one, new Color(0.15f, 0.15f, 0.15f, 0.5f));
            _evenLineBackgroundTexture = CreateTexture(Vector2.one, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            _selectedLineBackoundTexture = CreateTexture(Vector2.one, new Color(62.0f / 255.0f, 95.0f / 255.0f, 149.0f / 255.0f, 1.0f));
            _segmentingLineTexture = CreateTexture(Vector2.one, Color.black);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            RefreshWindowSize();
            RefreshScrollViewArea();
            ShowInstruction();
            GUILayout.Space(INSTRUCTION_HEIGHT + DEFAULT_PADDING);
            DrawButtons();
            DrawScharpArea();
            DrawSegmentingLine();
            DrawLuaArea();
            DrawDetailArea();
            OnInfoClick();
            EditorGUILayout.EndVertical();
        }
        private void RefreshWindowSize()
        {
            if (_windowSize.x != Screen.width || _windowSize.y != Screen.height)
            {
                _windowSize.x = Screen.width;
                _windowSize.y = Screen.height;
                _windowResize = true;
            }
            else
            {
                _windowResize = false;
            }
        }

        private void RefreshScrollViewArea()
        {
            if (_windowResize == true)
            {
                SetScrollViewArea();
                Repaint();
            }
        }

        private void SetScrollViewArea()
        {
            float usedSpace = INSTRUCTION_HEIGHT + BUTTON_HEIGHT + DEFAULT_PADDING;
            float remainedSpace = _windowSize.y - usedSpace;
            float oneFifthSpace = remainedSpace / 5.0f;

            _csharpAreaScrollViewHeight = oneFifthSpace * 2.0f - TITLE_HEIGHT;
            _luaAreaScrollViewHeight = _csharpAreaScrollViewHeight;
            _detailAreaScrollViewHeight = oneFifthSpace;

            yPostionOfSegmentLine1 = usedSpace + oneFifthSpace * 2.0f;
            yPostionOfSegmentLine2 = yPostionOfSegmentLine1 + oneFifthSpace * 2.0f;
            _segmentLineRect1.Set(0, yPostionOfSegmentLine1, Screen.width, 1f);
            _segmentLineRect2.Set(0, yPostionOfSegmentLine2, Screen.width, 1f);
        }
        private void ShowInstruction()
        {
            if (instructionStyle == null)
            {
                SetInstructionStyle();
            }
            GUI.Box(new Rect(DEFAULT_PADDING, 0, _windowSize.x - 2 * DEFAULT_PADDING, INSTRUCTION_HEIGHT), INSTRUCTION, instructionStyle);
        }
        private void SetInstructionStyle()
        {
            instructionStyle = new GUIStyle("flow node 0");
            instructionStyle.alignment = TextAnchor.UpperLeft;
            instructionStyle.wordWrap = false;
            instructionStyle.padding = new RectOffset(10, 10, 30, 0);
            instructionStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            instructionStyle.fontSize = DETAIL_INFO_FONT_SIZE;
        }

        private void DrawButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("打开活动面板后-记录", GUILayout.Height(BUTTON_HEIGHT)))
            {
                LeakDetector.Instance.RecordWhenPanelOpened();
            }

            if (GUILayout.Button("关闭活动面板后-检查", GUILayout.Height(BUTTON_HEIGHT)))
            {
                LeakDetector.Instance.CheckLeakWhenPanelClosed();
                _totalLeakInfoList = LeakDetector.Instance.LeakInfoList;
                SeperateLeakInfo();
            }

            if (GUILayout.Button("清空显示", GUILayout.Height(BUTTON_HEIGHT)))
            {
                _totalLeakInfoList.Clear();
                _csharpObjectLeakInfoList.Clear();
                _luaObjectLeakInfoList.Clear();
                _detailInfo = "";
                Repaint();
            }
            GUILayout.EndHorizontal();
        }

        private void SeperateLeakInfo()
        {
            _csharpObjectLeakInfoList.Clear();
            _luaObjectLeakInfoList.Clear();
            foreach (var item in _totalLeakInfoList)
            {
                if (item.Contains("C#"))
                {
                    _csharpObjectLeakInfoList.Add(item);
                }
                else
                {
                    _luaObjectLeakInfoList.Add(item);
                }
            }
            Repaint();
        }

        private void DrawScharpArea()
        {
            DrawBriefInfoArea(CSHARP_LEAK_TITLE, ref _csharpAreaScrollPosition, _csharpAreaScrollViewHeight, _csharpObjectLeakInfoList, 1);
        }

        private void DrawLuaArea()
        {
            DrawBriefInfoArea(LUA_LEAK_TITLE, ref _luaAreaScrollPosition, _luaAreaScrollViewHeight, _luaObjectLeakInfoList, 2);
        }

        private void DrawBriefInfoArea(string title, ref Vector2 scrollPosition, float viewHeight, List<string> infoList, int area)
        {
            DrawTitle(title);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(viewHeight));
            int length = infoList.Count;
            for (int i = 0; i < length; i++)
            {
                DrawInfo(area, i, infoList[i].Substring(0, infoList[i].IndexOf("\n")));
            }
            GUILayout.Space(length * INFO_LINE_HEIGHT);
            EditorGUILayout.EndScrollView();
        }

        private void DrawTitle(string content)
        {
            if (_detailInfoStyle == null)
            {
                SetDetailInfoStyle();
            }
            Color originalColor = _detailInfoStyle.normal.textColor;
            _detailInfoStyle.normal.textColor = new Color(0f, 1f, 0f, 1f);
            GUILayout.Label(content, _detailInfoStyle, GUILayout.Height(TITLE_HEIGHT));
            _detailInfoStyle.normal.textColor = originalColor;
        }
        private void SetDetailInfoStyle()
        {
            _detailInfoStyle = new GUIStyle();
            _detailInfoStyle.alignment = TextAnchor.UpperLeft;
            _detailInfoStyle.padding = new RectOffset(2, 2, 2, 2);
            _detailInfoStyle.wordWrap = true;
            _detailInfoStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            _detailInfoStyle.fontSize = DETAIL_INFO_FONT_SIZE;
        }

        private void DrawInfo(int area, int index, string message)
        {
            Rect rect = new Rect(0, index * INFO_LINE_HEIGHT, Screen.width - 18f, INFO_LINE_HEIGHT);
            DrawInfoBackground(area, index, rect);
            //使用内置的GUIStyle，初始化必须放在OnGUI内调用，不能放在OnEnable中，否则会报错。
            if (_briefInfoStyle == null || _briefInfoStyle.name != "CN EntryWarn")
            {
                SetBriefInfoStyle();
            }
            GUI.Label(rect, message, _briefInfoStyle);
            //添加cursorRect
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Text);
        }
        private void SetBriefInfoStyle()
        {
            _briefInfoStyle = new GUIStyle("CN EntryWarn");
            _briefInfoStyle.alignment = TextAnchor.UpperLeft;
            _briefInfoStyle.clipping = TextClipping.Clip;
            _briefInfoStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            _briefInfoStyle.fontSize = BRIEF_INFO_FONT_SIZE;
        }

        private void DrawInfoBackground(int area, int index, Rect rect, float topPadding = 30f)
        {
            if (area == _selectedArea && index == _selectedInfoIndex)
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

        private void DrawSegmentingLine()
        {
            GUI.DrawTexture(_segmentLineRect1, _segmentingLineTexture);
            GUI.DrawTexture(_segmentLineRect2, _segmentingLineTexture);
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
        private void DrawDetailArea()
        {
            if (_selectedArea == -1)
            {
                return;
            }
            _detailAreaScrollPosition = EditorGUILayout.BeginScrollView(_detailAreaScrollPosition, GUILayout.Height(_detailAreaScrollViewHeight));
            _detailInfo = "";

            if (_selectedArea == 1)
            {
                if (-1 < _selectedInfoIndex && _selectedInfoIndex < _csharpObjectLeakInfoList.Count)
                {
                    _detailInfo = _csharpObjectLeakInfoList[_selectedInfoIndex];
                }
            }
            else if (_selectedArea == 2)
            {
                if (-1 < _selectedInfoIndex && _selectedInfoIndex < _luaObjectLeakInfoList.Count)
                {
                    _detailInfo = _luaObjectLeakInfoList[_selectedInfoIndex];
                }
            }

            if (_detailInfoStyle == null)
            {
                SetDetailInfoStyle();
            }
            EditorGUILayout.TextArea(_detailInfo, _detailInfoStyle);

            //加两个空行
            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("");
            EditorGUILayout.EndScrollView();
        }

        private void OnInfoClick()
        {
            CalculateSelectedAreaAndIndex();
            if (_newSelectedArea == -1 || _newSelectedInfoIndex == -1)
            {
                return;
            }
            if (_newSelectedInfoIndex != _selectedInfoIndex || _newSelectedArea != _selectedArea)
            {
                _selectedInfoIndex = _newSelectedInfoIndex;
                _selectedArea = _newSelectedArea;

                //如果选中了详情区的TextArea，需要移除焦点，否则更新不了TextArea
                GUIUtility.keyboardControl = 0;
                GUIUtility.hotControl = 0;
                Repaint();
            }
        }

        private void CalculateSelectedAreaAndIndex()
        {
            if (Event.current.type != EventType.MouseUp)
            {
                return;
            }

            //鼠标弹起代表一次点击
            Vector2 originalMousePosition = Event.current.mousePosition;
            Vector2 currentMousePositionInCsharpArea = Event.current.mousePosition + _csharpAreaScrollPosition;
            Vector2 currentMousePositionInLuaArea = Event.current.mousePosition + _luaAreaScrollPosition;

            float csharpAreaMaxHeight = yPostionOfSegmentLine1 + _csharpAreaScrollPosition.y;
            float luaAreaMaxHeight = yPostionOfSegmentLine2 + _luaAreaScrollPosition.y;

            float headSpaceForCsharpArea = INSTRUCTION_HEIGHT + BUTTON_HEIGHT + DEFAULT_PADDING + TITLE_HEIGHT;
            float headSpaceForLuaArea = yPostionOfSegmentLine1 + TITLE_HEIGHT;

            if (originalMousePosition.y <= headSpaceForCsharpArea)
            {
                _newSelectedArea = -1;
                _newSelectedInfoIndex = -1;
            }
            else if (originalMousePosition.y <= yPostionOfSegmentLine1)
            {
                _newSelectedArea = 1;
                _newSelectedInfoIndex = GetSelectedIndex(headSpaceForCsharpArea, csharpAreaMaxHeight, INFO_LINE_HEIGHT, currentMousePositionInCsharpArea);
            }
            else if (originalMousePosition.y <= yPostionOfSegmentLine2)
            {
                _newSelectedArea = 2;
                _newSelectedInfoIndex = GetSelectedIndex(headSpaceForLuaArea, luaAreaMaxHeight, INFO_LINE_HEIGHT, currentMousePositionInLuaArea);
            }
            else
            {
                _newSelectedArea = -1;
                _newSelectedInfoIndex = -1;
            }
        }

        private int GetSelectedIndex(float min, float max, float increase, Vector2 mousePosition)
        {
            int index = -1;
            Rect currentRect;
            for (int y = (int)min; y < (int)max; y += (int)increase)
            {
                index++;
                currentRect = new Rect(0, y, Screen.width - 18, increase);
                if (currentRect.Contains(mousePosition) == true)
                {
                    return index;
                }
            }
            return -1;
        }
    }
}