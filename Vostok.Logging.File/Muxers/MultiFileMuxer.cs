using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public MultiFileMuxer(ISingleFileMuxerFactory factory)
        {
            this.factory = factory;

            states = new ConcurrentDictionary<FilePath, FileState>();
        }

        public long EventsLost =>
            states.Sum(pair => pair.Value.Muxer.EventsLost);

        public bool TryAdd(FilePath file, LogEventInfo info, object initiator) =>
            states.TryGetValue(file, out var state) && state.Muxer.TryAdd(info, ReferenceEquals(initiator, state.Owner));

        public Task FlushAsync(FilePath file) =>
            states.TryGetValue(file, out var state) ? state.Muxer.FlushAsync() : Task.CompletedTask;

        public Task FlushAsync() =>
            Task.WhenAll(states.Select(pair => pair.Value.Muxer.FlushAsync()));

        public IMuxerRegistration Register(FilePath file, FileLogSettings settings, object initiator)
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

            return new MuxerRegistration(states, file, initiator);
        }

        private class FileState
        {
            public readonly ISingleFileMuxer Muxer;

            public readonly object Owner;

            public readonly int References;

            private FileState(ISingleFileMuxer muxer, object owner, int references)
            {
                Muxer = muxer;
                Owner = owner;
                References = references;
            }

            public static FileState CreateInitial(ISingleFileMuxer muxer, object owner)
            {
                return new FileState(muxer, owner, 1);
            }

            public FileState AddParticipant(object participant)
            {
                return new FileState(Muxer, Owner ?? participant, References + 1);
            }

            public FileState RemoveParticipant(object participant)
            {
                return new FileState(Muxer, ReferenceEquals(participant, Owner) ? null : Owner, References - 1);
            }
        }

        private class MuxerRegistration : IMuxerRegistration
        {
            private readonly ConcurrentDictionary<FilePath, FileState> states;
            private readonly FilePath file;
            private object participant;

            public MuxerRegistration(ConcurrentDictionary<FilePath, FileState> states, FilePath file, object participant)
            {
                this.states = states;
                this.file = file;
                this.participant = participant;
            }

            public bool IsValid(FilePath against) => participant != null && Equals(file, against);

            public void Dispose()
            {
                var participantToRemove = Interlocked.Exchange(ref participant, null);
                if (participantToRemove == null)
                    return;

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