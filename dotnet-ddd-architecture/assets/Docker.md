# Docker 與部署指引

## Dockerfile

放置路徑：專案根目錄/Dockerfile

```dockerfile
# 多階段建置 Dockerfile

# ========== Build Stage ==========
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 複製專案檔案並還原套件
COPY ["src/{Project}.API/{Project}.API.csproj", "src/{Project}.API/"]
COPY ["src/{Project}.Service/{Project}.Service.csproj", "src/{Project}.Service/"]
COPY ["src/{Project}.Infrastructure/{Project}.Infrastructure.csproj", "src/{Project}.Infrastructure/"]
COPY ["src/{Project}.Domain/{Project}.Domain.csproj", "src/{Project}.Domain/"]

RUN dotnet restore "src/{Project}.API/{Project}.API.csproj"

# 複製所有原始碼
COPY . .

# 建置並發佈
WORKDIR "/src/src/{Project}.API"
RUN dotnet build "{Project}.API.csproj" -c Release -o /app/build
RUN dotnet publish "{Project}.API.csproj" -c Release -o /app/publish

# ========== Runtime Stage ==========
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 設定時區（選用）
ENV TZ=Asia/Taipei
RUN apt-get update && apt-get install -y tzdata && \
    ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && \
    echo $TZ > /etc/timezone

# 複製發佈檔案
COPY --from=build /app/publish .

# 設定環境變數
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# 暴露 Port
EXPOSE 8080

# 健康檢查
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# 啟動應用程式
ENTRYPOINT ["dotnet", "{Project}.API.dll"]
```

## .dockerignore

```
# Git
.git
.gitignore
.gitattributes

# Build outputs
**/bin/
**/obj/
**/out/
**/publish/

# IDE
.vs/
.vscode/
*.user
*.suo

# Tests
**/tests/
**/*Tests/

# Logs
logs/
*.log

# Others
README.md
LICENSE
*.md
```

## docker-compose.yml

放置路徑：專案根目錄/docker-compose.yml

```yaml
version: '3.8'

services:
  # API 服務
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: {project}_api
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=db;Database={Project}DB;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True;
      - JwtSettings__SecretKey=${JWT_SECRET_KEY}
      - JwtSettings__Issuer={Project}API
      - JwtSettings__Audience={Project}Client
    depends_on:
      db:
        condition: service_healthy
    networks:
      - {project}_network
    restart: unless-stopped
    volumes:
      - ./logs:/app/logs
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 3s
      retries: 3
      start_period: 40s

  # MSSQL 資料庫
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: {project}_db
    ports:
      - "1433:1433"
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Password
      - MSSQL_PID=Developer
    volumes:
      - mssql_data:/var/opt/mssql
    networks:
      - {project}_network
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Password -Q 'SELECT 1' || exit 1"]
      interval: 10s
      timeout: 3s
      retries: 10
      start_period: 10s

volumes:
  mssql_data:
    driver: local

networks:
  {project}_network:
    driver: bridge
```

## .env 檔案（環境變數）

放置路徑：專案根目錄/.env

```env
# JWT 設定
JWT_SECRET_KEY=YourSuperSecretKeyAtLeast32CharactersLong!

# 資料庫設定
DB_PASSWORD=YourStrong@Password

# 其他敏感設定
LINE_CHANNEL_SECRET=your-line-channel-secret
WEBHOOK_SECRET=your-webhook-secret
```

**注意**: .env 檔案應加入 .gitignore，不要提交到版控。

## Docker 指令

### 建置映像檔

```powershell
# 建置映像檔
docker build -t {project}-api:latest .

# 指定 Dockerfile 路徑
docker build -f Dockerfile -t {project}-api:latest .
```

### 執行容器

```powershell
# 單獨執行 API 容器
docker run -d -p 8080:8080 --name {project}_api {project}-api:latest

# 使用環境變數
docker run -d -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;..." `
  --name {project}_api {project}-api:latest
```

### Docker Compose 指令

```powershell
# 啟動所有服務（背景執行）
docker-compose up -d

# 查看日誌
docker-compose logs -f api

# 停止所有服務
docker-compose down

# 停止並刪除 Volume
docker-compose down -v

# 重新建置並啟動
docker-compose up -d --build

# 查看服務狀態
docker-compose ps
```

### 查看與除錯

```powershell
# 進入容器
docker exec -it {project}_api /bin/bash

# 查看日誌
docker logs {project}_api -f

# 查看容器資源使用
docker stats {project}_api

# 檢查健康狀態
docker inspect --format='{{.State.Health.Status}}' {project}_api
```

## Azure Container Registry (ACR) 部署

### 1. 建立 ACR

```powershell
# 登入 Azure
az login

# 建立資源群組
az group create --name {project}RG --location eastasia

# 建立 ACR
az acr create --resource-group {project}RG --name {project}acr --sku Basic
```

### 2. 推送映像檔到 ACR

```powershell
# 登入 ACR
az acr login --name {project}acr

# 標記映像檔
docker tag {project}-api:latest {project}acr.azurecr.io/{project}-api:latest

# 推送映像檔
docker push {project}acr.azurecr.io/{project}-api:latest
```

### 3. 部署到 Azure Container Instances (ACI)

```powershell
# 建立容器實例
az container create `
  --resource-group {project}RG `
  --name {project}-api `
  --image {project}acr.azurecr.io/{project}-api:latest `
  --dns-name-label {project}-api `
  --ports 8080 `
  --registry-login-server {project}acr.azurecr.io `
  --registry-username {project}acr `
  --registry-password $(az acr credential show --name {project}acr --query "passwords[0].value" -o tsv)
```

### 4. 部署到 Azure App Service (Container)

```powershell
# 建立 App Service Plan
az appservice plan create `
  --name {project}Plan `
  --resource-group {project}RG `
  --sku B1 `
  --is-linux

# 建立 Web App
az webapp create `
  --resource-group {project}RG `
  --plan {project}Plan `
  --name {project}-api `
  --deployment-container-image-name {project}acr.azurecr.io/{project}-api:latest

# 設定 ACR 認證
az webapp config container set `
  --name {project}-api `
  --resource-group {project}RG `
  --docker-registry-server-url https://{project}acr.azurecr.io `
  --docker-registry-server-user {project}acr `
  --docker-registry-server-password $(az acr credential show --name {project}acr --query "passwords[0].value" -o tsv)
```

## GitHub Actions CI/CD

放置路徑：.github/workflows/docker-publish.yml

```yaml
name: Docker Build and Push

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
    - name: Checkout repository
      uses: actions/checkout@v3

    - name: Log in to Container registry
      uses: docker/login-action@v2
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}

    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v4
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=sha,prefix={{branch}}-
          type=semver,pattern={{version}}

    - name: Build and push Docker image
      uses: docker/build-push-action@v4
      with:
        context: .
        push: true
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
```

## 部署最佳實踐

1. **多階段建置**: 減少最終映像檔大小，提升安全性
2. **健康檢查**: 設定 HEALTHCHECK，確保容器狀態可監控
3. **環境變數**: 敏感資訊使用環境變數，不寫死在映像檔中
4. **日誌管理**: 將日誌輸出到 Volume 或集中式日誌系統
5. **版本標籤**: 使用語意化版本標籤，不要只用 latest
6. **資源限制**: 設定 CPU 與記憶體限制，避免資源耗盡
7. **網路隔離**: 使用自訂網路，隔離不同服務
8. **定期更新**: 定期更新基底映像檔，修補安全漏洞
