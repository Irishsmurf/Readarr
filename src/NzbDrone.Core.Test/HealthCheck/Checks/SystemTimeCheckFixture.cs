using System;
using System.Text;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class SystemTimeCheckFixture : CoreTest<SystemTimeCheck>
    {
        [SetUp]
        public void Setup()
        {
            GivenServicesConfigured(true);
        }

        private void GivenServicesConfigured(bool configured)
        {
            Mocker.GetMock<IReadarrCloudRequestBuilder>()
                  .SetupGet(c => c.ServicesConfigured)
                  .Returns(configured);

            Mocker.GetMock<IReadarrCloudRequestBuilder>()
                  .SetupGet(c => c.Services)
                  .Returns(new HttpRequestBuilder("https://readarr.servarr.com/v1/").CreateFactory());
        }

        private void GivenResponse(string content)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), Encoding.ASCII.GetBytes(content)));
        }

        private void GivenServerTime(DateTime dateTime)
        {
            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>()))
                  .Returns("System time is off by more than 1 day. Scheduled tasks may not run correctly until the time is corrected");

            GivenResponse(new ServiceTimeResponse { DateTimeUtc = dateTime }.ToJson());
        }

        [Test]
        public void should_not_return_error_when_system_time_is_close_to_server_time()
        {
            GivenServerTime(DateTime.UtcNow);

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_error_when_system_time_is_more_than_one_day_from_server_time()
        {
            GivenServerTime(DateTime.UtcNow.AddDays(2));

            Subject.Check().ShouldBeError();
            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_not_make_a_request_when_no_services_url_is_configured()
        {
            GivenServicesConfigured(false);

            Subject.Check().ShouldBeOk();

            Mocker.GetMock<IHttpClient>()
                  .Verify(c => c.Execute(It.IsAny<HttpRequest>()), Times.Never());
        }

        [Test]
        public void should_not_report_a_clock_problem_when_the_response_is_not_json()
        {
            // A configured endpoint that does not serve /time says nothing about the clock.
            GivenResponse("Service Unavailable");

            Subject.Check().ShouldBeOk();
        }
    }
}
