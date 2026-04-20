using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UniRx;
using UnityEngine.EventSystems;
using UniRx.Triggers;

public static class CanvasUtility
{
    public static Rect GetRectOnCanvas(this Canvas canvas, RectTransform targetRect, RectTransform parentRect)
    {
        // 取得世界空間四個角點
        Vector3[] rectWorldCorners = new Vector3[4];
        targetRect.GetWorldCorners(rectWorldCorners);

        Vector2[] canvasLocalCorners = new Vector2[4];
        Canvas targetCanvas = targetRect.GetComponentInParent<Canvas>();

        // 轉換成目標 Canvas 的本地座標
        for (int i = 0; i < 4; i++)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                rectWorldCorners[i]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out canvasLocalCorners[i]);
        }

        // 找最小和最大 X、Y
        float minX = canvasLocalCorners[0].x;
        float minY = canvasLocalCorners[0].y;
        float maxX = canvasLocalCorners[0].x;
        float maxY = canvasLocalCorners[0].y;
        for (int i = 1; i < 4; i++)
        {
            if (canvasLocalCorners[i].x < minX) minX = canvasLocalCorners[i].x;
            if (canvasLocalCorners[i].y < minY) minY = canvasLocalCorners[i].y;
            if (canvasLocalCorners[i].x > maxX) maxX = canvasLocalCorners[i].x;
            if (canvasLocalCorners[i].y > maxY) maxY = canvasLocalCorners[i].y;
        }

        // 組合成 Rect
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}

public static class StringHelper
{
    public static string ReplaceTemplateKeys(this string template, IReadOnlyDictionary<string, string> kvp)
    {
        return Regex.Replace(template, @"\{:(\w+)\}", match =>
        {
            string key = match.Groups[1].Value;
            if (kvp.TryGetValue(key, out var value))
                return value;
            else
                return match.Value;
        });
    }
}

public static class ObservableButtonExtensions
{
    public enum PressType
    {
        Click,
        LongPress
    }
    
    public static IObservable<PressType> OnClickOrLongPressAsObservable(
        this UIBehaviour component, 
        float longPressDuration = 1f)
    {
        return component.OnPointerDownAsObservable()
            .SelectMany(_ => {
                
                var longPressTimer = Observable
                    .Timer(TimeSpan.FromSeconds(longPressDuration))
                    .TakeUntil(Observable.Merge(
                        component.OnPointerUpAsObservable(),
                        component.OnPointerExitAsObservable()))
                    .Select(_ => PressType.LongPress)
                    .CatchIgnore();

                var shortPress = component.OnPointerUpAsObservable()
                    .Select(_ => PressType.Click)
                    .Take(1);

                return Observable.Merge(longPressTimer, shortPress).First();
            });
    }
}
