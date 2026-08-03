using System;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Analytics;
using NzbDrone.Core.Analytics.Commands;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.AnalyticsTests
{
    [TestFixture]
    public class AnalyticsReportingServiceFixture : CoreTest<AnalyticsReportingService>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IReadarrCloudRequestBuilder>()
                  .SetupGet(c => c.Services)
                  .Returns(new HttpRequestBuilder("http://localhost/").CreateFactory());

            Mocker.GetMock<IMainDatabase>()
                  .SetupGet(c => c.DatabaseType)
                  .Returns(DatabaseType.SQLite);

            Mocker.GetMock<IConfigService>()
                  .SetupGet(c => c.MetadataSource)
                  .Returns("");

            Mocker.GetMock<IPlatformInfo>()
                  .SetupGet(c => c.Version)
                  .Returns(new Version("10.0.0"));
        }

        private void GivenAnalyticsEnabled(bool enabled)
        {
            Mocker.GetMock<IAnalyticsService>()
                  .SetupGet(c => c.IsEnabled)
                  .Returns(enabled);
        }

        [Test]
        public void should_not_send_a_report_when_disabled()
        {
            GivenAnalyticsEnabled(false);

            Subject.Execute(new SendAnalyticsCommand());

            Mocker.GetMock<IHttpClient>()
                  .Verify(c => c.Post(It.IsAny<HttpRequest>()), Times.Never());
        }

        [Test]
        public void should_post_the_allow_listed_fields_to_the_analytics_route_when_enabled()
        {
            GivenAnalyticsEnabled(true);

            Subject.Execute(new SendAnalyticsCommand());

            Mocker.GetMock<IHttpClient>()
                  .Verify(c => c.Post(It.Is<HttpRequest>(r =>
                                r.Url.ToString().EndsWith("/analytics") &&
                                r.Method == System.Net.Http.HttpMethod.Post &&
                                r.Headers.ContentType == "application/json")),
                          Times.Once());
        }

        [Test]
        public void should_swallow_failures_without_throwing()
        {
            GivenAnalyticsEnabled(true);

            Mocker.GetMock<IHttpClient>()
                  .Setup(c => c.Post(It.IsAny<HttpRequest>()))
                  .Throws(new HttpException(new HttpRequest("http://localhost/analytics"), new HttpResponse(new HttpRequest("http://localhost/analytics"), new HttpHeader(), Array.Empty<byte>(), System.Net.HttpStatusCode.ServiceUnavailable)));

            Assert.DoesNotThrow(() => Subject.Execute(new SendAnalyticsCommand()));
        }
    }
}
