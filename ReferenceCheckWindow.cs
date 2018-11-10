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
    private bool _segmentingLineChange = false;
    private float _segmentingLinePositonToWindowHeightRatio;
    private string _segmentingLineRatioKey = "SEGMENTING_LINE_RATIO";

    private Texture2D _segmentingLineTexture;
    private Rect _cursorRect;
    private int _cursorRectWidth = 10;
    private float _lastWindowHeight;
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
        ResizeMessageScrollViewArea();
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
        _summaryMessageScrollViewHeight = Screen.height / 2;
        _detailMessageScrollViewHeight = Screen.height / 2 + _buttonHeight + _defaultPadding;

        _segmentingLineRect = new Rect(0, _summaryMessageScrollViewHeight + _buttonHeight + _defaultPadding, Screen.width, 1f);

        _cursorRect = new Rect(0, _segmentingLineRect.y - _cursorRectWidth / 2, Screen.width, 10f);
        _needInitScrollViewArea = false;
    }

    private void ResizeMessageScrollViewArea()
    {
        if (Event.current.type == EventType.mouseDown && _cursorRect.Contains(Event.current.mousePosition))
        {
            _segmentingLineChange = true;
        }

        if (_segmentingLineChange == true)
        {
            _summaryMessageScrollViewHeight = Event.current.mousePosition.y - _buttonHeight - _defaultPadding;
            _detailMessageScrollViewHeight = Screen.height - Event.current.mousePosition.y - _defaultPadding;
            _segmentingLineRect.Set(_segmentingLineRect.x, Event.current.mousePosition.y, _segmentingLineRect.width, _segmentingLineRect.height);

            _cursorRect.Set(_cursorRect.x, _segmentingLineRect.y - _cursorRectWidth / 2, _cursorRect.width, _cursorRect.height);
            Repaint();

        }

        if (Event.current.type == EventType.MouseUp)
        {
            _segmentingLineChange = false;
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

}


