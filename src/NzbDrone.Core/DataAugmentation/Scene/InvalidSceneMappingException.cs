using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.DataAugmentation.Scene
{
    public class InvalidSceneMappingException : NzbDroneException
    {
        public InvalidSceneMappingException(IEnumerable<SceneMapping> mappings, string releaseTitle)
            : base(FormatMessage(mappings, releaseTitle))
        {

        }

        private static string FormatMessage(IEnumerable<SceneMapping> mappings, string releaseTitle)
        {
            return $"Scene Mappings contains a conflict for tvdbids {string.Join(",", mappings.Select(v => v.TvdbId.ToString()))}. Please notify Sonarr developers. ({releaseTitle})";
        }
    }
}
