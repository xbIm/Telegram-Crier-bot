module Tests

open CrierBot
open Xunit

[<Fact>]
let ``Enc/dec plain text`` () =

    let text = "1234567890"

    let key = Aes.createKey ()

    let result = text |> Aes.encrypt key |> Aes.decrypt key

    Assert.Equal(text, result)

[<Fact>]
let ``Enc/dec text`` () =

    let chatId: int64 = 1234567890

    let key = Aes.createKey ()

    let result = chatId |> Token.getToken key |> Token.getChatId key

    match result with
    | Ok result -> Assert.Equal(chatId, result)
    | Error e -> raise e
