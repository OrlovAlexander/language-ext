using System;
using LanguageExt.Traits;

namespace LanguageExt.DSL;

record IOCatchPop<A>(IO<A> Next) : IO<A>
{
    public override IO<B> Map<B>(Func<A, B> f) =>
        new IOCatchPop<B>(Next.Map(f));

    public override IO<B> Bind<B>(Func<A, K<IO, B>> f) =>
        new IOCatchPop<B>(Next.Bind(f));
    
    public override string ToString() => 
        "IO catch pop";
}
