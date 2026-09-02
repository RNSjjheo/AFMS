using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSediService
{
    internal sealed record SscCalculationSlot(
        int Id,
        string MeasureDate,
        string MeasureTime,
        DateTime SlotTime);

    internal sealed record SscMeasurementKey(string MeasureDate, string MeasureTime)
    {
        public string Timestamp => MeasureDate + MeasureTime;
    }

    internal sealed record ChannelMasterCell(
        int Number,
        int VelocityEastWest,
        int VelocityNorthSouth,
        int Echo1,
        int Echo2);

    internal sealed record ChannelMasterMeasurement(
        SscMeasurementKey Key,
        double Temperature,
        double Depth,
        double Pitch,
        double Roll,
        int CellCount,
        int CellSizeCm,
        int PingCount,
        int Frequency,
        int FirstCellDistanceCm,
        IReadOnlyList<ChannelMasterCell> Cells);

    internal sealed record SscCellCalculation(
        int CellNumber,
        int Echo1,
        int Echo2,
        double AverageEcho,
        double Mb,
        double Range,
        double SpreadingCoefficient,
        double SpreadingLoss,
        double WaterAbsorption,
        double WaterAbsorptionLoss,
        double SedimentAttenuation,
        double WaterCorrectedBackscatter,
        double SedimentCorrectedBackscatter);

    internal sealed record SscCalculationResult(
        string DeviceType,
        double AverageScb,
        double RegressionSlope,
        double RegressionIntercept,
        double SscSlope,
        double SscIntercept,
        double Ssc,
        double Discharge1,
        double Discharge2,
        double TotalSand1,
        double TotalSand2,
        IReadOnlyList<SscCellCalculation> Cells);
}
