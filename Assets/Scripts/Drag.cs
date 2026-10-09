using UnityEngine;

[DisallowMultipleComponent]
public sealed class Drag : MonoBehaviour
{
    private Camera mainCamera;
    private Plane dragPlane;
    private Vector3 dragOffset;
    private bool isDragging;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BeginDrag();
        }

        if (!isDragging)
        {
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            isDragging = false;
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float distance))
        {
            transform.position = ray.GetPoint(distance) + dragOffset;
        }
    }

    private void BeginDrag()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit) || !IsPartOfCharacter(hit.transform))
        {
            return;
        }

        dragPlane = new Plane(mainCamera.transform.forward, transform.position);
        if (dragPlane.Raycast(ray, out float distance))
        {
            dragOffset = transform.position - ray.GetPoint(distance);
            isDragging = true;
        }
    }

    private bool IsPartOfCharacter(Transform hitTransform)
    {
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }
}
