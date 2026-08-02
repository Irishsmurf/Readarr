using System;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Update;

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
    }
}
