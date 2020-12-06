using System;
using System.Runtime.CompilerServices;
using MLAPI.Transports.Tasks;
using UnityEngine;


static class TaskUtils
{

    //public static SocketTasksAwaiter GetAwaiter(this SocketTasks sockets)
    //{
    //    return new SocketTasksAwaiter(sockets);
    //}
}

struct SocketTasksAwaiter : INotifyCompletion
{
    private SocketTasks sockets;
    private Action continuation;
    public SocketTasksAwaiter(SocketTasks sockets)
    {
        this.sockets = sockets;
        continuation = null;
    }

    public void OnCompleted(Action continuation)
    {
        if (IsCompleted)
            continuation();

        this.continuation = continuation;
    }

    public bool IsCompleted
    {
        get
        {
            return sockets.IsDone;
        }
    }
    public void GetResult() { }
}