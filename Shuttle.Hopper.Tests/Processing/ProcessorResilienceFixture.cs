using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shuttle.Reflection;
using Shuttle.Threading;

namespace Shuttle.Hopper.Tests;

/// <summary>
///     Guards against processing "dying" on one or more processor threads.  The inbox uses a fixed pool of threads and
///     there is a single deferred message thread; neither is ever re-created, so a thread that exits its work loop is
///     orphaned for the lifetime of the endpoint.  Once every inbox thread has been orphaned all message processing
///     stops even though the endpoint still reports itself as started.
/// </summary>
[TestFixture]
public class ProcessorResilienceFixture
{
    private const string DeferredServiceKey = "DeferredMessageProcessor";
    private const string DeferredUri = "resilience://resilience/deferred";
    private const string ErrorUri = "resilience://resilience/error";
    private const string InboxServiceKey = "InboxProcessor";
    private const string WorkUri = "resilience://resilience/work";

    private const int MaximumFailureCount = 3;
    private const int ThreadCount = 5;

    [Test]
    public async Task Should_not_orphan_processor_threads_when_a_handler_throws_an_operation_canceled_exception()
    {
        // An `OperationCanceledException` (such as the `TaskCanceledException` raised by an `HttpClient` timeout, or
        // by a `Task.Delay` on an unrelated token) that has nothing to do with the endpoint being stopped.
        var tracker = new ResilienceTracker();

        await using var context = await ResilienceContext.StartAsync(async message =>
        {
            tracker.Attempted(message);

            await Task.CompletedTask;

            if (message.Behaviour.Equals(ResilienceBehaviour.Cancel))
            {
                throw new TaskCanceledException("[simulated http client timeout]");
            }

            tracker.Handled(message);
        });

        const int messageCount = 10;

        await context.SendAsync(messageCount, ResilienceBehaviour.Cancel);

        // Each message should fail `MaximumFailureCount` times and then be sent to the error transport.
        var completed = await context.WaitAsync(() => context.ErrorTransport.SendCount >= messageCount);

        Assert.Multiple(() =>
        {
            Assert.That(context.OrphanedThreads, Is.Empty, $"One or more processor threads exited while the endpoint was still running:{Environment.NewLine}{context.OrphanedThreadsText}");
            Assert.That(completed, Is.True, $"Processing stalled: attempts = {tracker.TotalAttempts} / expected = {messageCount * MaximumFailureCount}; error transport = {context.ErrorTransport.SendCount} / expected = {messageCount}.");
            Assert.That(tracker.TotalAttempts, Is.EqualTo(messageCount * MaximumFailureCount));
            Assert.That(context.WorkTransport.UnacknowledgedCount, Is.Zero, "The inbox work transport has messages that were neither acknowledged nor released.");
        });
    }

    [Test]
    public async Task Should_process_all_messages_when_using_multiple_threads_with_retries_and_deferred_messages()
    {
        var tracker = new ResilienceTracker();

        await using var context = await ResilienceContext.StartAsync(async message =>
        {
            var attempt = tracker.Attempted(message);

            await Task.CompletedTask;

            switch (message.Behaviour)
            {
                case ResilienceBehaviour.AlwaysFail:
                {
                    throw new InvalidOperationException($"[simulated permanent failure] : id = '{message.Id}' / attempt = {attempt}");
                }
                case ResilienceBehaviour.RetryThenSucceed when attempt < MaximumFailureCount:
                {
                    throw new InvalidOperationException($"[simulated transient failure] : id = '{message.Id}' / attempt = {attempt}");
                }
            }

            tracker.Handled(message);
        });

        const int messageCount = 8;

        // Handled directly off the inbox work transport.
        await context.SendAsync(messageCount, ResilienceBehaviour.Succeed);

        // Deferred, therefore routed to the deferred transport and returned by the deferred message processor.
        await context.SendAsync(messageCount, ResilienceBehaviour.Succeed, builder => builder.DeferFor(TimeSpan.FromMilliseconds(Random.Shared.Next(100, 500))));

        // Fails, is retried via the work transport (with an `IgnoreUntil`, so also via the deferred transport) and
        // then succeeds on the last permissible attempt.
        await context.SendAsync(messageCount, ResilienceBehaviour.RetryThenSucceed);

        // Fails every time and should end up on the error transport.
        await context.SendAsync(messageCount, ResilienceBehaviour.AlwaysFail);

        var completed = await context.WaitAsync(() => tracker.HandledCount >= messageCount * 3 && context.ErrorTransport.SendCount >= messageCount);

        Assert.Multiple(() =>
        {
            Assert.That(context.OrphanedThreads, Is.Empty, $"One or more processor threads exited while the endpoint was still running:{Environment.NewLine}{context.OrphanedThreadsText}");
            Assert.That(completed, Is.True, $"Processing stalled: handled = {tracker.HandledCount} / expected = {messageCount * 3}; error transport = {context.ErrorTransport.SendCount} / expected = {messageCount}.");
            Assert.That(context.ProcessorExceptions, Is.Empty, $"The processor threads reported exceptions that the pipelines should have handled:{Environment.NewLine}{context.ProcessorExceptionsText}");
            Assert.That(context.WorkTransport.Count, Is.Zero, "The inbox work transport still contains messages.");
            Assert.That(context.WorkTransport.UnacknowledgedCount, Is.Zero, "The inbox work transport still has unacknowledged messages.");
            Assert.That(context.DeferredTransport.Count, Is.Zero, "The deferred transport still contains messages.");
            Assert.That(context.DeferredTransport.UnacknowledgedCount, Is.Zero, "The deferred transport still has unacknowledged messages.");
            Assert.That(context.InboxThreadIds, Has.Count.EqualTo(ThreadCount), "Not every inbox thread took part in the processing.");
        });
    }

    [Test]
    public async Task Should_recover_from_a_transient_deferred_transport_failure()
    {
        // A transport fault within the deferred message pipeline has to be handled by that pipeline: the deferred
        // message has to be released, the processor context has to be given a result (so that the reset interval is
        // applied rather than the thread spinning without any backoff) and the fault may not reach the thread.
        var tracker = new ResilienceTracker();

        await using var context = await ResilienceContext.StartAsync(async message =>
        {
            tracker.Attempted(message);

            await Task.CompletedTask;

            tracker.Handled(message);
        });

        const int faultCount = 5;

        context.DeferredTransport.FailNextReceives(faultCount);

        await context.SendAsync(1, ResilienceBehaviour.Succeed, builder => builder.DeferFor(TimeSpan.FromMilliseconds(200)));

        var completed = await context.WaitAsync(() => tracker.HandledCount >= 1);

        // Comfortably above what the configured idle duration and reset interval allow, yet orders of magnitude below
        // an un-throttled loop, which manages tens of thousands of iterations per second.
        const int maximumDeferredExecutions = 500;

        Assert.Multiple(() =>
        {
            Assert.That(context.OrphanedThreads, Is.Empty, $"One or more processor threads exited while the endpoint was still running:{Environment.NewLine}{context.OrphanedThreadsText}");
            Assert.That(completed, Is.True, "The deferred message was never returned to the inbox work transport.");
            Assert.That(context.ProcessorExceptions, Is.Empty, $"The {faultCount} injected transport faults should have been handled by the deferred message pipeline rather than reaching the processor thread:{Environment.NewLine}{context.ProcessorExceptionsText}");
            Assert.That(context.DeferredTransport.UnacknowledgedCount, Is.Zero, "The deferred transport still has unacknowledged messages.");
            Assert.That(context.DeferredExecutionCount, Is.LessThan(maximumDeferredExecutions), $"The deferred message processor ran {context.DeferredExecutionCount} times, so no backoff is being applied between iterations.");
        });
    }

    [Test]
    public async Task Should_stop_the_deferred_message_thread_when_the_endpoint_is_stopped()
    {
        var tracker = new ResilienceTracker();

        var context = await ResilienceContext.StartAsync(async message =>
        {
            tracker.Attempted(message);

            await Task.CompletedTask;

            tracker.Handled(message);
        });

        await context.SendAsync(1, ResilienceBehaviour.Succeed);

        var completed = await context.WaitAsync(() => tracker.HandledCount >= 1);

        await context.DisposeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True, "The message was never handled.");
            Assert.That(context.StoppedThreads, Does.Contain(DeferredServiceKey), "The deferred message thread was never stopped; it was only cancelled and then abandoned.");
            Assert.That(context.StoppedThreads.Count(serviceKey => serviceKey.Equals(InboxServiceKey)), Is.EqualTo(ThreadCount), "Not every inbox thread was stopped.");
        });
    }

    /// <summary>
    ///     Records what the processor threads are doing so that a test can tell the difference between "still working"
    ///     and "the thread is gone".
    /// </summary>
    private sealed class ResilienceMonitor
    {
        private readonly ConcurrentDictionary<int, byte> _inboxThreadIds = new();
        private readonly ConcurrentBag<string> _orphanedThreads = [];
        private readonly ConcurrentBag<string> _processorExceptions = [];
        private readonly ConcurrentBag<string> _stoppedThreads = [];

        private int _deferredExecutionCount;
        private volatile bool _stopping;

        public int DeferredExecutionCount => Volatile.Read(ref _deferredExecutionCount);
        public IReadOnlyCollection<int> InboxThreadIds => _inboxThreadIds.Keys.ToList();
        public IReadOnlyCollection<string> OrphanedThreads => _orphanedThreads.ToList();
        public IReadOnlyCollection<string> ProcessorExceptions => _processorExceptions.ToList();

        /// <summary>
        ///     The service keys of the threads that were explicitly stopped, which only happens by way of
        ///     `ProcessorThread.StopAsync`.  A thread that is merely cancelled never appears here.
        /// </summary>
        public IReadOnlyCollection<string> StoppedThreads => _stoppedThreads.ToList();

        public void ThreadStopped(string serviceKey)
        {
            _stoppedThreads.Add(serviceKey);
        }

        public void Executing(string serviceKey, int managedThreadId)
        {
            if (serviceKey.Equals(InboxServiceKey))
            {
                _inboxThreadIds.TryAdd(managedThreadId, 0);
            }

            if (serviceKey.Equals(DeferredServiceKey))
            {
                Interlocked.Increment(ref _deferredExecutionCount);
            }
        }

        public void ProcessorException(string serviceKey, Exception? exception)
        {
            if (_stopping)
            {
                return;
            }

            _processorExceptions.Add($"[{serviceKey}] : {exception?.AllMessages() ?? "(none)"}");
        }

        public void Stopping()
        {
            _stopping = true;
        }

        public void ThreadExited(string serviceKey, int managedThreadId, string reason)
        {
            if (_stopping)
            {
                return;
            }

            _orphanedThreads.Add($"[{serviceKey}/{managedThreadId}] : {reason}");
        }
    }

    private sealed class ResilienceContext(ServiceProvider serviceProvider, IBusControl busControl, IBus bus, ResilienceTransportFactory transportFactory, ResilienceMonitor monitor)
        : IAsyncDisposable
    {
        public ResilienceTransport DeferredTransport { get; } = transportFactory.Get(DeferredUri);
        public ResilienceTransport ErrorTransport { get; } = transportFactory.Get(ErrorUri);
        public ResilienceTransport WorkTransport { get; } = transportFactory.Get(WorkUri);

        public int DeferredExecutionCount => monitor.DeferredExecutionCount;
        public IReadOnlyCollection<int> InboxThreadIds => monitor.InboxThreadIds;
        public IReadOnlyCollection<string> OrphanedThreads => monitor.OrphanedThreads;
        public IReadOnlyCollection<string> ProcessorExceptions => monitor.ProcessorExceptions;
        public IReadOnlyCollection<string> StoppedThreads => monitor.StoppedThreads;

        public string OrphanedThreadsText => string.Join(Environment.NewLine, OrphanedThreads);
        public string ProcessorExceptionsText => string.Join(Environment.NewLine, ProcessorExceptions);

        public static async Task<ResilienceContext> StartAsync(Func<ResilienceCommand, Task> handler)
        {
            var monitor = new ResilienceMonitor();

            var services = new ServiceCollection();

            services
                .AddHopper(options =>
                {
                    options.Inbox.WorkTransportUri = new(WorkUri);
                    options.Inbox.DeferredTransportUri = new(DeferredUri);
                    options.Inbox.ErrorTransportUri = new(ErrorUri);
                    options.Inbox.ThreadCount = ThreadCount;
                    options.Inbox.MaximumFailureCount = MaximumFailureCount;
                    options.Inbox.IgnoreOnFailureDurations = [TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50)];
                    options.Inbox.IdleDurations = [TimeSpan.FromMilliseconds(10)];
                    options.Inbox.DeferredMessageProcessorIdleDuration = TimeSpan.FromMilliseconds(50);
                    options.Inbox.DeferredMessageProcessorResetInterval = TimeSpan.FromMilliseconds(250);
                })
                .AddMessageHandler(handler);

            services
                .AddSingleton<ResilienceTransportFactory>()
                .AddSingleton<ITransportFactory>(serviceProvider => serviceProvider.GetRequiredService<ResilienceTransportFactory>())
                .Configure<ThreadingOptions>(options =>
                {
                    options.ProcessorExecuting += async (args, _) =>
                    {
                        monitor.Executing(args.ServiceKey, args.ManagedThreadId);

                        await Task.CompletedTask;
                    };

                    options.ProcessorException += async (args, _) =>
                    {
                        monitor.ProcessorException(args.ProcessorThread.ServiceKey, args.Exception);

                        await Task.CompletedTask;
                    };

                    options.ProcessorThreadOperationCanceled += async (args, _) =>
                    {
                        monitor.ThreadExited(args.ProcessorThread.ServiceKey, args.ManagedThreadId, "operation-canceled");

                        await Task.CompletedTask;
                    };

                    options.ProcessorThreadStopping += async (args, _) =>
                    {
                        monitor.ThreadExited(args.ProcessorThread.ServiceKey, args.ManagedThreadId, "stopping");

                        await Task.CompletedTask;
                    };

                    options.ProcessorThreadStopped += async (args, _) =>
                    {
                        monitor.ThreadStopped(args.ProcessorThread.ServiceKey);

                        await Task.CompletedTask;
                    };
                });

            var provider = services.BuildServiceProvider();

            var control = await provider.GetRequiredService<IBusControl>().StartAsync();

            return new(provider, control, provider.GetRequiredService<IBus>(), provider.GetRequiredService<ResilienceTransportFactory>(), monitor);
        }

        public async Task SendAsync(int count, string behaviour, Action<TransportMessageBuilder>? configure = null)
        {
            for (var i = 0; i < count; i++)
            {
                await bus.SendAsync(new ResilienceCommand(Guid.NewGuid(), behaviour), builder =>
                {
                    builder.ToSelf();

                    configure?.Invoke(builder);
                });
            }
        }

        /// <summary>
        ///     Waits for the given condition, returning `false` should it not be met within the timeout.
        /// </summary>
        public async Task<bool> WaitAsync(Func<bool> condition, int timeoutSeconds = 30)
        {
            var timeout = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);

            while (DateTimeOffset.UtcNow < timeout)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(25);
            }

            return condition();
        }

        public async ValueTask DisposeAsync()
        {
            monitor.Stopping();

            await busControl.DisposeAsync();
            await serviceProvider.DisposeAsync();
        }
    }

    private sealed class ResilienceTracker
    {
        private readonly ConcurrentDictionary<Guid, int> _attempts = new();
        private readonly ConcurrentDictionary<Guid, byte> _handled = new();

        public int HandledCount => _handled.Count;
        public int TotalAttempts => _attempts.Values.Sum();

        public int Attempted(ResilienceCommand message)
        {
            return _attempts.AddOrUpdate(message.Id, 1, (_, count) => count + 1);
        }

        public void Handled(ResilienceCommand message)
        {
            _handled.TryAdd(message.Id, 0);
        }
    }
}
