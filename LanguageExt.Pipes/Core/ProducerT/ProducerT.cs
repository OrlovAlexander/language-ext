using System;
using System.Diagnostics.Contracts;
using LanguageExt.Traits;

namespace LanguageExt.Pipes;

/// <summary>
/// `ProducerT` streaming producer monad-transformer instance
/// </summary>
public readonly record struct ProducerT<OUT, M, A>(PipeT<Unit, OUT, M, A> Proxy) : K<ProducerT<OUT, M>, A>
    where M : MonadIO<M>
{
    [Pure]
    public ProducerT<OUT1, M, A> Compose<OUT1>(PipeT<OUT, OUT1, M, A> rhs) =>
        Proxy.Compose(rhs).ToProducer();

    [Pure]
    public EffectT<M, A> Compose(ConsumerT<OUT, M, A> rhs) =>
        Proxy.Compose(rhs.Proxy).ToEffect();

    [Pure]
    public static EffectT<M, A> operator | (ProducerT<OUT, M, A> lhs, ConsumerT<OUT, M, A> rhs) =>
        lhs.Proxy.Compose(rhs.Proxy).ToEffect();
    
    [Pure]
    public ProducerT<OUT, M, B> Map<B>(Func<A, B> f) =>
        Proxy.Map(f);
    
    [Pure]
    public ProducerT<OUT, M, B> MapM<B>(Func<K<M, A>, K<M, B>> f) =>
        Proxy.MapM(f);

    [Pure]
    public ProducerT<OUT, M, B> MapIO<B>(Func<IO<A>, IO<B>> f) =>
        Proxy.MapIO(f);

    [Pure]
    public ProducerT<OUT, M, B> ApplyBack<B>(ProducerT<OUT, M, Func<A, B>> ff) =>
        Proxy.ApplyBack(ff.Proxy);
    
    [Pure]
    public ProducerT<OUT, M, B> Action<B>(ProducerT<OUT, M, B> fb) =>
        Proxy.Action(fb.Proxy);
    
    [Pure]
    public ProducerT<OUT, M, B> Bind<B>(Func<A, ProducerT<OUT, M, B>> f) =>
        Proxy.Bind(x => f(x).Proxy);
    
    [Pure]
    public ProducerT<OUT, M, B> Bind<B>(Func<A, K<M, B>> f) => 
        Proxy.Bind(f); 
    
    [Pure]
    public ProducerT<OUT, M, B> Bind<B>(Func<A, IO<B>> f) => 
        Proxy.Bind(f); 
       
    [Pure]
    public ProducerT<OUT, M, B> Bind<B>(Func<A, Pure<B>> f) =>
        Proxy.Bind(f);
   
    [Pure]
    public ProducerT<OUT, M, B> Bind<B>(Func<A, Lift<B>> f) =>
        Proxy.Bind(f);

    [Pure]
    public ProducerT<OUT, M, B> Select<B>(Func<A, B> f) =>
        Map(f);
   
    [Pure]
    public ProducerT<OUT, M, C> SelectMany<B, C>(Func<A, ProducerT<OUT, M, B>> f, Func<A, B, C> g) =>
        Proxy.SelectMany(x => f(x).Proxy, g);
   
    [Pure]
    public ProducerT<OUT, M, C> SelectMany<B, C>(Func<A, K<M, B>> f, Func<A, B, C> g) =>
        Proxy.SelectMany(f, g);
   
    [Pure]
    public ProducerT<OUT, M, C> SelectMany<B, C>(Func<A, IO<B>> f, Func<A, B, C> g) =>
        Proxy.SelectMany(f, g);
   
    [Pure]
    public ProducerT<OUT, M, C> SelectMany<B, C>(Func<A, Pure<B>> f, Func<A, B, C> g) =>
        Proxy.SelectMany(f, g);
   
    [Pure]
    public ProducerT<OUT, M, C> SelectMany<B, C>(Func<A, Lift<B>> f, Func<A, B, C> g) =>
        Proxy.SelectMany(f, g);
                
    /// <summary>
    /// Fold the given pipe until the `Schedule` completes.
    /// Once complete, the pipe yields the aggregated value downstream.
    /// </summary>
    /// <param name="Time">Schedule to run each item</param>
    /// <param name="Fold">Fold function</param>
    /// <param name="Init">Initial state</param>
    /// <typeparam name="OUT">Stream value to produce</typeparam>
    /// <typeparam name="M">Lifted monad type</typeparam>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns></returns>
    public ProducerT<OUT, M, Unit> Fold(
        Schedule Time,
        Func<OUT, A, OUT> Fold, 
        OUT Init) =>
        PipeT.fold(Time, Fold, Init, Proxy);

    /// <summary>
    /// Fold the given pipe until the predicate is `true`.  Once `true` the pipe yields the
    /// aggregated value downstream.
    /// </summary>
    /// <param name="Fold">Fold function</param>
    /// <param name="Pred">Until predicate</param>
    /// <param name="Init">Initial state</param>
    /// <typeparam name="OUT">Stream value to produce</typeparam>
    /// <typeparam name="M">Lifted monad type</typeparam>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns></returns>
    public ProducerT<OUT, M, Unit> FoldUntil(
        Func<OUT, A, OUT> Fold, 
        Func<(OUT State, A Value), bool> Pred, 
        OUT Init) =>
        PipeT.foldUntil(Fold, Pred, Init, Proxy);
        
    /// <summary>
    /// Fold the given pipe until the predicate is `true` or the `Schedule` completes.
    /// Once `true`, or completed, the pipe yields the aggregated value downstream.
    /// </summary>
    /// <param name="Time">Schedule to run each item</param>
    /// <param name="Fold">Fold function</param>
    /// <param name="Pred">Until predicate</param>
    /// <param name="Init">Initial state</param>
    /// <typeparam name="OUT">Stream value to produce</typeparam>
    /// <typeparam name="M">Lifted monad type</typeparam>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns></returns>
    public ProducerT<OUT, M, Unit> FoldUntil(
        Schedule Time,
        Func<OUT, A, OUT> Fold, 
        Func<(OUT State, A Value), bool> Pred, 
        OUT Init) =>
        PipeT.foldUntil(Time, Fold, Pred, Init, Proxy);
        
    /// <summary>
    /// Fold the given pipe while the predicate is `true`.  Once `false` the pipe yields the
    /// aggregated value downstream.
    /// </summary>
    /// <param name="Fold">Fold function</param>
    /// <param name="Pred">Until predicate</param>
    /// <param name="Init">Initial state</param>
    /// <typeparam name="OUT">Stream value to produce</typeparam>
    /// <typeparam name="M">Lifted monad type</typeparam>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns></returns>
    public ProducerT<OUT, M, Unit> FoldWhile(
        Func<OUT, A, OUT> Fold, 
        Func<(OUT State, A Value), bool> Pred, 
        OUT Init) =>
        PipeT.foldWhile(Fold, Pred, Init, Proxy);
        
    /// <summary>
    /// Fold the given pipe while the predicate is `true` or the `Schedule` completes.
    /// Once `false`, or completed, the pipe yields the aggregated value downstream.
    /// </summary>
    /// <param name="Time">Schedule to run each item</param>
    /// <param name="Fold">Fold function</param>
    /// <param name="Pred">Until predicate</param>
    /// <param name="Init">Initial state</param>
    /// <typeparam name="OUT">Stream value to produce</typeparam>
    /// <typeparam name="M">Lifted monad type</typeparam>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns></returns>
    public ProducerT<OUT, M, Unit> FoldWhile(
        Schedule Time,
        Func<OUT, A, OUT> Fold, 
        Func<(OUT State, A Value), bool> Pred, 
        OUT Init) =>
        PipeT.foldWhile(Time, Fold, Pred, Init, Proxy);   
    
    [Pure]
    public static implicit operator ProducerT<OUT, M, A>(PipeT<Unit, OUT, M, A> pipe) =>
        pipe.ToProducer();
    
    [Pure]
    public static implicit operator ProducerT<OUT, M, A>(Pure<A> rhs) =>
        ProducerT.pure<OUT, M, A>(rhs.Value);

    [Pure]
    internal K<M, A> Run() =>
        Proxy.Run();
}
