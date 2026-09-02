using AFMSDll;
using Microsoft.Extensions.Options;
using System.Data;

namespace AFMSSediService
{
    internal abstract class WorkerSlot: BackgroundService
    {

        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
        protected abstract void CreateMissingSlots(CancellationToken token);

        protected IHostApplicationLifetime Lifetime;
        protected ILogger Logger;
        protected IOptions<SSCServiceOptions> Options;
        protected CancellationTokenSource StartupCancel;
        public WorkerSlot(ILogger logger, IHostApplicationLifetime lifetime, IOptions<SSCServiceOptions> options)
        {
            Lifetime = lifetime;
            Logger = logger;
            Options = options;

        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            StartupCancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Lifetime.ApplicationStopping);
            CancellationToken stoppingToken = StartupCancel.Token;

            stoppingToken.ThrowIfCancellationRequested();

            List<string> tableLogs = [];
            if (!FBProvider.Instance.ExistTable(FbtAFMSSediTimeslot.TABLE_NAME, tableLogs))
            {
                throw new InvalidOperationException(
                    $"{FbtAFMSSediTimeslot.TABLE_NAME} 테이블을 준비하지 못해 서비스를 시작할 수 없습니다.");
            }

            CreateMissingSlots(stoppingToken);
            Logger.LogInformation("초기 SEDI 슬롯 준비를 완료했습니다.");

            stoppingToken.ThrowIfCancellationRequested();
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(PollInterval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        CreateMissingSlots(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception exception)
                    {
                        Logger.LogError(exception, "SEDI 슬롯 생성 중 오류가 발생했습니다.");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }
    }
}
