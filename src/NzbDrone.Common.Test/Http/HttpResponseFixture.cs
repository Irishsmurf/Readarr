using System;
using System.Linq;
using System.Net;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test.Http
{
    [TestFixture]
    public class HttpResponseFixture : TestBase
    {
        private const string Url = "https://api.bookinfo.club/v1/author/changed";

        private static HttpResponse Response(string content, string contentType = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var headers = contentType == null ? new HttpHeader() : new HttpHeader { ContentType = contentType };

            return new HttpResponse(new HttpRequest(Url), headers, content, statusCode);
        }

        [Test]
        public void should_throw_invalid_json_response_when_body_is_not_json()
        {
            // The reported failure: a reverse proxy answering a metadata request with prose.
            var response = Response("Service Unavailable", "text/plain", HttpStatusCode.ServiceUnavailable);

            var exception = Assert.Throws<InvalidJsonResponseException>(() => new HttpResponse<TestResource>(response));

            // The reader exception is kept as the inner one: it carries the path and position
            // that say where the body stopped looking like JSON.
            exception.InnerException.Should().BeOfType<JsonReaderException>();
        }

        [Test]
        public void should_throw_invalid_json_response_when_body_does_not_match_the_resource()
        {
            var response = Response("[1, 2, 3]", "application/json");

            var exception = Assert.Throws<InvalidJsonResponseException>(() => new HttpResponse<TestResource>(response));

            exception.InnerException.Should().BeOfType<JsonSerializationException>();
        }

        [Test]
        public void should_describe_the_failing_request_in_the_message()
        {
            var response = Response("Service Unavailable", "text/plain", HttpStatusCode.ServiceUnavailable);

            var exception = Assert.Throws<InvalidJsonResponseException>(() => new HttpResponse<TestResource>(response));

            exception.Message.Should().Contain("503");
            exception.Message.Should().Contain("ServiceUnavailable");
            exception.Message.Should().Contain("text/plain");
            exception.Message.Should().Contain(Url);
            exception.Message.Should().Contain("Service Unavailable");

            exception.Response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            exception.Request.Url.FullUri.Should().Be(Url);
        }

        [Test]
        public void should_say_so_when_the_response_carried_no_content_type()
        {
            var response = Response("Service Unavailable");

            var exception = Assert.Throws<InvalidJsonResponseException>(() => new HttpResponse<TestResource>(response));

            exception.Message.Should().Contain("no content type");
        }

        [Test]
        public void should_truncate_a_long_body_in_the_message()
        {
            var body = string.Join(Environment.NewLine, Enumerable.Repeat("<p>Something went wrong.</p>", 200));

            var response = Response(body, "text/html", HttpStatusCode.BadGateway);

            var exception = Assert.Throws<InvalidJsonResponseException>(() => new HttpResponse<TestResource>(response));

            exception.Message.Length.Should().BeLessThan(400);
            exception.Message.Should().Contain("...");
        }

        [Test]
        public void should_deserialize_json_sent_with_an_unexpected_content_type()
        {
            // Plenty of indexers and download clients label their JSON wrongly. Guarding the
            // deserializer must not turn into a content-type allowlist - this fails the moment
            // anyone adds one.
            var response = Response("{ \"field\": \"value\" }", "text/plain");

            new HttpResponse<TestResource>(response).Resource.Field.Should().Be("value");
        }

        [Test]
        public void should_deserialize_json_sent_with_no_content_type()
        {
            var response = Response("{ \"field\": \"value\" }");

            new HttpResponse<TestResource>(response).Resource.Field.Should().Be("value");
        }

        public class TestResource
        {
            public string Field { get; set; }
        }
    }
}
