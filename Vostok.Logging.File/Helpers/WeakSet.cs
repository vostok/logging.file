using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Vostok.Logging.File.Helpers
{
    internal class WeakSet<T> : IEnumerable<WeakReference<T>>
        where T : class
    {
        private readonly object guard = new object();
        private HashSet<WeakReference<T>> references = new HashSet<WeakReference<T>>();

        public void Add(T target)
        {
            lock (guard)
                references.Add(new WeakReference<T>(target));
        }

        public void Remove(T target)
        {
            lock (guard)
                references.Remove(new WeakReference<T>(target));
        }

        public void Purge()
        {
            var newSet = new HashSet<WeakReference<T>>();

            foreach (var reference in this.Where(reference => reference.TryGetTarget(out _)))
                newSet.Add(reference);

            lock (guard)
                references = newSet;
        }

        public IEnumerator<WeakReference<T>> GetEnumerator()
        {
            List<WeakReference<T>> snapshot;

            lock (guard)
                snapshot = references.ToList();

            return snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}