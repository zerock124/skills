# 設定與啟動

## appsettings 建議區段

- Logging
- Serilog
- AppSettings
- JwtSetting
- ExternalServices
- FeatureFlags

## 啟動設定

在 Program.cs 中通常會集中處理：

- Controller 與 Json 設定
- Swagger
- CORS
- Authentication / Authorization
- AutoMapper
- DbContext
- DI 註冊
- Background Job
- 日誌

## JWT

- 先定義 Issuer、Audience、SecretKey
- 再定義 Token 生命週期與 Claims
- 需要匿名的端點必須明確標註

## CORS

- 只允許必要來源
- 開發與正式環境分開管理
- 不要為了方便直接全開

## 資料庫

- 若採 Database First，變更資料表後要重新檢查 scaffold 或對應類別
- 若採 Code First，則要定義 migration 與部署流程
- 不要兩種模式混著用卻沒交代清楚

## 日誌

- 保留足夠上下文
- 例外要能追蹤來源與關鍵參數
- 不要把敏感資訊直接寫進 log
