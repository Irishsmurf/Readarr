using NUnit.Framework;
using NzbDrone.Common.Cloud;
using NzbDrone.Core.Analytics;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.AnalyticsTests
{
    [TestFixture]
    public class AnalyticsServiceFixture : CoreTest<AnalyticsService>
    {
        private void GivenAnalyticsEnabled(bool enabled)
        {
            Mocker.GetMock<IConfigFileProvider>()
                  .SetupGet(c => c.AnalyticsEnabled)
                  .Returns(enabled);
        }

        private void GivenServicesConfigured(bool configured)
        {
            Mocker.GetMock<IReadarrCloudRequestBuilder>()
                  .SetupGet(c => c.ServicesConfigured)
                  .Returns(configured);
        }

        [TestCase(true, true, ExpectedResult = true)]
        [TestCase(true, false, ExpectedResult = false)]
        [TestCase(false, true, ExpectedResult = false)]
        [TestCase(false, false, ExpectedResult = false)]
        public bool should_only_be_enabled_when_opted_in_and_an_endpoint_is_configured(bool analyticsEnabled, bool servicesConfigured)
        {
            GivenAnalyticsEnabled(analyticsEnabled);
            GivenServicesConfigured(servicesConfigured);

            return Subject.IsEnabled;
        }
    }
}
