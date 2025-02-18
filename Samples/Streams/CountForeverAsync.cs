#pragma warning disable LX_StreamT

using LanguageExt;
using LanguageExt.Pipes;
using static LanguageExt.Prelude;
using static Streams.Console;

namespace Streams;

public static class CountForeverAsync
{
    public static IO<Unit> run =>
        from f in fork(example.RunAsync())
        from k in readKey
        from r in f.Cancel 
        select unit;

    static StreamT<IO, long> naturals =>
        StreamT.lift<IO, long>(naturalsEnum());
    
    static StreamT<IO, Unit> example =>
        from v in naturals
        from _ in writeLine($"{v:N0}")
        where false
        select unit;

    static async IAsyncEnumerable<long> naturalsEnum()
    {
        for (var i = 0L; i < long.MaxValue; i++)
        {
            yield return i;
            await Task.Delay(1000);
        }
    }
}
