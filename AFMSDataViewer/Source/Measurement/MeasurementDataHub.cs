namespace AFMSDataViewer
{
    public interface IRealtimeMeasurement
    {
        DateTime Time { get; }
    }

    public sealed record VelocityMeasurement(DateTime Time, string SourceType, string DeviceKey, int TransectNo, double Value) : IRealtimeMeasurement;

    public sealed record LevelMeasurement(DateTime Time, string DeviceKey, double Value) : IRealtimeMeasurement;

    public sealed record DischargeMeasurement(DateTime Time, string DeviceType, int DeviceId, string Method, double Value) : IRealtimeMeasurement;

    public sealed record VoltageMeasurement(DateTime Time, string DeviceKey, double? InputVoltage, double? OutputVoltage) : IRealtimeMeasurement;

    public sealed class MeasurementSlot
    {
        public MeasurementSlot(DateTime time)
        {
            Time = time;
        }

        public DateTime Time { get; }
        public List<VelocityMeasurement> Velocities { get; } = new();
        public List<LevelMeasurement> Levels { get; } = new();
        public List<DischargeMeasurement> Discharges { get; } = new();
        public List<VoltageMeasurement> Voltages { get; } = new();

        internal MeasurementSlot Clone()
        {
            MeasurementSlot clone = new(Time);
            clone.Velocities.AddRange(Velocities);
            clone.Levels.AddRange(Levels);
            clone.Discharges.AddRange(Discharges);
            clone.Voltages.AddRange(Voltages);
            return clone;
        }
    }

    public sealed class MeasurementDataChangedEventArgs(DateTime rangeStart, DateTime rangeEnd, long version) : EventArgs
    {
        public DateTime RangeStart { get; } = rangeStart;
        public DateTime RangeEnd { get; } = rangeEnd;
        public long Version { get; } = version;
    }

    /// <summary>
    /// 실시간 측정 화면에서 사용하는 10분 슬롯을 메모리에 보관합니다.
    /// 모든 변경은 이 객체를 통해 Upsert되며 외부에는 복사된 스냅샷만 제공합니다.
    /// </summary>
    public sealed class MeasurementDataHub
    {
        public static readonly TimeSpan SlotInterval = TimeSpan.FromMinutes(10);

        private readonly object _syncRoot = new();
        private readonly List<MeasurementSlot> _slots = new();
        private TimeSpan _retention = TimeSpan.FromHours(12);
        private long _version;

        public event EventHandler<MeasurementDataChangedEventArgs>? Changed;

        public TimeSpan Retention
        {
            get
            {
                lock (_syncRoot) return _retention;
            }
        }

        public long Version
        {
            get
            {
                lock (_syncRoot) return _version;
            }
        }

        public void Reset(DateTime rangeEnd, TimeSpan retention)
        {
            ValidateRetention(retention);
            DateTime alignedEnd = AlignToSlot(rangeEnd);
            DateTime rangeStart = alignedEnd - retention;

            lock (_syncRoot)
            {
                _retention = retention;
                _slots.Clear();
                for (DateTime time = rangeStart; time <= alignedEnd; time += SlotInterval)
                    _slots.Add(new MeasurementSlot(time));
                _version++;
            }

            RaiseChanged();
        }

        public void AdvanceTo(DateTime time)
        {
            DateTime alignedTime = AlignToSlot(time);
            bool changed = false;

            lock (_syncRoot)
            {
                if (_slots.Count == 0)
                {
                    DateTime start = alignedTime - _retention;
                    for (DateTime slotTime = start; slotTime <= alignedTime; slotTime += SlotInterval)
                        _slots.Add(new MeasurementSlot(slotTime));
                    changed = true;
                }
                else
                {
                    DateTime next = _slots[^1].Time + SlotInterval;
                    while (next <= alignedTime)
                    {
                        _slots.Add(new MeasurementSlot(next));
                        next += SlotInterval;
                        changed = true;
                    }
                }

                DateTime cutoff = alignedTime - _retention;
                int removed = _slots.RemoveAll(slot => slot.Time < cutoff);
                changed |= removed > 0;
                if (changed) _version++;
            }

            if (changed) RaiseChanged();
        }

        public void Apply(MeasurementBatch batch)
        {
            ArgumentNullException.ThrowIfNull(batch);
            bool changed = false;

            lock (_syncRoot)
            {
                foreach (VelocityMeasurement item in batch.Velocities)
                {
                    MeasurementSlot slot = EnsureSlotCore(item.Time);
                    Upsert(slot.Velocities, item, current =>
                        current.SourceType == item.SourceType &&
                        current.DeviceKey == item.DeviceKey &&
                        current.TransectNo == item.TransectNo);
                    changed = true;
                }

                foreach (LevelMeasurement item in batch.Levels)
                {
                    MeasurementSlot slot = EnsureSlotCore(item.Time);
                    Upsert(slot.Levels, item, current => current.DeviceKey == item.DeviceKey);
                    changed = true;
                }

                foreach (DischargeMeasurement item in batch.Discharges)
                {
                    MeasurementSlot slot = EnsureSlotCore(item.Time);
                    Upsert(slot.Discharges, item, current =>
                        current.DeviceType == item.DeviceType &&
                        current.DeviceId == item.DeviceId &&
                        current.Method == item.Method);
                    changed = true;
                }

                foreach (VoltageMeasurement item in batch.Voltages)
                {
                    MeasurementSlot slot = EnsureSlotCore(item.Time);
                    Upsert(slot.Voltages, item, current => current.DeviceKey == item.DeviceKey);
                    changed = true;
                }

                if (changed)
                {
                    RemoveExpiredCore();
                    _version++;
                }
            }

            if (changed) RaiseChanged();
        }

        public IReadOnlyList<MeasurementSlot> CreateSnapshot()
        {
            lock (_syncRoot)
                return _slots.Select(slot => slot.Clone()).ToArray();
        }

        public IReadOnlyList<MeasurementSlot> CreateSnapshot(DateTime from, DateTime to)
        {
            if (from > to) throw new ArgumentException("시작 시간은 종료 시간보다 늦을 수 없습니다.");
            DateTime alignedFrom = AlignToSlot(from);
            DateTime alignedTo = AlignToSlot(to);

            lock (_syncRoot)
                return _slots.Where(slot => slot.Time >= alignedFrom && slot.Time <= alignedTo)
                    .Select(slot => slot.Clone()).ToArray();
        }

        public static DateTime AlignToSlot(DateTime time)
        {
            long ticks = time.Ticks / SlotInterval.Ticks * SlotInterval.Ticks;
            return new DateTime(ticks, time.Kind);
        }

        private MeasurementSlot EnsureSlotCore(DateTime time)
        {
            DateTime alignedTime = AlignToSlot(time);
            int index = _slots.BinarySearch(
                new MeasurementSlot(alignedTime), MeasurementSlotTimeComparer.Instance);
            if (index >= 0) return _slots[index];

            MeasurementSlot slot = new(alignedTime);
            _slots.Insert(~index, slot);
            return slot;
        }

        private void RemoveExpiredCore()
        {
            if (_slots.Count == 0) return;
            DateTime cutoff = _slots[^1].Time - _retention;
            _slots.RemoveAll(slot => slot.Time < cutoff);
        }

        private static void Upsert<T>(List<T> items, T item, Func<T, bool> matches)
        {
            int index = items.FindIndex(current => matches(current));
            if (index >= 0) items[index] = item;
            else items.Add(item);
        }

        private void RaiseChanged()
        {
            MeasurementDataChangedEventArgs args;
            lock (_syncRoot)
            {
                DateTime start = _slots.Count == 0 ? DateTime.MinValue : _slots[0].Time;
                DateTime end = _slots.Count == 0 ? DateTime.MinValue : _slots[^1].Time;
                args = new MeasurementDataChangedEventArgs(start, end, _version);
            }
            Changed?.Invoke(this, args);
        }

        private static void ValidateRetention(TimeSpan retention)
        {
            if (retention < SlotInterval)
                throw new ArgumentOutOfRangeException(nameof(retention), "보관 기간은 10분 이상이어야 합니다.");
            if (retention.Ticks % SlotInterval.Ticks != 0)
                throw new ArgumentException("보관 기간은 10분 단위여야 합니다.", nameof(retention));
        }

        private sealed class MeasurementSlotTimeComparer : IComparer<MeasurementSlot>
        {
            public static MeasurementSlotTimeComparer Instance { get; } = new();
            public int Compare(MeasurementSlot? x, MeasurementSlot? y) =>
                Nullable.Compare(x?.Time, y?.Time);
        }
    }
}
