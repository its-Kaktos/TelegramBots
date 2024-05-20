FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY Instagram69bot.Downloader.csproj .
RUN dotnet restore "Instagram69bot.Downloader.csproj"

COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

RUN apt-get update \
    && apt-get install ffmpeg libgdiplus curl -y

ENTRYPOINT ["dotnet", "Instagram69bot.Downloader.dll"]