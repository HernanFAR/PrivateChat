﻿using CrossCutting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using VSlices.Core.Abstracts.Presentation;

// ReSharper disable once CheckNamespace
namespace Core.UseCases.ChatHubConnection;

public class ChatHubConnectionEndpoint : IEndpointDefinition
{
    public void DefineEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapHub<ChatHub>("/chat")
            .RequireAuthorization();
    }

    public static void DefineDependencies(IServiceCollection services)
    {

    }
}