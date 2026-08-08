using System;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.BookInfo;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MetadataSource.BookInfo
{
    [TestFixture]
    public class BookInfoProxyChangedAuthorsFixture : CoreTest<BookInfoProxy>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IMetadataRequestBuilder>()
                  .Setup(s => s.GetRequestBuilder())
                  .Returns(new HttpRequestBuilder("https://api.bookinfo.club/v1/{route}").CreateFactory());
        }

        // The metadata request sets SuppressHttpError, so an error body reaches the
        // deserializer rather than becoming an HttpException. Building the real
        // HttpResponse<T> inside the Returns lambda means Moq runs the genuine
        // deserialization at call time - the exception under test is the real one, not a
        // stand-in thrown by the mock.
        private void GivenResponse(string content, string contentType, HttpStatusCode statusCode)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Get<RecentUpdatesResource>(It.IsAny<HttpRequest>()))
                  .Returns((HttpRequest request) => new HttpResponse<RecentUpdatesResource>(
                      new HttpResponse(request, new HttpHeader { ContentType = contentType }, content, statusCode)));
        }

        [Test]
        public void should_return_null_when_metadata_server_returns_a_plain_text_error()
        {
            GivenResponse("Service Unavailable", "text/plain", HttpStatusCode.ServiceUnavailable);

            Subject.GetChangedAuthors(DateTime.UtcNow.AddDays(-1)).Should().BeNull();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_return_null_when_metadata_server_is_unreachable()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Get<RecentUpdatesResource>(It.IsAny<HttpRequest>()))
                  .Throws(new HttpRequestException("No such host is known"));

            Subject.GetChangedAuthors(DateTime.UtcNow.AddDays(-1)).Should().BeNull();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_return_null_when_the_response_is_limited()
        {
            GivenResponse("{ \"limited\": true, \"ids\": [] }", "application/json", HttpStatusCode.OK);

            Subject.GetChangedAuthors(DateTime.UtcNow.AddDays(-1)).Should().BeNull();
        }

        [Test]
        public void should_return_the_changed_author_ids()
        {
            GivenResponse("{ \"limited\": false, \"ids\": [1, 2] }", "application/json", HttpStatusCode.OK);

            Subject.GetChangedAuthors(DateTime.UtcNow.AddDays(-1)).Should().BeEquivalentTo(new[] { "1", "2" });
        }
    }
}
