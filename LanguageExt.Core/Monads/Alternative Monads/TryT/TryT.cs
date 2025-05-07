﻿using System;
using LanguageExt.Common;
using LanguageExt.Traits;
using static LanguageExt.Prelude;

namespace LanguageExt;

/// <summary>
/// `TryT` monad transformer, which allows for an  optional `Error` result and catches exceptions,
/// converting them to `Error`. 
/// </summary>
/// <typeparam name="M">Given monad trait</typeparam>
/// <typeparam name="A">Bound value type</typeparam>
public record TryT<M, A>(K<M, Try<A>> runTry) : 
    Fallible<TryT<M, A>, TryT<M>, Error, A>
    where M : Monad<M>
{
    /// <summary>
    /// Lift a pure value into the monad-transformer
    /// </summary>
    /// <param name="value">Value to lift</param>
    /// <returns>`TryT`</returns>
    public static TryT<M, A> Succ(A value) =>
        Lift(M.Pure(value));
    
    /// <summary>
    /// Lift a fail value into the monad-transformer
    /// </summary>
    /// <param name="value">Value to lift</param>
    /// <returns>`TryT`</returns>
    public static TryT<M, A> Fail(Error value) =>
        Lift(Fin<A>.Fail(value));

    /// <summary>
    /// Lifts a given monad into the transformer
    /// </summary>
    /// <returns>`TryT`</returns>
    public static TryT<M, A> Lift(Pure<A> pure) =>
        Succ(pure.Value);

    /// <summary>
    /// Lifts a given monad into the transformer
    /// </summary>
    /// <returns>`TryT`</returns>
    public static TryT<M, A> Lift(Fin<A> result) =>
        new(M.Pure(Try.lift(result)));

    /// <summary>
    /// Lifts a given monad into the transformer
    /// </summary>
    /// <returns>`TryT`</returns>
    public static TryT<M, A> Lift(Func<Fin<A>> result) =>
        new(M.Pure(Try.lift(result)));

    /// <summary>
    /// Lifts a given monad into the transformer
    /// </summary>
    /// <param name="fail">Monad to lift</param>
    /// <returns>`TryT`</returns>
    public static TryT<M, A> Lift(Fail<Error> fail) =>
        Lift(Fin<A>.Fail(fail.Value));

    /// <summary>
    /// Lifts a given monad into the transformer
    /// </summary>
    /// <param name="monad">Monad to lift</param>
    /// <returns>`TryT`</returns>
    public static TryT<M, A> Lift(K<M, A> monad) =>
        new(M.Map(Try<A>.Succ, monad));

    /// <summary>
    /// Lifts a given monad into the transformer
    /// </summary>
    /// <param name="monad">Monad to lift</param>
    /// <returns>`TryT`</returns>
    public static TryT<M, A> LiftIO(IO<A> monad) =>
        new(M.LiftIOMaybe(monad.Try().Run()).Map(Try<A>.Lift));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Match
    //

    /// <summary>
    /// Match the bound value and return a result (which gets packages back up inside the inner monad)
    /// </summary>
    /// <param name="Succ">Success branch</param>
    /// <param name="Fail">Fail branch</param>
    /// <returns>Inner monad with the result of the `Succ` or `Fail` branches</returns>
    public K<M, B> Match<B>(Func<A, B> Succ, Func<Error, B> Fail) =>
        M.Map(mx => mx.Match(Succ, Fail), this.Run());

    /// <summary>
    /// Match the bound value and return a result (which gets packages back up inside the inner monad)
    /// </summary>
    /// <param name="Succ">Success branch</param>
    /// <param name="Fail">Fail branch</param>
    /// <returns>Inner monad with the result of the `Succ` or `Fail` branches</returns>
    public K<M, A> IfFail(Func<Error, A> Fail) =>
        Match(identity, Fail);

    /// <summary>
    /// Match the bound value and return a result (which gets packages back up inside the inner monad)
    /// </summary>
    /// <param name="Succ">Success branch</param>
    /// <param name="Fail">Fail branch</param>
    /// <returns>Inner monad with the result of the `Succ` or `Fail` branches</returns>
    public K<M, A> IfFailM(Func<Error, K<M, A>> Fail) =>
        Match(M.Pure, Fail).Flatten();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Map
    //

    /// <summary>
    /// Maps the bound monad
    /// </summary>
    /// <param name="f">Mapping function</param>
    /// <typeparam name="M1">Target monad type</typeparam>
    /// <typeparam name="B">Target bound value type</typeparam>
    /// <returns>Mapped monad</returns>
    public TryT<M1, B> MapT<M1, B>(Func<K<M, Fin<A>>, K<M1, Fin<B>>> f)
        where M1 : Monad<M1> =>
        new(f(this.Run()).Map(Try.lift));

    /// <summary>
    /// Maps the bound value
    /// </summary>
    /// <param name="f">Mapping function</param>
    /// <typeparam name="B">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, B> Map<B>(Func<A, B> f) =>
        new(M.Map(mx => mx.Map(f), runTry));

    /// <summary>
    /// Maps the bound value
    /// </summary>
    /// <param name="f">Mapping function</param>
    /// <returns>`TryT`</returns>
    public TryT<M, A> MapFail(Func<Error, Error> f) =>
        new(M.Map(mx => mx.MapFail(f), runTry));
    
    /// <summary>
    /// Maps the bound value
    /// </summary>
    /// <param name="f">Mapping transducer</param>
    /// <typeparam name="B">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, B> Select<B>(Func<A, B> f) =>
        new(M.Map(mx => mx.Map(f), runTry));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Bind
    //

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="f">Mapping function</param>
    /// <typeparam name="B">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, B> Bind<B>(Func<A, K<TryT<M>, B>> f) =>
        Map(f).Flatten();

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="f">Mapping function</param>
    /// <typeparam name="B">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, B> Bind<B>(Func<A, TryT<M, B>> f) =>
        Map(f).Flatten();

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="Succ">Success mapping function</param>
    /// <param name="Fail">Failure mapping function</param>
    /// <typeparam name="B">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, B> BiBind<B>(Func<A, TryT<M, B>> Succ, Func<Error, TryT<M, B>> Fail) =>
        new (runTry.Bind(
                 ta => ta.runTry()
                         .Match(Succ: Succ, Fail: Fail)
                         .runTry));

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="Fail">Failure mapping function</param>
    /// <typeparam name="B">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, A> BindFail(Func<Error, TryT<M, A>> Fail) =>
        BiBind(Succ, Fail);

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="f">Mapping function</param>
    /// <typeparam name="B">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, B> Bind<B>(Func<A, IO<B>> f) =>
        Bind(a => TryT<M, B>.LiftIO(f(a)));

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="f">Mapping function</param>
    /// <typeparam name="B">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, B> Bind<B>(Func<A, Pure<B>> f) =>
        Map(a => f(a).Value);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  SelectMany
    //

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="bind">Monadic bind function</param>
    /// <param name="project">Projection function</param>
    /// <typeparam name="B">Intermediate bound value type</typeparam>
    /// <typeparam name="C">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, C> SelectMany<B, C>(Func<A, K<TryT<M>, B>> bind, Func<A, B, C> project) =>
        SelectMany(x => bind(x).As(), project);

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="bind">Monadic bind function</param>
    /// <param name="project">Projection function</param>
    /// <typeparam name="B">Intermediate bound value type</typeparam>
    /// <typeparam name="C">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, C> SelectMany<B, C>(Func<A, TryT<M, B>> bind, Func<A, B, C> project) =>
        Bind(x => bind(x).Map(y => project(x, y)));

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="bind">Monadic bind function</param>
    /// <param name="project">Projection function</param>
    /// <typeparam name="B">Intermediate bound value type</typeparam>
    /// <typeparam name="C">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, C> SelectMany<B, C>(Func<A, K<M, B>> bind, Func<A, B, C> project) =>
        SelectMany(x => TryT<M, B>.Lift(bind(x)), project);

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="bind">Monadic bind function</param>
    /// <param name="project">Projection function</param>
    /// <typeparam name="B">Intermediate bound value type</typeparam>
    /// <typeparam name="C">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, C> SelectMany<B, C>(Func<A, Fin<B>> bind, Func<A, B, C> project) =>
        SelectMany(x => TryT<M, B>.Lift(bind(x)), project);

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="bind">Monadic bind function</param>
    /// <param name="project">Projection function</param>
    /// <typeparam name="B">Intermediate bound value type</typeparam>
    /// <typeparam name="C">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, C> SelectMany<B, C>(Func<A, Pure<B>> bind, Func<A, B, C> project) =>
        Map(x => project(x, bind(x).Value));

    /// <summary>
    /// Monad bind operation
    /// </summary>
    /// <param name="bind">Monadic bind function</param>
    /// <param name="project">Projection function</param>
    /// <typeparam name="B">Intermediate bound value type</typeparam>
    /// <typeparam name="C">Target bound value type</typeparam>
    /// <returns>`TryT`</returns>
    public TryT<M, C> SelectMany<B, C>(Func<A, IO<B>> bind, Func<A, B, C> project) =>
        SelectMany(x => M.LiftIOMaybe(bind(x)), project);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Operators
    //

    /// <summary>
    /// Sequentially compose two actions, discarding any value produced by the first, like sequencing operators (such
    /// as the semicolon) in C#.
    /// </summary>
    /// <param name="lhs">First action to run</param>
    /// <param name="rhs">Second action to run</param>
    /// <returns>Result of the second action</returns>
    public static TryT<M, A> operator >> (TryT<M, A> lhs, TryT<M, A> rhs) =>
        lhs.Bind(_ => rhs);
    
    /// <summary>
    /// Sequentially compose two actions, discarding any value produced by the first, like sequencing operators (such
    /// as the semicolon) in C#.
    /// </summary>
    /// <param name="lhs">First action to run</param>
    /// <param name="rhs">Second action to run</param>
    /// <returns>Result of the second action</returns>
    public static TryT<M, A> operator >> (TryT<M, A> lhs, K<TryT<M>, A> rhs) =>
        lhs.Bind(_ => rhs);

    /// <summary>
    /// Sequentially compose two actions.  The second action is a unit returning action, so the result of the
    /// first action is propagated. 
    /// </summary>
    /// <param name="lhs">First action to run</param>
    /// <param name="rhs">Second action to run</param>
    /// <returns>Result of the first action</returns>
    public static TryT<M, A> operator >> (TryT<M, A> lhs, TryT<M, Unit> rhs) =>
        lhs.Bind(x => rhs.Map(_ => x));
    
    /// <summary>
    /// Sequentially compose two actions.  The second action is a unit returning action, so the result of the
    /// first action is propagated. 
    /// </summary>
    /// <param name="lhs">First action to run</param>
    /// <param name="rhs">Second action to run</param>
    /// <returns>Result of the first action</returns>
    public static TryT<M, A> operator >> (TryT<M, A> lhs, K<TryT<M>, Unit> rhs) =>
        lhs.Bind(x => rhs.Map(_ => x));
    
    public static implicit operator TryT<M, A>(Pure<A> ma) =>
        Succ(ma.Value);
    
    public static implicit operator TryT<M, A>(Error ma) =>
        Lift(Fin<A>.Fail(ma));
    
    public static implicit operator TryT<M, A>(Fail<Error> ma) =>
        Lift(Fin<A>.Fail(ma.Value));
    
    public static implicit operator TryT<M, A>(Fail<Exception> ma) =>
        Lift(Fin<A>.Fail(ma.Value));
    
    public static implicit operator TryT<M, A>(IO<A> ma) =>
        LiftIO(ma);
    
    public static TryT<M, A> operator +(TryT<M, A> lhs, TryT<M, A> rhs) =>
        lhs.Combine(rhs);

    public static TryT<M, A> operator +(K<TryT<M>, A> lhs, TryT<M, A> rhs) =>
        lhs.As().Combine(rhs);

    public static TryT<M, A> operator +(TryT<M, A> lhs, K<TryT<M>, A> rhs) =>
        lhs.Combine(rhs.As());

    public static TryT<M, A> operator +(TryT<M, A> ma, Pure<A> mb) =>
        ma.Combine(mb);

    public static TryT<M, A> operator +(TryT<M, A> ma, Fail<Error> mb) =>
        ma.Combine(mb);

    public static TryT<M, A> operator +(TryT<M, A> ma, Fail<Exception> mb) =>
        ma.Combine(mb);
    
    public static TryT<M, A> operator |(TryT<M, A> lhs, TryT<M, A> rhs) =>
        lhs.Choose(rhs).As();

    public static TryT<M, A> operator |(K<TryT<M>, A> lhs, TryT<M, A> rhs) =>
        lhs.As().Choose(rhs).As();

    public static TryT<M, A> operator |(TryT<M, A> lhs, K<TryT<M>, A> rhs) =>
        lhs.Choose(rhs.As()).As();

    public static TryT<M, A> operator |(TryT<M, A> ma, Pure<A> mb) =>
        ma.Choose(Succ(mb.Value)).As();

    public static TryT<M, A> operator |(TryT<M, A> ma, Fail<Error> mb) =>
        ma.Choose(Fail(mb.Value)).As();

    public static TryT<M, A> operator |(TryT<M, A> ma, Fail<Exception> mb) =>
        ma.Choose(Fail(mb.Value)).As();

    public static TryT<M, A> operator |(TryT<M, A> ma, CatchM<Error, TryT<M>, A> mb) =>
        (ma.Kind() | mb).As();

    public TryT<M, A> Combine(TryT<M, A> rhs) =>
        this.Kind().Combine(rhs).As();
}
