using System;
using System.Collections.Generic;
using UnityEngine;
using SwiftXP.SPT.TheModfather.Client.Data;
using SwiftXP.SPT.TheModfather.Client.Enums;
using System.Globalization;

namespace SwiftXP.SPT.TheModfather.Client.UI;

public static class ModfatherUI
{
    private static Vector2 s_scrollPos = Vector2.zero;
    private static float s_scale = 1f;

    private static Texture2D? s_texScrollTrack;
    private static Texture2D? s_texScrollThumb;

    private static class Layout
    {
        public const float ReferenceHeight = 1080f;
        public const float WindowWidth = 900f;
        public const float WindowHeight = 600f;

        public const float ButtonWidth = 260f;
        public const float ButtonHeight = 45f;
        public const float BottomMargin = 15f;
        public const float SideMargin = 30f;

        public const float HeaderHeight = 45f;
        public const float ProgressBarHeight = 45f;
    }

    public static void Draw(
        PluginState pluginState,
        UpdateUiState updateUiState,
        Action onAccept,
        Action onDecline,
        Action onCancel,
        Action onErrorContinue)
    {
        s_scale = Screen.height / Layout.ReferenceHeight;

        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s_scale, s_scale, 1f));

        try
        {
            float virtualScreenWidth = Screen.width / s_scale;
            float virtualScreenHeight = Screen.height / s_scale;

            Rect screenRect = new(0, 0, virtualScreenWidth, virtualScreenHeight);
            Rect winRect = new(
                (virtualScreenWidth - Layout.WindowWidth) / 2f,
                (virtualScreenHeight - Layout.WindowHeight) / 2f,
                Layout.WindowWidth,
                Layout.WindowHeight
            );

            DrawBackground(screenRect);
            DrawWindowFrame(winRect);
            DrawHeader(winRect, "THE MODFATHER // UPDATE-CHECK");
            DrawStatusText(winRect, updateUiState.StatusText);

            if (pluginState == PluginState.Error)
            {
                DrawErrorView(winRect, updateUiState, onErrorContinue);
            }
            else if (ShouldShowProgress(pluginState))
            {
                DrawProgressView(winRect, updateUiState, onCancel);
            }
            else if (pluginState == PluginState.UpdateAvailable)
            {
                DrawProposalView(winRect, updateUiState, onAccept, onDecline);
            }
        }
        finally
        {
            GUI.matrix = oldMatrix;
        }
    }

    private static void DrawErrorView(Rect winRect, UpdateUiState state, Action onErrorContinue)
    {
        Rect barRect = GetProgressBarRect(winRect);
        DrawProgressBar(barRect, state.Progress, state.ProgressDetail, new Color(0.7f, 0.0f, 0.0f, 1f));

        Rect btnRect = GetCenteredButtonRect(winRect);
        if (DrawButton(btnRect, "IGNORE & START GAME", false))
            onErrorContinue?.Invoke();
    }

    private static void DrawProgressView(Rect winRect, UpdateUiState state, Action onCancel)
    {
        Rect barRect = GetProgressBarRect(winRect);
        DrawProgressBar(barRect, state.Progress, state.ProgressDetail, ModfatherUIColors.AccentOrange);

        if (!string.IsNullOrEmpty(state.ProgressHeader))
            DrawProgressHeader(barRect, state.ProgressHeader);

        if (state.Progress == 1f)
            return;

        Rect btnRect = GetCenteredButtonRect(winRect);
        if (DrawButton(btnRect, "CANCEL", false))
            onCancel?.Invoke();
    }

    private static void DrawProposalView(Rect winRect, UpdateUiState state, Action onAccept, Action onDecline)
    {
        if (state.SyncActions == null)
            return;

        float btnY = GetButtonY(winRect);

        float listStartY = winRect.y + 95f;
        float listHeight = btnY - 20f - listStartY;

        Rect listRect = new(winRect.x + 20, listStartY, winRect.width - 40, listHeight);
        DrawActionList(listRect, state.SyncActions);

        Rect acceptBtnRect = new(winRect.x + Layout.SideMargin, btnY, Layout.ButtonWidth, Layout.ButtonHeight);
        if (DrawButton(acceptBtnRect, "ACCEPT OFFER", true))
            onAccept?.Invoke();

        Rect declineBtnRect = new(winRect.x + winRect.width - Layout.ButtonWidth - Layout.SideMargin, btnY, Layout.ButtonWidth, Layout.ButtonHeight);
        if (DrawButton(declineBtnRect, "WALK AWAY", false))
            onDecline?.Invoke();
    }

    private static bool ShouldShowProgress(PluginState state)
    {
        return state == PluginState.CheckingForUpdates
            || state == PluginState.Updating
            || state == PluginState.Initializing
            || state == PluginState.UpdateComplete;
    }

    private static void DrawProgressHeader(Rect barRect, string text)
    {
        Rect headerRect = new(barRect.x, barRect.y - 25f, barRect.width, 22f);

        GUIStyle headerStyle = new(GUI.skin.label)
        {
            alignment = TextAnchor.LowerCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = ModfatherUIColors.TextWhite }
        };

        GUI.Label(headerRect, text, headerStyle);
    }

    private static void DrawProgressBar(Rect rect, float progress, string text, Color fillColor)
    {
        DrawRect(rect, new Color(0.05f, 0.05f, 0.05f, 1f));
        DrawBorder(rect, ModfatherUIColors.WindowBorder);

        float fillWidth = rect.width * Mathf.Clamp01(progress);
        if (fillWidth > 1f)
            DrawRect(new Rect(rect.x, rect.y, fillWidth, rect.height), fillColor);

        if (!string.IsNullOrEmpty(text))
        {
            GUIStyle textStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16, normal = { textColor = Color.white } };
            GUIStyle shadowStyle = new(textStyle); shadowStyle.normal.textColor = new Color(0, 0, 0, 0.7f);

            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, shadowStyle);
            GUI.Label(rect, text, textStyle);
        }
    }

    private static void DrawActionList(Rect rect, IReadOnlyList<SyncAction> items)
    {
        DrawRect(rect, new Color(0.08f, 0.08f, 0.08f, 1f));

        float headerH = 26f;
        Rect headerRect = new(rect.x, rect.y, rect.width, headerH);
        DrawListHeader(headerRect, items);

        float itemH = 32f;
        Rect scrollVisible = new(rect.x, rect.y + headerH, rect.width, rect.height - headerH);
        Rect scrollContent = new(0, 0, rect.width - 15f, items.Count * itemH);

        EnsureScrollTextures();

        GUIStyle vScroll = new(GUI.skin.verticalScrollbar);
        vScroll.normal.background = s_texScrollTrack; vScroll.fixedWidth = 10f;

        GUIStyle vThumb = new(GUI.skin.verticalScrollbarThumb);
        vThumb.normal.background = s_texScrollThumb; vThumb.fixedWidth = 10f;

        s_scrollPos = GUI.BeginScrollView(scrollVisible, s_scrollPos, scrollContent, false, false, GUIStyle.none, vScroll);

        for (int i = 0; i < items.Count; i++)
        {
            Rect itemRect = new(0, i * itemH, scrollContent.width, itemH);
            DrawListItem(itemRect, items[i], i % 2 == 0);
        }

        GUI.EndScrollView();
    }

    private static void DrawListHeader(Rect rect, IReadOnlyList<SyncAction> items)
    {
        DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

        GUIStyle style = new(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 12, normal = { textColor = Color.gray } };
        GUI.Label(new Rect(rect.x + 10, rect.y, 150, rect.height), $"CHANGES ({items.Count})", style);

        float bW = 90f;

        if (GUI.Button(new Rect(rect.x + rect.width - bW - 5, rect.y + 2, bW, 22), "Select None"))
        {
            foreach (SyncAction item in items)
                if (item.Type != SyncActionType.Blacklist)
                    item.IsSelected = false;
        }

        if (GUI.Button(new Rect(rect.x + rect.width - (bW * 2) - 10, rect.y + 2, bW, 22), "Select All"))
        {
            foreach (SyncAction item in items)
                item.IsSelected = true;
        }
    }

    private static void DrawListItem(Rect rect, SyncAction item, bool isEven)
    {
        if (!isEven)
            DrawRect(rect, new Color(1, 1, 1, 0.03f));

        Vector2 scaledMouse = Event.current.mousePosition / s_scale;
        if (rect.Contains(scaledMouse))
            DrawRect(rect, ModfatherUIColors.HoverOverlay);

        float centerY = rect.y + (rect.height - 16) / 2;

        Rect checkRect = new(rect.x + 8, centerY, 16, 16);
        DrawRect(checkRect, ModfatherUIColors.WindowBorder);

        bool isBlacklisted = item.Type == SyncActionType.Blacklist;

        if (isBlacklisted)
        {
            item.IsSelected = true;
            DrawRect(new Rect(checkRect.x + 3, checkRect.y + 3, 10, 10), Color.gray);
        }
        else if (item.IsSelected)
        {
            DrawRect(new Rect(checkRect.x + 3, checkRect.y + 3, 10, 10), ModfatherUIColors.AccentOrange);
        }

        Color typeCol = GetTypeColor(item.Type);
        Rect badgeRect = new(rect.x + 32, centerY - 1, 60, 18);
        DrawRect(badgeRect, new Color(typeCol.r, typeCol.g, typeCol.b, 0.15f));

        GUIStyle typeStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = typeCol } };
        GUI.Label(badgeRect, item.Type.ToString().ToUpper(CultureInfo.InvariantCulture), typeStyle);

        Color textColor = isBlacklisted ? Color.gray : (item.IsSelected ? ModfatherUIColors.TextWhite : Color.gray);
        GUIStyle pathStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = 13, fontStyle = FontStyle.Normal, normal = { textColor = textColor } };
        GUI.Label(new Rect(rect.x + 100, rect.y, rect.width - 100, rect.height), item.RelativeFilePath, pathStyle);

        if (!isBlacklisted && GUI.Button(rect, string.Empty, GUIStyle.none))
            item.IsSelected = !item.IsSelected;
    }

    private static void DrawHeader(Rect winRect, string title)
    {
        GUIStyle titleStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = ModfatherUIColors.AccentOrange } };
        GUI.Label(new Rect(winRect.x + 20, winRect.y + 5, winRect.width - 40, Layout.HeaderHeight), title, titleStyle);

        DrawRect(new Rect(winRect.x + 20, winRect.y + 48, winRect.width - 40, 1), new Color(1, 1, 1, 0.1f));
    }

    private static void DrawStatusText(Rect winRect, string text)
    {
        GUIStyle bodyStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, normal = { textColor = ModfatherUIColors.TextWhite } };
        GUI.Label(new Rect(winRect.x, winRect.y + 55, winRect.width, 35), text, bodyStyle);
    }

    private static bool DrawButton(Rect rect, string text, bool isPrimary)
    {
        Color baseColor = isPrimary ? ModfatherUIColors.AccentOrange : ModfatherUIColors.ButtonSecondary;
        Color textColor = isPrimary ? Color.black : ModfatherUIColors.TextWhite;

        DrawRect(rect, baseColor);

        Vector2 scaledMouse = Event.current.mousePosition / s_scale;
        if (rect.Contains(scaledMouse))
            DrawRect(rect, ModfatherUIColors.HoverOverlay);

        GUIStyle btnStyle = new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 15, normal = { textColor = textColor } };
        GUI.Label(rect, text, btnStyle);

        Color oldColor = GUI.color;
        GUI.color = Color.clear;
        bool clicked = GUI.Button(rect, string.Empty);
        GUI.color = oldColor;

        return clicked;
    }

    private static void DrawWindowFrame(Rect rect)
    {
        DrawRect(rect, ModfatherUIColors.WindowBorder);
        DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), ModfatherUIColors.WindowBackground);
        DrawRect(new Rect(rect.x, rect.y, rect.width, 3), ModfatherUIColors.AccentOrange);
    }

    private static void DrawBackground(Rect rect)
    {
        DrawRect(rect, ModfatherUIColors.DimmerBackground);
    }

    private static void DrawRect(Rect position, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(position, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }

    private static void DrawBorder(Rect rect, Color color)
    {
        DrawRect(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, 1), color);
        DrawRect(new Rect(rect.x - 1, rect.y + rect.height, rect.width + 2, 1), color);
        DrawRect(new Rect(rect.x - 1, rect.y, 1, rect.height), color);
        DrawRect(new Rect(rect.x + rect.width, rect.y, 1, rect.height), color);
    }

    private static Rect GetProgressBarRect(Rect winRect)
    {
        float barWidth = winRect.width - 150f;

        return new Rect(
            winRect.x + 75f,
            winRect.y + (Layout.WindowHeight / 2f) - (Layout.ProgressBarHeight / 2f) - 20f,
            barWidth,
            Layout.ProgressBarHeight
        );
    }

    private static Rect GetCenteredButtonRect(Rect winRect)
    {
        float btnY = GetButtonY(winRect);

        return new Rect(winRect.x + (winRect.width - Layout.ButtonWidth) / 2f, btnY, Layout.ButtonWidth, Layout.ButtonHeight);
    }

    private static float GetButtonY(Rect winRect)
    {
        return winRect.y + Layout.WindowHeight - Layout.ButtonHeight - Layout.BottomMargin;
    }

    private static Color GetTypeColor(SyncActionType type)
    {
        return type switch
        {
            SyncActionType.Add => new Color(0.4f, 0.8f, 0.4f),
            SyncActionType.Delete => new Color(0.9f, 0.4f, 0.4f),
            SyncActionType.Update => new Color(0.4f, 0.6f, 1f),
            SyncActionType.Adopt => new Color(0.8f, 0.8f, 0.4f),
            SyncActionType.Blacklist => new Color(0.5f, 0.5f, 0.5f),
            _ => Color.gray
        };
    }

    private static void EnsureScrollTextures()
    {
        if (s_texScrollTrack == null)
            s_texScrollTrack = MakeTex(1, 1, new Color(0.05f, 0.05f, 0.05f, 1f));

        if (s_texScrollThumb == null)
            s_texScrollThumb = MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f, 1f));
    }

    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }
}