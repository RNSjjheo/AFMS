using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{

    public enum ApiMethod
    {
        GET,
        POST
    }

    public enum WebConfig
    {
        WebPort = 0,
        WebVisionPath
    }

    public enum MpdsConfig
    { 
        MpdsPort = 0,
    }

    public enum MpdsDevType
    { 
        None = 0,
        RG =1,
        Geolux=2,
        ESV = 3,
        Unknown = 99
    }

    public enum HydroVideoType
    {
        NONE = 0,
        HydroSEM,
        HydroRNS
    }

    public enum HydroMeterType
    { 
        None= 0,
        ChannelMaster = 10,
        SonTek = 20,
        RQ30D = 30,
        RnDMpdsCollector = 40,
        RnDVideoCollector = 50,
    }

    public enum CommunicationType
    {
        None = 0,
        Serial,
        TcpSocket,
        WebApi,
        DBQuery
    }

    public enum VideoMeasureStatus
    {
        Normal = 0,
        Missing,
        Error
    }



    public enum DatabaseSetting
    {
        DatabaseIP = 0,
        DatabaseName,
        DatabaseAccount,
        DatabasePort,
    }

    public enum DischargeMethod
    {
        None = 0,
        SurfaceVelo = 1,    // 지표유속법
        MidSection = 2,     // 중간단면적법
        VeloDist = 3,       // 유속분포법
        RatingCurve = 4     // 수위-유량rhrtjsqjq
    }

    public enum DischargeCalculationStatus
    {
        Calculated = 0,
        BelowRatingCurveMinimum = 1,
        CalculationFailed = 2
    }

    public enum DiscVerSurfaceVelo
    { 
        Ver00 = 0,
    }

    public enum DiscVerMidSection
    {
        Ver00 = 0,
    }

    public enum DiscVerRatingCurve
    {
        Ver00 = 0,
    }

    public enum MeasurementDeviceType
    {
        None = 0,
        VelocityMeter = 1,
        WaterLevelGauge = 2
    }

    public enum DiscVerVelocityDistribution
    {
        Ver00 = 0,
    }

    public enum VelocityDistributionFitMode
    {
        AutoAsymmetric = 0,
        AutoCommonBeta = 1,
        Manual = 2,
    }
}
