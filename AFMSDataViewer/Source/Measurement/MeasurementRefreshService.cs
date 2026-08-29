using System.Diagnostics;
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

    public sealed record MeasurementDataSourceLoadTiming(string Name, TimeSpan Elapsed, bool Succeeded);

    public sealed class MeasurementDataLoadCompletedEventArgs(
        IReadOnlyList<MeasurementDataSourceLoadTiming> dataSources,
        TimeSpan totalElapsed) : EventArgs
    {
        public IReadOnlyList<MeasurementDataSourceLoadTiming> DataSources { get; } = dataSources;
        public TimeSpan TotalElapsed { get; } = totalElapsed;
    }

    /// <summary>
    /// 10분 경계마다 데이터 소스를 증분 조회하고 MeasurementDataHub를 갱신합니다.
    /// </summary>
    public sealed class MeasurementRefreshService : BackgroundService
    {
        private sealed record LoadDataSourcesResult(
            MeasurementBatch Batch,
            IReadOnlyList<MeasurementDataSourceLoadTiming> Timings,
            TimeSpan TotalElapsed);

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

        public event EventHandler<MeasurementDataLoadCompletedEventArgs>? FullLoadCompleted;

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
            try
            {
                await RefreshCoreAsync(true, stoppingToken).ConfigureAwait(false);

                while (!stoppingToken.IsCancellationRequested)
                {
                    TimeSpan delay = GetDelayUntilNextRefresh(DateTime.Now);
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                    await RefreshCoreAsync(false, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host 종료로 인한 정상적인 백그라운드 작업 취소입니다.
            }
        }

        private async Task ReloadCoreAsync(TimeSpan retention, CancellationToken cancellationToken)
        {
            await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _lastSuccessfulLoads.Clear();
                LoadDataSourcesResult result = await LoadDataSourcesAsync(true, retention, cancellationToken).ConfigureAwait(false);
                _dataHub.Reset(DateTime.Now, retention, result.Batch.Levels, result.Batch.Voltages);
                _dataHub.Apply(result.Batch);
                _requiresFullReload = false;
                RaiseFullLoadCompleted(result);
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
                if (fullReload) _lastSuccessfulLoads.Clear();

                LoadDataSourcesResult result = await LoadDataSourcesAsync(fullReload, _dataHub.Retention, cancellationToken).ConfigureAwait(false);
                MeasurementBatch batch = result.Batch;
                if (fullReload)
                {
                    _dataHub.Reset(currentSlot, _dataHub.Retention, batch.Levels, batch.Voltages);
                    _requiresFullReload = false;
                }
                else
                {
                    _dataHub.AdvanceTo(currentSlot, batch.Levels, batch.Voltages);
                }

                _dataHub.Apply(batch);
                if (fullReload) RaiseFullLoadCompleted(result);
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private async Task<LoadDataSourcesResult> LoadDataSourcesAsync(bool fullReload, TimeSpan retention, CancellationToken cancellationToken)
        {
            DateTime to = MeasurementDataHub.AlignToSlot(DateTime.Now);
            DateTime fullRangeStart = to - retention;
            MeasurementBatch combinedBatch = new();
            List<MeasurementDataSourceLoadTiming> timings = new(_dataSources.Count);
            Stopwatch totalStopwatch = Stopwatch.StartNew();

            foreach (IMeasurementDataSource dataSource in _dataSources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DateTime from = fullReload || !_lastSuccessfulLoads.TryGetValue(dataSource, out DateTime lastLoaded)
                    ? fullRangeStart
                    : lastLoaded - QueryOverlap;
                if (from < fullRangeStart) from = fullRangeStart;

                Stopwatch stopwatch = Stopwatch.StartNew();
                bool succeeded = false;
                try
                {
                    MeasurementBatch batch = await dataSource.LoadAsync(from, to, cancellationToken)
                        .ConfigureAwait(false);
                    combinedBatch.Velocities.AddRange(batch.Velocities);
                    combinedBatch.Levels.AddRange(batch.Levels);
                    combinedBatch.Discharges.AddRange(batch.Discharges);
                    combinedBatch.Voltages.AddRange(batch.Voltages);
                    _lastSuccessfulLoads[dataSource] = to;
                    succeeded = true;
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
                finally
                {
                    stopwatch.Stop();
                    timings.Add(new MeasurementDataSourceLoadTiming(dataSource.Name, stopwatch.Elapsed, succeeded));
                }
            }

            totalStopwatch.Stop();
            return new LoadDataSourcesResult(combinedBatch, timings, totalStopwatch.Elapsed);
        }

        private void RaiseFullLoadCompleted(LoadDataSourcesResult result) =>
            FullLoadCompleted?.Invoke(this,
                new MeasurementDataLoadCompletedEventArgs(result.Timings, result.TotalElapsed));

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
