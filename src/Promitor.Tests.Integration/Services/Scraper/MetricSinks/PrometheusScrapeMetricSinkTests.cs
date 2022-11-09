﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Promitor.Agents.Core;
using Promitor.Tests.Integration.Data;
using Promitor.Tests.Integration.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Promitor.Tests.Integration.Services.Scraper.MetricSinks
{
    public class PrometheusScrapeMetricSinkTests : ScraperIntegrationTest
    {
        public PrometheusScrapeMetricSinkTests(ITestOutputHelper testOutput)
          : base(testOutput)
        {
        }

        [Fact]
        public async Task Prometheus_Scrape_ReturnsOk()
        {
            // Arrange
            var prometheusClient = PrometheusClientFactory.CreateForPrometheusScrapingEndpointInScraperAgent(Configuration);

            // Act
            var response = await prometheusClient.ScrapeWithResponseAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("promitor_ratelimit_arm")]
        [InlineData("promitor_scrape_success")]
        [InlineData("promitor_scrape_error")]
        [ClassData(typeof(AvailableMetricsTestInputGenerator))]
        public async Task Prometheus_Scrape_ExpectedMetricIsAvailable(string expectedMetricName)
        {
            // Arrange
            var prometheusClient = PrometheusClientFactory.CreateForPrometheusScrapingEndpointInScraperAgent(Configuration);

            // Act
            var gaugeMetric = await prometheusClient.WaitForPrometheusMetricAsync(expectedMetricName);

            // Assert
            Assert.NotNull(gaugeMetric);
            Assert.Equal(expectedMetricName, gaugeMetric.Name);
            Assert.NotNull(gaugeMetric.Measurements);
            Assert.False(gaugeMetric.Measurements.Count < 1);
        }

        [Theory]
        [ClassData(typeof(AvailableMetricsTestInputGenerator))]
        public async Task Prometheus_Scrape_EveryMetricHasAnErrorMetric(string expectedMetricName)
        {
            // Arrange
            const string errorMetricName = "promitor_scrape_error";
            var prometheusClient = PrometheusClientFactory.CreateForPrometheusScrapingEndpointInScraperAgent(Configuration);

            // Act
            var gaugeMetric = await prometheusClient.WaitForPrometheusMetricAsync(errorMetricName, "metric_name", expectedMetricName);

            // Assert
            Assert.NotNull(gaugeMetric);
        }

        [Theory]
        [ClassData(typeof(AvailableMetricsTestInputGenerator))]
        public async Task Prometheus_Scrape_EveryMetricHasAnSuccessMetric(string expectedMetricName)
        {
            // Arrange
            const string errorMetricName = "promitor_scrape_success";
            var prometheusClient = PrometheusClientFactory.CreateForPrometheusScrapingEndpointInScraperAgent(Configuration);

            // Act
            var gaugeMetric = await prometheusClient.WaitForPrometheusMetricAsync(errorMetricName, "metric_name", expectedMetricName);

            // Assert
            Assert.NotNull(gaugeMetric);
        }

        [Fact]
        public async Task Prometheus_Scrape_Get_ReturnsVersionHeader()
        {
            // Arrange
            var prometheusClient = PrometheusClientFactory.CreateForPrometheusScrapingEndpointInScraperAgent(Configuration);

            // Act
            var response = await prometheusClient.ScrapeWithResponseAsync();

            // Assert
            Assert.True(response.Headers.Contains(HttpHeaders.AgentVersion));
            Assert.Equal(ExpectedVersion, response.Headers.GetFirstOrDefaultHeaderValue(HttpHeaders.AgentVersion));
        }

        [Theory(Skip = "todo disabled because it causes infinite(?) loop")]
        [MemberData(nameof(DimensionsData))]
        public async Task Prometheus_Scrape_ExpectedDimensionsAreAvailable(string expectedMetricName, IReadOnlyCollection<string> expectedDimensionNames)
        {
            // Arrange
            var prometheusClient = PrometheusClientFactory.CreateForPrometheusScrapingEndpointInScraperAgent(Configuration);

            // Act
            var gaugeMetric = await prometheusClient.WaitForPrometheusMetricAsync(expectedMetricName);

            // Assert
            Assert.NotNull(gaugeMetric);
            Assert.Equal(expectedMetricName, gaugeMetric.Name);
            Assert.NotNull(gaugeMetric.Measurements);
            Assert.False(gaugeMetric.Measurements.Count < 1);

            foreach (var expectedDimensionName in expectedDimensionNames)
            {
                Assert.True(gaugeMetric.Measurements[0].Labels.ContainsKey(expectedDimensionName.SanitizeForPrometheusLabelKey()));
                Assert.NotEqual("unknown", gaugeMetric.Measurements[0].Labels[expectedDimensionName]);
            }
        }
        
        public static IEnumerable<object[]> DimensionsData(){
            yield return new object[] { "promitor_demo_frontdoor_backend_health_per_backend_pool", new List<string>{ "BackendPool" } };
            yield return new object[] { "promitor_demo_frontdoor_backend_health_per_backend_pool_and_backend", new List<string>{ "BackendPool", "Backend" } };
        }
    }
}
