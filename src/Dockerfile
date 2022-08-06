FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CrierBot.fsproj", "./"]
RUN dotnet restore "CrierBot.fsproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "CrierBot.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CrierBot.fsproj" -c Release -o /app/publish

FROM base AS final

RUN apt update && apt upgrade && \
      apt install -y curl

EXPOSE 8443
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CrierBot.dll"]

HEALTHCHECK CMD curl --fail http://localhost:8443/healthcheck || exit

