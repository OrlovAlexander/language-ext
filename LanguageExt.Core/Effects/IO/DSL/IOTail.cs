using System;
using System.Threading.Tasks;
using LanguageExt.Traits;

namespace LanguageExt.DSL;

record IOTail<A>(IO<A> Tail) : IO<A>
{
    public override IO<B> Map<B>(Func<A, B> f) => 
        throw new NotSupportedException("You can't map a tail call");

    public override IO<B> Bind<B>(Func<A, K<IO, B>> f) =>
        throw new NotSupportedException("You can't chain a tail call");

    public override IO<B> BindAsync<B>(Func<A, ValueTask<K<IO, B>>> f) => 
        throw new NotSupportedException("You can't chain a tail call");

    public override string ToString() => 
        "IO tail";
}
