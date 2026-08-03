using System;
using System.Runtime.InteropServices;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Analytics.Commands;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Analytics
{
    // The real reporter: its own schedule (see SendAnalyticsCommand's entry in
    // TaskManager) and its own transport, replacing the `active` flag that used to
    // ride along on update-check requests. See docs/analytics.md and
    // docs/ingest-endpoint.md §2 in the Readarr repo for the contract this
    // implements against.
    public class AnalyticsReportingService : IExecute<SendAnalyticsCommand>
    {
        private readonly IHttpClient _httpClient;
        private readonly IReadarrCloudRequestBuilder _cloudRequestBuilder;
        private readonly IAnalyticsService _analyticsService;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IConfigService _configService;
        private readonly IPlatformInfo _platformInfo;
        private readonly IMainDatabase _mainDatabase;
        private readonly Logger _logger;

        public AnalyticsReportingService(IHttpClient httpClient,
                                          IReadarrCloudRequestBuilder cloudRequestBuilder,
                                          IAnalyticsService analyticsService,
                                          IConfigFileProvider configFileProvider,
                                          IConfigService configService,
                                          IPlatformInfo platformInfo,
                                          IMainDatabase mainDatabase,
                                          Logger logger)
        {
            _httpClient = httpClient;
            _cloudRequestBuilder = cloudRequestBuilder;
            _analyticsService = analyticsService;
            _configFileProvider = configFileProvider;
            _configService = configService;
            _platformInfo = platformInfo;
            _mainDatabase = mainDatabase;
            _logger = logger;
        }

        public void Execute(SendAnalyticsCommand message)
        {
            if (!_analyticsService.IsEnabled)
            {
                return;
            }

            try
            {
                var payload = new AnalyticsPayload
                {
                    Version = BuildInfo.Version.ToString(),
                    Branch = _configFileProvider.Branch,
                    Os = OsInfo.Os.ToString().ToLowerInvariant(),
                    Arch = RuntimeInformation.OSArchitecture.ToString(),
                    RuntimeVersion = _platformInfo.Version.ToString(),
                    DbType = MapDbType(_mainDatabase.DatabaseType),
                    UsingCustomMetadataSource = _configService.MetadataSource.IsNotNullOrWhiteSpace()
                };

                var request = _cloudRequestBuilder.Services.Create()
                                                   .Resource("/analytics")
                                                   .Post()
                                                   .Build();

                request.Headers.ContentType = "application/json";
                request.SetContent(payload.ToJson());

                _httpClient.Post(request);
            }
            catch (Exception ex)
            {
                // Analytics is the least important thing this application does: a
                // dropped report must never delay anything, warn a health check, or
                // log above Debug (docs/analytics.md, Readarr repo).
                _logger.Debug(ex, "Failed to send analytics report");
            }
        }

        private static string MapDbType(DatabaseType databaseType)
        {
            return databaseType == DatabaseType.PostgreSQL ? "postgres" : "sqlite";
        }
    }
}
