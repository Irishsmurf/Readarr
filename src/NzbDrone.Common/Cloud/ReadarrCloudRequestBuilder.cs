using System;
using NzbDrone.Common.Http;

namespace NzbDrone.Common.Cloud
{
    public interface IReadarrCloudRequestBuilder
    {
        IHttpRequestBuilderFactory Services { get; }
        IHttpRequestBuilderFactory Metadata { get; }

        // False unless an update service has been configured explicitly. Anything
        // built on Services must check this first — see UpdatePackageProvider.
        bool ServicesConfigured { get; }
    }

    public class ReadarrCloudRequestBuilder : IReadarrCloudRequestBuilder
    {
        // Upstream's services host belongs to the retired Readarr project. Even when
        // it answers, it can only offer upstream builds, which do not carry this
        // fork's fixes — installing one would silently undo them. Update checks are
        // therefore off unless this points somewhere the operator controls.
        public const string ServicesUrlEnvironmentVariable = "READARR__SERVICES_URL";

        private const string UpstreamServicesUrl = "https://readarr.servarr.com/v1/";

        public ReadarrCloudRequestBuilder()
        {
            var servicesUrl = Environment.GetEnvironmentVariable(ServicesUrlEnvironmentVariable);

            ServicesConfigured = !string.IsNullOrWhiteSpace(servicesUrl);

            Services = new HttpRequestBuilder(ServicesConfigured
                    ? servicesUrl.TrimEnd('/') + "/"
                    : UpstreamServicesUrl)
                .CreateFactory();

            // The metadata endpoint is already overridable from the UI via the
            // MetadataSource setting — see MetadataRequestBuilder — which is how a
            // third-party mirror such as rreading-glasses gets used. This stays as
            // the fallback for installs that have not set one.
            Metadata = new HttpRequestBuilder("https://api.bookinfo.club/v1/{route}")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Services { get; }

        public IHttpRequestBuilderFactory Metadata { get; }

        public bool ServicesConfigured { get; }
    }
}
