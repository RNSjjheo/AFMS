using AFMSDll;
using log4net;
using System.Text;

namespace AFMSLoggerVideoHydorsem
{
    public class RestApiManager
    {
        private const string METHOD_GET = "GET";
        private const string METHOD_POST = "POST";

        private static readonly ILog Log = LogManager.GetLogger("API");
        private readonly WebApplication restApi;

        public RestApiManager(WebApplication app)
        {
            restApi = app;
        }

        public async Task StartAsync()
        {
            await restApi.StartAsync();
        }

        public async Task StopAsync()
        {
            await restApi.StopAsync();
            await restApi.DisposeAsync();
        }

        public void Regist()
        {
            string path = Configuration.Instance.WebPath;

            restApi.MapPost($"/{path}", async (HttpRequest request, IRequestTaskQueue queue) =>
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
                Log.Warn($"[{item.Key}] Response 503, Path={item.Path}, Method={item.Method} RequestId={item.Id}, Queue is full.");
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            else
            {
                Log.Info($"[{item.Key}] Response 200, Path={item.Path}, Method={item.Method}, RequestId={item.Id}, Queued successfully.");
                return Results.Text("success");
            }
        }
    }
}
