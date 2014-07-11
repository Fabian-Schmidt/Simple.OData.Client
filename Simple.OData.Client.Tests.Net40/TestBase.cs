﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
# if !NETFX_CORE
using Simple.OData.Client.TestUtils;
using Simple.OData.NorthwindModel;
#endif

#pragma warning disable 3001,3008

namespace Simple.OData.Client.Tests
{
    public class TestBase : IDisposable
    {
        protected string _serviceUri;
# if !NETFX_CORE
        protected TestService _service;
#endif
        protected IODataClient _client;

        public TestBase()
        {
#if NETFX_CORE
            _serviceUri = "http://NORTHWIND/Northwind.svc/";
#else
            _service = new TestService(typeof(NorthwindService));
            _serviceUri = _service.ServiceUri.AbsoluteUri;
#endif
            _client = CreateClientWithDefaultSettings();
        }

        public IODataClient CreateClientWithDefaultSettings()
        {
            return new ODataClient(_serviceUri);
        }

        public void Dispose()
        {
            if (_client != null)
            {
                DeleteTestData().Wait();
            }

#if NETFX_CORE
#else
            if (_service != null)
            {
                _service.Dispose();
                _service = null;
            }
#endif
        }

        private async Task DeleteTestData()
        {
            var products = await _client.FindEntriesAsync("Products");
            foreach (var product in products)
            {
                if (product["ProductName"].ToString().StartsWith("Test"))
                    await _client.DeleteEntryAsync("Products", product);
            }
            var categories = await _client.FindEntriesAsync("Categories");
            foreach (var category in categories)
            {
                if (category["CategoryName"].ToString().StartsWith("Test"))
                    await _client.DeleteEntryAsync("Categories", category);
            }
            var transports = await _client.FindEntriesAsync("Transport");
            foreach (var transport in transports)
            {
                if (int.Parse(transport["TransportID"].ToString()) > 2)
                    await _client.DeleteEntryAsync("Transport", transport);
            }
            var employees = await _client.FindEntriesAsync("Employees");
            foreach (var employee in employees)
            {
                if (employee["LastName"].ToString().StartsWith("Test"))
                    await _client.DeleteEntryAsync("Employees", employee);
            }
        }

        public async static Task AssertThrowsAsync<T>(Func<Task> testCode) where T : Exception
        {
            try
            {
                await testCode();
                throw new Exception(string.Format("Expected exception: {0}", typeof (T)));
            }
            catch (T)
            {
            }
            catch (AggregateException exception)
            {
                Assert.IsType<T>(exception.InnerExceptions.Single());
            }
        }
    }
}
