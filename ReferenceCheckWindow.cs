
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
    private string _searchFilter;
    private int _selectedTab = 0;//选中的tab index
    private List<string> _tabNames = new List<string>();
    private int _buttonHeight = 30;
    private int _defaultPadding = 2;
    private int _messageLineHeight = 30;

    private Vector2 _summaryMessageScrollPosition = Vector2.zero;
    private Vector2 _detailMessageScrollPosition = Vector2.zero;

    private bool _needInitScrollViewArea = false;
    private float _summaryMessageScrollViewHeight;
    private float _detailMessageScrollViewHeight;

    private GUIStyle _detailMessageStyle;
    private int _detailMessageFontSize = 16;
    private GUIStyle _summaryMessageStyle;
    private int _summaryMessageFontSize = 16;


    private Rect _segmentingLineRect;
    private bool _segmentingLineMove = false;
    private float _segmentingLinePositonToWindowHeightRatio;
    private string _segmentingLineRatioKey = "SEGMENTING_LINE_RATIO";

    private Texture2D _segmentingLineTexture;
    private Texture2D _oddLineBackgroundTexture;
    private Texture2D _evenLineBackgroundTexture;
    private Texture2D _selectedLineBackoundTexture;

    private Rect _cursorRect;
    private int _cursorRectWidth = 10;
    private float _lastWindowHeight;
    private bool _windowResize = false;

    private int _selectedMessageIndex = -1;

    #endregion

    [MenuItem("PandoraTools/ReferenceChecker")]
    public static void ShowWindow()
    {
        GetWindow<ReferenceCheckWindow>(false, "ReferenceChecker", true);
    }

    private void OnEnable()
    {

        _detailMessageStyle = new GUIStyle();
        _detailMessageStyle.alignment = TextAnchor.UpperLeft;
        _detailMessageStyle.padding = new RectOffset(2, 2, 2, 2);
        _detailMessageStyle.wordWrap = true;
        _detailMessageStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
        _detailMessageStyle.fontSize = _detailMessageFontSize;
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
        InitMessageScrollViewArea();
        DrawHeader();
        DrawSegmentingLine();
        OnMouseEvent();
        OnScreenHeightResize();
        RefreshMessageScrollViewArea();
        DrawSummaryMessageArea();

        DrawDetailMessageArea();
        OnMessageClick();

        EditorGUILayout.EndVertical();
    }

    private void DrawHeader()
    {
        GUILayout.BeginHorizontal();
        DrawSearchArea();
        DrawButtons();
        GUILayout.EndHorizontal();
    }
    private void DrawSearchArea()
    {
        string newSearchFilter = EditorGUILayout.TextField("", _searchFilter, "SearchTextField");
        if (GUILayout.Button("", "SearchCancelButton", GUILayout.Width(18f)))
        {
            newSearchFilter = "";
            GUIUtility.keyboardControl = 0;//把焦点从输入框移走
        }
        if (newSearchFilter != _searchFilter)
        {
            // todo 筛选显示项
        }
    }

    private void DrawMessage( int index, string message, MessageType messageType, float topPadding = 30f )
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

        //EditorGUILayout.TextArea(message, guiStyleName);
        Rect rect = new Rect(0, index * _messageLineHeight, Screen.width - 18f, _messageLineHeight);
        DrawMessageBackground(index, rect);
        _summaryMessageStyle = GetSummaryMessageStyle(guiStyleName);
        GUI.Label(rect, message, _summaryMessageStyle);
        //添加cursorRect
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Text);
    }

    private GUIStyle GetSummaryMessageStyle( string styleName )
    {
        GUIStyle style = new GUIStyle(styleName);
        style.alignment = TextAnchor.UpperLeft;
        style.clipping = TextClipping.Clip;
        style.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
        style.fontSize = _summaryMessageFontSize;
        return style;
    }

    private void OnMessageClick()
    {
        int index = GetSeletedMessageIndex();
        if (_selectedMessageIndex != index && index != -1)
        {
            _selectedMessageIndex = index;
        }

    }


    private int GetSeletedMessageIndex()
    {
        if (Event.current.type != EventType.MouseUp)
        {
            return -1;
        }
        //鼠标弹起代表一次点击
        Vector2 currentMousePosition = Event.current.mousePosition + _summaryMessageScrollPosition;
        float height = _segmentingLinePositonToWindowHeightRatio * Screen.height + _summaryMessageScrollPosition.y;


        if (currentMousePosition.y < _buttonHeight + _defaultPadding + _summaryMessageScrollPosition.y || currentMousePosition.y > height)
        {
            return -1;
        }
        int index = -1;
        Rect currentRect;
        for (int y = _buttonHeight + _defaultPadding; y < height; y += _messageLineHeight)
        {
            index++;
            currentRect = new Rect(0, y, Screen.width - 18, _messageLineHeight);
            if (currentRect.Contains(currentMousePosition) == true)
            {
                return index;
            }
        }

        return -1;

    }


    private void DrawButtons()
    {
        if (GUILayout.Button("快照", GUILayout.Width(80f), GUILayout.Height(_buttonHeight)))
        {

        }

        if (GUILayout.Button("检查", GUILayout.Width(80f), GUILayout.Height(_buttonHeight)))
        {

        }
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

        MessageType type;
        int num = 20;
        for (int i = 0; i < num; i++)
        {
            type = MessageType.Info;
            if (i == 0)
            {
                type = MessageType.Warn;
            }
            if (i == num - 1)
            {
                type = MessageType.Error;
            }
            DrawMessage(i, "haipi", type);
        }

        GUILayout.Space(num * _messageLineHeight);
        EditorGUILayout.EndScrollView();
    }

    private void DrawDetailMessageArea()
    {
        _detailMessageScrollPosition = EditorGUILayout.BeginScrollView(_detailMessageScrollPosition, GUILayout.Height(_detailMessageScrollViewHeight));
        EditorGUILayout.TextArea("百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。百度新闻是包含海量资讯的新闻服务平台,真实反映每时每刻的新闻热点。您可以搜索新闻事件、热点话题、人物动态、产品资讯等,快速了解它们的最新进展。。end", _detailMessageStyle);

        //加两个空行
        EditorGUILayout.LabelField("");
        EditorGUILayout.LabelField("");

        EditorGUILayout.EndScrollView();
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

    private void RefreshMessageScrollViewArea()
    {
        //只有分割线移动时计算radio,窗口变动不计算
        if (_segmentingLineMove == true)
        {
            _segmentingLinePositonToWindowHeightRatio = Event.current.mousePosition.y / Screen.height;

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

    private void DrawMessageBackground( int index, Rect rect, float topPadding = 30f )
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
            _selectedLineBackoundTexture = CreateTexture(Vector2.one, new Color(0, 0, 0.5f, 0.3f));
        }


        if (index == _selectedMessageIndex)
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

    private Texture2D CreateTexture( Vector2 size, Color color )
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


