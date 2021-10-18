﻿using System;
using System.Runtime.CompilerServices;

namespace NzbDrone.Core.HealthCheck.Checks
{
    public class MediaInfoDllCheck : HealthCheckBase
    {
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public override HealthCheck Check()
        {
            try
            {
                var mediaInfo = new MediaInfo.MediaInfo();
            }
            catch (Exception e)
            {
                return new HealthCheck(GetType(), HealthCheckResult.Warning, $"MediaInfo Library could not be loaded {e.Message}");
            }

            return new HealthCheck(GetType());
        }
    }
}
