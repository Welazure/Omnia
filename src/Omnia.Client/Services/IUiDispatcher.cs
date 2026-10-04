namespace Omnia.Client.Services;

public interface IUiDispatcher
{
    void Post(Action action);
}
