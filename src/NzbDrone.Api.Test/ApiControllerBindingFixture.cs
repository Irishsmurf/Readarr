using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Host;
using NzbDrone.Test.Common;

namespace NzbDrone.Api.Test
{
    [TestFixture]
    public class ApiControllerBindingFixture : TestBase
    {
        private IConfiguration CreateTestConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                { "dataProtectionFolder", Path.GetTempPath() }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private IServiceCollection CreateServiceCollection()
        {
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IConfigFileProvider>());
            return services;
        }

        [Test]
        public void configure_services_should_disable_implicit_from_services_parameters()
        {
            var services = CreateServiceCollection();
            var configuration = CreateTestConfiguration();
            var startup = new Startup(configuration);

            startup.ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();
            var apiBehaviorOptions = serviceProvider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;

            apiBehaviorOptions.DisableImplicitFromServicesParameters.Should().BeTrue(
                "Implicit FromServices parameters must be disabled so that DryIoc does not bind action parameters from DI instead of request bodies");
        }

        [Test]
        public void api_controller_action_parameters_should_not_implicitly_bind_from_services()
        {
            var services = CreateServiceCollection();
            var configuration = CreateTestConfiguration();
            var startup = new Startup(configuration);

            startup.ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();
            var actionDescriptorCollectionProvider = serviceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

            var descriptors = actionDescriptorCollectionProvider.ActionDescriptors.Items;
            descriptors.Should().NotBeEmpty();

            foreach (var descriptor in descriptors.OfType<ControllerActionDescriptor>())
            {
                foreach (var parameter in descriptor.Parameters.OfType<ControllerParameterDescriptor>())
                {
                    if (parameter.BindingInfo?.BindingSource == BindingSource.Services)
                    {
                        var hasFromServicesAttr = parameter.ParameterInfo.GetCustomAttributes(typeof(FromServicesAttribute), true).Any();
                        hasFromServicesAttr.Should().BeTrue(
                            $"Parameter '{parameter.Name}' on action '{descriptor.DisplayName}' was inferred as FromServices without an explicit [FromServices] attribute");
                    }
                }
            }
        }
    }
}
