using Core.UseCases.CreateUser;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

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

        var httpResponse1 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse2 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse2.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse3 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse3.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse4 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse4.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse5 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse5.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse6 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse6.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse7 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse7.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse8 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse8.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse9 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse9.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse10 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse10.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse11 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse11.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse12 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse12.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse13 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse13.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse14 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse14.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse15 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse15.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse16 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse16.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse17 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse17.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse18 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse18.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse19 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse19.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse20 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse20.StatusCode.Should().Be(HttpStatusCode.OK);
        var httpResponse21 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse21.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var httpResponse22 = await httpClient.PostAsJsonAsync("/user", contract);
        httpResponse22.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

    }
}
