using System.Threading;
using System.Threading.Tasks;
using LanguageExt.Common;

namespace LanguageExt.Pipes.Concurrent;

internal class SourceInternal
{
    public static async ValueTask<bool> ReadyToRead<A>(Seq<SourceIterator<A>> sources, CancellationToken token)
    {
        if (sources.Count == 0) throw Errors.SourceClosed;

        var             remaining  = sources.Count;
        using var       wait       = new CountdownEvent(remaining);
        using var       src        = new CancellationTokenSource();
        await using var reg        = token.Register(() => src.Cancel());
        var             childToken = src.Token;
        var             ready      = false;

        try
        {
            sources.Map(s => s.ReadyToRead(childToken)
                              .Map(f =>
                                   {
                                       ready = f || ready;
                                       if (ready)
                                       {
                                           // Clear all signals
                                           // ReSharper disable once AccessToDisposedClosure
                                           wait.Signal(remaining);
                                       }
                                       else
                                       {
                                           // Clear one signal
                                           // ReSharper disable once AccessToDisposedClosure
                                           wait.Signal();
                                           Interlocked.Decrement(ref remaining);
                                       }
                                       return f;
                                   }))
                   .Strict();

            wait.Wait(token);
            return ready;
        }
        finally
        {
            await src.CancelAsync();
        }
    }
    
    public static async ValueTask<A> Read<A>(Seq<SourceIterator<A>> sources, CancellationToken token)
    {
        if (sources.Count == 0) throw Errors.SourceClosed;

        var                remaining  = sources.Count;
        using var          wait       = new CountdownEvent(remaining);
        using var          src        = new CancellationTokenSource();
        await using var    reg        = token.Register(() => src.Cancel());
        var                childToken = src.Token;
        var                flag       = 0;
        SourceIterator<A>? source     = null;

        try
        {
            sources.Map(s => s.ReadyToRead(childToken)
                              .Map(f =>
                                   {
                                       if (f && Interlocked.CompareExchange(ref flag, 1, 0) == 0)
                                       {
                                           // The source that is ready to yield a value
                                           source = s;
                                           flag = 2;

                                           // Clear all signals
                                           // ReSharper disable once AccessToDisposedClosure
                                           wait.Signal(remaining);
                                       }
                                       else
                                       {
                                           // Clear one signal
                                           // ReSharper disable once AccessToDisposedClosure
                                           wait.Signal();
                                           Interlocked.Decrement(ref remaining);
                                       }

                                       return f;
                                   }))
                   .Strict();

            wait.Wait(token);
            return flag == 2
                       ? await source!.ReadValue(token)
                       : throw Errors.SourceClosed;
        }
        finally
        {
            await src.CancelAsync();
        }
    }
}
