# 編程規範

> 更新日期：2026-09-19
> 本文件是開發準則；不代表既有程式已全面符合。現況以對應程式及系統文件為準。

## 技術與命名

- Unity 非同步使用 UniTask，將取消 Token 沿必要工作傳遞，擁有者須監督並等待清理。
- 可觀察狀態與事件流使用 UniRx，訂閱生命週期隨擁有者結束；明確的建構／協調依賴仍可直接注入。
- 可選值優先使用 Option。外部輸入、Unity 序列化與 Validator 邊界仍需必要的 null／有效性檢查。
- 資料傳遞優先 Record；唯讀集合使用唯讀介面，但兩者皆不自動保證深度不可變。
- Inspector 配置使用 Odin 支援設計師工作流程，遊戲數值放在內容配置。
- 私有方法採 _PascalCase，私有欄位採 _camelCase，其餘依 C# 慣例。
- 共用與卡牌列舉分別參考 GameEnum、CardEnum，領域專用列舉置於所屬模組，不強制集中至單一檔案。
- 所有新文件、註解及說明使用台灣繁體中文。

## 職責分工

Model 管理規則，View 負責呈現與互動，Presenter 協調；共享契約放在 Presentation Abstractions。Data 保存配置，Property／LifeTime／Session 實體由 GameModel Factory 建立。

GameData 與 GameModel 目前仍同屬 Runtime，可執行的資料查詢仍有 Runtime 依賴，見 [架構總覽](SystemArchitecture.md)。資產製作遵循 [GameData 規範](GameData_Asset_Guidelines.md)。

## 數值責任

通用 Value／算術支援整數與缺值，不套用傷害或 Buff 領域限制。Resolver 判斷數值是否符合效果語意；Entity／Manager 維護最終 HP、護盾、能量及層數範圍。

Buff Level 不低於 0，存在 MaxLevel 時亦不超過上限；0 層 Buff 仍存在，不自動移除。非法效果不得以反向狀態變更或假的 Result／Event 表達。詳見 [Value](Value.md)。

## Validator 與 Runtime

| 情境 | 責任 |
|---|---|
| Library ID、Operation、Condition 等必要企劃資料缺失 | Validator 提前報錯，避免核心流程層層掩蓋資產錯誤 |
| 排隊後目標、來源區域或 Buff Layer 消失 | Runtime 正常失效，安全 No-op／Rejected，不產生假事件或動畫 |
| 存檔、網路、玩家輸入等外部資料 | 輸入邊界驗證，不能假設已受專案 Validator 保證 |
| 兩份相同語意輸入需要反覆核對 | 優先消除重複資料來源 |

驗證與 Runtime 不互相取代：前者排除製作錯誤，後者處理正常遊戲時序。

## 變更驗證

修改 Assets/Scripts 下的 C# 後，依專案指令透過 Unity 工具刷新（新檔時）、重新編譯並修正至 0 error；warning 應回報。測試依行為風險選擇，避免只重述實作。不要主動 stage 或 commit。
