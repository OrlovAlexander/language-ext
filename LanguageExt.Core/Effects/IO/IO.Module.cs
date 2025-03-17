using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt.Common;
using LanguageExt.DSL;
using LanguageExt.Traits;
using static LanguageExt.Prelude;

namespace LanguageExt;

public partial class IO
{
    /// <summary>
    /// Lift a pure value into an IO computation
    /// </summary>
    /// <param name="value">value</param>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns>IO in a success state.  Always yields the lifted value.</returns>
    public static IO<A> pure<A>(A value) =>
        IO<A>.Pure(value);
    
    /// <summary>
    /// Lift a pure value into an IO computation
    /// </summary>
    /// <param name="value">value</param>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns>IO in a success state.  Always yields the lifted value.</returns>
    internal static IO<A> pureAsync<A>(Task<A> value) =>
        new IOPureAsync<A>(new ValueTask<A>(value));
    
    /// <summary>
    /// Lift a pure value into an IO computation
    /// </summary>
    /// <param name="value">value</param>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns>IO in a success state.  Always yields the lifted value.</returns>
    internal static IO<A> pureVAsync<A>(ValueTask<A> value) =>
        new IOPureAsync<A>(value);
    
    /// <summary>
    /// Put the IO into a failure state
    /// </summary>
    /// <param name="value">Error value</param>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns>IO in a failed state.  Always yields an error.</returns>
    public static IO<A> fail<A>(Error value) =>
        IO<A>.Fail(value);
    
    /// <summary>
    /// Put the IO into a failure state
    /// </summary>
    /// <param name="value">Error value</param>
    /// <typeparam name="A">Bound value type</typeparam>
    /// <returns>IO in a failed state.  Always yields an error.</returns>
    public static IO<A> fail<A>(string value) =>
        IO<A>.Fail(Error.New(value));
    
    /// <summary>
    /// Lift an action into the IO monad
    /// </summary>
    /// <param name="f">Action to lift</param>
    public static IO<Unit> lift(Action f) =>
        lift(() =>
             {
                 f();
                 return unit;
             });

    /// <summary>
    /// Creates a local cancellation environment
    /// </summary>
    /// <remarks>
    /// A local cancellation environment stops other IO computations, that rely on the same
    /// environmental cancellation token, from being taken down by a regional cancellation.
    ///
    /// If a `IO.cancel` is invoked locally then it will still create an exception that
    /// propagates upwards and so catching cancellations is still important. 
    /// </remarks>
    /// <param name="ma">Computation to run within the local context</param>
    /// <typeparam name="A">Bound value</typeparam>
    /// <returns>Result of the computation</returns>
    public static K<M, A> local<M, A>(K<M, A> ma) 
        where M : MonadIO<M> =>
        M.LocalIO(ma);

    /// <summary>
    /// Creates a local cancellation environment
    /// </summary>
    /// <remarks>
    /// A local cancellation environment stops other IO computations, that rely on the same
    /// environmental cancellation token, from being taken down by a regional cancellation.
    ///
    /// If a `IO.cancel` is invoked locally then it will still create an exception that
    /// propagates upwards and so catching cancellations is still important. 
    /// </remarks>
    /// <param name="ma">Computation to run within the local context</param>
    /// <typeparam name="A">Bound value</typeparam>
    /// <returns>Result of the computation</returns>
    public static IO<A> local<A>(K<IO, A> ma) => 
        ma.As().Local();
    
    public static IO<A> lift<A>(Either<Error, A> ma) =>
        ma switch
        {
            Either.Right<Error, A> (var r) => IO<A>.Pure(r),
            Either.Left<Error, A> (var l)  => IO<A>.Fail(l),
            _                              => IO<A>.Fail(Errors.Bottom)
        };
    
    public static IO<A> lift<A>(Fin<A> ma) =>
        lift(ma.ToEither());
    
    public static IO<A> lift<A>(Func<A> f) => 
        IO<A>.Lift(f);
    
    public static IO<A> lift<A>(Func<EnvIO, A> f) => 
        IO<A>.Lift(f);
    
    public static IO<A> lift<A>(Func<Fin<A>> f) => 
        IO<A>.Lift(() => f().ThrowIfFail());
    
    public static IO<A> lift<A>(Func<EnvIO, Fin<A>> f) => 
        IO<A>.Lift(e => f(e).ThrowIfFail());
    
    public static IO<A> lift<A>(Func<Either<Error, A>> f) => 
        IO<A>.Lift(() => f().ToFin().ThrowIfFail());
    
    public static IO<A> lift<A>(Func<EnvIO, Either<Error, A>> f) => 
        IO<A>.Lift(e => f(e).ToFin().ThrowIfFail());

    public static IO<A> liftAsync<A>(Func<Task<A>> f) => 
        IO<A>.LiftAsync(f);

    public static IO<A> liftAsync<A>(Func<EnvIO, Task<A>> f) => 
        IO<A>.LiftAsync(f);

    public static IO<A> liftVAsync<A>(Func<ValueTask<A>> f) => 
        IO<A>.LiftVAsync(f);

    public static IO<A> liftVAsync<A>(Func<EnvIO, ValueTask<A>> f) => 
        IO<A>.LiftVAsync(f);

    public static readonly IO<EnvIO> env = 
        lift(e => e);
    
    public static readonly IO<CancellationToken> token = 
        new IOToken<CancellationToken>(pure);
    
    public static readonly IO<CancellationTokenSource> source = 
        lift(e => e.Source);
    
    public static readonly IO<Option<SynchronizationContext>> syncContext = 
        lift(e => Optional(e.SyncContext));

    public static IO<A> empty<A>() =>
        IO<A>.Empty;

    public static IO<A> combine<A>(K<IO, A> ma, K<IO, A> mb) => 
        ma.As() | mb.As();

    /// <summary>
    /// Queue this IO operation to run on the thread-pool. 
    /// </summary>
    /// <param name="timeout">Maximum time that the forked IO operation can run for. `None` for no timeout.</param>
    /// <returns>Returns a `ForkIO` data-structure that contains two IO effects that can be used to either cancel
    /// the forked IO operation or to await the result of it.
    /// </returns>
    [Pure]
    [MethodImpl(Opt.Default)]
    public static K<M, B> mapIO<M, A, B>(K<M, A> ma, Func<IO<A>, IO<B>> f)
        where M : Maybe.MonadIO<M>, Monad<M> =>
        M.MapIO(ma, f);    

    /// <summary>
    /// Queue this IO operation to run on the thread-pool. 
    /// </summary>
    /// <param name="timeout">Maximum time that the forked IO operation can run for. `None` for no timeout.</param>
    /// <returns>Returns a `ForkIO` data-structure that contains two IO effects that can be used to either cancel
    /// the forked IO operation or to await the result of it.
    /// </returns>
    [Pure]
    [MethodImpl(Opt.Default)]
    public static K<M, ForkIO<A>> fork<M, A>(K<M, A> ma, Option<TimeSpan> timeout = default)
        where M : MonadIO<M>, Monad<M> =>
        M.ForkIO(ma, timeout);

    /// <summary>
    /// Yield the thread for the specified duration or until cancelled.
    /// </summary>
    /// <param name="duration">Amount of time to yield for</param>
    /// <returns>Unit</returns>
    [Pure]
    [MethodImpl(Opt.Default)]
    public static IO<Unit> yieldFor(Duration duration) =>
        Math.Abs(duration.Milliseconds) < 0.00000001
            ? unitIO
            : IO<Unit>.LiftAsync(e => yieldFor(duration, e.Token));

    /// <summary>
    /// Yield the thread for the specified duration or until cancelled.
    /// </summary>
    /// <param name="timeSpan">Amount of time to yield for</param>
    /// <returns>Unit</returns>
    [Pure]
    [MethodImpl(Opt.Default)]
    public static IO<Unit> yieldFor(TimeSpan timeSpan) =>
        Math.Abs(timeSpan.TotalMilliseconds) < 0.00000001
            ? unitIO
            : IO<Unit>.LiftAsync(e => yieldFor(timeSpan, e.Token));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Internal
    //
    
    /// <summary>
    /// Yields the thread for the `Duration` specified allowing for concurrency
    /// on the current thread 
    /// </summary>
    internal static async Task<Unit> yieldFor(Duration d, CancellationToken token)
    {
        await Task.Delay((TimeSpan)d, token);
        return unit;
    }
}
