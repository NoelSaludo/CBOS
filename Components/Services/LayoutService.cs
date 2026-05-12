namespace CBOS.Components.Services;
public class LayoutService
{
    public bool IsSidebarCollapsed { get; private set; }
    public event Action? OnSidebarToggled;

    public void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
        OnSidebarToggled?.Invoke();
    }

    public void SetSidebarCollapsed(bool collapsed)
    {
        if (IsSidebarCollapsed != collapsed)
        {
            IsSidebarCollapsed = collapsed;
            OnSidebarToggled?.Invoke();
        }
    }
}