using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public static class EnumPaser
    {
        public static MpdsDevType ConvertingMpdsDevType(byte data)
        {
            MpdsDevType value = (MpdsDevType)data;

            return Enum.IsDefined(value) ? value : MpdsDevType.Unknown;
        }

        public static string GetKorString(DischargeMethod method)
        {
            switch (method)
            {
                case DischargeMethod.SurfaceVelo:
                    return "지표유속";
                case DischargeMethod.MidSection:
                    return "중간단면적";
                case DischargeMethod.VeloDist:
                    return "유속분포";
                case DischargeMethod.RatingCurve:
                    return "수위-유량곡선";
                default:
                    return "";
            }
        }

        public static string GetKorString(HydroMeterType meter)
        {
            switch (meter)
            {
                case HydroMeterType.RnDVideoCollector:
                    return "영상유속계";
                case HydroMeterType.RnDMpdsCollector:
                    return "전자파표면유속계";
                case HydroMeterType.RQ30D:
                    return "RQ30D";
                case HydroMeterType.ChannelMaster:
                    return "ChannelMaster";
                case HydroMeterType.SonTek:
                    return "Argonaut";
                default:
                    return "알수없음";

            }
        }
    }
}
