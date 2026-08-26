using AFMSDll;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDischargeService
{
    internal class DischargeCalculationWorker : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
        private List<_QBase> calculators = new();
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(PollInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {

                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }
    }
}
