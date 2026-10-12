namespace LaboresNet.Data.Navigation;

public class NavigationService
{
    public event EventHandler? BackRequested;

    public void RequestBack()
    {
        BackRequested?.Invoke(this, EventArgs.Empty);
    }
}
