using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AFMSDataViewer
{
    public sealed class MeasurementBatch
    {
        public List<VelocityMeasurement> Velocities { get; } = new();
        public List<LevelMeasurement> Levels { get; } = new();
        public List<DischargeMeasurement> Discharges { get; } = new();
        public List<VoltageMeasurement> Voltages { get; } = new();
    }

    /// <summary>
    /// 유속계, 수위계, 유량, 전압 등 개별 DB 조회 구현이 따라야 하는 계약입니다.
    /// 구현체 하나는 일부 측정 유형만 반환해도 됩니다.
    /// </summary>
    public interface IMeasurementDataSource
    {
        string Name { get; }

        Task<MeasurementBatch> LoadAsync(DateTime from, DateTime to, CancellationToken cancellationToken);
    }

    /// <summary>
    /// 10분 경계마다 데이터 소스를 증분 조회하고 MeasurementDataHub를 갱신합니다.
    /// </summary>
    public sealed class MeasurementRefreshService : BackgroundService
    {
        private static readonly TimeSpan QueryOverlap = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(5);

        private readonly MeasurementDataHub _dataHub;
        private readonly IReadOnlyList<IMeasurementDataSource> _dataSources;
        private readonly ILogger<MeasurementRefreshService> _logger;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly Dictionary<IMeasurementDataSource, DateTime> _lastSuccessfulLoads = new();
        private bool _requiresFullReload = true;

        public MeasurementRefreshService(MeasurementDataHub dataHub, IEnumerable<IMeasurementDataSource> dataSources, ILogger<MeasurementRefreshService> logger)
        {
            _dataHub = dataHub;
            _dataSources = dataSources.ToArray();
            _logger = logger;
        }

        public Task ReloadAsync(TimeSpan retention, CancellationToken cancellationToken = default)
        {
            if (retention < MeasurementDataHub.SlotInterval ||
                retention.Ticks % MeasurementDataHub.SlotInterval.Ticks != 0)
                throw new ArgumentOutOfRangeException(nameof(retention), "조회 기간은 10분 단위여야 합니다.");

            return ReloadCoreAsync(retention, cancellationToken);
        }

        public Task RefreshNowAsync(CancellationToken cancellationToken = default) => RefreshCoreAsync(false, cancellationToken);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RefreshCoreAsync(true, stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan delay = GetDelayUntilNextRefresh(DateTime.Now);
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                await RefreshCoreAsync(false, stoppingToken).ConfigureAwait(false);
            }
        }

        private async Task ReloadCoreAsync(TimeSpan retention, CancellationToken cancellationToken)
        {
            await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _dataHub.Reset(DateTime.Now, retention);
                _lastSuccessfulLoads.Clear();
                _requiresFullReload = false;
                await LoadDataSourcesAsync(true, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task RefreshCoreAsync(bool forceFullReload, CancellationToken cancellationToken)
        {
            await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                bool fullReload = forceFullReload || _requiresFullReload;
                DateTime currentSlot = MeasurementDataHub.AlignToSlot(DateTime.Now);
                if (fullReload)
                {
                    _dataHub.Reset(currentSlot, _dataHub.Retention);
                    _lastSuccessfulLoads.Clear();
                    _requiresFullReload = false;
                }
                else
                {
                    _dataHub.AdvanceTo(currentSlot);
                }

                await LoadDataSourcesAsync(fullReload, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task LoadDataSourcesAsync(bool fullReload, CancellationToken cancellationToken)
        {
            DateTime to = MeasurementDataHub.AlignToSlot(DateTime.Now);
            DateTime fullRangeStart = to - _dataHub.Retention;

            foreach (IMeasurementDataSource dataSource in _dataSources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DateTime from = fullReload || !_lastSuccessfulLoads.TryGetValue(dataSource, out DateTime lastLoaded)
                    ? fullRangeStart
                    : lastLoaded - QueryOverlap;
                if (from < fullRangeStart) from = fullRangeStart;

                try
                {
                    MeasurementBatch batch = await dataSource.LoadAsync(from, to, cancellationToken)
                        .ConfigureAwait(false);
                    _dataHub.Apply(batch);
                    _lastSuccessfulLoads[dataSource] = to;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception,
                        "측정 데이터 갱신 실패: {DataSource}, {From:yyyy-MM-dd HH:mm} ~ {To:yyyy-MM-dd HH:mm}",
                        dataSource.Name, from, to);
                }
            }
        }

        private static TimeSpan GetDelayUntilNextRefresh(DateTime now)
        {
            DateTime currentSlot = MeasurementDataHub.AlignToSlot(now);
            DateTime nextRefresh = currentSlot + MeasurementDataHub.SlotInterval + SettleDelay;
            TimeSpan delay = nextRefresh - now;
            return delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1);
        }

        public override void Dispose()
        {
            _refreshLock.Dispose();
            base.Dispose();
        }
    }
}
