FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base

ENV ASPNETCORE_ENVIRONMENT=Development

WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MVPAPI/MVPAPI.csproj", "MVPAPI/"]
RUN dotnet restore "MVPAPI/MVPAPI.csproj"
COPY . .
WORKDIR "/src/MVPAPI"
RUN dotnet build "MVPAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MVPAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MVPAPI.dll"]
