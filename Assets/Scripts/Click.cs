using UnityEngine;
using UnityEngine.EventSystems;

public class Click : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    private TransparentWindow transparentWindow;

private void Start()
{
    transparentWindow = FindFirstObjectByType<TransparentWindow>();
    transparentWindow?.SetMenu(menu.GetComponent<RectTransform>());
    SetMenuVisible(false);
}

private void Update()
{
    if (Input.GetMouseButtonDown(1))
    {
        if (IsPointerOverUI())
        {
            return;
        }

        if (IsClick())
        {
            SetMenuVisible(!menu.activeSelf);
        }
    }
}

    private bool IsClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(
            Input.mousePosition
        );

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return true;
        }

        return false;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }

public void OpenChat()
{
    SetMenuVisible(false);
}

public void OpenSettings()
{
    SetMenuVisible(false);
}

public void ToggleVoice()
{
    SetMenuVisible(false);
}

    public void ExitApplication()
    {
        Application.Quit();
    }

private void SetMenuVisible(bool visible)
{
    menu.SetActive(visible);
}


}
