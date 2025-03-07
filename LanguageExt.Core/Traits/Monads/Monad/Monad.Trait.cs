﻿using System;
using static LanguageExt.Prelude;

namespace LanguageExt.Traits;

/// <summary>
/// Monad trait
/// </summary>
/// <typeparam name="M">Self referring trait</typeparam>
public interface Monad<M> : 
    Applicative<M>, 
    MonadIO<M> 
    where M : Monad<M>
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Abstract members
    //
    
    public static abstract K<M, B> Bind<A, B>(K<M, A> ma, Func<A, K<M, B>> f);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Default implementations
    //
    
    public static virtual K<M, A> Flatten<A>(K<M, K<M, A>> mma) =>
        M.Bind(mma, identity);
}
