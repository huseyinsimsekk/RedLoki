using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VSDiagnostics;

namespace RedLoki.Benchmarks
{
    [CPUUsageDiagnoser]
    public class RedisLockFilterBenchmark
    {
        private RedisLockFilter _filter;
        private ActionExecutingContext _context;
        private Mock<IConnectionMultiplexer> _redisMock;
        private Mock<IDatabase> _dbMock;
        [GlobalSetup]
        public void Setup()
        {
            // Setup Redis mock
            _redisMock = new Mock<IConnectionMultiplexer>();
            _dbMock = new Mock<IDatabase>();
            _dbMock.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
            _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
            // Setup ActionExecutingContext
            var httpContext = new DefaultHttpContext();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(_redisMock.Object);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();
            // Add route data
            var routeData = new RouteData();
            routeData.Values.Add("id", "test-123");
            var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
            var actionArguments = new Dictionary<string, object>
            {
                {
                    "id",
                    "test-123"
                }
            };
            _context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), actionArguments, controller: null);
            _filter = new RedisLockFilter("id", TimeUnit.Minutes, 1);
        }

        [Benchmark]
        public async Task OnActionExecutionAsync_WithRouteLock()
        {
            await _filter.OnActionExecutionAsync(_context, async () =>
            {
                return new ActionExecutedContext(_context, new List<IFilterMetadata>(), controller: null);
            });
        }

        [Benchmark]
        public async Task OnActionExecutionAsync_MultipleRequests()
        {
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = _filter.OnActionExecutionAsync(_context, async () =>
                {
                    return new ActionExecutedContext(_context, new List<IFilterMetadata>(), controller: null);
                });
            }

            await Task.WhenAll(tasks);
        }
    }
}