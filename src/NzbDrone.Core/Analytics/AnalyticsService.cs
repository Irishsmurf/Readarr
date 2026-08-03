using NzbDrone.Common.Cloud;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Analytics
{
    public interface IAnalyticsService
    {
        bool IsEnabled { get; }
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IReadarrCloudRequestBuilder _cloudRequestBuilder;

        public AnalyticsService(IConfigFileProvider configFileProvider, IReadarrCloudRequestBuilder cloudRequestBuilder)
        {
            _configFileProvider = configFileProvider;
            _cloudRequestBuilder = cloudRequestBuilder;
        }

        // Reporting fires only when both the operator opted in and an endpoint has
        // been configured to receive it - an install that hasn't set
        // READARR__SERVICES_URL sends nothing, ever, regardless of this setting.
        // See docs/analytics.md and docs/ingest-endpoint.md §2.2 in the Readarr repo.
        public bool IsEnabled => _configFileProvider.AnalyticsEnabled && _cloudRequestBuilder.ServicesConfigured;
    }
}
