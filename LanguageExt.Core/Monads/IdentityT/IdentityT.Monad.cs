﻿using System;
using LanguageExt.Traits;

namespace LanguageExt;

/// <summary>
/// Identity module
/// </summary>
public class IdentityT<M> : 
    MonadT<IdentityT<M>, M>, 
    Choice<IdentityT<M>>,
    MonadIO<IdentityT<M>>
    where M : Monad<M>, Choice<M>
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Module
    //
    
    public static K<IdentityT<M>, A> Pure<A>(A value) =>
        IdentityT<M, A>.Pure(value);
    
    public static IdentityT<M, B> bind<A, B>(IdentityT<M, A> ma, Func<A, IdentityT<M, B>> f) =>
        ma.As().Bind(f);

    public static IdentityT<M, B> map<A, B>(Func<A, B> f, IdentityT<M, A> ma) => 
        ma.As().Map(f);

    public static IdentityT<M, B> apply<A, B>(IdentityT<M, Func<A, B>> mf, IdentityT<M, A> ma) =>
        new(mf.As().Value.Apply(ma.As().Value));

    public static IdentityT<M, B> action<A, B>(IdentityT<M, A> ma, IdentityT<M, B> mb) =>
        ma.Bind(_ => mb);
 
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Monad
    //
    
    static K<IdentityT<M>, B> Monad<IdentityT<M>>.Bind<A, B>(K<IdentityT<M>, A> ma, Func<A, K<IdentityT<M>, B>> f) =>
        ma.As().Bind(f);

    static K<IdentityT<M>, B> Functor<IdentityT<M>>.Map<A, B>(Func<A, B> f, K<IdentityT<M>, A> ma) => 
        ma.As().Map(f);

    static K<IdentityT<M>, B> Applicative<IdentityT<M>>.Apply<A, B>(K<IdentityT<M>, Func<A, B>> mf, K<IdentityT<M>, A> ma) =>
        mf.As().Bind(f => ma.As().Map(f));

    static K<IdentityT<M>, B> Applicative<IdentityT<M>>.Action<A, B>(K<IdentityT<M>, A> ma, K<IdentityT<M>, B> mb) =>
        ma.As().Bind(_ => mb);

    static K<IdentityT<M>, A> MonadT<IdentityT<M>, M>.Lift<A>(K<M, A> ma) =>
        IdentityT<M, A>.Lift(ma);

    static K<IdentityT<M>, A> Maybe.MonadIO<IdentityT<M>>.LiftIO<A>(IO<A> ma) => 
        IdentityT<M, A>.Lift(M.LiftIO(ma));

    static K<IdentityT<M>, IO<A>> Maybe.MonadIO<IdentityT<M>>.ToIO<A>(K<IdentityT<M>, A> ma) =>
        new IdentityT<M, IO<A>>(ma.As().Value.ToIO()); 

    static K<IdentityT<M>, A> SemigroupK<IdentityT<M>>.Combine<A>(K<IdentityT<M>, A> ma, K<IdentityT<M>, A> mb) =>
        new IdentityT<M, A>(M.Combine(ma.As().Value, mb.As().Value));

    static K<IdentityT<M>, A> Choice<IdentityT<M>>.Choose<A>(K<IdentityT<M>, A> ma, K<IdentityT<M>, A> mb) =>
        new IdentityT<M, A>(M.Combine(ma.As().Value, mb.As().Value));

    static K<IdentityT<M>, A> Choice<IdentityT<M>>.Choose<A>(K<IdentityT<M>, A> ma, Func<K<IdentityT<M>, A>> mb) => 
        new IdentityT<M, A>(M.Combine(ma.As().Value, mb().As().Value));
}
