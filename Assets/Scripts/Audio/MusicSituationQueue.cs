using System.Collections.Generic;

namespace BladeSpinners.Audio
{
    /// <summary>
    /// Small unique FIFO used for non-interrupting menu music navigation.
    /// A situation can be queued again after it has been consumed.
    /// </summary>
    public sealed class MusicSituationQueue
    {
        private readonly Queue<MusicSituation> queue =
            new Queue<MusicSituation>();
        private readonly HashSet<MusicSituation> queued =
            new HashSet<MusicSituation>();

        public int Count => queue.Count;

        public bool Enqueue(MusicSituation situation)
        {
            if (!queued.Add(situation))
                return false;

            queue.Enqueue(situation);
            return true;
        }

        public bool Contains(MusicSituation situation)
        {
            return queued.Contains(situation);
        }

        public bool TryDequeue(out MusicSituation situation)
        {
            if (queue.Count == 0)
            {
                situation = default;
                return false;
            }

            situation = queue.Dequeue();
            queued.Remove(situation);
            return true;
        }

        public void Clear()
        {
            queue.Clear();
            queued.Clear();
        }
    }
}
