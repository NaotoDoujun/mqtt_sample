FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# certificates
COPY key /key
RUN openssl pkcs12 -export -out server.pfx -inkey /key/server.key -in /key/server.crt -passin pass:localstack -passout pass:localstack

# copy everything else and build app
COPY Bff ./Bff/
COPY Common ./Common/
WORKDIR /app/Bff
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet restore && \
  dotnet ef migrations add Initial --context ApplicationDbContext && \
  dotnet ef database update --context ApplicationDbContext && \
  dotnet ef migrations add Initial --context MovieDbContext && \
  dotnet ef database update --context MovieDbContext
RUN dotnet publish -c Release -o out
RUN cp bff.db out/bff.db && cp movie.db out/movie.db

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime

# Timezone setting
ENV TZ=Asia/Tokyo

WORKDIR /app
COPY --from=build /app/Bff/out ./
COPY --from=build /app/server.pfx /app/.
ENTRYPOINT ["dotnet", "Bff.dll"]