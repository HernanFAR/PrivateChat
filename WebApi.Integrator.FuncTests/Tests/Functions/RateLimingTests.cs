using Core.UseCases.CreateUser;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace WebApi.Integrator.FuncTests.Tests.Functions;

public class RateLimingTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public RateLimingTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WebApi_ShouldReturnTooManyRequestWhenAreMoreThan20RequestInLessThat10Seconds()
    {
        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        var contract = new CreateUserContract("Hernán");

        await Task.WhenAll(
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract),
            httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract));

        var response = await httpClient.PostAsJsonAsync(CreateUserEndpoint.Url, contract);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

    }
}
