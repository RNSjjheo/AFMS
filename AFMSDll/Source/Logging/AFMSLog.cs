using System.Text;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Layout.Pattern;
using log4net.Repository;

namespace AFMSDll
{
    /// <summary>
    /// AFMS 프로그램에서 사용하는 log4net 구성을 직접 생성합니다.
    /// </summary>
    public static class AFMSLog
    {
        public const int DefaultTagWidth = 16;
        public const int DefaultMessageWidth = 62;

        private static readonly object SyncRoot = new();
        private static bool initialized;

        public static string LogDirectory { get; private set; } = string.Empty;

        public static void Initialize(
            bool enableConsole,
            string processName,
            int tagWidth = DefaultTagWidth,
            string? logDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(processName))
                throw new ArgumentException("프로그램명이 필요합니다.", nameof(processName));
            if (tagWidth < 1)
                throw new ArgumentOutOfRangeException(nameof(tagWidth));

            lock (SyncRoot)
            {
                if (initialized) return;

                LogDirectory = Path.GetFullPath(
                    logDirectory ?? Path.Combine(AppContext.BaseDirectory, "Logs"));
                Directory.CreateDirectory(LogDirectory);

                ILoggerRepository repository = LogManager.GetRepository();
                repository.ResetConfiguration();

                var appenders = new List<IAppender>
                {
                    CreateFileAppender(processName, tagWidth)
                };
                if (enableConsole)
                    appenders.Add(CreateConsoleAppender(tagWidth));

                BasicConfigurator.Configure(repository, appenders.ToArray());
                initialized = true;

                ILog log = LogManager.GetLogger(processName);
                if (enableConsole) log.Info("Console log start");
                log.Info($"File log start({processName}_{DateTime.Now:yyyyMMdd}.log)");
                log.Info($"Log appenders: {string.Join(", ", appenders.Select(x => x.Name))}");
                log.Info($"log4net v{typeof(LogManager).Assembly.GetName().Version}");
            }
        }

        public static void Shutdown()
        {
            lock (SyncRoot)
            {
                if (!initialized) return;
                LogManager.Shutdown();
                initialized = false;
            }
        }

        private static IAppender CreateConsoleAppender(int tagWidth)
        {
            var appender = new ConsoleAppender
            {
                Name = "Console",
                Target = ConsoleAppender.ConsoleOut,
                Layout = CreateLayout(tagWidth)
            };
            appender.ActivateOptions();
            return appender;
        }

        private static IAppender CreateFileAppender(string processName, int tagWidth)
        {
            var appender = new RollingFileAppender
            {
                Name = "DailyFile",
                File = Path.Combine(LogDirectory, $"{processName}_"),
                DatePattern = "yyyyMMdd'.log'",
                RollingStyle = RollingFileAppender.RollingMode.Date,
                StaticLogFileName = false,
                AppendToFile = true,
                ImmediateFlush = true,
                Encoding = new UTF8Encoding(false),
                LockingModel = new FileAppender.MinimalLock(),
                Layout = CreateLayout(tagWidth)
            };
            appender.ActivateOptions();
            return appender;
        }

        private static PatternLayout CreateLayout(int tagWidth)
        {
            var layout = new PatternLayout();
            layout.AddConverter("fixedlogger", typeof(FixedWidthLoggerPatternConverter));
            layout.AddConverter("blockmessage", typeof(BlockMessagePatternConverter));
            layout.ConversionPattern =
                $"[%date{{yy-MM-dd HH:mm:ss.ff}} %fixedlogger{{{tagWidth}}}] " +
                $"%blockmessage{{{DefaultMessageWidth},{tagWidth}}}%newline%exception";
            layout.ActivateOptions();
            return layout;
        }
    }

    public sealed class FixedWidthLoggerPatternConverter : PatternLayoutConverter
    {
        private int width = AFMSLog.DefaultTagWidth;
        private bool optionRead;

        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            if (!optionRead)
            {
                if (int.TryParse(Option, out int parsedWidth) && parsedWidth > 0)
                    width = parsedWidth;
                optionRead = true;
            }

            string tag = loggingEvent.LoggerName ?? string.Empty;
            if (tag.Length > width)
            {
                tag = width <= 2
                    ? new string('.', width)
                    : $"{tag[..(width - 2)]}..";
            }
            writer.Write(tag.PadRight(width));
        }
    }

    /// <summary>
    /// 쉼표로 구분된 메시지 블록을 지정된 폭 안에서 줄 단위로 배치합니다.
    /// </summary>
    public sealed class BlockMessagePatternConverter : PatternLayoutConverter
    {
        private const int DateAreaWidth = 21;
        private int messageWidth = AFMSLog.DefaultMessageWidth;
        private int tagWidth = AFMSLog.DefaultTagWidth;
        private bool optionRead;

        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            ReadOption();

            string message = loggingEvent.RenderedMessage ?? string.Empty;
            if (!message.Contains(',') || message.Contains('\r') || message.Contains('\n'))
            {
                writer.Write(message);
                return;
            }

            string[] parts = message.Split(',');
            int currentLineLength = 0;
            string continuationIndent = new(' ', DateAreaWidth + tagWidth + 3);

            for (int index = 0; index < parts.Length; index++)
            {
                string block = parts[index].Trim();
                if (index < parts.Length - 1) block += ',';

                int separatorLength = currentLineLength == 0 ? 0 : 1;
                if (currentLineLength > 0 &&
                    currentLineLength + separatorLength + block.Length > messageWidth)
                {
                    writer.WriteLine();
                    writer.Write(continuationIndent);
                    currentLineLength = 0;
                    separatorLength = 0;
                }

                if (separatorLength > 0)
                {
                    writer.Write(' ');
                    currentLineLength++;
                }

                writer.Write(block);
                currentLineLength += block.Length;
            }
        }

        private void ReadOption()
        {
            if (optionRead) return;

            string[] options = (Option ?? string.Empty).Split(',');
            if (options.Length > 0 && int.TryParse(options[0], out int parsedMessageWidth) && parsedMessageWidth > 0)
                messageWidth = parsedMessageWidth;
            if (options.Length > 1 && int.TryParse(options[1], out int parsedTagWidth) && parsedTagWidth > 0)
                tagWidth = parsedTagWidth;

            optionRead = true;
        }
    }
}
