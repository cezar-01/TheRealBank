# Usa a imagem do .NET 9 para construir o projeto
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia os arquivos do projeto e restaura as dependencias
COPY . .
RUN dotnet restore "./TheRealBank.UI/TheRealBank.UI.csproj"

# Constroi a aplicacao (Release)
RUN dotnet publish "./TheRealBank.UI/TheRealBank.UI.csproj" -c Release -o /app/publish

# Cria a imagem final leve apenas para rodar o site
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Garante que o ASP.NET escute na 8080 (padrao do .NET 9), em todas as interfaces
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Comando para iniciar o site
ENTRYPOINT ["dotnet", "TheRealBank.UI.dll"]