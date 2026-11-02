# AI_LOG — MvpArSubmitter 协作日志

> 本文件由 AI 助手（MCPlato）维护，记录每轮人机协作的真实过程。
> 规则：只追加不覆盖；未执行的测试不得写成 PASS；Prompt 原文缺失处标注来源。

## 历史说明

- S4 → S5.5b 各阶段（S4 自动化提交、S5.1 打包与 Vulkan/ARCore 修复、S5.2 真实坐标、S5.3/S5.3b 标记与触摸修正、S5.4/S5.5b 硬超时与明文 HTTP）：
  `[原文仅存片段，见 docs/PROGRESS.md]`（PROGRESS [1]-[19] 为逐阶段台账，含全部命令原文与判据输出）

---

## Bonus 1 — AR 标记按优先级/状态变色

- 阶段：Bonus 1 开始
- 状态：[LIVE]
- 目标：按优先级/状态改变 AR 标记样式（high红/medium黄/low绿；resolved 灰色降透明度）
- 当前状态：准备开始，尚未完成
- 回滚点：`b3c0ad8`（chore(s6.1): 管理端验收截图（回滚点 bonus1-before-marker-color））
- 证据：
  - 已完整读取 PlaneTapPlacer.cs / SubmitPanel.cs / SubmitUIBootstrap.cs / IssueModels.cs / docs/PROGRESS.md（本轮无凭记忆修改）
  - `git log --oneline -5` 读取时 HEAD = `e4a55dc`；回滚点 commit 建于其上 = `b3c0ad8`
  - 本轮命令与输出见下方各步记录（随执行追加）

### Bonus 1 执行记录（随执行追加）

1. 读取与回滚点
   - `git status --short`：仅 3 张未跟踪截图（docs/s6-*.png），无未提交源码修改 → 满足「有未提交修改则停止」的前置条件
   - 回滚点提交：`b3c0ad8 chore(s6.1): 管理端验收截图（回滚点 bonus1-before-marker-color）`（3 files）
2. 实现（一次一文件，改后整文件重读）
   - PlaneTapPlacer.cs：+颜色常量/`ColorForPriority`/`ColorForState`（唯一决策入口）/`SetCurrentPriority`/`ApplyIssueStyle`；marker 创建改 `new Material(共享材质)` 实例上色
   - SubmitPanel.cs：`SetPriority` 同步优先级；`OnSubmitFinished` 成功分支调 `ApplyIssueStyle(result.issue.priority, result.issue.status)`；失败分支不动标记
3. 验证
   - A 静态保留 8 项 grep 全过（详见 docs/PROGRESS [20] 与本报告原文）
   - B 颜色规则：代码级证据 PASS（运行时显示 UNVERIFIED）
   - C 编译：`Logs/bonus1-import.log` → `errorCS=0`、`CompileScripts: 9629.006ms`、`Batchmode quit successfully` 计数 1 → PASS
   - D 回归：diff --stat 仅 2 文件；React/Go/backend data 未动
4. 状态更新
   - 阶段：Bonus 1 结束（本轮）
   - 状态：[LIVE] → 代码与编译完成；**真机/Play 模式颜色显示 UNVERIFIED（本轮未打 APK、未运行 Play 模式）**
   - commit：`bonus1-marker-color`

### Bonus 1 复核修正（第二轮）

- 状态：[LIVE]
- 阶段：Bonus 1 复核修正
- 发现：原实现只修改了 alpha，但 Marker.mat 为 Standard Opaque 模式（`_Mode: 0`/`_SrcBlend: 1`/`_DstBlend: 0`/`_ZWrite: 1`、`m_ValidKeywords: []`，本轮读取资产原文核实），半透明效果未被证明有效
- 修正：为 marker 专属材质实例补充透明/不透明渲染状态切换——新增 `ApplyMarkerVisualStyle(Color, bool resolved)`（resolved=true：`_Mode=3`、`_SrcBlend=5(SrcAlpha)`、`_DstBlend=10(OneMinusSrcAlpha)`、`_ZWrite=0`、开 `_ALPHABLEND_ON`、关 `_ALPHATEST_ON`/`_ALPHAPREMULTIPLY_ON`、renderQueue=3000；false：恢复 `_Mode=0`/1/0/`_ZWrite=1`、关 `_ALPHABLEND_ON`、renderQueue=2000），只作用于 `markerMaterialInstance`，Marker.mat 资产零改动；`SetCurrentPriority`/`ApplyIssueStyle`/创建处统一收敛到 `RefreshMarkerStyle()`。另修：`HandleTap` 命中平面准备更新位置时重置 `currentStatus="open"`（保留 currentPriority），防旧 resolved 灰色污染新标记；提交失败分支未动。
- 发现：旧记录中的"准备开始，尚未完成"与后续结束记录并列，已追加当前状态澄清（本条即澄清，历史原文未删）
- 实际 commit：`e9ca53d` fix(bonus1): resolved 真半透明（Standard 透明渲染状态切换）+ 命中新平面重置状态为 open（3 files，+93/−4）
- 真机颜色测试：仍标记 UNVERIFIED（本轮未运行 APK、未跑 Play 模式；编译 Logs/bonus1b-import.log error CS=0）

### Bonus 1 运行时验证轮（第三轮）

- 状态：[LIVE]
- 阶段：Bonus 1 运行时验证（不改业务代码、不开始 Bonus 2）
- 本轮真实 commit 基线：HEAD = `f8d5466`（含 e9ca53d 全部修正；`git status --short --untracked-files=all` 空、`git diff --check` 空）
- 重新打 APK：是。构建命令 `Unity.exe -batchmode -quit -executeMethod S5Build.Build -logFile Logs/bonus1-runtime-build.log`；`BUILD_RESULT=Succeeded size=352820390 errors=0`；日志 `error CS`=0；源码=HEAD f8d5466（构建后 git log -1 仍 f8d5466，确认包含 e9ca53d 后代码）
- APK：脚本硬编码 `Builds/mvp-ar.apk`（18,478,786 字节，2026-10-31 15:17）；按指令未大范围改脚本，构建后复制为独立文件名 `Builds/bonus1-runtime.apk`（同大小 18,478,786 字节，15:23 复制）
- high/medium/low 运行时测试：**UNVERIFIED**——本机无 adb（`where adb` 无输出）、无已连接手机，agent 环境无法装机；Play 模式需 GUI 编辑器 + AR 设备/模拟，headless 无法执行。未伪造任何运行结果。
- 提交成功测试（运行时）：UNVERIFIED（同上，需装机人工执行）
- 提交失败保样式测试：UNVERIFIED（需装机或 GUI Play 模式；代码级静态确认：SubmitPanel 失败分支只 SetResult，不动 marker/不清输入）
- resolved 测试入口检查：**不存在自然触发入口**——全工程 grep "PATCH" 0 匹配（Unity 无 PATCH 调用）；Assets/Editor 仅 S4TestRunner/S5Build/S5MarkerAsset/S5Setup，均无 resolved 注入；POST 新建默认 open。未伪造截图、未手工改 issues.json。resolved 运行时效果保持 UNVERIFIED，静态代码路径 PASS（ColorForState/ApplyMarkerVisualStyle 见 [21]）
- 回归检查：GetTouch(i)=152 / GetTouch(0) 仅注释 149 / PlaneWithinPolygon=194 / RaycastAll=176 / marker==null 复用 213 / Fake 0 匹配 / ApiConfig git diff 空且 BaseUrl 未变 / React、Go、backend data 不在 git status → 全 PASS
- 未修改任何源码（本轮零 Edit）；未开始 Bonus 2

### Bonus 1 人工运行时验收归档（第四轮）

- 状态：[LIVE] → 归档（纯文档轮）
- 阶段：Bonus 1 人工运行时验收归档 + 台账一致性检查
- 证据来源：**用户实际安装 APK 后的人工观察**（非 agent 执行；用户未提供截图/视频，本记录不含任何截图路径）
- APK：`Builds/bonus1-runtime.apk`（第三轮构建产物，18,478,786 字节，源码 = f8d5466 含 e9ca53d 修正）
- 本轮没有修改 Unity 源码；本轮没有重新打 APK；本轮没有开始 Bonus 2

逐项验收结果（用户实际观察）：

| 验收项 | 结论 |
|---|---|
| medium 实际显示黄色 | PASS |
| high 实际显示红色 | PASS |
| low 实际显示绿色 | PASS |
| 重新点击平面后颜色正常 | PASS |
| 提交成功后颜色正常 | PASS |
| 提交失败后 marker 是否保留 | UNVERIFIED |
| 提交失败后输入是否保留 | UNVERIFIED |
| resolved 灰色半透明 | UNVERIFIED |

最终判断：

- Bonus 1 主路径运行时验证：**PASS**
- Bonus 1 总体结论：**PARTIAL**
- 未完成验证项：提交失败回归（marker/输入保留均未测试）、resolved 灰色半透明
- 当前 Unity 没有自然触发 resolved 的业务入口（无 PATCH 调用，POST 默认 open）
- 备注：「重新点击平面后颜色正常」仅为用户观察到的普通颜色重放结果，不构成 "resolved → 新平面 open 重置" 的验证
- Bonus 2：未开始

### Bonus 2 实现轮（第五轮）

- 状态：[LIVE]
- 阶段：Bonus 2 轻量本地规则智能判级——实现（依据已评审设计方案；不改契约、不动 Bonus 1 核心逻辑）
- 基线：HEAD = `42dfda8`；开工检查 git status 空、git log -5 正常、git diff --check 空
- 实现内容（3 文件）：
  - 新建 `LocalPriorityRules.cs`（纯 C# 静态规则类，零网络、零外部 AI）：high（漏电/火灾/燃气/触电/坍塌等）> medium（故障/异常/损坏/渗水等）> low（划痕/轻微/建议/优化等）；先标题后描述；命中即返回并附命中词原因（可解释）；双空/未命中/异常一律 matched=false（可回退）
  - `SubmitPanel.cs`：`SetPriority`（手动锁定）/ `SetPriorityInitial`（启动默认）/ `OnAdoptSuggestion`（采用建议）三入口收敛到共用 `ApplyPriority`（内含 Bonus 1 原颜色联动 SetCurrentPriority）；输入变化经 `OnInputChanged` + `Update` 0.4s 防抖评估；建议只展示不自动覆盖；提交仍走 `PriorityValues[priorityIndex]`，提交/成功/失败路径零改动
  - `SubmitUIBootstrap.cs`：新增建议文本（淡黄）+ "采用"按钮（初始 interactable=false）；两 InputField onValueChanged 接入防抖；启动改调 `SetPriorityInitial(1)`；新控件在 Canvas 下自动被 RaycastAll 拦截，AR 点击不受影响
- 验证：静态回归 10 项 PASS（GetTouch(i)=152 / GetTouch(0) 仅注释 / PlaneWithinPolygon=194 / RaycastAll=176 / ApplyIssueStyle 仍在成功分支 / Fake 0 匹配 / BaseUrl 未变 / PlaneTapPlacer 零改动）；编译 `Logs/bonus2-import.log` → errorCS=0、CompileScripts 4399.496ms、Batchmode quit successfully 1 → PASS
- 本轮未打 APK；运行时效果（建议刷新/采用生效/手动覆盖/防抖）UNVERIFIED，待装机或 Play 模式验证
- 本轮 commit：见 git 记录（feat(bonus2) 开头）

### Bonus 2 增量修正与规则验证（第六轮）

- 状态：[LIVE]
- 阶段：Bonus 2 增量修正（修正三项已确认偏差 + 真实规则测试；不重做功能、不改历史、不打 APK）
- 基线保护：读取 5 文件 + git 原始输出——`git status --short --untracked-files=all` 空（开工时工作区干净）；`git log -3 --oneline` = e62b7ff / e12988e / 42dfda8；`git show --stat` 确认 e12988e=5 文件（AI_LOG+13、LocalPriorityRules.cs 新+89、SubmitPanel+116、SubmitUIBootstrap+15/−2、PROGRESS+21）、e62b7ff=LocalPriorityRules.cs.meta+11；`git diff --check` 空。未执行任何 reset/amend/rebase/checkout/clean/restore。

三项偏差修正（只改 2 个文件，SubmitUIBootstrap.cs 绑定名未变无需修改）：
1. **冲突处理**：`Evaluate` 由"标题先命中即返回（Scan(t) ?? Scan(d)）"改为标题+描述逐级共同判断——high 在标题或描述任一命中即 high；否则 medium 任一命中即 medium；否则 low。同级双命中时先扫标题再扫描述，原因文案取标题命中词（只影响原因，不改变等级）。类注释同步更新；删除旧 `Scan` 单文本方法。
2. **关键词**：high += "砸"、"急"（现 12 词）；medium += "漏水"（现 11 词）；确认 "漏水" 不在 low（low 仍 10 词未动）。除这 3 个词外无其他增删。
3. **移除 debounce**：删除 `suggestionDirty`/`suggestionEvalAt`/`SuggestionDebounceSeconds` 字段与 `Update()` 方法；`OnInputChanged` 改为直接同步调用 `EvaluateSuggestionNow()`。grep 防抖残留 = 0。

规则测试（**实际执行**，非静态阅读）：项目无测试框架（Packages/manifest.json grep "test" 0 匹配；Assets/Editor 仅 S4/S5 四个脚本，无规则测试入口）。采用 Unity 自带 Mono 编译器真实执行项目源文件：`mono.exe .../lib/mono/4.5/mcs.exe -out:RuleTests.exe RuleTests.cs <项目真实 LocalPriorityRules.cs>` → mcs EXIT=0 → `mono RuleTests.exe` RUN_EXIT=0。10 个指定场景 + 4 个新增关键词场景逐条输出（输入/实际 priority/实际 matched/实际 reason/期望/结论）：
- T1 配电箱漏电/空→high PASS；T2 空调异常噪音/空→medium PASS；T3 墙面轻微划痕/空→low PASS；T4 墙面划痕/发现配电箱漏电→high（命中:漏电）PASS；T5 空调故障/建议优化外观→medium（命中:故障）PASS；T6 巡检记录/普通巡检记录→matched=false PASS；T7 双空→matched=false 不崩 PASS；T8 双 null→matched=false 不崩 PASS；T9 轻微划痕+设备故障并有漏电危险→high PASS；T10 照明设备异常/存在坠落危险→high（命中:坠落）PASS；附加 KA_ZA 被砸→high(砸) PASS、KA_JI 紧急→high(紧急) PASS、KA_LOUSHUI 天花板漏水→medium(漏水) PASS、KA_LOUSHUI_LOW 轻微划痕+地面漏水→medium（证明漏水不在 low）PASS。
- **规则测试结论：PASS（14/14，真实执行项目源文件）**；测试 harness 位于沙箱 mvp\ruletests\（项目目录外，未入库）。

第三节不变式检查（grep 原始输出）：PriorityValues 单一定义 1 处（line 21）+ 3 处使用（IndexOf/Length/priorityIndex）无第二套映射；SetPriority→manualLocked=true→ApplyPriority（line 41-45）；OnAdoptSuggestion 走 ApplyPriority 且不置 manualLocked（line 55-68）；无匹配/非法 priority 时 OnAdoptSuggestion 提前 return（line 57-64），采用按钮无匹配时 interactable=false（RefreshSuggestionDisplay）；提交仍 `PriorityValues[priorityIndex]`（line 199）；输入变化不触碰 priorityIndex（EvaluateSuggestionNow 只写 lastSuggestion+刷新显示）。

Bonus 1 兼容性（静态证据）：GetTouch(i)=152 / GetTouch(0) 仅注释 149 / PlaneWithinPolygon=194 / RaycastAll=176 / ApplyIssueStyle 定义 PlaneTapPlacer:118、成功分支调用 SubmitPanel:231 / SetCurrentPriority 定义 :106、调用 :141；PlaneTapPlacer/IssueApi/IssueModels/ApiConfig `git diff --name-only` 空（零改动）；BaseUrl 仍 `http://192.168.5.29:8080` → **PASS（静态）**。

编译：`Logs/bonus2b-import.log`（新名）→ **errorCS=0、`313: CompileScripts: 1613.299ms`、Batchmode quit successfully=1 → PASS**；BUILD_RESULT=无（本轮仅编译不打包，编译日志无此字段属预期）。编译 PASS ≠ 运行时通过。

git：`git diff --check` 空 EXIT=0；`git status` 仅 ` M LocalPriorityRules.cs`、` M SubmitPanel.cs`；`git diff --stat` 2 files +17/−33；`git diff --name-only` 恰为该 2 文件 → 范围符合。

- **本轮结论：规则测试 PASS（14/14 真实执行）；编译 PASS；静态不变式+Bonus 1 兼容性 PASS；Play/真机 UNVERIFIED；本轮未打 APK。**
- 涉及文件：Assets/Scripts/LocalPriorityRules.cs、SubmitPanel.cs、AI_LOG.md、docs/PROGRESS.md（台账追加）。
- Bonus 2 剩余 UNVERIFIED：建议实时刷新、采用生效、手动覆盖的 Play/真机行为（需装机，本轮禁打 APK）。

### Bonus 2 收尾与打包验收（第七轮）

- 状态：[LIVE]
- 阶段：Bonus 2 收尾——收窄单字"急"误判风险 + 补测 + 编译 + 打包 APK（不开始新功能、不改 Bonus 1、不改 SubmitPanel/SubmitUIBootstrap 行为逻辑）
- 基线保护：开工读 5 文件 + git 原始输出——`git status --short --untracked-files=all` 空；`git log -3 --oneline` = 4ad0a26 / e62b7ff / e12988e；`git diff --check` 空（EXIT=0）。**本轮回滚点 = HEAD `4ad0a26`**。未执行 reset/amend/rebase/checkout/clean/restore，e12988e/e62b7ff/4ad0a26 均保留。

词表修正（只改 Assets/Scripts/LocalPriorityRules.cs 一处数组 + 注释）：
- high 移除单字"急"（会误命中"不着急/别急"等否定或安抚语境）；保留"紧急"；新增"急修""急停"覆盖维修语境；medium/low 词表零改动；联合判级逻辑与"（命中:xxx）"可解释格式不变。
- **high 词表最终内容**：`{ "漏电", "火灾", "燃气", "触电", "坍塌", "坠落", "人身", "危险", "紧急", "重大", "砸", "急修", "急停" }`（13 词）。

规则测试（真实执行：Unity 自带 Mono mcs 编译项目真实 LocalPriorityRules.cs + 沙箱 harness，harness 在项目外 mvp\ruletests\ 不入库）：mcs EXIT=0、mono RUN_EXIT=0。T1-T14 全部重跑 PASS + 新增 T15-T18：
- T15 不着急处理，可下周一并安排/空 → matched=False priority=''（未命中关键词）→ **非 high** PASS；
- T16 紧急处理配电柜冒烟/空 → high（命中:紧急）PASS；
- T17 急停按钮损坏/空 → high（命中:急停）PASS；
- T18 别急，慢慢来/空 → matched=False priority=''（未命中关键词）→ **非 high** PASS。
- **18/18 PASS**（注：harness 汇总行文案仍写 "ALL PASS (14/14)"，为 harness 内旧的硬编码字符串未同步更新，逐条输出实际为 18 行 PASS，此处如实披露；harness 非项目交付物不入库）。

编译：`Logs/bonus2c-import.log`（新名）→ errorCS=0、`304: CompileScripts: 10057.407ms`、Batchmode quit successfully=1 → **PASS**。
打包：首次用全名 `-executeMethod MvpArSubmitter.S5Build.Build` 失败（日志原文 `executeMethod class 'S5Build' could not be found`，S5Build 在全局命名空间，属调用参数失误非代码问题）；按历史惯例改短名 `S5Build.Build` 重试 1 次成功：`BUILD_RESULT=Succeeded size=352842739 errors=0`（2850 行）、构建日志 errorCS=0；APK = `Builds/mvp-ar.apk` 18,470,462 字节（15:04 新生成），复制为 `Builds/bonus2-runtime.apk` 同大小，原 mvp-ar.apk 保留未覆盖。

静态回归：UnityWebRequest/HttpClient/http(s):// 在 LocalPriorityRules.cs 0 匹配；PriorityValues 定义 1 处；防抖/Update 残留 0；构建后 git status 仅 ` M Assets/Scripts/LocalPriorityRules.cs` → 禁改文件全部零改动；Go/React/backend data 零改动。

证据顺序更正（第八节要求）：**上一轮（第六轮）报告第 8 节的 git diff --stat / --name-only 是在修改台账文件之前采集的，仅覆盖 2 个源码文件，不能证明台账文件的改动范围**；本轮起改为在所有文件（含台账）写完后、提交前采集 diff 证据并贴完整输出（见本轮报告第 8 节）。

- **本轮结论：词表修正 PASS；规则测试 18/18 PASS（真实执行）；编译 PASS；打包 PASS（APK 产出）；Play/真机 UNVERIFIED（由用户人工验收，agent 不代填结果）。**
- 涉及文件：Assets/Scripts/LocalPriorityRules.cs、AI_LOG.md、docs/PROGRESS.md（台账追加）。

### [FIX-1] 提交提示不可见 + 标记颜色黑 修复（第八轮）

- 状态：[LIVE]
- 阶段：[FIX-1] 先取证 → 按证据选分支 → 只改命中分支 → 自检上屏 → 打包一次
- 回滚点：HEAD `5fbf2c65faf89e258075eff3fc93d27a8e8900bf`（开工 status 空）；未执行 reset/amend/rebase/checkout/clean/restore
- 取证结论：resultText 为 legacy Text，Place(100,340,880,40) 中心锚点 left=−340 越出屏幕左缘（提示不可见根因）；`renderer.material = markerMaterialInstance` **不存在**（仅 225 行 `renderer.sharedMaterial = markerMaterialInstance`，赋值对象即实例、渲染等价）；父链无 LayoutGroup；绑定非 null；alpha=1、active
- 分支判定：**A 命中**（越界）；B/C/D/E 未命中（E 按用户判据逐字核对：字面句不存在，但等价赋值已在 225 行 → 不补赋值；黑标根因静态未定位，Bonus 1 期真机曾验证颜色正常，靠本轮诊断行+兜底排查）
- 三处冲突与裁定：
  1. 必做2 finally 直接恢复 interactable → 会解锁飞行期防重锁；裁定方案甲：`dispatched` 标志，仅异常/未派发时恢复
  2. 分支A `hintText+(0,−35)` 与建议行 (420,450) 重叠；裁定 `Place((420,343),(700,40))`
  3. S5Build 硬编码 mvp-ar.apk 与「禁止覆盖」冲突；裁定构建后复制 fix1-runtime.apk 并如实声明覆盖
  附加：诊断增加 Shader/实例两行；echoText 动态文本不含「假坐标」→ 跳过清理（该词仅在注释 16/89 行）
- 改动（4 个 Assets/Scripts .cs，一次一文件）：SubmitUIBootstrap 106-113（Place+Wrap+Overflow+transform.SetAsLastSibling）；SubmitPanel OnClickSubmit 167-231 / OnSubmitFinished 233-269（try/catch/finally + dispatched，无空 catch）/ SetResult 271-279（双通道 LastResult）；PlaneTapPlacer 诊断字段 41-46、RefreshMarkerStyle 108-114、ApplyAntiBlackFallback 117-163、RefreshDiagnostics 166-191、创建块 314、拦截 274（−200→−350）；ARDebugHud LastResult 19、resultRectLine 22、ResultRectCheck 35/40-64、Box 330 于 155、追加行 164-179
- 编译：首编译 Logs/fix1-import.log **FAIL**——`error CS1061` ×3（SubmitUIBootstrap.cs(113,24) SetAsLastSibling 非 Text 方法；我的失误，按纪律停止贴原文，用户批准单行修复）→ 重编译 fix1-import2.log：errorCS=0、`312: CompileScripts: 3101.612ms`、quit=1 → **PASS**
- S2（台账写入前采集）：diff --name-only 恰为 4 个 Assets/Scripts .cs；无 .mat/.shader/.unity/ProjectSettings/后端/前端；diff --check 空 → PASS
- 打包：`BUILD_RESULT=Succeeded size=353054985 errors=0`（Logs/fix1-build.log errorCS=0）；mvp-ar.apk 18,485,858 字节（**被本次构建覆盖**，等价旧产物=bonus2-runtime.apk）→ 复制 `Builds/fix1-runtime.apk` 18,485,858 字节；bonus1/bonus2 未动
- 结果行矩形：设计值 x∈[70,770]、y∈[323,363] 在屏内；**真机实测 L/R/T/B/判定 UNVERIFIED**
- 诊断各行实测值（材质/颜色/渲染/Shader/实例/兜底）：**UNVERIFIED**（未运行 Play/装机）
- 本轮 commit：见 git 记录（fix(fix1) 开头）

### 真机验收记录 2026-11-01（第九轮）

- 状态：[LIVE] → 归档（纯文档轮，零源码改动）
- 编号说明：按本文件「第 N 轮」序列顺延（第八轮=[FIX-1]），本轮为第九轮；轮内条目自 1 起编（任务方原文条目 12–24 按此顺延重编为 1–13，文本逐字照抄未改）。
- 证据来源：用户真机/浏览器逐字观测，非代理生成。
- 涉及文件：仅本文件（Unity 代码/场景/材质/ProjectSettings/Packages 零改动）。

**【真机验收记录 2026-11-01】**

1. 诊断面板：AR 会话: SessionTracking ｜ 相机组件: 组件已存在
2. 诊断面板：检测到的平面: 4 个（另次观察到 12 个、7 个、3 个）
3. 诊断面板：后端: http://192.168.5.29:8080 ｜ 后端连通: 已连通#24 0.0s
4. 诊断面板：最近一次点击: UI拦截(raycast)
5. 点击地面放置标记成功，面板显示「标记 (0.979, -1.829, -2.788)」
6. 提交成功后屏幕正文与面板同时显示「提交成功 @ (0.979, -1.829, -2.788)」
7. 空标题提交显示「标题不能为空」；随后填入标题可再次提交成功
8. 手机飞行模式下提交显示「无法连接后端」；恢复网络后可再次提交成功
9. 结果行自检：L=67 R=734 T=1359 B=1321 屏W=1220 屏H=2712 判定=在屏内
10. 材质: OK ｜ 颜色: #FF0000 ｜ 渲染: Mode=0 Src=1 Dst=0 ZW=1 Q=2000
    ｜ Shader: Standard supported=True ｜ 实例: Marker id=-418 ｜ 兜底: 无
11. 优先级 高→marker 红色、中→黄绿 #7EFB04、低→绿色，三者肉眼可辨
12. React 端浏览器显示「后端连接：正常 共 15 条」，Unity 提交记录与 React 修改的状态
    在同一份数据中一致，F5 后状态保持
13. 说明：以上均为用户真机/浏览器逐字观测，非代理生成；对应需求 3.1.1–3.1.6、3.2.x、3.3.8

- **用例数核对（只追加、不改写历史条目）**：`grep -c "Case(" ruletests/RuleTests.cs` = **19**（其中 1 行为方法定义 `private static void Case(`），用例调用 `grep -c 'Case("'` = **18** → 实际用例 **18**，与 README §9「18/18」一致，数字无需修改。
- **段号描述更正（只追加、不改写历史条目）**：历史条目「T1-T14 全部重跑 PASS」（第七轮）与 harness 文件头「跑 10 个指定场景」为段号/描述错误——实际用例构成为 **T1-T10（10 条）+ KA_ZA / KA_JI / KA_LOUSHUI / KA_LOUSHUI_LOW（4 条）+ T15-T18（4 条）= 18 条，不存在 T11-T14 用例**；第六轮「14/14」是当时 14 条用例的真实执行结果（当时成立），第七轮「18/18」为补测后的真实结果；harness 内「ALL PASS (14/14)」为旧硬编码字符串（第七轮已披露），本轮未改 harness 文件。

### 按钮按压缩放动效 + 深底浅字蓝白配色（第十轮）2026-11-02

- **改动范围**（白名单 + 用户裁决追加）：仅 `Assets/Scripts/SubmitUIBootstrap.cs`（M）+ 新增 `UIButtonPressAnimator.cs`、`PriorityButtonColorSync.cs`、`ResultTextColorizer.cs`（各 + .meta）；PlaneTapPlacer.cs / IssueApi.cs / IssueModels.cs / ApiConfig.cs / ARDebugHud.cs / SubmitPanel.cs、任何 .unity/.mat/.shader/ProjectSettings/Packages 零改动。
- **E1–E12 落盘（行号为最终磁盘行号，逐条 grep 复验）**：
  1. E1 配色常量：`LabelColor`@16、`HintTextColor`@19、`InputBackgroundColor`@22、`InputPlaceholderColor`@25、`AdoptBackgroundColor`@28、`AdoptDisabledColor`@31
  2. E2 `titleLabel.color = LabelColor`@82；E3 `descLabel.color`@89；E4 `priorityLabel.color`@96（仅加颜色语句，Place 参数未动）
  3. E5 循环外声明 `priorityImages`@101 / `priorityButtonTexts`@102，循环内 `transition = Selectable.Transition.None`@107 + `GetComponent<Image>`@108 + `GetComponentInChildren<Text>(true)`@109（一次缓存）
  4. E6 `hintText.color = HintTextColor`@119；E7 `suggestionText.color = HintTextColor`@124
  5. E8 采用按钮：`adoptImage.color = AdoptBackgroundColor`@128 + ColorBlock 五态@130–135（四态=AdoptBackgroundColor，disabled=AdoptDisabledColor）+ `targetGraphic = adoptImage`@136
  6. E9 提交按钮：`submitImage.color = Color.white`@143 先设纯白 + ColorBlock 五态@145–149（#0071E3/#0077ED/#0068D0/#0071E3/#B0B0B5）+ `targetGraphic = submitImage`@151
  7. E10 绑定段末挂 `PriorityButtonColorSync`@183–188（`Bind(panel, priorityImages, priorityButtonTexts)`）与 `ResultTextColorizer`@191–196（`Bind(resultText)`），全部直接传引用，零 Find / FindObjectOfType
  8. E11 输入框 `background.color = InputBackgroundColor`@245、`placeholderText.color = InputPlaceholderColor`@266（斜体/字号未动）
  9. E12 `CreateButton` 默认底 `AdoptBackgroundColor`@282、`targetGraphic = background`@284、`GetComponent<UIButtonPressAnimator>()` 判空后 `AddComponent`@287–289
- **新增组件**：`UIButtonPressAnimator`（按下 0.97/90ms、松开与移出 1.0/140ms，Update `Time.unscaledDeltaTime` 线性逼近，OnDisable/OnDestroy 复位 localScale=Vector3.one，只操作 localScale）；`PriorityButtonColorSync`（只读 `panel.priorityIndex`，LateUpdate 逐分量差值 >0.004 才写 Image.color/Text.color，transition=None 配合）；`ResultTextColorizer`（文案前缀映射 4 色，缓存上次文案仅变化时判定，只写 Text.color）。三 GUID 无冲突。
- **编译修复**：首次 batchmode 报 `error CS1061 ×6`——`Button.AddComponent` 不存在（AddComponent 属 GameObject）→ 改 `priorityButtons[0].gameObject.AddComponent<...>`@186、`submitButton.gameObject.AddComponent<...>`@194，复验后重跑。
- **构建取证**：`error CS` 计数 **0**；`CompileScripts: 2770.331ms`；`BUILD_RESULT=Succeeded size=353180604 errors=0`；Unity exit=0。
- **APK**：`cp Builds/mvp-ar.apk Builds/ui-runtime.apk` → fix1-runtime.apk **18,485,858**（精确匹配）/ bonus1-runtime.apk 18,478,786 / bonus2-runtime.apk 18,470,462 / ui-runtime.apk 18,490,870。
- **真机验收 9/10/11 只列不代填**：9) 点「高」等 2 秒仍是蓝底白字；10) 连点低中高颜色跟随无残留；11) 提交成功变绿、空标题变红、「请先点击地面放置标记」橙色。

