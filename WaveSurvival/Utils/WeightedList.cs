using WaveSurvival.Utils.Extensions;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using WaveSurvival.Json.Converters.Utils;

namespace WaveSurvival.Utils
{
    [JsonConverter(typeof(WeightedListConverterFactory))]
    public sealed class WeightedList<T> : IEnumerable<T> where T : IWeightable
    {
        // List that contains a set of weighted elements for random selection without replacement.

        public readonly static WeightedList<T> Empty = new();
        private readonly static Random s_random = new();

        private List<Node> _heap = EmptyList<Node>.Instance;
        private List<T> _values = EmptyList<T>.Instance;
        private bool _isDirty = false;

        public List<T> Values
        {
            get => _values;
            set
            {
                _values = value;
                _isDirty = true;
            }
        }

        private List<Node> Heap
        {
            get
            {
                if (_isDirty)
                {
                    _isDirty = false;
                    GenerateHeap();
                }
                return _heap;
            }
        }

        public WeightedList() { }
        public WeightedList(List<T> list) => Values = list;

        public static implicit operator WeightedList<T>?(List<T>? list) => list != null ? new(list) : null;
        public static implicit operator List<T>?(WeightedList<T>? weightedList) => weightedList?.Values;

        public T this[int index]
        {
            get => _values[index];
            set
            {
                _values[index] = value;
                _isDirty = true;
            }
        }

        public int Count => Values.Count;

        public void Add(T value)
        {
            if (Count == 0)
                _values = new();
            _values.Add(value);
            _isDirty = true;
        }

        public void Remove(T value)
        {
            if (Count != 0)
                _isDirty = _values.Remove(value);
        }

        public void RemoveAt(int index)
        {
            if (Count != 0)
            {
                _isDirty = true;
                _values.RemoveAt(index);
            }
        }

        public int RemoveAll(Predicate<T> predicate)
        {
            int removed = _values.RemoveAll(predicate);
            if (removed != 0)
                _isDirty = true;
            return removed;
        }

        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _values.GetEnumerator();

        public bool TryGetRandom([MaybeNullWhen(false)] out T obj)
        {
            if (Count == 0 || Heap[1].TotalWeight == 0)
            {
                obj = default;
                return false;
            }

            obj = PopFromHeap();
            return true;
        }

        public T GetRandom()
        {
            if (Count == 0) throw new IndexOutOfRangeException("Can't get random element from 0-length list!");

            if (Heap[1].TotalWeight == 0)
                RefillHeap();
            return PopFromHeap();
        }

        public void Refill()
        {
            if (Count != 0 && !_isDirty)
                RefillHeap();
        }

        private void GenerateHeap()
        {
            _heap = new(Count + 1) { null! };

            foreach (T value in _values)
                _heap.Add(new Node(value));

            for (int i = Heap.Count - 1; i > 1; i--)
                _heap[i >> 1].TotalWeight += _heap[i].TotalWeight;
        }

        private void RefillHeap()
        {
            for (int i = 1; i < Heap.Count; i++)
            {
                Heap[i].Weight = _values[i - 1].Weight;
                Heap[i].TotalWeight = _values[i - 1].Weight;
            }

            for (int i = Heap.Count - 1; i > 1; i--)
                Heap[i >> 1].TotalWeight += Heap[i].TotalWeight;
        }

        private T PopFromHeap()
        {
            if (Heap[1].TotalWeight == 0)
                throw new IndexOutOfRangeException("Cannot get an element from WeightedShuffleList with 0 total weight remaining!");

            if (Count == 1) return _values[0];

            float runningWeight = s_random.NextSingle(Heap[1].TotalWeight);
            int i = 1;

            while (runningWeight >= Heap[i].Weight)
            {
                runningWeight -= Heap[i].Weight;
                i <<= 1;

                if (runningWeight >= Heap[i].TotalWeight)
                {
                    runningWeight -= Heap[i].TotalWeight;
                    i += 1;
                }
            }

            float weight = Heap[i].Weight;
            T result = Heap[i].Value;

            Heap[i].Weight = 0;

            while (i > 0)
            {
                Heap[i].TotalWeight -= weight;
                i >>= 1;
            }

            return result;
        }

        class Node
        {
            public T Value { get; set; }
            public float Weight { get; set; }
            public float TotalWeight { get; set; }

            public Node(T value)
            {
                Value = value;
                Weight = value.Weight;
                TotalWeight = Weight;
            }
        }
    }

    public interface IWeightable
    {
        public float Weight { get; }
    }
}
