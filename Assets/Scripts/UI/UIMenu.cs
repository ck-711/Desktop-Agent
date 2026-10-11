using UnityEngine;

public class UIMenu : MonoBehaviour
{
    [SerializeField] private RectTransform menuRect;
    [SerializeField] private float offsetX = 400f;

    private void Update()
    {
        var direction = UIUtility.GetOutOfScreenDirection(
            menuRect,
            Camera.main
        );

        Vector3 pos = menuRect.localPosition;

        switch (direction)
        {
            case UIUtility.OutOfScreenDirection.Left:
                pos.x += offsetX;
                menuRect.localPosition = pos;
                break;

            case UIUtility.OutOfScreenDirection.Right:
                pos.x -= offsetX;
                menuRect.localPosition = pos;
                break;
        }
    }
}