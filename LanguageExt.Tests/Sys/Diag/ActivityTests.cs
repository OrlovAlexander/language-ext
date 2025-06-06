﻿using Xunit;
using System;
using System.Threading.Tasks;
using LanguageExt.Sys.Diag;
using LanguageExt.Sys.Test;
using System.Diagnostics;
using FluentAssertions;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;

namespace LanguageExt.Tests.Sys.Diag;

using A = Activity<Runtime>;

public static class ActivityTests
{
    static readonly ActivitySource Source = new(nameof(ActivityTests));
    
    static ActivityTests() =>
        ActivitySource.AddActivityListener(
            new ActivityListener
            {
                ShouldListenTo = source => source.Name == nameof(ActivityTests),
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                             ActivitySamplingResult.AllData
            });

    static T ArrangeAndAct<T>(this K<Eff<Runtime>, T> effect)
    {
        using var rt = Runtime.New();
        var rt1 = rt with { Env = rt.Env with { Activity = new ActivityEnv(Source, default, default) } };
        return effect.As().Run(rt1, EnvIO.New()).ThrowIfFail();
    }

    [Fact(DisplayName = "An activity span can be created and effect run within")]
    public static void Case1() =>
        A.span("test", SuccessEff<Runtime, string>("a")).ArrangeAndAct().Should().Be("a");

    [Fact(DisplayName = "The trace state can be read")]
    public static void Case2()
    {
        var result = A.span("test", from _ in A.setTraceState("test") from r in A.traceState select r)
            .ArrangeAndAct();
        result.IsSome.Should().BeTrue();
        result.Case.Should().Be("test");
    }

    [Fact(DisplayName = "The trace id state can be read")]
    public static void Case3()
    {
        var result = A.span("test", A.traceId)
            .ArrangeAndAct();
        result.IsSome.Should().BeTrue();
        result.Case.Should().NotBeNull();
    }

    [Fact(DisplayName = "The baggage can be set and read")]
    public static void Case4()
    {
        var baggage = A.span(
                "test",
                from _ in A.addBaggage("a", "b")
                from result in A.baggage
                select result
            )
            .ArrangeAndAct();
        
        Assert.True(baggage.Find("a").ForAll(v => v == "b"));
    }

    [Fact(DisplayName = "The tags can be set and read")]
    public static void Case5()
    {
        var baggage = A.span(
                "test",
                from _1 in A.addTag("a", "b")
                from result in A.tags
                select result)
            .ArrangeAndAct();
        
        Assert.True(baggage.Find("a").ForAll(v => v == "b"));
    }

    [Fact(DisplayName = "The tag objects can be read")]
    public static void Case6()
    {
        var baggage = A.span(
                "test",
                from _ in A.addTag("a", 1)
                from result in A.tagObjects
                select result)
            .ArrangeAndAct();
        
        Assert.True(baggage.Find("a").ForAll(v => v is 1));
    }

    [Fact(DisplayName = "The context can be read")]
    public static void Case7()
    {
        var result = A.span("test", A.context).ArrangeAndAct();
        result.IsSome.Should().BeTrue();
    }

    [Fact(DisplayName = "The duration can be read")]
    public static void Case8()
    {
        var result = A.span("test", A.duration).ArrangeAndAct();
        result.IsSome.Should().BeTrue();
    }

    static readonly ActivityEvent TestEvent = new("some event");

    [Fact(DisplayName = "The events can be read")]
    public static void Case9()
    {
        var events = A.span("test", from _ in A.addEvent(TestEvent) from result in A.events select result)
            .ArrangeAndAct();
        Assert.False(events.IsEmpty);
        events.AsIterable().Head().ValueUnsafe().Should().Be(TestEvent);
    }

    [Fact(DisplayName = "The id can be read")]
    public static void Case10()
    {
        var result = A.span("test", A.id).ArrangeAndAct();
        result.IsSome.Should().BeTrue();
    }

    [Fact(DisplayName = "The kind can be read")]
    public static void Case11()
    {
        var result = A.span("test", ActivityKind.Consumer, A.kind)
            .ArrangeAndAct();
        result.IsSome.Should().BeTrue();
        result.Case.Should().Be(ActivityKind.Consumer);
    }

    [Fact(DisplayName = "The links can be read")]
    public static void Case12()
    {
        var r = A.span(
                "a",
                from co in A.context.As()
                from context in co.ToEff((Error)"context should be set")
                from result in A.span(
                    "b",
                    ActivityKind.Consumer,
                    context,
                    default,
                    Seq(new ActivityLink(context)),
                    DateTimeOffset.Now,
                    A.links
                )
                select result
            )
            .ArrangeAndAct();
        Assert.False(r.IsEmpty);
    }

    [Fact(DisplayName = "The current can be read and is not None in a span")]
    public static void Case13()
    {
        var result = A.span("test", A.current).ArrangeAndAct();
        result.IsSome.Should().BeTrue();
    }

    [Fact(DisplayName = "The current can be read and is None outside a span")]
    public static void Case14()
    {
        var result = A.current.ArrangeAndAct();
        result.IsNone.Should().BeTrue();
    }

    [Fact(DisplayName = "The parent id can be read and is not None in a nested span")]
    public static void Case15()
    {
        var id = A.span(
                "a",
                from co in A.context.As()
                from context in co.ToEff((Error)"context should be set")
                from result in A.span(
                    "b",
                    ActivityKind.Client,
                    context,
                    default,
                    default,
                    default,
                    A.parentId
                )
                select result
            )
            .ArrangeAndAct();
        id.IsSome.Should().BeTrue();
    }

    [Fact(DisplayName = "The parent id can be read and is None in a single level span")]
    public static void Case16()
    {
        var id = A.span("a", A.parentId).ArrangeAndAct();
        id.IsNone.Should().BeTrue();
    }

    [Fact(DisplayName = "The parent span id can be read and is not None in a nested span")]
    public static void Case17()
    {
        var id = A.span(
                "a",
                from co in A.context.As()
                from context in co.ToEff((Error)"context should be set")
                from result in A.span(
                    "b",
                    ActivityKind.Client,
                    context,
                    default,
                    default,
                    default,
                    A.parentSpanId
                )
                select result
            )
            .ArrangeAndAct();
        id.IsSome.Should().BeTrue();
    }

    [Fact(DisplayName = "The recorded can be read")]
    public static void Case18()
    {
        var result = A.span("test", A.recorded).ArrangeAndAct();
        result.IsSome.Should().BeTrue();
    }

    [Fact(DisplayName = "The display name can be read")]
    public static void Case19()
    {
        var result = A.span("test", A.displayName).ArrangeAndAct();
        result.IsSome.Should().BeTrue();
    }

    [Fact(DisplayName = "The operation name can be read")]
    public static void Case20()
    {
        var result = A.span("test", A.operationName)
            .ArrangeAndAct();
        result.IsSome.Should().BeTrue();
    }

    [Fact(DisplayName = "The root id can be read")]
    public static void Case21()
    {
        var result = A.span("test", A.rootId).ArrangeAndAct();
        result.IsSome.Should().BeTrue();
    }

    [Fact(DisplayName = "The span id can be read")]
    public static void Case22()
    {
        var result = A.span("test", A.spanId).ArrangeAndAct();
        result.IsSome.Should().BeTrue();
    }

    [Fact(DisplayName = "The state time can be read")]
    public static void Case23()
    {
        var result = A.span("test", A.startTimeUTC)
            .ArrangeAndAct();
        result.IsSome.Should().BeTrue();
    }

    [Fact(DisplayName = "Test span overload 1")]
    public static void Case24()
    {
        var (kind, tags) = A.span(
                "test",
                ActivityKind.Client,
                HashMap(("1", "a" as object), ("2", "b")),
                A.kind.Zip(A.tags))
            .ArrangeAndAct();
        kind.IsSome.Should().BeTrue();
        kind.Case.Should().Be(ActivityKind.Client);
        
        Assert.True(tags.Find("1").ForAll(v => v is "a"));
        Assert.True(tags.Find("2").ForAll(v => v is "b"));
    }

    [Fact(DisplayName = "Test span overload 2")]
    public static void Case25() =>
        A.span("test", SuccessEff<Runtime, Unit>(unit)).ArrangeAndAct();

    [Fact(DisplayName = "Test span overload 3")]
    public static void Case26()
    {
        var result = A.span("test", ActivityKind.Consumer, A.kind.As()).ArrangeAndAct();
        result.Case.Should().Be(ActivityKind.Consumer);
    }

    [Fact(DisplayName = "Test span overload 4")]
    public static void Case27()
    {
        var (kind, tags) = A.span("test",
                                  ActivityKind.Client,
                                  HashMap(("1", "a" as object), ("2", "b")),
                                  A.kind.Zip(A.tags))
                            .ArrangeAndAct();
        
        Assert.True(kind.IsSome);
        Assert.Equal(ActivityKind.Client, kind.Case);
       
        Assert.True(tags.Find("1").ForAll(v => v is "a"));
        Assert.True(tags.Find("2").ForAll(v => v is "b"));
    }

    [Fact(DisplayName = "Test span overload 5")]
    public static void Case28()
    {
        var (kind, tags) = A.span(
                "A",
                from co in A.context.As()
                from context in co.ToEff((Error)"context should be set")
                from result in A.span(
                    "B",
                    ActivityKind.Client,
                    context,
                    HashMap(("1", "a" as object), ("2", "b")),
                    Seq<ActivityLink>.Empty,
                    DateTimeOffset.Now,
                    A.kind.Zip(A.tags))
                select result
            )
            .ArrangeAndAct();
        kind.IsSome.Should().BeTrue();
        kind.Case.Should().Be(ActivityKind.Client);
        
        Assert.True(tags.Find("1").ForAll(v => v is "a"));
        Assert.True(tags.Find("2").ForAll(v => v is "b"));
    }
}
