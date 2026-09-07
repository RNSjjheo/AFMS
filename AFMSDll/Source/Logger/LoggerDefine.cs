using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public enum LoggerKind
    { 
        // 유속계: 0~ 49
        // 수위계: 50 ~ 59
        // VTH: 60 ~ 70
        ChannelMaster = 0,
        EWSVRnDCollector = 10,
        VideoHydrosem = 40,
    }

    public static class LoggerDefine
    {
        public static string GetSerivceName(LoggerKind logger)
        {
            switch (logger)
            {
                case LoggerKind.ChannelMaster:
                    return "AFMSLoggerChannelMaster";

                case LoggerKind.EWSVRnDCollector:
                    return "AFMSLoggerEWSVRnDCollector";

                case LoggerKind.VideoHydrosem:
                    return "AFMSLoggerVideoHydrosem";
            }

            return "AMFSUnknown";
        }

        public static string GetDeviceName(LoggerKind logger)
        {
            switch (logger)
            {
                case LoggerKind.ChannelMaster:
                    return "초음파유속계";

                case LoggerKind.EWSVRnDCollector:
                    return "전자파유속계";

                case LoggerKind.VideoHydrosem:
                    return "영상유속계";
            }

            return "AMFSUnknown";
        }

        public static string GetDescription(LoggerKind logger)
        {
            switch (logger)
            {
                case LoggerKind.ChannelMaster:
                    return "초음파유속계(ChannelMaster)";

                case LoggerKind.EWSVRnDCollector:
                    return "전자파유속계(RNSEA R&D)";

                case LoggerKind.VideoHydrosem:
                    return "영상유속계(하이드로셈)";
            }

            return "AMFSUnknown";
        }
    }
}
