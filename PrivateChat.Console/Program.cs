using PrivateChat.Console.Core;

PrivateChatFacade facade;

while (true)
{
    Console.WriteLine("¿Cual es tú nombre? ");
    var username = Console.ReadLine() ?? "";

    var facadeCreationResult = await PrivateChatFacade.CreateForNameAsync(username,
        (fromUser, fromUserId, _, message) =>
        {
            Console.WriteLine($"{fromUser}#{fromUserId}: {message}");
        }, true);

    if (facadeCreationResult.IsT0)
    {
        facade = facadeCreationResult.AsT0;

        break;
    }

    foreach (var errorString in facadeCreationResult.AsT1.Value)
    {
        Console.WriteLine(errorString);
    }
}

await using (facade)
{
    var message = string.Empty;

    while (true)
    {
        if (message == "/salir")
        {
            break;
        }

        string roomId;

        while (true)
        {
            Console.WriteLine("¿A que habitación quieres unirte? ");
            roomId = Console.ReadLine() ?? "";

            var enterRoomResult = await facade.EnterRoomAsync(roomId);

            if (enterRoomResult.IsT0)
            {
                break;
            }

            foreach (var errorString in enterRoomResult.AsT1.Value)
            {
                Console.WriteLine(errorString);
            }
        }

        while (true)
        {
            Console.Write($"{facade.UserName}: ");
            message = Console.ReadLine() ?? "";

            if (message is "/salir-sala" or "/salir")
            {
                break;
            }

            var sendMessageResult = await facade.SendMessageAsync(roomId, message);

            if (sendMessageResult.IsT0)
            {
                continue;
            }

            foreach (var errorString in sendMessageResult.AsT1.Value)
            {
                Console.WriteLine(errorString);
            }
        }
    }
}
