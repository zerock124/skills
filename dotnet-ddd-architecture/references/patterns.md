# 開發模式

## AutoMapper

建議保留兩層 Mapping：

- Service 層：Entity ↔ DTO
- API 層：Request / Response ↔ DTO

### 服務層

```csharp
CreateMap<Entity, FeatureDTO>();
CreateMap<EditFeatureDTO, Entity>();
```

### API 層

```csharp
CreateMap<CreateFeatureRequest, EditFeatureDTO>();
CreateMap<UpdateFeatureRequest, EditFeatureDTO>();
CreateMap<SearchFeatureRequest, SearchFeatureDTO>();
CreateMap<FeatureDTO, FeatureResponse>();
```

## Repository

- 共用資料存取優先使用泛型 Repository
- 若真的需要特定查詢再考慮特殊 Repository
- Service 不直接碰 DbContext，除非有非常明確的理由

## Unit of Work

當一個流程需要跨多個 Repository 並且要保證交易一致性時，使用 Unit of Work。

## DI 原則

- API 註冊 API 層所需服務
- Service 註冊業務服務
- Infrastructure 註冊基礎設施實作
- 測試只注入必要 mock 或最小真實元件

## Job / 排程

- 先定義排程目的
- 再定義執行頻率與失敗策略
- 最後才做具體工作實作

## Webhook / Adapter / SignalR

- 先定義事件邊界與資料格式
- 再定義錯誤處理與重試
- 最後才決定是否要放在 Service、Helper 或 Infrastructure

## 匯出 / 匯入

- 先定義檔案格式與欄位來源
- 再定義驗證規則與錯誤回報
- 最後才決定檔案生成方式與存放位置
