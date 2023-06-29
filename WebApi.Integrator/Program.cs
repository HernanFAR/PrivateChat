using Core.UseCases.ChatHubConnection;
using Core.UseCases.CreateUser;
using Core.UseCases.EnterInRoom;
using Core.UseCases.LeaveRoom;
using Core.UseCases.SendMessage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCrossCuttingConcerns(builder.Configuration);

builder.Services.AddEndpointDefinition<ChatHubConnectionEndpoint>();
builder.Services.AddEndpointDefinition<CreateUserEndpoint>();
builder.Services.AddEndpointDefinition<EnterInRoomEndpoint>();
builder.Services.AddEndpointDefinition<LeaveRoomEndpoint>();
builder.Services.AddEndpointDefinition<SendMessageEndpoint>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.UseEndpointDefinitions();

app.Run();

public partial class Program
{

}