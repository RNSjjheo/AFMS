using AFMSDll;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AFMSLoggerVideoHydorsem
{
    internal class RequestTaskWorker : BackgroundService
    {
        private static readonly ILog Log = LogManager.GetLogger("API");
        private readonly IRequestTaskQueue _queue;
        private readonly TcpLoggingDiagnostics diagnostics;
        private const int WorkerCount = 4;

        public RequestTaskWorker(IRequestTaskQueue queue, TcpLoggingDiagnostics diagnostics)
        {
            _queue = queue;
            this.diagnostics = diagnostics;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var workers = Enumerable.Range(1, WorkerCount).Select(workerNo => RunWorkerAsync(workerNo, stoppingToken)).ToArray();

            await Task.WhenAll(workers);
        }

        private async Task RunWorkerAsync(int workerNo, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                RequestWorkItem item;

                try
                {
                    item = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    await ProcessAsync(workerNo, item, stoppingToken);
                }
                catch (Exception ex)
                {
                    Log.Error("====================");
                    Log.Error($"작업 처리 실패. WorkerNo={workerNo}, RequestId={item.Id}");
                    Log.Error(ex.Message);
                    Log.Error("====================");
                }
            }
        }

        private async Task ProcessAsync(int workerNo, RequestWorkItem item, CancellationToken cancellationToken)
        {
            // Converting은 MeasureVideo?를 반환하므로 nullable로 받고 검사
            MeasureVideo? data = VideoParser.Converting(item.Message, Configuration.Instance.SiteCode, out string errorMsg);

            if (data is null)
            {
                Log.Error("====================");
                Log.Error($"Video 파싱 실패. WorkerNo={workerNo}, RequestId={item.Id}");
                Log.Error($"파싱 에러: {errorMsg ?? "<null>"}");
                Log.Error("====================");
                return;
            }

            bool result = VideoDbWriter.Insert(data);
            if (!result)
            {
                Log.Error($"[{item.Key}] 영상유속계 데이터를 DB에 기록하지 못했습니다.");
                return;
            }

            diagnostics.ReportMeasurement(data.Datetime);
            Log.Info($"[{item.Key}] Index: {data.Id}, V: {data.Velocity:0.000}, Level: {data.WaterLevel:0.00}");


            foreach (var cell in data.Cells)
            {
                string msgcell = $"[{item.Key}] Cell: {cell.No}, ";
                msgcell += $"V: {cell.Velocity.ToString("0.000")}, ";
                msgcell += $"X: {cell.PosX.ToString("0.00")}, ";
                msgcell += $"Y: {cell.PosY.ToString("0.00")}, ";
                msgcell += $"U: {cell.Uncertainty.ToString("0.00")}";

                Log.Info(msgcell);
            }

            await Task.Delay(1000, cancellationToken);
        }
    }
}
