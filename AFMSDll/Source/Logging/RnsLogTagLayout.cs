using System.Text.RegularExpressions;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Layout.Pattern;

namespace AFMSDll
{
    public static class RnsLogTagLayout
    {
        public const int DefaultWidth = 16;

        private static readonly Regex LoggerPattern = new(
            @"%(?:-?\d+)?(?:\.\d+)?(?:logger|c)(?:\{\d+\})?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 모든 log4net appender의 태그를 왼쪽 정렬 고정 폭으로 표시합니다.
        /// 짧은 태그는 오른쪽을 공백으로 채우고 긴 태그는 폭만큼 잘라냅니다.
        /// </summary>
        public static void Apply(int width = DefaultWidth)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));

            string fixedWidthPattern = $"%fixedlogger{{{width}}}";
            foreach (IAppender appender in LogManager.GetRepository().GetAppenders())
            {
                if (appender is not AppenderSkeleton appenderSkeleton ||
                    appenderSkeleton.Layout is not PatternLayout layout) continue;

                string conversionPattern = layout.ConversionPattern;
                string updatedPattern = LoggerPattern.Replace(conversionPattern, fixedWidthPattern);
                if (updatedPattern == conversionPattern) continue;

                layout.AddConverter("fixedlogger", typeof(FixedWidthLoggerPatternConverter));
                layout.ConversionPattern = updatedPattern;
                layout.ActivateOptions();
                appenderSkeleton.ActivateOptions();
            }
        }
    }

    public sealed class FixedWidthLoggerPatternConverter : PatternLayoutConverter
    {
        private int width = RnsLogTagLayout.DefaultWidth;
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
}
