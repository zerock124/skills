---
name: dotnet-ddd-architecture
description: 'DDD .NET 開發架構 SKILL。適用於 ASP.NET Core 分層式專案的新功能開發、新模組建立與新專案骨架產出，涵蓋 Clean Architecture / DDD 分層、CRUD 與非 CRUD 流程、AutoMapper、DI、測試、資料庫變更與交付驗證。Use when: 需要建立新功能、新模組、新專案骨架、重構既有流程、補齊測試與文件、或需要明確的開發步驟與檢查清單。'
---

# DDD .NET 開發架構

這份 SKILL 把 ASP.NET Core 分層式專案裡真正可重複使用的開發方式固定下來，讓後續開發新專案、新功能或新模組時，可以直接照著做。

## 核心原則

- 先理解專案邊界，再開始寫程式
- 先確認輸入、輸出、資料來源、錯誤情境，再設計檔案
- 先用既有模板與規範，不要重造一套新做法
- 能小改就不要大改，能沿用就不要重寫
- 每次交付都要包含程式、測試與驗證方法

---

## Agent Flow：新專案啟動

當使用者說要開發新專案時，Agent 必須依下列階段執行，不可跳過。

### Phase 1：需求收集

先向使用者確認以下資訊，未提供的項目要主動詢問：

| 項目 | 說明 | 範例 |
|------|------|------|
| 專案名稱 | 用於 namespace、資料夾與 solution 命名 | `LabelTagProject` |
| 專案簡述 | 一句話描述這個系統做什麼 | 標籤管理與庫存追蹤 |
| 主要服務對象 | 誰會用這個系統 | 內部員工 / 外部客戶 / 兩者皆有 |
| 專案目的 | 解決什麼問題或取代什麼流程 | 取代 Excel 手動盤點 |
| 核心功能模組 | 預計需要哪些功能 | 帳號、商品、訂單、庫存、報表 |
| 目標框架 | .NET 版本 | .NET 8 / .NET 10 |
| 資料庫 | 類型與存取策略 | MSSQL + Database First / Code First |
| 認證方式 | 認證機制 | JWT / Cookie / 外部 OAuth |
| 部署方式 | 目標環境 | Docker / IIS / Azure |
| 特殊需求 | 額外整合或限制 | SignalR、排程、Line/IG Webhook、匯出 |

### Phase 2：架構決策

根據 Phase 1 的結果，Agent 必須產出以下決策：

1. Solution 名稱與各層專案命名
2. 分層結構確認（Domain / Infrastructure / Service / API / Tests）
3. 資料庫策略（Database First 或 Code First）與 scaffold / migration 指令
4. 認證策略與 JWT / OAuth 設定方式
5. 必要的 NuGet 套件清單
6. CORS 與環境設定規劃
7. 日誌策略
8. 是否需要 Docker / CI/CD

產出格式為一份「架構決策摘要」，讓使用者確認後再進入下一階段。

### Phase 3：專案骨架產出

使用者確認架構決策後，Agent 依序建立：

1. Solution 檔與各層 csproj
2. 各層基礎目錄結構
3. 共用基底類別（BaseController、ResponseDTO、PaginationDTO、SearchDTO）
4. DbContext 骨架
5. GenericRepository 與 UnitOfWork
6. Program.cs 基礎設定（DI、AutoMapper、JWT、CORS、Swagger、Serilog）
7. appsettings.json 結構
8. Dockerfile 與 docker-compose（若需要）
9. 測試專案與基礎測試設定
10. .gitignore

### Phase 4：功能開發

骨架完成後，依使用者提供的功能模組清單，逐一開發。每個功能依下列流程：

- 標準 CRUD：套用 assets 模板，依序建立 Entity → DTO → Service → Controller → Test
- 非標準流程：先定義流程邊界，再決定放哪一層，最後實作與測試

每完成一個功能模組，產出該模組的檔案清單與驗證方式。

### Phase 5：驗證與交付

所有功能完成後，Agent 產出最終交付清單：

- 完整檔案清單
- DI 註冊總覽
- AutoMapper Profile 總覽
- 資料庫設定與 migration / scaffold 指令
- 測試覆蓋範圍
- 部署步驟
- 已知限制與風險

---

## Agent Flow：既有專案新增功能

當使用者在既有專案上要求新功能時，Agent 依下列流程處理：

1. 先確認功能名稱與目的
2. 確認是否涉及資料庫變更
3. 確認是 CRUD 還是非標準流程
4. 找到專案中最接近的既有實作作為參照
5. 依 assets 模板產出新檔案
6. 同步更新 DI、Mapping、Program.cs
7. 補上測試
8. 產出變更清單與驗證方式

---

## 啟用情境

- 你要我根據既有專案建立新功能
- 你要我從零建立新專案骨架
- 你要我新增 Controller、Service、Repository、Job、Filter、Adapter、Webhook、SignalR、匯出匯入流程
- 你要我補測試、補文件、補 DI、補 AutoMapper
- 你要我針對某個功能做可重複使用的模板化設計

## 工作順序

當收到需求時，固定依下列順序處理：

1. 判斷是新專案還是既有專案新增功能
2. 若是新專案，走 Agent Flow Phase 1 → 5
3. 若是既有功能，走既有專案新增功能流程
4. 先找對應的模板與參考文件
5. 再找目前專案中最接近的實作
6. 接著確認資料流與依賴方向
7. 再決定要新增哪些檔案
8. 最後才開始實作與測試

如果需求是標準 CRUD，先套模板再微調。如果需求是非標準流程，例如 Job、Webhook、Adapter、SignalR、批次匯入匯出、排程通知，就先找流程邊界再設計。

## 判斷規則

### 標準 CRUD

適合使用以下路徑：

- Domain Entity
- Domain DTO
- Service Interface
- Service Implementation
- API Request / Response
- Mapping
- Controller
- Unit Test

### 非標準流程

適合先定義流程，再決定放哪裡：

- Job：先定義排程、觸發條件、例外處理，再做 Job 與註冊
- Webhook：先定義來源驗證、簽章、回應格式，再做 Filter / Controller / Service
- Adapter：先定義第三方輸入輸出與失敗重試，再做 Adapter / Helper
- SignalR：先定義事件、頻率、推播對象，再做 Hub 與 Service
- 匯出匯入：先定義檔案格式、欄位對應、驗證規則，再做 Service / Helper

## 必做清單

每次我幫你產出內容時，至少要回答這些問題：

- 這個功能放在哪一層
- 需要新增哪些檔案
- 哪些既有檔案要同步修改
- AutoMapper 要怎麼接
- DI 要怎麼註冊
- 測試要怎麼寫
- 這次變更如何驗證

## 交付格式

如果你要我直接動手，優先產出以下內容：

- 受影響檔案清單
- 新增或修改的類別
- 需要同步調整的設定或 DI
- 測試案例
- 驗證方式
- 若有資料庫變更，提供 scaffold / migration / deployment 方式

## 禁止事項

- 不要在沒有確認邊界前先大量重構
- 不要把業務邏輯塞進 Controller
- 不要把資料存取邏輯塞進 API 層
- 不要讓測試依賴真實外部系統，除非需求明確要求整合測試
- 不要忽略 assets 與 references，尤其是模板與規則文件

## 使用順序

開始任何任務時，依序檢查：

1. development-web.md：看使用者是否已預先填寫專案定義，已填寫的項目直接採用，空白的才詢問
2. assets：看對應模板
3. references：看架構、模式、設定、測試
4. 專案現況：看實際檔案與命名
5. 再開始產出實作

## 專案定義檔

- [development-web.md](./development-web.md)：使用者預先填寫的專案需求定義，Agent 讀取後跳過已回答的問題，未填寫的依 Agent Flow 詢問

## 參考文件

- [project-intake.md](./references/project-intake.md)：新專案啟動時的需求收集與決策流程
- [architecture.md](./references/architecture.md)：分層架構與各層責任
- [patterns.md](./references/patterns.md)：AutoMapper、Repository、UoW、Job 等開發模式
- [configuration.md](./references/configuration.md)：設定、認證、CORS、資料庫、日誌
- [testing.md](./references/testing.md)：測試策略、命名、工具與覆蓋率

## 模板資產

- [ServiceInterface.md](./assets/ServiceInterface.md)
- [Service.md](./assets/Service.md)
- [Mapping.md](./assets/Mapping.md)
- [Controller.md](./assets/Controller.md)
- [ServiceTest.md](./assets/ServiceTest.md)

## 完成定義

一個任務只有在同時滿足以下條件時才算完成：

- 程式碼可編譯或可被合理閱讀
- 必要的設定已補齊
- 測試已新增或更新
- 變更說明清楚
- 若有風險，已在交付時說明
