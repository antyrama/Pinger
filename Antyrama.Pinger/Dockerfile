ARG RUNTIME_IMAGE_VERSION
ARG SDK_IMAGE_VERSION

FROM $RUNTIME_IMAGE_VERSION AS base
WORKDIR /app

FROM $SDK_IMAGE_VERSION AS build
WORKDIR /src
COPY ["Antyrama.Pinger/Antyrama.Pinger.csproj", "Antyrama.Pinger/"]
RUN dotnet restore "Antyrama.Pinger/Antyrama.Pinger.csproj"
COPY . .
WORKDIR "/src/Antyrama.Pinger"
RUN dotnet build "Antyrama.Pinger.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Antyrama.Pinger.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Antyrama.Pinger.dll"]
