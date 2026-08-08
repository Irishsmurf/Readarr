using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Update;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.UpdateTests
{
    public class UpdatePackageProviderFixture : CoreTest<UpdatePackageProvider>
    {
        // These used to call the live upstream update service over real HTTP. That
        // service belongs to the retired project and can only ever offer upstream
        // builds, so this fork does not talk to it unless an operator configures
        // their own endpoint. What matters now is that nothing is requested, and
        // that no call goes out, when none is configured.
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IReadarrCloudRequestBuilder>()
                  .SetupGet(c => c.ServicesConfigured)
                  .Returns(false);
        }

        [Test]
        public void should_not_offer_an_update_when_no_update_service_is_configured()
        {
            Subject.GetLatestUpdate("main", new Version(0, 1)).Should().BeNull();
        }

        [Test]
        public void should_return_no_recent_updates_when_no_update_service_is_configured()
        {
            Subject.GetRecentUpdates("main", new Version(0, 1), null).Should().BeEmpty();
        }

        [Test]
        public void should_not_make_a_request_when_no_update_service_is_configured()
        {
            Subject.GetLatestUpdate("main", new Version(0, 1));
            Subject.GetRecentUpdates("main", new Version(0, 1), null);

            Mocker.GetMock<IHttpClient>()
                  .Verify(c => c.Get<UpdatePackageAvailable>(It.IsAny<HttpRequest>()), Times.Never());
        }

        // A configured endpoint is not necessarily an update service: READARR__SERVICES_URL
        // is shared with analytics, so it can quite reasonably answer /update with something
        // that is not an update package. That is "no update", not a failed task.
        private void GivenConfiguredUpdateService()
        {
            Mocker.GetMock<IReadarrCloudRequestBuilder>()
                  .SetupGet(c => c.ServicesConfigured)
                  .Returns(true);

            Mocker.GetMock<IReadarrCloudRequestBuilder>()
                  .SetupGet(c => c.Services)
                  .Returns(new HttpRequestBuilder("https://readarr.servarr.com/v1/").CreateFactory());

            Mocker.GetMock<IMainDatabase>()
                  .SetupGet(c => c.DatabaseType)
                  .Returns(DatabaseType.SQLite);

            Mocker.GetMock<IPlatformInfo>()
                  .SetupGet(c => c.Version)
                  .Returns(new Version(6, 0));
        }

        [Test]
        public void should_not_offer_an_update_when_the_update_service_does_not_return_json()
        {
            GivenConfiguredUpdateService();

            Mocker.GetMock<IHttpClient>()
                  .Setup(c => c.Get<UpdatePackageAvailable>(It.IsAny<HttpRequest>()))
                  .Returns((HttpRequest request) => new HttpResponse<UpdatePackageAvailable>(
                      new HttpResponse(request, new HttpHeader { ContentType = "text/plain" }, "Service Unavailable", HttpStatusCode.ServiceUnavailable)));

            Subject.GetLatestUpdate("main", new Version(0, 1)).Should().BeNull();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_return_no_recent_updates_when_the_update_service_does_not_return_json()
        {
            GivenConfiguredUpdateService();

            Mocker.GetMock<IHttpClient>()
                  .Setup(c => c.Get<List<UpdatePackage>>(It.IsAny<HttpRequest>()))
                  .Returns((HttpRequest request) => new HttpResponse<List<UpdatePackage>>(
                      new HttpResponse(request, new HttpHeader { ContentType = "text/plain" }, "Service Unavailable", HttpStatusCode.ServiceUnavailable)));

            Subject.GetRecentUpdates("main", new Version(0, 1), null).Should().BeEmpty();

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
