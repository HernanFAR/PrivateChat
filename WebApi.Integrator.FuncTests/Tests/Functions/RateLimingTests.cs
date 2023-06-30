using Core.UseCases.CreateUser;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace WebApi.Integrator.FuncTests.Tests.Functions;

public class CreateUserTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public CreateUserTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WebApi_ShouldReturnTooManyRequestWhenAreMoreThan20RequestInLessThat10Seconds()
    {
        var httpClient = _fixture.PrivateChatWebApi.CreateClient();
        var contract = new CreateUserContract("Hernán");

        await Task.WhenAll(
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract),
            httpClient.PostAsJsonAsync("/user", contract));

        var response = await httpClient.PostAsJsonAsync("/user", contract);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

    }
}
