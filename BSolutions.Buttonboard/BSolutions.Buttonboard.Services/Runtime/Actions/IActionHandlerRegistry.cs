namespace BSolutions.Buttonboard.Services.Runtime.Actions
{
    public interface IActionHandlerRegistry
    {
        bool TryResolve(string key, out IActionHandler handler);
    }
}
