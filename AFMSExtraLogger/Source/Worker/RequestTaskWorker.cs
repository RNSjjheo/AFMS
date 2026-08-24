using AFMSDll;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AFMSExtraLogger
{
    internal class RequestTaskWorker : BackgroundService
    {
        private static readonly ILog Log = LogManager.GetLogger("API");
        private readonly IRequestTaskQueue _queue;
        private const int WorkerCount = 4;

        public RequestTaskWorker(IRequestTaskQueue queue)
        {
            _queue = queue;
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
            MeasureVideo? data = VideoParser.Converting(item.Message, out string errorMsg);

            if (data is null)
            {
                Log.Error("====================");
                Log.Error($"Video 파싱 실패. WorkerNo={workerNo}, RequestId={item.Id}");
                Log.Error($"파싱 에러: {errorMsg ?? "<null>"}");
                Log.Error("====================");
                return;
            }

            bool result = DBWriter.VideoInsert(data);

            DiagnosticsOwner.Instance.VideoMeasDate = data.Datetime.ToString("yyyy-MM-dd");
            DiagnosticsOwner.Instance.VideoMeasTime = data.Datetime.ToString("HH:mm:ss");
            DiagnosticsOwner.Instance.VideoMeasVelo = data.Velocity;
            DiagnosticsOwner.Instance.VideoMeasCellLen = data.CellLength;
            DiagnosticsOwner.Instance.VideoMeasCellCnt = data.CellCount;
            DiagnosticsOwner.Instance.VideoMeasCert= data.VeloUncertainty;

            DiagnosticsOwner.Instance.UpdateCurrentProcessMemoryMB();

            TcpBrocastBuffer.WriteLog("API", $"[{item.Key}] Index: {data.Id}, V: {data.Velocity.ToString("0.000")}, Level: {data.WaterLevel.ToString("0.00")}");


            foreach (var cell in data.Cells)
            {
                string msgcell = $"[{item.Key}] Cell: {cell.No}, ";
                msgcell += $"V: {cell.Velocity.ToString("0.000")}, ";
                msgcell += $"X: {cell.PosX.ToString("0.00")}, ";
                msgcell += $"Y: {cell.PosY.ToString("0.00")}, ";
                msgcell += $"U: {cell.Uncertainty.ToString("0.00")}";

                TcpBrocastBuffer.WriteLog("API", msgcell);
            }

            await Task.Delay(1000, cancellationToken);
        }
    }
}
