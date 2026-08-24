using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;

namespace AFMSDll
{
    public static class WindowsServiceManager
    {
        public static bool IsServiceInstalled(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("서비스 이름이 필요합니다.", nameof(serviceName));

            ServiceController[] services = ServiceController.GetServices();

            try
            {
                return services.Any(service =>
                    string.Equals(service.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                foreach (ServiceController service in services)
                    service.Dispose();
            }
        }

        public static ServiceInstallResult EnsureInstalled(string programPath, string serviceName, string? displayName = null, bool startAfterInstall = false)
        {
            if (string.IsNullOrWhiteSpace(programPath))
                throw new ArgumentException("프로그램 경로가 필요합니다.", nameof(programPath));

            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("서비스 이름이 필요합니다.", nameof(serviceName));

            string fullProgramPath = Path.GetFullPath(programPath);

            if (!File.Exists(fullProgramPath))
            {
                return new ServiceInstallResult
                {
                    Status = ServiceInstallStatus.ProgramNotFound,
                    ServiceName = serviceName,
                    ProgramPath = fullProgramPath,
                    Message = "Windows Service로 등록할 프로그램을 찾을 수 없습니다."
                };
            }

            if (IsServiceInstalled(serviceName))
            {
                return new ServiceInstallResult
                {
                    Status = ServiceInstallStatus.AlreadyInstalled,
                    ServiceName = serviceName,
                    ProgramPath = fullProgramPath,
                    Message = "이미 등록된 Windows Service입니다."
                };
            }

            if (!IsAdministrator())
            {
                return new ServiceInstallResult
                {
                    Status = ServiceInstallStatus.AdministratorRequired,
                    ServiceName = serviceName,
                    ProgramPath = fullProgramPath,
                    Message = "Windows Service 등록에는 관리자 권한이 필요합니다."
                };
            }

            displayName ??= serviceName;

            ServiceInstallResult installResult = InstallService(fullProgramPath, serviceName, displayName);

            if (installResult.Status != ServiceInstallStatus.Installed) return installResult;

            if (!startAfterInstall) return installResult;

            return StartService(serviceName, fullProgramPath);
        }

        private static ServiceInstallResult InstallService(string programPath, string serviceName, string displayName)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "sc.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            startInfo.ArgumentList.Add("create");
            startInfo.ArgumentList.Add(serviceName);

            startInfo.ArgumentList.Add("binPath=");
            startInfo.ArgumentList.Add(programPath);

            startInfo.ArgumentList.Add("start=");
            startInfo.ArgumentList.Add("auto");

            startInfo.ArgumentList.Add("DisplayName=");
            startInfo.ArgumentList.Add(displayName);

            using Process? process = Process.Start(startInfo);

            if (process == null)
            {
                return new ServiceInstallResult
                {
                    Status = ServiceInstallStatus.InstallFailed,
                    ServiceName = serviceName,
                    ProgramPath = programPath,
                    Message = "sc.exe를 실행하지 못했습니다."
                };
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return new ServiceInstallResult
                {
                    Status = ServiceInstallStatus.InstallFailed,
                    ServiceName = serviceName,
                    ProgramPath = programPath,
                    ExitCode = process.ExitCode,
                    Message = string.IsNullOrWhiteSpace(error) ? output : error
                };
            }

            return new ServiceInstallResult
            {
                Status = ServiceInstallStatus.Installed,
                ServiceName = serviceName,
                ProgramPath = programPath,
                ExitCode = process.ExitCode,
                Message = "Windows Service 등록이 완료되었습니다."
            };
        }

        private static ServiceInstallResult StartService(string serviceName, string programPath)
        {
            try
            {
                using ServiceController service = new(serviceName);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));

                return new ServiceInstallResult
                {
                    Status = ServiceInstallStatus.InstalledAndStarted,
                    ServiceName = serviceName,
                    ProgramPath = programPath,
                    Message = "Windows Service 등록 및 시작이 완료되었습니다."
                };
            }
            catch (Exception ex)
            {
                return new ServiceInstallResult
                {
                    Status = ServiceInstallStatus.StartFailed,
                    ServiceName = serviceName,
                    ProgramPath = programPath,
                    Message = $"서비스는 등록되었지만 시작하지 못했습니다. {ex.Message}"
                };
            }
        }

        public static bool IsAdministrator()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();

            WindowsPrincipal principal = new(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
