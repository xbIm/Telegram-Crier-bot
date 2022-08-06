module CrierBot.Handlers

open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Giraffe
open Microsoft.Extensions.Logging
open Telegram.Bot
open Telegram.Bot.Types.Enums

type BotMsg =
    | Start
    | Token
    | Help
    | Unknown

[<CLIMutable>]
type BotConfig = { token: string; host: string; key: string }

let parseBotMsg (msg: string) =
    match msg.ToLower() with
    | x when x.Contains("/start") -> BotMsg.Start
    | "/token" -> BotMsg.Token
    | "/help" -> BotMsg.Help
    | _ -> BotMsg.Unknown

let messageHandler (token: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let key = ctx.GetService<BotConfig>().key
        let client = ctx.GetService<ITelegramBotClient>()
        let logger = ctx.GetService<ILogger<HttpHandler>>()
        task {
            let msg = ctx.Request.Query["message"]
            if msg.Count = 0 then
                return! RequestErrors.BAD_REQUEST "message is empty" next ctx
            else                
                match Token.getChatId key token with
                | Ok chatId ->
                    try
                        let! _ = client.SendTextMessageAsync(chatId, msg)
                        logger.LogInformation("message to {0}", chatId)
                        return! text "ok" next ctx
                    with e ->
                        logger.LogWarning("Wrong Token", e)
                        return! RequestErrors.BAD_REQUEST "Wrong Token" next ctx                               
                | Error _ ->
                    return! RequestErrors.BAD_REQUEST "Token is empty" next ctx                
        }

let hookHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let logger = ctx.GetService<ILogger<HttpHandler>>()

        task {
            let! update = ctx.BindJsonAsync<Types.Update>()

            match update.Type with
            | UpdateType.Message ->
                let key = ctx.GetService<BotConfig>().key
                let chatId = update.Message.Chat.Id

                let msg =
                    match parseBotMsg update.Message.Text with
                    | BotMsg.Start ->
                        logger.LogInformation("new token generated")
                        Token.getToken key chatId |> fun s -> $"Your token: %s{s}"
                    | BotMsg.Token -> Token.getToken key chatId |> fun s -> $"Your new token: %s{s}"
                    | BotMsg.Help -> "DO this"
                    | BotMsg.Unknown -> "Bzz"

                do! (ctx.GetService<ITelegramBotClient>().SendTextMessageAsync(chatId, msg) :> Task)

            | _ -> ()

            return! Successful.OK () next ctx
        }
