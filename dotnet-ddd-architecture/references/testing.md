# 測試指引

## 測試目標

測試不是只是驗證有沒有跑，而是確認：

- 主要流程正確
- 錯誤情境有處理
- 依賴有被正確呼叫
- 重要資料轉換沒有錯

## 常見工具

- xUnit
- Moq
- coverlet.collector

## 測試邊界

- 測試專案優先依賴 Service 與 Domain
- 不直接依賴 API 與 Infrastructure
- 若要做整合測試，應明確區分，不要混在單元測試裡

## 測試命名

建議格式：

{Method}_{Scenario}_{ExpectedResult}

例如：

- Get_WhenEntityExists_ReturnsDTO
- Get_WhenEntityNotFound_ReturnsNull
- Create_WithValidData_ReturnsTrue
- Delete_WhenEntityNotFound_ReturnsFalse

## 測試內容

至少覆蓋以下類型：

- 成功路徑
- 找不到資料
- 驗證失敗
- 例外情境
- 空值或邊界值

## 好的測試特性

- 安排清楚
- 行為單一
- 結果可讀
- 依賴可控制
- 失敗時容易定位
