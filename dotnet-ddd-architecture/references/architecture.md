# 架構指引

## 目標架構

ASP.NET Core DDD 分層式專案建議採用以下依賴方向：

API → Service → Domain ← Infrastructure

測試層只依賴 Service 與 Domain。

## 各層責任

### Domain

- 放 Entity、DTO、Enum、Interface、共用工具
- 不依賴 API 或 Infrastructure
- 只保留最核心、最穩定的資料與抽象

### Infrastructure

- 放 DbContext、Repository、Unit of Work、外部介接與技術細節
- 負責資料庫、第三方服務、檔案存取等基礎設施
- 不承擔業務規則

### Service

- 負責業務邏輯與流程協調
- 組合 Repository、Mapper、Logger、Unit of Work
- 控制交易、驗證、查詢、資料轉換

### API

- 負責 HTTP 端點、Model、Controller、Filter、Job、Hub、Swagger、啟動設定
- 不放複雜業務邏輯
- 主要做輸入輸出轉換與回應包裝

## 典型目錄

```text
src/
├── Project.Domain/
├── Project.Infrastructure/
├── Project.Service/
└── Project.API/
tests/
└── Project.Tests/
```

## 開發優先順序

1. 先確認需求屬於哪一層
2. 再確認資料是否已存在
3. 再確認是否需要 DTO 與 Mapping
4. 再確認是否需要 DI 註冊
5. 最後補測試與文件

## 常見風險

- 只改 API，卻漏掉 Service 與測試
- 只加 DTO，卻沒加 Mapping
- 改了 Repository，卻沒更新 DI
- 新增 Job，卻沒補排程註冊
- 改了資料表，卻沒同步檢查 scaffold 與實體
