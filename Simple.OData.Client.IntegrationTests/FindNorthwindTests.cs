﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
    public class FindNorthwindTestsV2Atom : FindNorthwindTests
    {
        public FindNorthwindTestsV2Atom() : base(NorthwindV2ReadOnlyUri, ODataPayloadFormat.Atom) {}
    }

    public class FindNorthwindTestsV2Json : FindNorthwindTests
    {
        public FindNorthwindTestsV2Json() : base(NorthwindV2ReadOnlyUri, ODataPayloadFormat.Json) { }
    }

    public class FindNorthwindTestsV3Atom : FindNorthwindTests
    {
        public FindNorthwindTestsV3Atom() : base(NorthwindV3ReadOnlyUri, ODataPayloadFormat.Atom) { }
    }

    public class FindNorthwindTestsV3Json : FindNorthwindTests
    {
        public FindNorthwindTestsV3Json() : base(NorthwindV3ReadOnlyUri, ODataPayloadFormat.Json) { }
    }

    public class FindNorthwindTestsV4Json : FindNorthwindTests
    {
        public FindNorthwindTestsV4Json() : base(NorthwindV4ReadOnlyUri, ODataPayloadFormat.Json) { }
    }

    public abstract class FindNorthwindTests : TestBase
    {
        protected FindNorthwindTests(string serviceUri, ODataPayloadFormat payloadFormat) : base(serviceUri, payloadFormat) {}

        protected override async Task DeleteTestData()
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
            var employees = await _client.FindEntriesAsync("Employees");
            foreach (var employee in employees)
            {
                if (employee["LastName"].ToString().StartsWith("Test"))
                    await _client.DeleteEntryAsync("Employees", employee);
            }
        }

        [Fact]
        public async Task Filter()
        {
            var products = await _client
                .For("Products")
                .Filter("ProductName eq 'Chai'")
                .FindEntriesAsync();
            Assert.Equal("Chai", products.Single()["ProductName"]);
        }

        [Fact]
        public async Task FilterStringExpression()
        {
            var products = await _client
                .For("Products")
                .Filter("substringof('ai',ProductName)")
                .FindEntriesAsync();
            Assert.Equal("Chai", products.Single()["ProductName"]);
        }

        [Fact]
        public async Task Get()
        {
            var category = await _client
                .For("Categories")
                .Key(1)
                .FindEntryAsync();
            Assert.Equal(1, category["CategoryID"]);
        }

        [Fact]
        public async Task GetNonExisting()
        {
            await AssertThrowsAsync<WebRequestException>(async () => await _client
                .For("Categories")
                .Key(-1)
                .FindEntryAsync());
        }

        [Fact]
        public async Task SkipOne()
        {
            var products = await _client
                .For("Products")
                .Skip(1)
                .FindEntriesAsync();
            Assert.Equal(20, products.Count());
        }

        [Fact]
        public async Task TopOne()
        {
            var products = await _client
                .For("Products")
                .Top(1)
                .FindEntriesAsync();
            Assert.Equal(1, products.Count());
        }

        [Fact]
        public async Task SkipOneTopOne()
        {
            var products = await _client
                .For("Products")
                .Skip(1)
                .Top(1)
                .FindEntriesAsync();
            Assert.Equal(1, products.Count());
        }

        [Fact]
        public async Task OrderBy()
        {
            var product = (await _client
                .For("Products")
                .OrderBy("ProductName")
                .FindEntriesAsync()).First();
            Assert.Equal("Alice Mutton", product["ProductName"]);
        }

        [Fact]
        public async Task OrderByDescending()
        {
            var product = (await _client
                .For("Products")
                .OrderByDescending("ProductName")
                .FindEntriesAsync()).First();
            Assert.Equal("Zaanse koeken", product["ProductName"]);
        }

        [Fact]
        public async Task SelectSingle()
        {
            var product = await _client
                .For("Products")
                .Select("ProductName")
                .FindEntryAsync();
            Assert.Contains("ProductName", product.Keys);
            Assert.DoesNotContain("ProductID", product.Keys);
        }

        [Fact]
        public async Task SelectSingleHomogenize()
        {
            var product = await _client
                .For("Products")
                .Select("Product_Name")
                .FindEntryAsync();
            Assert.Contains("ProductName", product.Keys);
            Assert.DoesNotContain("ProductID", product.Keys);
        }

        [Fact]
        public async Task SelectMultiple()
        {
            var product = await _client
                .For("Products")
                .Select("ProductID", "ProductName")
                .FindEntryAsync();
            Assert.Contains("ProductName", product.Keys);
            Assert.Contains("ProductID", product.Keys);
        }

        [Fact]
        public async Task ExpandOne()
        {
            var product = (await _client
                .For("Products")
                .OrderBy("ProductID")
                .Expand("Category")
                .FindEntriesAsync()).Last();
            Assert.Equal("Confections", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task ExpandMany()
        {
            var category = await _client
                .For("Categories")
                .Expand("Products")
                .Filter("CategoryName eq 'Beverages'")
                .FindEntryAsync();
            Assert.Equal(12, (category["Products"] as IEnumerable<object>).Count());
        }

        [Fact]
        public async Task ExpandSecondLevel()
        {
            var product = (await _client
                .For("Products")
                .OrderBy("ProductID")
                .Expand("Category/Products")
                .FindEntriesAsync()).Last();
            Assert.Equal(13, ((product["Category"] as IDictionary<string, object>)["Products"] as IEnumerable<object>).Count());
        }

        [Fact]
        public async Task Count()
        {
            var count = await _client
                .For("Products")
                .Count()
                .FindScalarAsync();
            Assert.Equal(77, int.Parse(count.ToString()));
        }

        [Fact]
        public async Task FilterCount()
        {
            var count = await _client
                .For("Products")
                .Filter("ProductName eq 'Chai'")
                .Count()
                .FindScalarAsync();
            Assert.Equal(1, int.Parse(count.ToString()));
        }

        [Fact]
        public async Task TotalCount()
        {
            var productsWithCount = await _client
                .For("Products")
                .FindEntriesWithCountAsync(true);
            Assert.Equal(77, productsWithCount.Item2);
            Assert.Equal(20, productsWithCount.Item1.Count());
        }

        [Fact]
        public async Task CombineAll()
        {
            var product = (await _client
                .For("Products")
                .OrderBy("ProductName")
                .Skip(2)
                .Top(1)
                .Expand("Category")
                .Select("Category")
                .FindEntriesAsync()).Single();
            Assert.Equal("Seafood", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task CombineAllReverse()
        {
            var product = (await _client
                .For("Products")
                .Select("Category")
                .Expand("Category")
                .Top(1)
                .Skip(2)
                .OrderBy("ProductName")
                .FindEntriesAsync()).Single();
            Assert.Equal("Seafood", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task NavigateToSingle()
        {
            var category = await _client
                .For("Products")
                .Key(new Entry() { { "ProductID", 2 } })
                .NavigateTo("Category")
                .FindEntryAsync();
            Assert.Equal("Beverages", category["CategoryName"]);
        }

        [Fact]
        public async Task NavigateToMultiple()
        {
            var products = await _client
                .For("Categories")
                .Key(2)
                .NavigateTo("Products")
                .FindEntriesAsync();
            Assert.Equal(12, products.Count());
        }

        [Fact]
        public async Task NavigateToRecursive()
        {
            var employee = await _client
                .For("Employees")
                .Key(6)
                .NavigateTo("Employee1")
                .NavigateTo("Employee1")
                .NavigateTo("Employees1")
                .Key(5)
                .FindEntryAsync();
            Assert.Equal("Steven", employee["FirstName"]);
        }

        [Fact]
        public async Task NavigateToRecursiveSingleClause()
        {
            var employee = await _client
                .For("Employees")
                .Key(6)
                .NavigateTo("Employee1/Employee1/Employees1")
                .Key(5)
                .FindEntryAsync();
            Assert.Equal("Steven", employee["FirstName"]);
        }
    }
}