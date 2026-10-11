using UnityEngine;

public static class UIUtility
{
    public enum OutOfScreenDirection
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }

    public static OutOfScreenDirection GetOutOfScreenDirection(
        RectTransform rect,
        Camera uiCamera)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            Vector3 p = RectTransformUtility.WorldToScreenPoint(
                uiCamera, corners[i]);

            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        if (minX < 0)
            return OutOfScreenDirection.Left;

        if (maxX > Screen.width)
            return OutOfScreenDirection.Right;

        if (maxY > Screen.height)
            return OutOfScreenDirection.Top;

        if (minY < 0)
            return OutOfScreenDirection.Bottom;

        return OutOfScreenDirection.None;
    }
}