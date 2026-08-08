using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Update
{
    public interface IUpdatePackageProvider
    {
        UpdatePackage GetLatestUpdate(string branch, Version currentVersion);
        List<UpdatePackage> GetRecentUpdates(string branch, Version currentVersion, Version previousVersion = null);
    }

    public class UpdatePackageProvider : IUpdatePackageProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly IReadarrCloudRequestBuilder _cloudRequestBuilder;
        private readonly IHttpRequestBuilderFactory _requestBuilder;
        private readonly IPlatformInfo _platformInfo;
        private readonly IMainDatabase _mainDatabase;
        private readonly Logger _logger;

        public UpdatePackageProvider(IHttpClient httpClient, IReadarrCloudRequestBuilder requestBuilder, IPlatformInfo platformInfo, IMainDatabase mainDatabase, Logger logger)
        {
            _platformInfo = platformInfo;
            _cloudRequestBuilder = requestBuilder;
            _requestBuilder = requestBuilder.Services;
            _httpClient = httpClient;
            _mainDatabase = mainDatabase;
            _logger = logger;
        }

        public UpdatePackage GetLatestUpdate(string branch, Version currentVersion)
        {
            if (!_cloudRequestBuilder.ServicesConfigured)
            {
                return null;
            }

            var request = _requestBuilder.Create()
                                         .Resource("/update/{branch}")
                                         .AddQueryParam("version", currentVersion)
                                         .AddQueryParam("os", OsInfo.Os.ToString().ToLowerInvariant())
                                         .AddQueryParam("arch", RuntimeInformation.OSArchitecture)
                                         .AddQueryParam("runtime", "netcore")
                                         .AddQueryParam("runtimeVer", _platformInfo.Version)
                                         .AddQueryParam("dbType", _mainDatabase.DatabaseType)
                                         .AddQueryParam("includeMajorVersion", true)
                                         .SetSegment("branch", branch);

            UpdatePackageAvailable update;

            try
            {
                update = _httpClient.Get<UpdatePackageAvailable>(request.Build()).Resource;
            }
            catch (Exception ex)
            {
                // An endpoint that is down, or that answers something other than the update
                // contract, means we cannot tell whether an update exists - which is the same
                // outcome as there not being one. Failing the check instead would put an error
                // in the log every six hours.
                _logger.Warn(ex, "Unable to check for updates");
                return null;
            }

            if (update == null || !update.Available)
            {
                return null;
            }

            return update.UpdatePackage;
        }

        public List<UpdatePackage> GetRecentUpdates(string branch, Version currentVersion, Version previousVersion)
        {
            if (!_cloudRequestBuilder.ServicesConfigured)
            {
                return new List<UpdatePackage>();
            }

            var request = _requestBuilder.Create()
                                         .Resource("/update/{branch}/changes")
                                         .AddQueryParam("version", currentVersion)
                                         .AddQueryParam("os", OsInfo.Os.ToString().ToLowerInvariant())
                                         .AddQueryParam("arch", RuntimeInformation.OSArchitecture)
                                         .AddQueryParam("runtime", "netcore")
                                         .AddQueryParam("runtimeVer", _platformInfo.Version)
                                         .SetSegment("branch", branch);

            if (previousVersion != null && previousVersion != currentVersion)
            {
                request.AddQueryParam("prevVersion", previousVersion);
            }

            try
            {
                return _httpClient.Get<List<UpdatePackage>>(request.Build()).Resource;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to get recent updates");
                return new List<UpdatePackage>();
            }
        }
    }
}
