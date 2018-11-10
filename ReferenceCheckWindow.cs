//分活动tab展示
//分栏显示，简报和详情 
//单条记录头部加图标，加背景图，可点击看详情
//底部按钮



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ReferenceCheckWindow : EditorWindow
{
    enum MessageType
    {
        Info,
        Warn,
        Error,
    }

    #region 参数
    private int _selectedTab = 0;//选中的tab index
    private List<string> _tabNames = new List<string>();
    private float _buttonHeight = 30f;
    private float _defaultPadding = 2f;

    private Vector2 _summaryMessageScrollPosition = Vector2.zero;
    private Vector2 _detailMessageScrollPosition = Vector2.zero;

    private bool _needInitScrollViewArea = false;
    private float _summaryMessageScrollViewHeight;
    private float _detailMessageScrollViewHeight;


    private Rect _segmentingLineRect;
    private bool _segmentingLineMove = false;
    private float _segmentingLinePositonToWindowHeightRatio;
    private string _segmentingLineRatioKey = "SEGMENTING_LINE_RATIO";

    private Texture2D _segmentingLineTexture;
    private Rect _cursorRect;
    private int _cursorRectWidth = 10;
    private float _lastWindowHeight;
    private bool _windowResize = false;
    #endregion

    [MenuItem("PandoraTools/ReferenceChecker")]
    public static void ShowWindow()
    {
        GetWindow<ReferenceCheckWindow>(false, "ReferenceChecker", true);
    }
    private void OnFocus()
    {
        //每次获取到焦点时初始化一次
        _needInitScrollViewArea = true;
    }

    private void OnLostFocus()
    {
        //记录segmentingLineRatio
        EditorPrefs.SetFloat(_segmentingLineRatioKey, _segmentingLinePositonToWindowHeightRatio);
    }

    public void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        //DrawHeadTabs();
        InitMessageScrollViewArea();
        DrawButtons();
        DrawSegmentingLine();
        OnMouseEvent();
        OnScreenHeightResize();
        RefreshMessageScrollViewArea();

        DrawSummaryMessageArea();

        DrawDetailMessageArea();

        //DrawButtons();
        EditorGUILayout.EndVertical();
        //Repaint();
    }

    private void DrawHeadTabs()
    {
        int newSelectedTab = _selectedTab;

        newSelectedTab = GUILayout.Toolbar(newSelectedTab, _tabNames.ToArray());
        if (newSelectedTab != _selectedTab)
        {
            _selectedTab = newSelectedTab;
            //加载对应活动下的内容
        }
    }

    private void DrawMessage( string message, MessageType messageType )
    {
        string guiStyleName = "";
        switch (messageType)
        {
            case MessageType.Info:
                guiStyleName = "CN EntryInfo";
                break;
            case MessageType.Warn:
                guiStyleName = "CN EntryWarn";
                break;
            case MessageType.Error:
                guiStyleName = "CN EntryError";
                break;
            default:
                break;
        }

        EditorGUILayout.SelectableLabel(message, guiStyleName);

    }


    private void DrawButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("快照", GUILayout.Width(50f), GUILayout.Height(_buttonHeight)))
        {

        }

        if (GUILayout.Button("检查", GUILayout.Width(50f), GUILayout.Height(_buttonHeight)))
        {

        }
        EditorGUILayout.EndHorizontal();
    }


    private void InitMessageScrollViewArea()
    {
        if (_needInitScrollViewArea == false)
        {
            return;
        }
        else
        {
            _segmentingLinePositonToWindowHeightRatio = EditorPrefs.GetFloat(_segmentingLineRatioKey, 0.5f);

            SetMessageScrollViewArea();
            _needInitScrollViewArea = false;

        }
    }

    private void ResizeMessageScrollViewArea()
    {
        if (Event.current.type == EventType.mouseDown && _cursorRect.Contains(Event.current.mousePosition))
        {
            _segmentingLineMove = true;
        }

        if (_segmentingLineMove == true)
        {
            _summaryMessageScrollViewHeight = Event.current.mousePosition.y - _buttonHeight - _defaultPadding;
            _detailMessageScrollViewHeight = Screen.height - Event.current.mousePosition.y - _defaultPadding;
            _segmentingLineRect.Set(_segmentingLineRect.x, Event.current.mousePosition.y, _segmentingLineRect.width, _segmentingLineRect.height);

            _cursorRect.Set(_cursorRect.x, _segmentingLineRect.y - _cursorRectWidth / 2, _cursorRect.width, _cursorRect.height);
            Repaint();

        }

        if (Event.current.type == EventType.MouseUp)
        {
            _segmentingLineMove = false;
        }



    }

    private void DrawSummaryMessageArea()
    {
        _summaryMessageScrollPosition = EditorGUILayout.BeginScrollView(_summaryMessageScrollPosition, GUILayout.Height(_summaryMessageScrollViewHeight));
        DrawMessage("haipi", MessageType.Warn);

        for (int i = 0; i < 20; i++)
        {
            DrawMessage("haipi", MessageType.Info);
        }
        DrawMessage("haipi", MessageType.Error);
        EditorGUILayout.EndScrollView();
    }

    private void DrawDetailMessageArea()
    {
        _detailMessageScrollPosition = EditorGUILayout.BeginScrollView(_detailMessageScrollPosition, GUILayout.Height(_detailMessageScrollViewHeight));
        //EditorGUILayout.SelectableLabel("小说千千万万,好看的小说也\r\n层出不穷,今天小鹦鹉\r\n我就来给大家,推3本看\r\n了过瘾的科幻小说,每本都创意满满,让人追出\r\n黑眼圈,希望大家能够喜欢。小说\r\n千千万万,好看\r\n的小说也层出不穷,今天小鹦鹉我就来给大家,推3本看了过瘾的科幻小说\r\n,每本都创意满满,让人追出黑眼圈,希望大家\r\n能够喜欢。小说千千万万,好看\r\n的小说也层\r\n出不穷,今天小鹦鹉我就来给大家,推3本看了过\r\n小说千千万万,好看的小说也\r\n层出不穷,今天小鹦鹉\r\n我就来给大家,推3本看\r\n了过瘾的科幻小说,每本都创意满满,让人追出\r\n黑眼圈,希望大家能够喜欢。小说\r\n千千万万,好看\r\n的小说也层出不穷,今天小鹦鹉我就来给大家,推3本看了过瘾的科幻小说\r\n,每本都创意满满,让人追出黑眼圈,希望大家\r\n能够喜欢。小说千千万万,好看\r\n的小说也层\r\n出不穷,今天小鹦鹉我就来给大家,推3本看了过\r\n小说千千万万,好看的小说也\r\n层出不穷,今天小鹦鹉\r\n我就来给大家,推3本看\r\n了过瘾的科幻小说,每本都创意满满,让人追出\r\n黑眼圈,希望大家能够喜欢。小说\r\n千千万万,好看\r\n的小说也层出不穷,今天小鹦鹉我就来给大家,推3本看了过瘾的科幻小说\r\n,每本都创意满满,让人追出黑眼圈,希望大家\r\n能够喜欢。小说千千万万,好看\r\n的小说也层\r\n出不穷,今天小鹦鹉我就来给大家,推3本看了过\r\n");

        DrawMessage("haipi", MessageType.Warn);

        for (int i = 0; i < 20; i++)
        {
            DrawMessage("haipi", MessageType.Info);
        }
        DrawMessage("haipi", MessageType.Error);
        EditorGUILayout.EndScrollView();
    }

    private void DrawSegmentingLine()
    {
        if (_segmentingLineTexture == null)
        {
            _segmentingLineTexture = new Texture2D(1, 1);
            _segmentingLineTexture.hideFlags = HideFlags.DontSave;
            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    _segmentingLineTexture.SetPixel(i, j, Color.black);
                }
            }
            _segmentingLineTexture.Apply();
            _segmentingLineTexture.filterMode = FilterMode.Point;
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

    private void RefreshMessageScrollViewArea()
    {
        //只有分割线移动时计算radio,窗口变动不计算
        if (_segmentingLineMove == true)
        {
            Debug.LogWarning(string.Format("lineMove = {0},windowResize = {1}", _segmentingLineMove, _windowResize));
            _segmentingLinePositonToWindowHeightRatio = Event.current.mousePosition.y / Screen.height;
            Debug.LogError(string.Format("ratio:{0}", _segmentingLinePositonToWindowHeightRatio.ToString()));



        }
        if (_segmentingLineMove == true || _windowResize == true)
        {

            SetMessageScrollViewArea();


            Repaint();
        }

    }

    private void SetMessageScrollViewArea()
    {
        float screenHeight = Screen.height;
        float ySegmentingLinePosition = screenHeight * _segmentingLinePositonToWindowHeightRatio;

        _summaryMessageScrollViewHeight = ySegmentingLinePosition - _buttonHeight - _defaultPadding;
        _detailMessageScrollViewHeight = screenHeight - ySegmentingLinePosition - _defaultPadding;

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


}


