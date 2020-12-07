using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MLAPI.Messaging;
using MLAPI.Transports.Tasks;
using UnityEngine;

static class ExtMethods
{

    public static int IndexOf<T>(this IEnumerable<T> collection, T element)
    {
        return IndexOf(collection, t => t.Equals(element));
    }
    public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
    {
        int index = 0;

        var it = collection.GetEnumerator();

        while (it.MoveNext())
        {
            if (predicate(it.Current)) return index;

            index++;
        }
        return -1;
    }

}


public static class TaskUtils
{
    public static SocketTasksAwaiter GetAwaiter(this SocketTasks sockets) => new SocketTasksAwaiter(sockets);
    public static SocketTasksAwaiter GetAwaiter(this SocketTask socket) => new SocketTasksAwaiter(socket.AsTasks());

    public static RPCResponseAwaiter<T> GetAwaiter<T>(this RpcResponse<T> self) => new RPCResponseAwaiter<T>(self);
}

public class RPCResponseAwaiter<T> : INotifyCompletion
{
    private RpcResponse<T> response;
    private Action continuation;

    public RPCResponseAwaiter(RpcResponse<T> response)
    {
        this.response = response;
        pollUntilDone();
    }
    private async void pollUntilDone()
    {
        while (Application.isPlaying && !IsCompleted) await Task.Yield();
        continuation?.Invoke();
    }

    public void OnCompleted(Action continuation)
    {
        if (response.IsDone)
            continuation();
        this.continuation = continuation;
    }
    public bool IsCompleted => response.IsDone;

    public RpcResponse<T> GetResult() => response;
}

public class SocketTasksAwaiter : INotifyCompletion
{
    private SocketTasks sockets;
    private Action continuation;
    public SocketTasksAwaiter(SocketTasks sockets)
    {
        this.sockets = sockets;
        continuation = null;

        pollUntilDone();
    }

    private async void pollUntilDone()
    {
        while (Application.isPlaying && !IsCompleted) await Task.Yield();
        continuation?.Invoke();
    }

    public void OnCompleted(Action continuation)
    {
        if (IsCompleted)
            continuation();

        this.continuation = continuation;
    }

    public bool IsCompleted => sockets.IsDone;
    public bool GetResult() => sockets.Success;
}