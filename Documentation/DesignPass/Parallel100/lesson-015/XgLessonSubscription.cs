using System;
using LingGuangV05.XingGuang;

namespace LingGuangV05.Desktop.XingGuang
{
    // Main-thread only. Bind(null) suspends; Dispose permanently ends ownership.
    // Does not implement pulse rendering or decide which successful lessons pulse.
    public sealed class XgLessonSubscription : IDisposable
    {
        XgSim source;
        Action<XgCard, XgAnswer> delivery;
        Action<XgCard, XgAnswer> receive;
        bool disposed;

        public XgLessonSubscription(Action<XgCard, XgAnswer> receive)
        {
            if (receive == null) throw new ArgumentNullException("receive");
            this.receive = receive;
        }

        public void Bind(XgSim next)
        {
            if (disposed) throw new ObjectDisposedException("XgLessonSubscription");
            if (ReferenceEquals(source, next)) return;
            Detach();
            if (next == null) return;
            source = next;
            Action<XgCard, XgAnswer> ticket = null;
            ticket = (card, answer) =>
            {
                // Removing a handler cannot cancel a delegate already captured by
                // an in-progress event invocation. Each binding needs its own ticket,
                // including A -> B -> A and disable -> re-enable on the same Sim.
                if (disposed || !ReferenceEquals(source, next) ||
                    !ReferenceEquals(delivery, ticket)) return;
                receive(card, answer);
            };
            delivery = ticket;
            source.LessonTaught += delivery;
        }

        void Detach()
        {
            var previous = source;
            var previousDelivery = delivery;
            source = null;
            delivery = null;
            if (previous != null) previous.LessonTaught -= previousDelivery;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Detach();
            receive = null;
        }
    }
}
