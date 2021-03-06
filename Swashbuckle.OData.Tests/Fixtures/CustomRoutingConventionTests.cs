﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Owin.Hosting;
using NUnit.Framework;
using Owin;
using Swashbuckle.Swagger;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNet.OData;

namespace Swashbuckle.OData.Tests
{
    [TestFixture]
    public class CustomRoutingConventionTests
    {
        [Test]
        public async Task It_supports_custom_attribute_routing_convention()
        {
            using (WebApp.Start(HttpClientUtils.BaseAddress, appBuilder => MillsSetup.Configuration(appBuilder, typeof(MillsSetup.MillsController))))
            {
                // Arrange
                var httpClient = HttpClientUtils.GetHttpClient(HttpClientUtils.BaseAddress);
                // Verify that the OData route in the test controller is valid
                var results = await httpClient.GetJsonAsync<ODataResponse<List<MillsSetup.Mill>>>("odata/Mills");
                results.Should().NotBeNull();
                results.Value.Count.Should().Be(4);

                // Act
                var swaggerDocument = await httpClient.GetJsonAsync<SwaggerDocument>("swagger/docs/v1");

                // Assert
                PathItem pathItem;
                swaggerDocument.paths.TryGetValue("/odata/Mills", out pathItem);
                pathItem.Should().NotBeNull();

                await ValidationUtils.ValidateSwaggerJson();
            }
        }
    }

    public class MillsSetup
    {
        public static void Configuration(IAppBuilder appBuilder, Type targetController)
        {
            var config = appBuilder.GetStandardHttpConfig(targetController);

            var edmModel = GetEdmModel();

            const string routeName = "ODataRoute";

            // See http://www.asp.net/web-api/overview/odata-support-in-aspnet-web-api/odata-routing-conventions
            // Create the default collection of built-in conventions.
            var conventions = ODataRoutingConventions.CreateDefault();
            // Insert the custom convention at the start of the collection.
            conventions.Insert(0, new MyAttributeRoutingConvention(routeName, config));

            config.MapODataServiceRoute(routeName, "odata", edmModel, new DefaultODataPathHandler(), conventions);

            config.EnsureInitialized();
        }

        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();

            builder.EntitySet<Mill>("Mills");

            return builder.GetEdmModel();
        }

        public class Mill
        {
            [Key]
            public long Id { get; set; }
            public string Variation { get; set; }
        }

        public class MillsController : ODataController
        {
            [EnableQuery]
            public IQueryable<Mill> GetMills()
            {
                var mills = new[]
                {
                    new Mill { Id=1, Variation = "a"},
                    new Mill { Id=2, Variation = "b"},
                    new Mill { Id=3, Variation = "c"},
                    new Mill { Id=4, Variation = "d"}
                };

                return mills.AsQueryable();
            }
        }

        public class MyAttributeRoutingConvention : AttributeRoutingConvention
        {
            public MyAttributeRoutingConvention(string routeName, HttpConfiguration configuration) : base(routeName, configuration)
            {
            }

            public MyAttributeRoutingConvention(string routeName, HttpConfiguration configuration, IODataPathTemplateHandler pathTemplateHandler) : base(routeName, configuration, pathTemplateHandler)
            {
            }

            public MyAttributeRoutingConvention(string routeName, IEnumerable<HttpControllerDescriptor> controllers) : base(routeName, controllers)
            {
            }

            public MyAttributeRoutingConvention(string routeName, IEnumerable<HttpControllerDescriptor> controllers, IODataPathTemplateHandler pathTemplateHandler) : base(routeName, controllers, pathTemplateHandler)
            {
            }
        }
    }
}