using AFMSDll;
using log4net;
using System.Text;

namespace AFMSExtraLogger
{
    public class RestApiManager
    {
        private const string METHOD_GET = "GET";
        private const string METHOD_POST = "POST";

        private static readonly ILog Log = LogManager.GetLogger("API");
        private WebApplication _RestApi;
        private TcpPacketServer _TcpServer;
        public RestApiManager(WebApplicationBuilder builder, WebApplication app)
        {
            string path = $"http://0.0.0.0:{DiagnosticsOwner.Instance.WebPort}";

            _RestApi = app;
            builder.WebHost.UseUrls(path);
        }

        public void SetTcpServer(TcpPacketServer tcpserver)
        {
            _TcpServer = tcpserver;
        }

        public async Task StartAsync()
        {
            await _RestApi.StartAsync();
        }

        public async Task StopAsync()
        {
            await _RestApi.StopAsync();
            await _RestApi.DisposeAsync();
        }

        public void Regist()
        {
            string path = DiagnosticsOwner.Instance.WebPath;

            _RestApi.MapPost($"/{path}", async (HttpRequest request, IRequestTaskQueue queue) =>
            {
                string jsonBody;

                using (var reader = new StreamReader(request.Body, Encoding.UTF8))
                {
                    jsonBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(jsonBody))
                {
                    return Results.BadRequest(new { message = "JSON 데이터가 없습니다." });
                }

                var item = new RequestWorkItem
                {
                    Id = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTimeOffset.Now,
                    Path = request.Path,
                    Method = ApiMethod.POST,
                    Message = jsonBody,
                };

                return EnqueueRequest(item, queue);
            });
        }

        private IResult EnqueueRequest(RequestWorkItem item, IRequestTaskQueue queue)
        {
            item.SetKey();

            if (!queue.TryQueue(item))
            {
                TcpBrocastBuffer.WriteLog("API", $"[{item.Key}] Response 503, Path={item.Path}, Method={item.Method} RequestId={item.Id},  Queue is full.");
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            else
            {
                TcpBrocastBuffer.WriteLog("API", $"[{item.Key}] Response 200, Path={item.Path}, Method={item.Method}, RequestId={item.Id}, Queued successfully.");
                return Results.Text("success");
            }
        }
    }
}
