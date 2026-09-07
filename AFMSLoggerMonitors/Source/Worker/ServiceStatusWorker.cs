using System.ComponentModel;
using System.ServiceProcess;

namespace AFMSLoggerMonitors
{
    internal enum ServiceRunState
    {
        NotInstalled = -1,
        Stopped = 0,
        Running = 1
    }

    internal sealed class ServiceStatusCheckedEventArgs : EventArgs
    {
        public ServiceStatusCheckedEventArgs(string serviceName, ServiceRunState state)
        {
            ServiceName = serviceName;
            State = state;
        }

        public string ServiceName { get; }
        public ServiceRunState State { get; }
        public int StateCode => (int)State;
    }

    internal sealed class ServiceStatusWorker : IDisposable
    {
        private readonly string[] _serviceNames;
        private readonly TimeSpan _interval;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _workerTask;
        private bool _disposed;

        public ServiceStatusWorker(IEnumerable<string> serviceNames, TimeSpan interval)
        {
            ArgumentNullException.ThrowIfNull(serviceNames);
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

            _serviceNames = serviceNames.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            _interval = interval;
        }

        public event EventHandler<ServiceStatusCheckedEventArgs>? StatusChecked;
        public event EventHandler<Exception>? CheckFailed;

        public void Start()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_workerTask != null) return;

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            _workerTask = Task.Run(() => RunAsync(cancellationToken), cancellationToken);
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            CheckServices();

            using PeriodicTimer timer = new PeriodicTimer(_interval);
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false)) CheckServices();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        private void CheckServices()
        {
            ServiceController[] services;
            try
            {
                services = ServiceController.GetServices();
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
            {
                CheckFailed?.Invoke(this, exception);
                return;
            }

            try
            {
                Dictionary<string, ServiceController> installedServices = services.ToDictionary(service => service.ServiceName, StringComparer.OrdinalIgnoreCase);
                foreach (string serviceName in _serviceNames) PublishStatus(serviceName, installedServices);
            }
            finally
            {
                foreach (ServiceController service in services) service.Dispose();
            }
        }

        private void PublishStatus(string serviceName, IReadOnlyDictionary<string, ServiceController> installedServices)
        {
            if (!installedServices.TryGetValue(serviceName, out ServiceController? service))
            {
                StatusChecked?.Invoke(this, new ServiceStatusCheckedEventArgs(serviceName, ServiceRunState.NotInstalled));
                return;
            }

            try
            {
                ServiceRunState state = service.Status == ServiceControllerStatus.Running ? ServiceRunState.Running : ServiceRunState.Stopped;
                StatusChecked?.Invoke(this, new ServiceStatusCheckedEventArgs(serviceName, state));
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
            {
                CheckFailed?.Invoke(this, exception);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cancellationTokenSource?.Cancel();
            try
            {
                _workerTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _workerTask = null;
        }
    }
}
