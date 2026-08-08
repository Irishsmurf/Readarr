using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common.Http
{
    // A server that answers with prose instead of JSON - a reverse proxy's "Service
    // Unavailable", a captive portal, an API that moved - used to surface as a bare
    // JsonReaderException naming neither the host nor the status. Everything needed to
    // work out which server misbehaved goes in the message, so it survives log and
    // crash-report truncation.
    public class InvalidJsonResponseException : HttpException
    {
        private const int ContentSampleLength = 128;

        private static readonly Regex CollapseWhitespaceRegex = new (@"\s+", RegexOptions.Compiled);

        public InvalidJsonResponseException(HttpResponse response, JsonException innerException)
            : base(response.Request, response, BuildMessage(response), innerException)
        {
        }

        private static string BuildMessage(HttpResponse response)
        {
            var contentType = response.Headers.ContentType.IsNotNullOrWhiteSpace()
                ? response.Headers.ContentType
                : "no content type";

            return "Server responded with content that could not be parsed as JSON. This disruption may be temporary, please try again later." +
                   $" [{(int)response.StatusCode}:{response.StatusCode}] [{contentType}] [{response.Request.Url}] Content: '{GetContentSample(response)}'";
        }

        private static string GetContentSample(HttpResponse response)
        {
            var content = response.Content;

            if (content.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            // An html error page arrives as hundreds of newline-separated tags; collapsing
            // first means the sample holds real text rather than the first line of markup.
            var collapsed = CollapseWhitespaceRegex.Replace(content, " ").Trim();

            return collapsed.Length > ContentSampleLength
                ? collapsed.Substring(0, ContentSampleLength) + "..."
                : collapsed;
        }
    }
}
