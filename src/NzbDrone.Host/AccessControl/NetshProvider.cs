using System;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.Processes;

namespace NzbDrone.Host.AccessControl
{
    public interface INetshProvider
    {
        ProcessOutput Run(string arguments);
    }

    public class NetshProvider : INetshProvider
    {
        private readonly IProcessProvider _processProvider;
        private readonly ILogger<NetshProvider> _logger;

        public NetshProvider(IProcessProvider processProvider, ILogger<NetshProvider> logger)
        {
            _processProvider = processProvider;
            _logger = logger;
        }

        public ProcessOutput Run(string arguments)
        {
            try
            {
                var output = _processProvider.StartAndCapture("netsh.exe", arguments);

                return output;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing netsh with arguments: {Arguments}", arguments);
            }

            return null;
        }
    }
}
