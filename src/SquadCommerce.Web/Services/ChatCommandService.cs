namespace SquadCommerce.Web.Services;

public class ChatCommandService
{
    public event Action<string>? OnCommandRequested;

    public void SendCommand(string command)
    {
        OnCommandRequested?.Invoke(command);
    }
}
