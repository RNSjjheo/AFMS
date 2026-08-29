using AFMSDll;

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

    public sealed class MeasurementDataChangedEventArgs(DateTime rangeStart, DateTime rangeEnd, long version) : EventArgs
    {
        public DateTime RangeStart { get; } = rangeStart;
        public DateTime RangeEnd { get; } = rangeEnd;
        public long Version { get; } = version;
    }

    /// <summary>
    /// 실시간 측정 화면에서 사용하는 10분 슬롯을 메모리에 보관합니다.
    /// 모든 변경은 이 객체를 통해 Upsert되며 외부에는 슬롯의 공유 참조를 제공합니다.
    /// </summary>
    public sealed class MeasurementDataHub
    {
        public static readonly TimeSpan SlotInterval = TimeSpan.FromMinutes(10);

        private readonly object _syncRoot = new();
        private readonly List<MeasurementSlot> _slots = new();
        private readonly Func<DateTime, CrossSectionDefinition> _resolveCrossSectionDefinition;
        private TimeSpan _retention = TimeSpan.FromHours(12);
        private long _version;

        public MeasurementDataHub(Func<DateTime, CrossSectionDefinition> resolveCrossSectionDefinition)
        {
            ArgumentNullException.ThrowIfNull(resolveCrossSectionDefinition);
            _resolveCrossSectionDefinition = resolveCrossSectionDefinition;
        }

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

        public void Reset(DateTime rangeEnd, TimeSpan retention, IReadOnlyList<LevelMeasurement> levels, IReadOnlyList<VoltageMeasurement> voltages)
        {
            ArgumentNullException.ThrowIfNull(levels);
            ArgumentNullException.ThrowIfNull(voltages);
            ValidateRetention(retention);
            DateTime alignedEnd = AlignToSlot(rangeEnd);
            DateTime rangeStart = alignedEnd - retention;
            IReadOnlyDictionary<DateTime, LevelMeasurement> levelsBySlot = BuildSlotLookup(levels);
            IReadOnlyDictionary<DateTime, VoltageMeasurement> voltagesBySlot = BuildSlotLookup(voltages);

            lock (_syncRoot)
            {
                _retention = retention;
                _slots.Clear();
                for (DateTime time = rangeStart; time <= alignedEnd; time += SlotInterval)
                    _slots.Add(CreateSlot(time, levelsBySlot, voltagesBySlot));
                _version++;
            }

            RaiseChanged();
        }

        public void AdvanceTo(DateTime time, IReadOnlyList<LevelMeasurement> levels, IReadOnlyList<VoltageMeasurement> voltages)
        {
            ArgumentNullException.ThrowIfNull(levels);
            ArgumentNullException.ThrowIfNull(voltages);
            DateTime alignedTime = AlignToSlot(time);
            IReadOnlyDictionary<DateTime, LevelMeasurement> levelsBySlot = BuildSlotLookup(levels);
            IReadOnlyDictionary<DateTime, VoltageMeasurement> voltagesBySlot = BuildSlotLookup(voltages);
            bool changed = false;

            lock (_syncRoot)
            {
                if (_slots.Count == 0)
                {
                    DateTime start = alignedTime - _retention;
                    for (DateTime slotTime = start; slotTime <= alignedTime; slotTime += SlotInterval)
                        _slots.Add(CreateSlot(slotTime, levelsBySlot, voltagesBySlot));
                    changed = true;
                }
                else
                {
                    DateTime next = _slots[^1].SlotTime + SlotInterval;
                    while (next <= alignedTime)
                    {
                        _slots.Add(CreateSlot(next, levelsBySlot, voltagesBySlot));
                        next += SlotInterval;
                        changed = true;
                    }
                }

                DateTime cutoff = alignedTime - _retention;
                int removed = _slots.RemoveAll(slot => slot.SlotTime < cutoff);
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
                    Upsert(slot.MeasurementDevices.HydroMeters, item, current =>
                        current.SourceType == item.SourceType &&
                        current.DeviceKey == item.DeviceKey &&
                        current.TransectNo == item.TransectNo);
                    changed = true;
                }

                foreach (LevelMeasurement item in batch.Levels)
                {
                    MeasurementSlot slot = EnsureSlotCore(item.Time);
                    slot.MeasurementDevices.WaterLevelGauge = item;
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
                    slot.MeasurementDevices.VoltageMeter = item;
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

        public IReadOnlyList<MeasurementSlot> GetSlots()
        {
            lock (_syncRoot)
                return _slots.ToArray();
        }

        public IReadOnlyList<MeasurementSlot> GetSlots(DateTime from, DateTime to)
        {
            if (from > to) throw new ArgumentException("시작 시간은 종료 시간보다 늦을 수 없습니다.");
            DateTime alignedFrom = AlignToSlot(from);
            DateTime alignedTo = AlignToSlot(to);

            lock (_syncRoot)
                return _slots.Where(slot => slot.SlotTime >= alignedFrom && slot.SlotTime <= alignedTo).ToArray();
        }

        public static DateTime AlignToSlot(DateTime time)
        {
            long ticks = time.Ticks / SlotInterval.Ticks * SlotInterval.Ticks;
            return new DateTime(ticks, time.Kind);
        }

        private static IReadOnlyDictionary<DateTime, T> BuildSlotLookup<T>(IEnumerable<T> measurements) where T : IRealtimeMeasurement
        {
            return measurements.GroupBy(item => AlignToSlot(item.Time)).ToDictionary(group => group.Key, group => group.OrderBy(item => item.Time).Last());
        }

        private MeasurementSlot CreateSlot(DateTime slotTime)
        {
            CrossSectionDefinition crossSectionDefinition = _resolveCrossSectionDefinition(slotTime);
            ArgumentNullException.ThrowIfNull(crossSectionDefinition);

            return new MeasurementSlot(slotTime, crossSectionDefinition);
        }

        private MeasurementSlot CreateSlot(
            DateTime slotTime,
            IReadOnlyDictionary<DateTime, LevelMeasurement> levelsBySlot,
            IReadOnlyDictionary<DateTime, VoltageMeasurement> voltagesBySlot)
        {
            MeasurementSlot slot = CreateSlot(slotTime);
            if (levelsBySlot.TryGetValue(slotTime, out LevelMeasurement? level)) slot.MeasurementDevices.WaterLevelGauge = level;
            if (voltagesBySlot.TryGetValue(slotTime, out VoltageMeasurement? voltage)) slot.MeasurementDevices.VoltageMeter = voltage;
            return slot;
        }

        private MeasurementSlot EnsureSlotCore(DateTime time)
        {
            DateTime alignedTime = AlignToSlot(time);
            int index = FindSlotIndex(alignedTime);
            if (index >= 0) return _slots[index];

            MeasurementSlot slot = CreateSlot(alignedTime);
            _slots.Insert(~index, slot);
            return slot;
        }

        private int FindSlotIndex(DateTime slotTime)
        {
            int low = 0;
            int high = _slots.Count - 1;

            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                int comparison = _slots[middle].SlotTime.CompareTo(slotTime);
                if (comparison == 0) return middle;
                if (comparison < 0) low = middle + 1;
                else high = middle - 1;
            }

            return ~low;
        }

        private void RemoveExpiredCore()
        {
            if (_slots.Count == 0) return;
            DateTime cutoff = _slots[^1].SlotTime - _retention;
            _slots.RemoveAll(slot => slot.SlotTime < cutoff);
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
                DateTime start = _slots.Count == 0 ? DateTime.MinValue : _slots[0].SlotTime;
                DateTime end = _slots.Count == 0 ? DateTime.MinValue : _slots[^1].SlotTime;
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

    }
}
