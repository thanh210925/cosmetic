# --- Giai đoạn 1: Build ứng dụng ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép file csproj vào và restore các package NuGet
COPY ["COSMETIC/COSMETIC.csproj", "COSMETIC/"]
RUN dotnet restore "COSMETIC/COSMETIC.csproj"

# Sao chép toàn bộ mã nguồn còn lại
COPY . .
WORKDIR "/src/COSMETIC"
RUN dotnet build "COSMETIC.csproj" -c Release -o /app/build

# --- Giai đoạn 2: Publish ---
FROM build AS publish
RUN dotnet publish "COSMETIC.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Giai đoạn 3: Chạy ứng dụng ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .

# Thay "COSMETIC.dll" bằng tên file dll thực tế của project bạn (thường trùng với tên project)
ENTRYPOINT ["dotnet", "COSMETIC.dll"]