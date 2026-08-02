using System.Net;
using System.Net.Http;
using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Integration.Test
{
    [TestFixture]
    public class AuthenticationRedirectFixture : IntegrationTest
    {
        private HttpClientHandler _handler;
        private HttpClient _httpClient;

        [SetUp]
        public void SetUp()
        {
            _handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            _httpClient = new HttpClient(_handler);
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient?.Dispose();
            _handler?.Dispose();
        }

        [Test]
        public void should_redirect_ui_request_to_login()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, RootUrl);
            var response = _httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location.ToString().Should().Contain("/login");
        }

        [Test]
        public void should_return_401_for_unauthenticated_api_request()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{RootUrl}api/v1/system/status");
            var response = _httpClient.Send(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
