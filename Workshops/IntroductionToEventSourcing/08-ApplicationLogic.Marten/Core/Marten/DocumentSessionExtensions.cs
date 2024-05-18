using ApplicationLogic.Marten.Core.Entities;
using ApplicationLogic.Marten.Core.Exceptions;
using Marten;

namespace ApplicationLogic.Marten.Core.Marten;

public static class DocumentSessionExtensions
{
    public static Task Add<T>(this IDocumentSession documentSession, Guid id, T aggregate, CancellationToken ct)
        where T : class, IAggregate
    {
        var _ = documentSession.Events.StartStream<T>(id, aggregate.DequeueUncommittedEvents());
        return documentSession.SaveChangesAsync(ct);
    }


    public static Task Add<T>(this IDocumentSession documentSession, Guid id, object[] events, CancellationToken ct)
        where T : class
    {
        var _ = documentSession.Events.StartStream<T>(id, events);
        return documentSession.SaveChangesAsync(ct);
    }

    public static async Task GetAndUpdate<T>(
        this IDocumentSession documentSession,
        Guid id,
        Func<T, object[]> handle,
        CancellationToken ct
    ) where T : class
    {
        var eventStream = await documentSession.Events.FetchForExclusiveWriting<T>(id, ct);
        var aggregate = eventStream.Aggregate ?? throw NotFoundException.For<T>(id);
        var events = handle(aggregate);
        eventStream.AppendMany(events);
        await documentSession.SaveChangesAsync(ct);
    } 

    public static async Task GetAndUpdate<T>(
        this IDocumentSession documentSession,
        Guid id,
        Action<T> handle,
        CancellationToken ct
    ) where T : class, IAggregate
    {
        var eventStream = await documentSession.Events.FetchForExclusiveWriting<T>(id, ct);
        var aggregate = eventStream.Aggregate ?? throw NotFoundException.For<T>(id);
        handle(aggregate);
        eventStream.AppendMany(aggregate.DequeueUncommittedEvents());
        await documentSession.SaveChangesAsync(ct);
    } 
}
