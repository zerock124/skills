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

Agent 應該確認：
- 專案名稱是什麼？
- 這些功能之間的關聯性？
- 有沒有優先順序？
- 資料庫與認證怎麼設定？

### 使用者不確定某些選項

提供建議預設值：
- 資料庫策略：若已有資料表，建議 Database First；若全新專案，建議 Code First
- 認證方式：前後端分離建議 JWT；傳統 MVC 可用 Cookie
- 日誌：Serilog 為主流選擇
- 容器化：建議使用 Docker，方便部署與環境一致性

### 使用者需求模糊或範圍過大

引導縮小範圍：
- 先確認第一階段核心功能
- 其他功能列為後續擴充
- 建議先做 MVP（最小可行產品）

## Agent 執行步驟總結

1. **收集需求**: 依照本文件的表格逐項確認
2. **產出決策摘要**: 讓使用者最終確認
3. **確認後再動手**: 避免做了一半發現方向錯誤
4. **記錄決策**: 將決策記錄在專案文件中，方便後續維護

## 與 development-web.md 的關係

- **development-web.md**: 使用者預先填寫的專案定義檔
- **project-intake.md**: Agent 的收集與決策流程

執行順序：
1. Agent 先讀取 development-web.md
2. 已填寫的項目直接採用
3. 未填寫的項目依本文件流程詢問
4. 最終產出「架構決策摘要」讓使用者確認

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
