# Blood Essence — Contract V1

06/11/2026 · v0.6 REVISED. **IMPLEMENTED/VERIFIED domain**, **BLOCKED production activation**. Xem [dual progression](DUAL_PROGRESSION_CONTRACT_V1.md) cho authority và Save gate.

## Tích lũy và sức chứa

Charges và fractional progress là dữ liệu gốc. Capacity tính từ actual realm/minor stage. Catalog fixture mở từ Kim Đan; entry realm bắt buộc capacity Sơ kỳ1, mỗi stage tăng1, higher realm dùng bảng explicit đã validation. Không tự tạo catalog production hoặc item tăng tốc.

Tick cô đọng thụ động theo rate cấu hình. `AccelerateBlood(owner,epoch,command,progress)` nhận nguồn giả lập, không tiêu item thật; nếu vượt chỗ trống thì reject nguyên lệnh `AtCapacity`, caller giữ tài nguyên. Khi đầy, phần tiến độ đã chuyển thành charge được tiêu đúng một lần; thời gian thụ động vượt cap không bank thêm. Ở/qua cap không tích lũy tiếp.

Khi settlement làm tụt tiểu cảnh giới, policy fixture `HoldExistingStopAccumulation` giữ tất cả charges và progress cũ, dù charges vượt capacity mới; không tự xóa. Đây là policy ngoài production, cần quyết định cuối về sử dụng/giữ phần dư trước khi tích hợp.

## Kích hoạt và bổ sung

`ActivateBlood(owner,epoch,attempt,currentHp,maxHp,out hpAfter)` chỉ thành công khi HP hữu hạn, sống, **0 < HP < 30% Max**, có charge, realm unlock, identity hợp lệ và không có Burning/pending cũ. Tiêu1 charge, trả HP đầy đúng một lần, tạo BurningTransaction30 giây. Host tương lai phải commit HP output và transaction cùng lifecycle; API thuần chưa điều khiển HP MainGame.

`ExtendBlood(owner,epoch,command,burningSession)` là command riêng: cùng owner/epoch/session đang Active, còn charge. Mỗi lượt tiêu1, cộng30 giây vào remaining và thêm1 chi phí. Không áp HP threshold lại, không trả HP heal, không reset thời gian dư, không tăng tầng buff hoặc nhân modifier lần nữa. Duplicate command/session cũ bị reject.

ActualRealm không đổi. `RealmTierFor(CombatAbility,true)` mới trả tier thực+1 khi Active. Breakthrough, PermanentLearning, Sect, MapEntry và ProgressionReward luôn dùng tier thực; ability không hỗ trợ forced execution cũng dùng tier thực. Đây là numeric eligibility fixture, không cấp successor/unlock/reward thật hoặc tạo ability Nguyên Anh.

## Modifier theo nguồn

`BloodEssenceModifiers.Reconcile(session,targetOwner,targetEpoch,modifiers,unbuffedValues)` yêu cầu target hiện tại; stale session không được reapply vào Hero đã Load. Caller cung cấp giá trị tuyệt đối đã evaluate **không gồm nguồn Burning**. Domain tạo delta flat bằng base tuyệt đối × (factor−1), source `cultivation:burn:<owner>:<session>`, thay đúng nguồn đó khi refresh; source gear/actor khác được giữ.

Whitelist thử nghiệm: Attack, MagicalAttack, Armor, MagicResistance, HealthRegeneration, KillHealBonus. Không nhận crit/dodge/reduction/CDR hoặc tỷ lệ %, MaxHealth, attack speed, move speed. Factor2 chỉ là cấu hình fixture. Không thay damage formula, CombatStats hoặc CombatManager. Active → Pending/hết hiệu ứng: reconcile xóa nguồn owned. Domain không có animation hay callback combat tự động.

## Khoản nợ và settlement

Lần đầu chụp `ReferenceFoundation`, `DebtRatio`, `ActualRealmId`. Top-up không thay snapshot này. `TotalDebt = reference × ratio × ChargesUsed`, không lãi kép hoặc reprice do căn cơ tăng/giảm trong Burning. Ví dụ fixture1000 và10%:1/2/3 lượt là100/200/300.

State machine:

```text
Không Burning --Activate--> Active --Extend--> Active (cộng30s/chi phí)
Active --hết giờ/EndBlood(reason)--> SettlementPending
SettlementPending --SettleBlood đúng identity--> Không Burning + receipt
```

`EndBlood` không xóa nợ. Pending chặn Tick/progression/command tài nguyên cho tới settlement. `SettleBlood` lập snapshot đầy đủ: trả bằng `min(foundation,debt)`, phần thiếu cộng injury, foundation không âm, actual realm giữ nguyên, minor stage tính lại, energy clamp theo injury, ghi receipt rồi mới bỏ transaction. Overflow/invalid không publish một phần. Receipt chống terminal callback trùng, còn owner/epoch/session chặn stale callback. Reconcile modifier dùng snapshot hiện tại sau terminal.

| Sự kiện | Policy domain/fixture | Trạng thái production |
|---|---|---|
| Hết30s game-time | Tick tạo Pending/Expired; explicit settlement | Chưa nối clock |
| Death, Retry, Travel, NewGame, Menu | EndBlood(reason) → Pending → settle explicit | Chưa nối lifecycle |
| Hủy explicit | Giữ khoản nợ, không có cancel miễn phí | Chưa có UI |
| Pause | Không giảm remaining, không cô đọng/treatment | Chưa bật runtime |
| SaveBoundary | Explicit end tạo pending; capture còn nợ | Policy Save thật chưa chốt |
| Load, CrashRecovery | Candidate fixture đưa Active về Pending, giữ reference/uses/remaining | Chưa durable hoặc tích hợp disk recovery |
| Duplicate/stale terminal | Reject receipt/identity, không trừ lần2 | Đã test domain |

Không được discard session đang có liability trước khi host hoàn thành transaction persistence/settlement theo policy được duyệt. Domain không ngăn caller tự bỏ toàn bộ object; production gate tồn tại vì lifecycle/disk transaction chưa được nối. Snapshot Load cũ có thể rollback cả timeline hiện hành; không tuyên bố chống rollback ngoài slot hoặc atomic crash-safe.

## Injury và hồi phục

Mất căn cơ và injury là hai giá trị khác nhau. Refinement bù căn cơ nhưng không xóa injury. Cộng reserve không chữa injury. `HealInjury`/treatment chỉ chữa injury; `RestoreSpiritualEnergy` chỉ phục hồi linh lực. Policy cấu hình có thể giảm yield/Max Energy và chặn trial theo ngưỡng injury; chưa có tỷ lệ balance production.

## Bằng chứng và quyết định còn thiếu

Mono kiểm HP/charge/identity, top-up, nguồn modifier, thời gian pause/expire, reference lock, linear debt, growth, shortfall injury, over-cap charges, mọi terminal reason, stale epoch, duplicate settlement, treatment và candidate Load. Unity native JsonUtility kiểm tài nguyên, active debt/remaining, recovery pending, receipt roundtrip và invalid candidate. MainGame chỉ nghiệm thu gate/UI/EXP/Save v1; **chưa nghiệm thu Burning trong combat**.

Cần chốt rate cô đọng, capacity toàn catalog/overflow, whitelist/factor cuối, injury penalties/limit/healing, transfer căn cơ sau breakthrough, Save khi Active và crash/load settlement, durable write và migration legacy. Không bật Burning trước khi tất cả gate persistence và lifecycle đạt.
