using System;
using System.Threading.Tasks;

public class AsyncPlayerRequest
{
    private readonly Task request;
    private readonly Func<Task> lazyRequest;
    public AsyncPlayerRequest(Func<Task> action)
    {
        lazyRequest = action;
    }
    public AsyncPlayerRequest(Task request)
    {
        this.request = request;
    }

    public AsyncPlayerRequest(Action request)
    {
        this.request = new Task(request);
    }

    public Task Invoke()
    {
        if (lazyRequest != null)
        {
            return lazyRequest.Invoke();
        }

        return request;
    }
}
