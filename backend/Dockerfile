FROM mcr.microsoft.com/dotnet/sdk:7.0 as build
WORKDIR /build
COPY . /build
RUN cd /build && dotnet publish -c Release
ENTRYPOINT ls -R /build

FROM mcr.microsoft.com/dotnet/aspnet:7.0 as run
WORKDIR /app
COPY --from=0 /build/Void.BetterNote/bin/Release/net7.0/publish/ .
RUN chmod +x /app/Void.BetterNote
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT /app/Void.BetterNote
