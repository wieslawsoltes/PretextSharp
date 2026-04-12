using Microsoft.UI.Dispatching;

namespace Pretext.Uno.Controls;

public sealed class UiRenderScheduler
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action _action;
    private bool _scheduled;

    public UiRenderScheduler(DispatcherQueue dispatcherQueue, Action action)
    {
        _dispatcherQueue = dispatcherQueue;
        _action = action;
    }

    public void Schedule()
    {
        if (_scheduled)
        {
            return;
        }

        _scheduled = true;
        _dispatcherQueue.TryEnqueue(() =>
        {
            _scheduled = false;
            _action();
        });
    }
}
