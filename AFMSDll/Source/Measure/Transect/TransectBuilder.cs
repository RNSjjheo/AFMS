using System.Text.Json;
using System.Text.Json.Serialization;

namespace AFMSDll
{
    public static class TransectBuilder
    {
        public const string JSON_NODE_TRANSECTS = "transects";
        public const string JSON_NODE_NO = "no";
        public const string JSON_NODE_DISTANCE = "distance";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        public static TransectCollection Build(IEnumerable<double> distances)
        {
            ArgumentNullException.ThrowIfNull(distances);

            TransectCollection result = new();
            int no = 0;
            foreach (double distance in distances)
            {
                no++;
                result.Add(new Transect
                {
                    No = no,
                    CenterLeftBankDistance = distance
                });
            }

            Validate(result);
            return result;
        }

        public static TransectCollection Build(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new TransectCollection();

            TransectDataJson? data = JsonSerializer.Deserialize<TransectDataJson>(json);

            if (data?.Transects == null)
                throw new JsonException("측선 좌표 배열을 찾을 수 없습니다.");

            TransectCollection result = new();
            foreach (TransectJson item in data.Transects)
            {
                result.Add(new Transect
                {
                    No = item.No,
                    CenterLeftBankDistance = item.Distance
                });
            }

            try
            {
                Validate(result);
            }
            catch (ArgumentException ex)
            {
                throw new JsonException(ex.Message, ex);
            }

            return result;
        }

        public static bool TryBuild(string json, out TransectCollection transects)
        {
            try
            {
                transects = Build(json);
                return transects.Count > 0;
            }
            catch (JsonException)
            {
                transects = new TransectCollection();
                return false;
            }
        }

        public static string GetJson(TransectCollection source)
        {
            ArgumentNullException.ThrowIfNull(source);
            List<Transect> ordered = source.OrderBy(transect => transect.No).ToList();
            Validate(ordered);

            TransectDataJson data = new TransectDataJson
            {
                Transects = ordered
                    .Select(transect => new TransectJson
                    {
                        No = transect.No,
                        Distance = transect.CenterLeftBankDistance
                    })
                    .ToList()
            };
            return JsonSerializer.Serialize(data, JsonOptions);
        }

        private static void Validate(IEnumerable<Transect> source)
        {
            int expectedNo = 1;
            double previousDistance = double.NegativeInfinity;

            foreach (Transect transect in source)
            {
                if (transect.No != expectedNo)
                    throw new ArgumentException($"측선 번호는 1부터 연속되어야 합니다. 예상 번호: {expectedNo}");
                if (!double.IsFinite(transect.CenterLeftBankDistance) || transect.CenterLeftBankDistance < 0.0)
                    throw new ArgumentException($"측선{transect.No} 거리가 올바르지 않습니다.");
                if (transect.CenterLeftBankDistance <= previousDistance)
                    throw new ArgumentException($"측선{transect.No} 측선 거리는 이전 측선보다 커야 합니다.");

                previousDistance = transect.CenterLeftBankDistance;
                expectedNo++;
            }
        }

        private sealed class TransectDataJson
        {
            [JsonPropertyName(JSON_NODE_TRANSECTS)]
            public List<TransectJson> Transects { get; set; } = new();
        }

        private sealed class TransectJson
        {
            [JsonPropertyName(JSON_NODE_NO)]
            public int No { get; set; }

            [JsonPropertyName(JSON_NODE_DISTANCE)]
            public double Distance { get; set; }
        }
    }
}
