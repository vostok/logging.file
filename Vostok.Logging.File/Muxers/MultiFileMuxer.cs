using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Commons.Collections;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Muxers
{
    internal class MultiFileMuxer : IMultiFileMuxer
    {
        private readonly ISingleFileMuxerFactory factory;
        private readonly ConcurrentDictionary<FilePath, FileState> states;
        private readonly ConcurrentDictionary<MuxerRegistration, MuxerRegistration> registrations;

        public MultiFileMuxer(ISingleFileMuxerFactory factory)
        {
            this.factory = factory;

            states = new ConcurrentDictionary<FilePath, FileState>();
            registrations = new ConcurrentDictionary<MuxerRegistration, MuxerRegistration>(ByReferenceEqualityComparer<MuxerRegistration>.Instance);
        }

        public long EventsLost =>
            states.Sum(pair => pair.Value.Muxer.EventsLost);

        public bool TryAdd(FilePath file, LogEventInfo info, WeakReference initiator) =>
            states.TryGetValue(file, out var state) && state.Muxer.TryAdd(info, ReferenceEquals(initiator, state.Owner));

        public Task FlushAsync(FilePath file) =>
            states.TryGetValue(file, out var state) ? state.Muxer.FlushAsync() : Task.CompletedTask;

        public Task FlushAsync() =>
            Task.WhenAll(states.Select(pair => pair.Value.Muxer.FlushAsync()));

        public IMuxerRegistration Register(FilePath file, FileLogSettings settings, WeakReference initiator)
        {
            while (true)
            {
                if (states.TryGetValue(file, out var currentState))
                {
                    if (states.TryUpdate(file, currentState.AddParticipant(initiator), currentState))
                        break;
                }
                else
                {
                    var initialState = FileState.CreateInitial(factory.Create(settings), initiator);

                    if (states.TryAdd(file, initialState))
                        break;
                }
            }

            var registration = new MuxerRegistration(states, registrations, file, initiator);

            registrations.TryAdd(registration, registration);

            return registration;
        }

        public void InitiateOrphanedRegistrationsCleanup(TimeSpan period)
        {
            Task.Run(
                async () =>
                {
                    while (true)
                    {
                        await Task.Delay(period).ConfigureAwait(false);

                        try
                        {
                            foreach (var registration in registrations.Select(pair => pair.Key))
                            {
                                if (registration.IsOrphaned)
                                    registration.Dispose();
                            }
                        }
                        catch (Exception error)
                        {
                            SafeConsole.ReportError("Failed to cleanup orphaned muxer registrations.", error);
                        }
                    }
                });
        }

        private class FileState
        {
            public readonly ISingleFileMuxer Muxer;

            public readonly WeakReference Owner;

            public readonly int References;

            private FileState(ISingleFileMuxer muxer, WeakReference owner, int references)
            {
                Muxer = muxer;
                Owner = owner;
                References = references;
            }

            public static FileState CreateInitial(ISingleFileMuxer muxer, WeakReference owner)
            {
                return new FileState(muxer, owner, 1);
            }

            public FileState AddParticipant(WeakReference participant)
            {
                return new FileState(Muxer, Owner ?? participant, References + 1);
            }

            public FileState RemoveParticipant(WeakReference participant)
            {
                return new FileState(Muxer, ReferenceEquals(participant, Owner) ? null : Owner, References - 1);
            }
        }

        private class MuxerRegistration : IMuxerRegistration
        {
            private readonly ConcurrentDictionary<FilePath, FileState> states;
            private readonly ConcurrentDictionary<MuxerRegistration, MuxerRegistration> registrations;
            private readonly FilePath file;
            private WeakReference participant;

            public MuxerRegistration(
                ConcurrentDictionary<FilePath, FileState> states,
                ConcurrentDictionary<MuxerRegistration, MuxerRegistration> registrations,
                FilePath file,
                WeakReference participant)
            {
                this.file = file;
                this.states = states;
                this.registrations = registrations;
                this.participant = participant;
            }

            public bool IsOrphaned => participant?.IsAlive == false;

            public bool IsValid(FilePath against) => participant != null && Equals(file, against);

            public void Dispose()
            {
                var participantToRemove = Interlocked.Exchange(ref participant, null);
                if (participantToRemove == null)
                    return;

                registrations.TryRemove(this, out _);

                while (true)
                {
                    if (!states.TryGetValue(file, out var currentState))
                        return;

                    var newState = currentState.RemoveParticipant(participantToRemove);
                    if (newState.References > 0)
                    {
                        if (states.TryUpdate(file, newState, currentState))
                            return;
                    }
                    else
                    {
                        if (states.Remove(file, currentState))
                        {
                            currentState.Muxer.Dispose();
                            return;
                        }
                    }
                }
            }
        }
    }
}
