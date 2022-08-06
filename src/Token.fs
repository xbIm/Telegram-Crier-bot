module CrierBot.Token

open System

let getToken key (chatId: int64) =
    chatId |> Convert.ToString |> Aes.encrypt key

let getChatId key (token: string) =
    try
        token |> Aes.decrypt key |> Convert.ToInt64 |> Result.Ok
    with
    | e -> Result.Error e
