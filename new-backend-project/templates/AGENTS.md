# 專案規範

## 文字編碼規則

本專案含有正體中文文字，避免亂碼與意外重新編碼是最高優先。

### 必要規則

- 讀取或編輯可能含正體中文的既有檔案前，先確認：編碼、是否有 BOM、換行風格
- 若懷疑出現亂碼，在確認編碼判讀可信之前不要存檔
- 既有檔案一律保留原本的編碼、BOM 與換行風格
- 「轉換為 UTF-8」視為獨立且需明確指示的任務，不要順手做
- 新檔案以 UTF-8 建立，並說明是否含 BOM
- 不要使用無法明確控制編碼的寫入方式（例如 shell 重導向）
- 寫入後重新開啟檔案，確認代表性的正體中文行內容正確
- 出現以下情況立即停止並回報：替換字元、非預期的 `?`、BOM 變動、換行風格變動、無業務理由的整檔差異

### Windows PowerShell 注意事項

PowerShell 5.1 讀取無 BOM 的 UTF-8 檔案時會以系統 ANSI 編碼解讀，導致正體中文變成亂碼。

- 讀檔一律加上 `-Encoding UTF8`
- 寫檔使用 `Out-File -Encoding utf8` 或 `Set-Content -Encoding utf8`，不要依賴預設值

### 變更回報格式

每個異動的文字檔請回報：路徑、編碼、是否有 BOM、換行風格、如何驗證、代表性中文內容是否完整。

## 架構規則

- **Service 層不得參考 Infrastructure**。Service 只依賴 Domain 的介面，實作由最外層在 DI 接線。需要新能力時，介面放 Domain、實作放 Infrastructure，不要加專案參考
- 資料提交一律經由 `IUnitOfWork.SaveChangesAsync()`，不要在 Repository 上開 SaveChanges
- Controller 不碰 Entity，Service 不碰 Request/Response。轉換鏈為 `Request → DTO → Entity → DTO → Response`
- JSON 維持 PascalCase（`PropertyNamingPolicy = null`），前端型別依賴這個約定
- 業務錯誤用 `ArgumentException` / `KeyNotFoundException` / `InvalidOperationException` 表達，由全域例外處理映射成狀態碼，不要自行回傳錯誤碼

## 開發規則

- EF Core migration 只用 `dotnet ef migrations add <Name>` 產生，**不要手改** migration 檔案
- 機密值（連線字串、JWT SecretKey）不寫進 `appsettings.json`。開發用 `dotnet user-secrets`，部署用環境變數（`AppSettings__ConnectionString`）
- 新增業務服務後，記得在 `Extensions/ServiceCollectionExtensions.cs` 手動註冊（本專案刻意不使用組件掃描自動註冊）
- `dotnet build` 應維持零錯誤零警告
