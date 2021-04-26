FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# copy everything else and build app
COPY EdgeNode ./EdgeNode/
COPY Common ./Common/
WORKDIR /app/EdgeNode
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet restore && dotnet ef migrations add Initial && dotnet ef database update
RUN dotnet publish -c Release -o out
RUN cp node.db out/node.db

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app

# certificates
COPY EdgeNode/key/rootCA.pem /usr/share/ca-certificates/mkcert.crt
RUN cd /etc/ && echo "mkcert.crt" >> ca-certificates.conf && update-ca-certificates

COPY --from=build /app/EdgeNode/out ./
ENTRYPOINT ["dotnet", "EdgeNode.dll"]