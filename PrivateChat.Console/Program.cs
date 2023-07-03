using PrivateChat.Console.Core;

PrivateChatFacade facade;

while (true)
{
    Console.WriteLine("¿Cual es tú nombre? ");
    var username = Console.ReadLine() ?? "";

    var facadeCreationResult = await PrivateChatFacade.CreateForNameAsync(username,
        (fromUser, fromUserId, _, message, datetime) =>
        {
            ClearCurrentConsoleLine();

            Console.WriteLine($"{fromUser}#{fromUserId}: {message} - A las {datetime.DateTime}");
            Console.Write($"{username}: ");
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
        if (message == "/salir-app")
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

            if (message is "/salir" or "/salir-app")
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

static void ClearCurrentConsoleLine()
{
    var currentLineCursor = Console.CursorTop;
    Console.SetCursorPosition(0, Console.CursorTop);
    for (var i = 0; i < Console.WindowWidth; i++)
        Console.Write(" ");
    Console.SetCursorPosition(0, currentLineCursor);
}
