# Build aşaması için .NET SDK image'ı kullanıyoruz
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VideoDownloaderAPI.csproj", "."]

# Gerekli bağımlılıkları indiriyoruz
RUN dotnet restore "./VideoDownloaderAPI.csproj"

COPY . .
WORKDIR "/src/."
RUN dotnet build "./VideoDownloaderAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish aşaması
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoDownloaderAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Base image olarak ASP.NET Core Runtime kullanıyoruz
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Python3 ve bağımlılıkları yüklemek
RUN apt-get update && apt-get install -y python3 python3-pip

# Tools klasöründeki dosyaları container'a kopyalıyoruz
COPY Tools /app/Tools

# Yt-dlp ve ffmpeg'e yürütme izni veriyoruz
RUN chmod +x /app/Tools/yt-dlp && chmod +x /app/Tools/ffmpeg

# Yt-dlp ve ffmpeg yollarını global olarak PATH'e ekliyoruz
ENV PATH="/app/Tools:${PATH}"

# Proje dosyalarını kopyalıyoruz
COPY --from=publish /app/publish .

# Çalışma ortamı ve portlar ayarlanıyor
ENV ASPNETCORE_URLS=http://+:8080

# Entrypoint belirleniyor
ENTRYPOINT ["dotnet", "VideoDownloaderAPI.dll"]
