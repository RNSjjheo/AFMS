using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public enum ServiceInstallStatus
    {
        AlreadyInstalled,
        Installed,
        InstalledAndStarted,
        ProgramNotFound,
        AdministratorRequired,
        InstallFailed,
        StartFailed
    }

    public sealed class ServiceInstallResult
    {
        public ServiceInstallStatus Status { get; init; }

        public string ServiceName { get; init; } = string.Empty;

        public string ProgramPath { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public int ExitCode { get; init; }
    }
}
