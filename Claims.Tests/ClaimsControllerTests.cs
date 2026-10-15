using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;
using Xunit;

namespace Claims.Tests
{
    public class ClaimsControllerTests
    {
        [Fact]
        public async Task Get_Claims_ReturnsOkWithJsonArray()
        {
            var application = new WebApplicationFactory<Program>();

            var client = application.CreateClient();

            Console.WriteLine(client.BaseAddress);

            var response = await client.GetAsync("/claims/get");

            response.EnsureSuccessStatusCode();

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            var content = await response.Content.ReadAsStringAsync();

            using var json = JsonDocument.Parse(content);

            Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        }
    }
}