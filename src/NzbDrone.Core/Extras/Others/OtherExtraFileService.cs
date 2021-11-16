using Microsoft.Extensions.Logging;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Extras.Others
{
    public interface IOtherExtraFileService : IExtraFileService<OtherExtraFile>
    {
    }

    public class OtherExtraFileService : ExtraFileService<OtherExtraFile>, IOtherExtraFileService
    {
        public OtherExtraFileService(IExtraFileRepository<OtherExtraFile> repository, ISeriesService seriesService, IDiskProvider diskProvider, IRecycleBinProvider recycleBinProvider, ILogger<OtherExtraFileService> logger)
            : base(repository, seriesService, diskProvider, recycleBinProvider, logger)
        {
        }
    }
}
