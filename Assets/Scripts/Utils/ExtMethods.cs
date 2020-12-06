using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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