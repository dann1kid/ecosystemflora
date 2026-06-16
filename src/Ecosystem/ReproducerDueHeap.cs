using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Min-heap ordered by <see cref="ReproducerEntry.NextAttemptHours"/> (lazy — stale nodes skipped at pop).</summary>
    internal sealed class ReproducerDueHeap
    {
        readonly List<ReproducerEntry> heap = new List<ReproducerEntry>();

        public int Count => heap.Count;

        public void Clear() => heap.Clear();

        public void Push(ReproducerEntry entry)
        {
            if (entry == null) return;

            int i = heap.Count;
            heap.Add(entry);
            SiftUp(i);
        }

        public bool TryPeek(double now, out ReproducerEntry entry)
        {
            if (heap.Count == 0)
            {
                entry = null;
                return false;
            }

            entry = heap[0];
            return entry.NextAttemptHours <= now;
        }

        public ReproducerEntry Pop()
        {
            int last = heap.Count - 1;
            ReproducerEntry root = heap[0];
            ReproducerEntry tail = heap[last];
            heap.RemoveAt(last);

            if (heap.Count > 0)
            {
                heap[0] = tail;
                SiftDown(0);
            }

            return root;
        }

        void SiftUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (!IsBefore(heap[i], heap[parent])) break;

                Swap(i, parent);
                i = parent;
            }
        }

        void SiftDown(int i)
        {
            int count = heap.Count;
            while (true)
            {
                int left = (i << 1) + 1;
                if (left >= count) break;

                int smallest = left;
                int right = left + 1;
                if (right < count && IsBefore(heap[right], heap[left]))
                {
                    smallest = right;
                }

                if (!IsBefore(heap[smallest], heap[i])) break;

                Swap(i, smallest);
                i = smallest;
            }
        }

        static bool IsBefore(ReproducerEntry a, ReproducerEntry b)
        {
            if (a.NextAttemptHours != b.NextAttemptHours)
            {
                return a.NextAttemptHours < b.NextAttemptHours;
            }

            return a.EntriesIndex < b.EntriesIndex;
        }

        void Swap(int a, int b)
        {
            ReproducerEntry tmp = heap[a];
            heap[a] = heap[b];
            heap[b] = tmp;
        }
    }
}
