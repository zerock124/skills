# 新專案啟動指引

本文件定義 Agent 在收到「建立新專案」需求時的完整收集與決策流程。

## 必須收集的資訊

Agent 在開始任何實作前，必須先確認以下項目。使用者未主動提供的，Agent 要主動詢問。

### 基本資訊

| 項目 | 用途 | 備註 |
|------|------|------|
| 專案名稱 | namespace、solution、資料夾命名 | 建議 PascalCase，例如 `OrderSystem` |
| 專案簡述 | 理解系統邊界 | 一到兩句話 |
| 主要服務對象 | 決定認證、權限、UI 需求 | 內部 / 外部 / 兩者 |
| 專案目的 | 確認核心價值 | 取代什麼流程、解決什麼問題 |

### 功能範圍

| 項目 | 用途 | 備註 |
|------|------|------|
| 核心功能模組 | 決定 Entity、Service、Controller 數量 | 例如：帳號、商品、訂單 |
| 非標準流程 | 決定是否需要 Job、Webhook、Adapter 等 | 例如：排程通知、Line 推播 |
| 匯出匯入需求 | 決定是否需要 Excel / PDF / CSV 處理 | 格式與欄位來源 |

### 技術決策

| 項目 | 選項 | 預設值 |
|------|------|--------|
| .NET 版本 | .NET 8 / .NET 10 | 依使用者指定 |
| 資料庫 | MSSQL / PostgreSQL / SQLite | MSSQL |
| 資料庫策略 | Database First / Code First | Database First |
| ORM | Entity Framework Core | EF Core |
| 認證方式 | JWT / Cookie / OAuth | JWT |
| 日誌 | Serilog / NLog / 內建 | Serilog |
| 即時通訊 | SignalR / 不需要 | 不需要 |
| 背景排程 | Quartz / Hangfire / 不需要 | 不需要 |
| 容器化 | Docker / 不需要 | Docker |
| CI/CD | GitHub Actions / Azure DevOps / 不需要 | 不需要 |

### 部署環境

| 項目 | 選項 |
|------|------|
| 部署目標 | Docker / IIS / Azure App Service / Linux |
| 開發用 Port | 預設 8080 |
| 正式環境域名 | 若已知則提供 |

## Agent 決策產出格式

收集完成後，Agent 必須產出一份「架構決策摘要」，格式如下：

```
## 架構決策摘要

- 專案名稱：{Project}
- 目標框架：.NET {Version}
- 分層結構：
  - {Project}.Domain
  - {Project}.Infrastructure
  - {Project}.Service
  - {Project}.API
  - {Project}.Tests

- 資料庫：{DB} + {Strategy}
- 認證：{Auth}
- 日誌：{Logger}
- 背景排程：{Scheduler}
- 即時通訊：{Realtime}
- 容器化：{Container}

- 核心功能模組：
  1. {Module1}
  2. {Module2}
  3. ...

- 非標準流程：
  1. {Flow1}
  2. ...

- NuGet 套件清單：
  - ...

- 預估檔案數量：約 {N} 個
```

使用者確認此摘要後，Agent 才能進入骨架產出階段。

## 常見問題處理

### 使用者只說了專案名稱

Agent 應該主動問：
- 這個系統主要給誰用？
- 主要要做什麼功能？
- 有沒有偏好的資料庫或認證方式？
- 需要排程或即時推播嗎？
- 會需要 Docker 部署嗎？

### 使用者只說了功能清單

Agent 應該主動問：
- 專案名稱要叫什麼？
- 這些功能是給內部還是外部使用？
- 資料庫要用什麼？
- 認證怎麼處理？

### 使用者說照舊專案做

Agent 應該：
1. 先找到舊專案的 SKILL 或架構設定
2. 確認要沿用的部分與要調整的部分
3. 產出架構決策摘要讓使用者確認
4. 再開始建立骨架
