open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Serilog
open Serilog.Templates
open AspNetCoreRateLimit
open Telegram.Bot
open CrierBot.Handlers

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    let botConfig = builder.Configuration.GetSection("BotConfiguration").Get<BotConfig>()
    let isProd = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") = "Production" 
    if not isProd then
      builder.Host
        .UseSerilog(fun _ (lt:LoggerConfiguration) -> lt.WriteTo.Console() |> ignore) |> ignore
    else
      builder.Host
        .UseSerilog(fun _ (lt:LoggerConfiguration) -> lt.WriteTo.Console(ExpressionTemplate("{ {timestamp: @t, message: @m, level: @l, ex: @x, ..@p} }\n")) |> ignore) |> ignore
   
    builder.Services.AddHealthChecks() |> ignore

    builder
        .Services
        .AddGiraffe()
        .AddSingleton<BotConfig>(botConfig)        
        .AddMemoryCache()
        .AddInMemoryRateLimiting()
        .AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>()
        .AddSingleton<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting").Get<IpRateLimitOptions>())
        .AddHttpClient("tgwebhook")
        .AddTypedClient<ITelegramBotClient>(fun httpClient ->
            TelegramBotClient(botConfig.token, httpClient) :> ITelegramBotClient)
    |> ignore

    let webApp =
        choose [ GET >=> choose [ route "/" >=> htmlFile "template/index.html"; routef "/%s/send" messageHandler ]
                 POST >=> route ($"/bot{botConfig.token}") >=> hookHandler ]

    let app = builder.Build()    
    
    let webHookAddr = $"{botConfig.host}/bot{botConfig.token}"
    app.Services.GetService<ITelegramBotClient>().SetWebhookAsync(webHookAddr).GetAwaiter().GetResult()

    app.UseIpRateLimiting()
        .UseSerilogRequestLogging(fun o-> o.EnrichDiagnosticContext <- fun ctx httpCtx -> ctx.Set("trace_id", httpCtx.TraceIdentifier) )
        .UseHealthChecks("/healthcheck")
        .UseGiraffe webApp
    
    app.Run("http://*:8443")

    0 // Exit code
