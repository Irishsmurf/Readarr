using System;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.HealthCheck.Checks
{
    public class SystemTimeCheck : HealthCheckBase
    {
        private readonly IHttpClient _client;
        private readonly IReadarrCloudRequestBuilder _cloudRequestBuilder;
        private readonly Logger _logger;

        public SystemTimeCheck(IHttpClient client, IReadarrCloudRequestBuilder cloudRequestBuilder, ILocalizationService localizationService, Logger logger)
            : base(localizationService)
        {
            _client = client;
            _cloudRequestBuilder = cloudRequestBuilder;
            _logger = logger;
        }

        public override HealthCheck Check()
        {
            // No reference clock to compare against unless an operator pointed us at one,
            // and this fork contacts no service they did not choose - see
            // ReadarrCloudRequestBuilder.
            if (!_cloudRequestBuilder.ServicesConfigured)
            {
                return new HealthCheck(GetType());
            }

            var request = _cloudRequestBuilder.Services.Create()
                                              .Resource("/time")
                                              .Build();

            var response = _client.Execute(request);

            if (!Json.TryDeserialize<ServiceTimeResponse>(response.Content, out var result))
            {
                // A configured endpoint that does not serve /time tells us nothing about the
                // system clock, so it is not a clock problem to report to the user.
                _logger.Debug("Unable to read the current time from {0}, skipping the system time check", request.Url);
                return new HealthCheck(GetType());
            }

            var systemTime = DateTime.UtcNow;

            // +/- more than 1 day
            if (Math.Abs(result.DateTimeUtc.Subtract(systemTime).TotalDays) >= 1)
            {
                _logger.Error("System time mismatch. SystemTime: {0} Expected Time: {1}. Update system time", systemTime, result.DateTimeUtc);
                return new HealthCheck(GetType(), HealthCheckResult.Error, _localizationService.GetLocalizedString("SystemTimeCheckMessage"), "#system-time-off");
            }

            return new HealthCheck(GetType());
        }
    }

    public class ServiceTimeResponse
    {
        public DateTime DateTimeUtc { get; set; }
    }
}
