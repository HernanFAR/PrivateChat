using Core.UseCases.CreateUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebApi.Integrator.FuncTests.Tests.UseCases;

public class CreateUserTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public CreateUserTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConnectToHub_ShouldConnectCorrectly()
    {
        var httpContext = _fixture.PrivateChatWebApi.CreateClient();
        var contract = new CreateUserContract("Hernán");

        var httpResponse = await httpContext.PostAsJsonAsync("/user", contract);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createUserResponse = await httpResponse.Content.ReadFromJsonAsync<CreateUserCommandResponse>();

        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(createUserResponse!.Token);

        jsonToken.Claims
            .Should().Contain(
                e => e.Type == ClaimTypes.NameIdentifier);
        jsonToken.Claims
            .Should().Contain(
                e => e.Type == ClaimTypes.Name && e.Value == contract.Name);
    }
}
