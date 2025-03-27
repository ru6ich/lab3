# Используем официальный образ .NET 8 SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проекта в контейнер
COPY ["Backend/Backend.csproj", "Backend/"]
RUN dotnet restore "Backend/Backend.csproj"

# Копируем все остальные файлы и строим проект
COPY . .
WORKDIR "/src/Backend"
RUN dotnet build "Backend.csproj" -c Release -o /app/build

# Публикуем проект для продакшн-среды
FROM build AS publish
RUN dotnet publish "Backend.csproj" -c Release -o /app/publish

# Создаем финальный контейнер с уже готовым приложением
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Копируем результат сборки из предыдущего контейнера
COPY --from=publish /app/publish .

# Указываем команду для запуска приложения
ENTRYPOINT ["dotnet", "Backend.dll"]
