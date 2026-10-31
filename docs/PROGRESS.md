# PROGRESS — Unity 工程 MvpArSubmitter（S4 起的本地台账）

## 【授权声明与自动执行边界（长期生效）】

一、授权
用户已授予「自动执行命令」权限，无需每条命令确认。授权仅覆盖「二、可自动执行」；
「三、必须停下问我」中的操作无论有无授权，都必须先用纯文字说明「我要做什么/为什么/影响什么」，
等用户明确回复同意后才能做。

二、可自动执行（不需要问）
1. 读取、创建、修改文件，仅限两个目录：
   - D:\Unity-AR work\MvpArSubmitter（Unity 工程）
   - 沙箱工程目录下的 mvp\（backend / web / docs / scripts）
2. 本项目相关命令：go build / go test / go vet（vet 本机静默失败已知，跳过）、npm / npx / node、
   curl 127.0.0.1:8080、启动和停止本项目自己的后端进程。
3. 运行 Unity：仅限 D:\Unity job\2022.3.62f3c1\Editor\Unity.exe，仅限
   -batchmode "-projectPath D:\Unity-AR work\MvpArSubmitter" 这类只读工程操作。
4. git 本地操作：init / add / commit / status / diff / log（只在本机，禁止 push）。

三、必须停下问我（未经明确同意一律不许做）
1. 写入或删除 C:\Program Files\ 下任何东西，尤其 C:\Program Files\Unity\ 与 D:\Unity job\（硬红线）。
2. 删除任何目录或批量删除文件（rm -rf、Remove-Item -Recurse 等），包括工程 Assets 目录。
   需要删除时先列完整路径清单说明理由，等同意。
3. 安装/卸载软件、修改注册表、系统环境变量、防火墙或网络设置。
4. 关机/重启/注销/杀非本项目启动的进程。
5. 修改 git 历史、git push、访问外网（除 npm/Go 包管理器必要下载）。
6. 涉及隐私数据的操作（读取或上传 D:\个人简历、微信/QQ、浏览器配置目录等）。
7. 任何「为了让数字对上」手工改数据文件的行为：改 backend/data/issues.json、伪造日志、伪造测试输出。

四、执行纪律
1. 合并命令减少往返；单条命令超时上限 900 秒；不把 Unity 拉成前台常驻进程。
2. 禁止 cmd.exe、禁止管道 | head（用 > 文件 2>&1; cat 文件）、禁止访问盘符根目录。
3. 每完成一个阶段在本文件追加一行：序号 / 做了什么 / 涉及文件 / 验证结果（PASS|FAIL|ENV-BLOCKED）/ 下一步。
4. 任何一步失败：立刻停止，贴原始输出 + 判断，不连换三种写法硬试。

---

## [1] 2026-10-30 基线快照（S4 前）
- 做了什么：写入授权声明（本文件顶部）；git init + .gitignore（Library/ Temp/ Logs/ obj/ *.csproj *.sln）+ 首次 commit
- 涉及文件：docs/PROGRESS.md、.gitignore
- 验证结果：见提交记录
- 下一步：修复 IssueApi.cs 第 38 行 WaitForCompletion（CS1061）→ 重跑导入编译 → S4 自动化提交

## [2] 2026-10-30 S4 自动化提交通过（假坐标，batchmode 端到端）

| 检查项 | 命令 | 退出码 | 关键输出 | 结论(PASS/FAIL) |
|---|---|---|---|---|
| 修 IssueApi.cs 缺 using | Edit 第 4 行前插入 `using UnityEngine;` | — | 改后 1-6 行：`using System;`/`using System.Collections;`/`using System.Text;`/`using UnityEngine;`/`using UnityEngine.Networking;` | PASS |
| 导入编译（修 CS1061 两文件后） | `Unity.exe -batchmode -quit -projectPath ... -logFile docs/s4-import.log` | EXIT=0 | `EXIT=0` | PASS |
| 判据1 无 CS 错误 | `grep -c "error CS" docs/s4-import.log` | grep 1 | `0`（无匹配） | PASS |
| 判据2 MovedFromExtractor 消失 | `grep -c "ApiUpdater.MovedFromExtractor" docs/s4-import.log` | grep 1 | `0`（无匹配，证实 62f3c1 安装修复有效） | PASS |
| 后端 health | `curl -s http://127.0.0.1:8080/api/health` | 0 | `{"status":"ok","time":"2026-10-30T07:38:06Z"}` | PASS |
| 基线 before | `curl -s http://127.0.0.1:8080/api/issues` | 0 | `"total":2`（1fad3f5b 机房空调异响 / 33a4dccd 3F 走廊墙面破损） | PASS |
| S4 自动化提交（短名首跑） | `Unity.exe ... -executeMethod S4TestRunner.Run` | EXIT=1 | 日志 308 行：`executeMethod class 'S4TestRunner' could not be found.`（类在 namespace MvpArSubmitter 内，命令用了短名；参数问题非代码问题，重试 1 次） | FAIL（已定位） |
| S4 自动化提交（全名重跑 1/1） | `Unity.exe ... -executeMethod MvpArSubmitter.S4TestRunner.Run` | EXIT=0 | `Logs/s4-test.log`：`HTTP 状态码: 201`/`成功: True`/`issues.json 对应条数: 前=2 后=3`/返回 id `e733b2d1-1ccf-46bd-b24e-fc40b7c68154` | PASS |
| 外部独立确认 | `curl -s http://127.0.0.1:8080/api/issues` | 0 | `"title":"S4-自动测试-20261030-155734"`、`"position":{"x":1,"y":0,"z":1}`、`"total":3`（= before+1） | PASS |

本轮发现并修复的问题：
1. `Assets/Scripts/IssueApi.cs` 缺 `using UnityEngine;` 导致 CS0246（MonoBehaviour / Coroutine），此前因旧编辑器（22f1 安装缺陷）编译从未走到 Assembly-CSharp 而一直被掩盖。
2. `IssueApi.cs:38` 与 `Assets/Editor/S4TestRunner.cs:64` 使用了本机 Unity 2022.3.62f3c1 不存在的 `AsyncOperation.WaitForCompletion()`（CS1061），同属被掩盖的历史问题；均改为 `UnityWebRequestAsyncOperation op = ...; while (!op.isDone) { Thread.Sleep(10); }`（request.timeout=8 兜底，不会死等）。
3. 调用参数问题：`-executeMethod S4TestRunner.Run` 短名找不到类，须用全名 `MvpArSubmitter.S4TestRunner.Run`（代码未改，命令修正，重试 1 次内解决）。
- 下一步：等用户确认后进入 Play 模式人工验证（SubmitUIBootstrap 运行时 UI + 空标题拦截 + 真实提交），或直接进入 S5（AR 集成）。

## [3] 2026-10-30 WaitForCompletion 重构为 WaitBounded（有上限+Abort），全套复验通过

说明：[2] 轮已用无上限轮询跑通；本轮按用户规格重构为 `WaitBounded`（15 秒上限 + Abort 硬保险 + 返回 bool），三处调用点统一，然后完整重跑步骤 2-6。

| 检查项 | 命令 | 退出码 | 关键输出 | 结论(PASS/FAIL) |
|---|---|---|---|---|
| 改动1 IssueApi.cs 两处 | Edit：SubmitSync 内改 `WaitBounded(request);`；SubmitRoutine 前新增 `public static bool WaitBounded(UnityWebRequest request, int timeoutMs = 15000)`（Stopwatch 上限，超时 `request.Abort()` 返回 false） | — | 30-60 行原文已贴（WaitBounded 调用 + 方法全文） | PASS |
| 改动2 S4TestRunner.cs 一处 | Edit：FetchTotal 内 `request.timeout = 8;` 后改 `IssueApi.WaitBounded(request);`（其后 result 判断未动） | — | 55-72 行原文已贴 | PASS |
| grep WaitForCompletion | `grep -rn "WaitForCompletion" Assets` | 首跑 grep 0（1 匹配） | 唯一匹配=IssueApi.cs:44 XML 注释文字「2022.3 没有 WaitForCompletion」（非代码调用）；按 0 匹配判据将注释措辞改为「2022.3 没有提供同步等待方法」（只改注释，如实披露） | FAIL→修正 |
| grep WaitForCompletion 复查 | `grep -rn "WaitForCompletion" Assets` | grep 1 | 空输出（0 匹配） | PASS |
| 导入编译 | `Unity.exe -batchmode -quit -projectPath ... -logFile docs/s4-import.log` | EXIT=0 | `EXIT=0` | PASS |
| 判据1 无 CS 错误 | `grep -c "error CS" docs/s4-import.log` | grep 1 | `0`（无匹配） | PASS |
| 判据2 MovedFromExtractor | `grep -c "ApiUpdater.MovedFromExtractor" docs/s4-import.log` | grep 1 | `0`（无匹配） | PASS |
| 后端 health | `curl -s http://127.0.0.1:8080/api/health` | 0 | `{"status":"ok","time":"2026-10-30T11:17:48Z"}` | PASS |
| 基线 before | `curl -s http://127.0.0.1:8080/api/issues` | 0 | `"total":3`（e733b2d1 上轮测试记录 / 1fad3f5b / 33a4dccd） | PASS |
| S4 自动化提交 | `Unity.exe ... -executeMethod MvpArSubmitter.S4TestRunner.Run` | EXIT=0 | `Logs/s4-test.log`：`HTTP 状态码: 201`/`成功: True`/`issues.json 对应条数: 前=3 后=4`/返回 id `800ea542-fdd7-4e74-bc94-08746ef6b59d`；命令立即返回，无 ~15 秒挂住（用户预告的三种阻塞征兆均未出现） | PASS |
| 外部独立确认 | `curl -s http://127.0.0.1:8080/api/issues` | 0 | `"title":"S4-自动测试-20261030-191809"`、`"position":{"x":1,"y":0,"z":1}`（y=0）、`"total":4`（= before+1） | PASS |

本轮发现并修复的问题：WaitForCompletion（CS1061）按用户规格统一收敛为 `IssueApi.WaitBounded`（上限 15s + Abort + bool 返回），消除无上限轮询在极端情况（timeout 字段失效）下 batchmode 无限挂住的理论风险；附带把 XML 注释里的 API 名改写以满足 grep 0 匹配判据（已如实披露，非代码改动）。
- 下一步：等用户确认 → Play 模式人工验证（运行时 UI）或进 S5（AR 集成）。

## [4] 2026-10-30 S5.1 第 1 节 S5Setup.Apply：停止（判据 c 未达标 planeMgr=False）

按「S5.1 续跑指令」第 1 节执行一次 `Unity.exe -batchmode -quit -executeMethod S5Setup.Apply -logFile docs/s5-setup.log`（EXIT=0，仅记录不作判据）。

| 判据 | 命令 | 实际输出 | 结论(PASS/FAIL) |
|---|---|---|---|
| a) `grep -c "S5Setup 完成"` | 期望 1 | `0`（无匹配）；但 c) 中第 4629 行存在完成行原文 `[S5Setup] 完成：Assets/Scenes/Main.unity 已创建并加入 Build Settings`——日志实际写的是 `[S5Setup] 完成：`，模式串 `S5Setup 完成` 中间隔了 `] ` 导致 grep 不命中，属判据模式笔误非执行缺失 | FAIL（模式笔误，执行实际完成） |
| b) `grep -c "error CS"` | 期望 0 | `0` | PASS |
| c) `grep "\[S5Setup\]"` | 贴全部行 | 4570 `[S5Setup] 玩家设置完成`；4579 `[S5Setup] activeInputHandler 已改为 2 (Both)`；4594 `[S5Setup] 菜单命令返回 okA=True okB=True`；4620 `[S5Setup] 校验 session=True xrorigin=True planeMgr=False hud=True`；4629 `[S5Setup] 完成：...`。**要求 planeMgr=True 实际为 False** | FAIL |
| d) `ls Assets/Scenes` | 存在 Main.unity | `Main.unity` + `Main.unity.meta` | PASS |
| 兜底分支日志 | `grep -c "兜底"` | `1` 无匹配（未出现） | 无兜底 |
| quit 证据 | `grep -c "Batchmode quit successfully"` | `1`（4638 行）；前后 30 行已贴，含 4620/4629 两行 + `at S5Setup.Apply() (at Assets/Editor/S5Setup.cs:101/:105)` 调用栈，证实 -executeMethod 真实执行 | PASS |

场景只读检查：`m_Name` 列表 = XR Origin / AR Session / Main Camera / Camera Offset / ARDebugHud；场景文本 grep ARPlaneManager/ARSession/AROrigin 均 0（YAML 按 GUID 引用，文本 grep 不作准，以编辑器内 FindObjectOfType 校验为准 → planeMgr=False 为权威结论）。
- 判断：okA=True okB=True 说明 ExecuteMenuItem 在 batchmode 下生效（新发现，记录在案）；但 XR Origin (Mobile AR) 菜单创建出的场景**缺 ARPlaneManager 组件**，平面检测将不可用（HUD 会显示 -1 个）。
- 已停止：未执行第 2 节 ARCore 检查、未发起 APK 构建（15-25 分钟的包打出来也达不到 S5.1 目标）。
- 涉及文件：仅本文件（未修改任何 .cs 源文件）
- 下一步：等用户选择修复方式（GUI 给 XR Origin 手工加 ARPlaneManager 并存场景，或授权改 S5Setup.cs 加 AddComponent 兜底）→ 再续跑第 2-5 节。

## [5] 2026-10-30 S5.1 v3：第 1 节四判据全 PASS；第 2 节 ARCore 检查 → 分支 B 停止

S5Setup.cs 已由外部定稿（132 行 / 6093 字节，新增 87-111 行「补挂 ARPlaneManager / ARRaycastManager」）。重跑 `S5Setup.Apply`（新日志 docs/s5-setup3.log，EXIT=0 仅记录不作判据）。

| 判据 | 实际输出 | 结论 |
|---|---|---|
| a) `grep -c "\[S5Setup\] 完成"` | `1` | PASS |
| b) `grep -c "error CS"` | `0` | PASS |
| c) `[S5Setup]` 全部行 | 玩家设置完成 / activeInputHandler 当前=2 / 菜单命令返回 okA=True okB=True / 补挂 ARPlaneManager（菜单版缺失） / 补挂 ARRaycastManager（菜单版缺失） / 管理器数量 plane=1 raycast=1 / 相机清点 name=Main Camera ARCameraManager=True ARCameraBackground=True / 校验 session=True xrorigin=True planeMgr=True hud=True / 完成：Assets/Scenes/Main.unity 已创建并加入 Build Settings | PASS（三条必需行全部出现） |
| d) `ls Assets/Scenes` | Main.unity + Main.unity.meta | PASS |
| 停止条件检查 | 兜底=0；警告：找不到 XROrigin=0；Batchmode quit successfully=1 | 全干净 |

第 2 节 ARCore 启用检查（三条只读命令）：
- `grep -r "ARCoreLoader" Assets/XR` → 无输出（0 匹配）
- `ls Assets/XR/Settings` → 无输出（目录不存在）
- `grep -n "ARCore" ProjectSettings/ProjectSettings.asset` → 无输出（0 匹配）
→ **分支 B：工程未启用 ARCore loader**。按指令停止，未发起第 3/4 节 APK 构建，已原样转达 GUI 操作给用户。
- 涉及文件：仅本文件（未修改任何 .cs 源文件、未动 manifest.json / XR 配置）
- 下一步：用户 GUI 勾选 ARCore 后 → 续跑第 3 节后台打包 + 第 4 节轮询。

## [15] 2026-10-30 S5.1 第一次 Android 打包：FAIL（Vulkan 与 ARCore 不兼容）

第 0 步 .gitignore 追加 `Builds/` 并提交（b733a96）；第 1 步三判据全过（`Assets/XR/Loaders/ARCoreLoader.asset` 401 字节存在、`m_InitManagerOnStart: 1`（XRGeneralSettingsPerBuildTarget.asset 第 31 行）、`D:\Unity job\2022.3.62f3c1\Editor\Unity.exe` 存在）；第 2 步后台发起 `S5Build.Build`（未前台等待，日志 Logs/s5-build.log）；第 3 步轮询约 2 分钟出结果。

| 检查项 | 原始输出 | 结论 |
|---|---|---|
| `grep "BUILD_RESULT=" Logs/s5-build.log` | `BUILD_RESULT=Failed size=0 errors=2` | FAIL |
| `ls -lh Builds/mvp-ar.apk` | 无文件（NO_APK） | FAIL |
| `grep -c "error CS"` | `0` | 无 C# 编译错误 |
| 失败标记扫描 | 仅第 1 行 `[Licensing::Module] Trying to connect to existing licensing client channel...`（启动期正常连接消息，非错误） | 无 Gradle build failed / CommandInvokationFailure / SDK-NDK-JDK / Keystore 类失败 |
| 根因（374 行原文） | `Error building Player: BuildFailedException: You have enabled the Vulkan graphics API, which is not supported by ARCore.`（来源 `./Library/PackageCache/com.unity.xr.arcore@5.1.5/Editor/ARCoreBuildProcessor.cs Line: 151`） | 已定位 |

耗时：后台发起到出现 BUILD_RESULT 约 2 分钟（失败发生在 Preprocess Player 阶段 `duration:369ms`，未进入 Gradle/IL2CPP，故远快于正常首包 8-25 分钟）。
- 涉及文件：.gitignore、docs/PROGRESS.md（**未修改任何 .cs 源文件**，未改 ProjectSettings，未删 Library，未装包）
- 判断：S5Setup 写入的图形 API 列表为 Vulkan + GLES3，ARCore 5.1.5 构建前置直接拒绝 Vulkan。需将 Android 图形 API 改为仅 OpenGLES3 才能出包。本轮禁改工程设置与代码，停在此处等裁定。
- 下一步：等用户裁定改法（改 S5Setup.cs 图形 API 列表 / 用户 GUI 手改 Player Settings > Graphics APIs 为 OpenGLES3 only）→ 重跑第 2-4 步。

## [16] 2026-10-30 S5.1 打包成功收尾：APK 18,436,516 字节，ARCore .so 已内嵌，commit 839ee0f

S5.1 打包成功。`BUILD_RESULT=Succeeded size=352497157 errors=0`；APK = `Builds/mvp-ar.apk`，**18,436,516 字节**（魔数 PK 有效）；构建耗时 436s（按日志时间戳）；日志 Vulkan 匹配数=0、error CS=0。
APK 内嵌 ARCore 原生库已核实（unzip -l 原始输出）：
```
   414624  lib/arm64-v8a/libUnityARCore.so
    68024  lib/arm64-v8a/libarcore_sdk_c.so
   100800  lib/arm64-v8a/libarcore_sdk_jni.so
   445360  lib/arm64-v8a/libarpresto_api.so
 30740248  lib/arm64-v8a/libil2cpp.so
```
收尾提交（commit **839ee0f** `feat(s5): ARCore 启用 + Android 图形 API 仅 OpenGLES3 + mvp-ar.apk 产出成功；UserSettings 移出跟踪`，44 files）内容：
- 修复链：[15] 的 Vulkan 失败 → 两处 sed（ProjectSettings.asset `m_APIs: 0b000000` 仅 OpenGLES3；S5Setup.cs 删 GraphicsDeviceType.Vulkan 行）→ 重打包成功。
- 仓库卫生：.gitignore 追加 `UserSettings/`、`docs/*.log`、`docs/*-console.txt`；`git rm --cached -r UserSettings/`（3 文件取消跟踪，本地保留）；docs 下 10 个构建日志撤出暂存（其中 s4-* 系列仍为历史已跟踪文件，ignore 规则对已跟踪文件不生效，仅 s5-setup* 由 check-ignore 验证返回忽略路径）。
- 入库新增：Assets/Editor/（S5Build/S5Setup）、Assets/Scripts/ARDebugHud.cs、Assets/Scenes/Main.unity、Assets/XR/ 全套（ARCoreLoader/Settings/GeneralSettings）、ProjectSettings（SceneTemplateSettings.json、XRPackageSettings.asset 等）、Packages manifest/lock。
- 途中一次按纪律停顿：首轮 `git add -A` 后 staged 中出现 UserSettings/（基线 commit 313e889 已跟踪，.gitignore 不生效），贴全量 status 等裁定，用户指定方案 A 处理后完成提交。
- 涉及文件：.gitignore、docs/PROGRESS.md、（承接上文）ProjectSettings.asset / S5Setup.cs 各 1 行；未新增任何代码改动、未重打包、未重跑 Setup、未装机。
- 下一步：装机验证由用户完成（S5.1 唯一目标：手机见摄像头画面 + 平面检测数量 HUD）；之后进 S5.2（手机连 LAN 后端 192.168.5.29:8080）。

## [17] 2026-10-31 S5.2 平面点选放置标记 + 真实坐标提交：打包成功（commit 4c1adfc）

目标：假坐标 (1,0,1) → 手指点平面放标记 → 提交真实命中点坐标。只改 4 个脚本文件，未动场景/工程设置/XR/Packages。
- ApiConfig.cs：BaseUrl `127.0.0.1:8080` → `http://192.168.5.29:8080`（手机访问电脑后端的 LAN 地址）。
- PlaneTapPlacer.cs（新建 4678 字节）：Awake 取 ARRaycastManager（null 则 LogError+禁用）；Update 里 touch Began 优先、鼠标兜底；三重拦截（EventSystem UI → panelRect 矩形 → 左上角 HUD 区 x<500 && y>height-160，命中即 Log "[Tap] 拦截:原因"）；`raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon)` 未命中 Log "[Tap] 未命中平面"；命中则单标记策略（Cube 命名 PlacementMarker，scale 0.08，黄色，pos=pose.position+up*0.04），public LastTapInfo="放置/UI拦截/未命中" 供 HUD。
- SubmitPanel.cs：删 FakeX/Y/Z 常量；提交前判 `placer==null || !placer.HasMarker` → "请先点击地面放置标记"；坐标 Round3(placer.MarkerPosition)；成功提示带坐标快照 `lastSubmittedPos`（请求发出前赋值，回调显示——首轮编译曾因 markerPos 跨方法引用报 18 条 CS0103，按用户批准方案加字段修复）。
- SubmitUIBootstrap.cs：panelRoot 改 RectTransform 创建；建 PlaneTapPlacer 注入 panelRect；"本阶段为假坐标"标注改为 SetHint 统一提示（默认"请点击地面放置标记"）。
- ARDebugHud.cs：面板加"标记: 未放置 / (x,y,z)"与"最近一次点击: 放置/UI拦截/未命中"两行，Box 480×150 与拦截区 (c) 的 160px 对齐。

| 验证 | 原始输出 | 结论 |
|---|---|---|
| 首轮编译（s52-import.log） | `error CS 计数: 18`，全部 `SubmitPanel.cs(111-118) CS0103: 'markerPos' does not exist`（我的跨方法作用域失误） | FAIL→停，报告，用户批准修复 |
| 局部核对 | markerPos 仅 90/98/99/100/106（全在 OnClickSubmit）；lastSubmittedPos 26 声明/106 赋值/115,116,121,122 使用（4 行 6 处 Round3，据实） | PASS |
| 复编译（s52-import2.log，新名防旧错） | `error CS 计数: 0`；CompileScripts 935ms + Batchmode quit successfully + return code 0（启动时残留空 UnityLockfile，已核实编译真实发生） | PASS |
| 打包（s5-build3.log，后台+60s 轮询 1 次命中） | `BUILD_RESULT=Succeeded size=352587264 errors=0`；APK `18442892` 字节（>15MB ✓，热缓存增量包 159s）；两日志 error CS 均 0；BaseUrl=192.168.5.29 | PASS |

commit **4c1adfc** `feat(s5.2): 平面点选放置标记 + 三端坐标真实化 + 提交坐标快照`（6 files：ApiConfig/SubmitPanel/SubmitUIBootstrap/ARDebugHud + PlaneTapPlacer.cs/.meta）。
- 下一步：用户装机验证（对准地面出平面 → 点平面放黄色方块 → 填标题提交 → 管理端核对坐标与现场一致；再验证 UI 上点击不误放、HUD 区点击不误放）。

## [18] 2026-10-31 S5.3 四处修正 + Marker 材质资产 + vite 验证：打包成功

按用户修正指令落实 4 处改动；未动场景/工程设置/XR/Packages；未重跑 S5Setup.Apply。
- 改动 1（新建 Assets/Editor/S5MarkerAsset.cs，841 字节）：`Create()` 开头 `IsValidFolder("Assets/Resources")` 为假则 `CreateFolder("Assets","Resources")`，紧跟 `AssetDatabase.Refresh()`（先刷新让 AssetDatabase 认识新目录），再 `Shader.Find("Standard")` → `new Material` → `CreateAsset("Assets/Resources/Marker.mat")` → `SaveAssets` → `Refresh`。
- 改动 2(b)（PlaneTapPlacer.cs）：拦截 a 从 `IsPointerOverGameObject` 换成 `EventSystem.current.RaycastAll(ped, uiHits)`，`uiHits.Count>0` 即拦（保留 `EventSystem.current != null` 判空），`LastTapInfo="UI拦截(raycast)"`，Log "[Tap] 拦截:UI 射线命中 N 个"。
- 改动 2(c)（精确删除）：PlaneTapPlacer.cs 删 panelRect 字段及注释与拦截 b 整块（`RectangleContainsScreenPoint`）；SubmitUIBootstrap.cs 删 `placer.panelRect = ...` 及注入注释；panelRoot 注释改回"承载 SubmitPanel 组件"。`grep -rn panelRect Assets/Scripts/` → 无残留。
- 改动 2(d)：标记材质只做 `Resources.Load<Material>("Marker")` 赋 `sharedMaterial`，未找到则 `Debug.LogError`；不再改 `.color`（黄色在材质创建时定死）。
- 改动 2(e)+4（ARDebugHud.cs）：拦截 (c) 阈值 `Screen.height-160f` → `-200f`；HUD Box `(10,10,480,150)` → `(10,10,480,190)`；8 行 y=14/34/54/74/94/114/134/154（"后端连通: "插在第 6 行 114），末行 154 底边 174 在 190 内；标题 → "S5.3 诊断面板"；新增 `CheckBackendLoop` 协程每 5s `GET {backend}/api/health`（timeout=5）写 `backendState`（已连通/未连通(err)/检测中…）。

| 步骤 | 原始输出 | 结论 |
|---|---|---|
| 生成 Marker.mat（s53-marker.log，-executeMethod S5MarkerAsset.Create） | `error CS 计数: 0`；`[S5MarkerAsset] 已创建 Assets/Resources/Marker.mat`；`ls Assets/Resources` → Marker.mat 2199 字节 + .meta | PASS |
| 打包（s5-build4.log，后台 + 60s 轮询 1 次命中） | `BUILD_RESULT=Succeeded size=352708500 errors=0`；APK `18470838` 字节（>15MB ✓，增量 274s）；s53-marker/s5-build4 两日志 error CS 均 0 | PASS |
| Marker 打入 APK | `unzip -l` 无散文件 Marker.mat——预期：Resources 内容序列化进 `assets/bin/Data/sharedassets0.assets`（8748 字节，较上轮增长含新材质） | PASS（说明性核实，非缺失） |
| 第 3 步 vite（原标注 UNVERIFIED） | `curl -s -o /dev/null -w "HTTP=%{http_code}" http://127.0.0.1:5173/` → `HTTP=200`（首次 curl 即中，未重试，未触发 UNVERIFIED 分支）；vite-dev.log：`VITE v5.4.21 ready in 1201ms` + Local http://localhost:5173/ | PASS |

- 涉及文件：Assets/Editor/S5MarkerAsset.cs（新）、PlaneTapPlacer.cs、SubmitUIBootstrap.cs、ARDebugHud.cs、Assets/Resources/Marker.mat（生成物）。
- vite dev server 在后台运行（sandbox mvp/web，/api 代理 → 127.0.0.1:8080）；手机访问 admin 需 `--host`（vite 默认只绑 localhost，本轮未要求，未改）。
- 下一步：commit → 用户装机验证（UI 点击 Logcat "[Tap] 拦截:UI 射线命中 N 个"；HUD 区不误放；Marker 材质缺失则 LogError；后端连通行 5s 刷新）。

## [19] 2026-10-31 S5.5 + S5.5b：明文 HTTP 解禁 + SendWebRequest 防护；首编译 3 错误经用户规格修复后打包成功

**根因（用户已确认，本轮未重新排查网络）**：ProjectSettings.asset 第 773 行 `insecureHttpOption: 0` = 播放器禁止 UnityWebRequest 访问 http://，手机浏览器可开 http://192.168.5.29:8080/api/health 而 App 内探测必失败即此故。

S5.5 四处改动：ProjectSettings.asset `insecureHttpOption: 0`→`2`；S5Setup.Apply 加 `PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;`（防重跑改回）；ARDebugHud 探测加 try/catch 异常上屏；IssueApi 两处裸 SendWebRequest 加 try/catch。未动场景/XR/Packages，未重跑 S5Setup.Apply。

**S5.5 首编译 FAIL（error CS=9，去重 3 条），按纪律停下报告，两处根因均定位：**
1. 我的失误：IssueApi 两处 Edit 的 old_string 都误含 `UnityWebRequest request = BuildCreateRequest(data);` 前缀（WaitBounded 参数即 request，无此行），两个 Edit 同时命中 SubmitRoutine 同一位置——WaitBounded 防护块（含 `return false`）被写进协程内（CS1622×2），WaitBounded 本身未改。
2. 用户规格代码触碰 C# 语言限制：catch 子句内禁止 yield（ARDebugHud.cs CS1631）。

**S5.5b 修复（一次一文件、整段替换、同文件禁并行）：**
- ARDebugHud.cs：catch 内只置 `sendFailed=true`，`yield return wait; continue;` 移到 try/catch 外的 `if (sendFailed)` 块（grep 确认 yield return wait 在 60/68/95 行，均 catch 外）。
- IssueApi.cs B-1：WaitBounded 整段替换（SendWebRequest 进 try/catch，异常与 op=null 均 `return false`——普通 bool 方法合法）；B-2：SubmitRoutine 误置块整段替换为 `sendFailed` 标志 + `Fail(0, "请求异常: …", null)` + `yield break`（协程内合法）；grep 确认 `return false` 仅在 WaitBounded 57/62/72/74 行。

| 步骤 | 原始输出 | 结论 |
|---|---|---|
| S5.5 首编译（s55-import.log） | `errorCS=9`：IssueApi.cs(71,17)/(73,76) CS1622 ×2 + ARDebugHud.cs(55,21) CS1631（三程序集重复报） | FAIL→停，报告，用户发 S5.5b 规格 |
| S5.5b 复编译（s55-import2.log 新名） | `errorCS=0`；`CompileScripts: 3629.512ms` + quit 计数 1 | PASS |
| 打包（s5-build4.log 按用户指定名覆盖旧日志，60s 轮询 2 次命中） | `BUILD_RESULT=Succeeded size=352758842 errors=0`；errorCS_build=0；APK `18474394` 字节 | PASS |
| insecureHttpOption 复核 | 第 773 行 `  insecureHttpOption: 2`（唯一 1 行） | PASS |

- 涉及文件：ProjectSettings/ProjectSettings.asset（1 行）、Assets/Editor/S5Setup.cs（1 行）、Assets/Scripts/ARDebugHud.cs、Assets/Scripts/IssueApi.cs。
- 仍未验证（需装机）：insecureHttpOption=2 后 HUD「后端连通」是否变「已连通 #N X.Xs」；探测异常上屏四态实际显示；提交硬超时 12s 路径。
- 下一步：commit → 装机验证。

## [20] 2026-10-31 加分项 1：AR 标记按优先级/状态变色（编译通过，未打 APK）

回滚点 commit **b3c0ad8**（chore(s6.1): 管理端验收截图（回滚点 bonus1-before-marker-color））。
改动只涉 2 文件（+79/−1）：PlaneTapPlacer.cs、SubmitPanel.cs；未动 IssueModels/ApiConfig/React/Go/backend/data/issues.json；**本轮不打 APK**。

- PlaneTapPlacer.cs：新增唯一颜色决策入口 `ColorForState(priority, status)`（内部委托 `ColorForPriority`）——status=="resolved" → 灰半透明 new Color(0.5,0.5,0.5,0.35) 优先；否则 high=Color.red / low=Color.green / default（medium/null/空/未知）=Color.yellow，switch default 兜底不抛异常。新增公开方法 `SetCurrentPriority`（放置前记录所选优先级并即时刷新已有标记）与 `ApplyIssueStyle(priority,status)`（提交成功后按后端返回值刷新）。marker 创建时 `new Material(Resources.Load<Material>("Marker"))` 克隆专属实例再改色——共享资产颜色永不变；Resources.Load 加载与 LogError 兜底保留。
- SubmitPanel.cs：`SetPriority` 里同步 `placer.SetCurrentPriority(PriorityValues[index])`；`OnSubmitFinished` 成功分支末尾 `placer.ApplyIssueStyle(result.issue.priority, result.issue.status)`；失败分支不动标记（原有回滚行为不变）。
- 跨类依赖方案：采用用户允许的最小方案「SubmitPanel 成功后调用 placer 公开方法 ApplyIssueStyle」，无硬塞依赖、无新增外部依赖。

| 验证 | 原始输出 | 结论 |
|---|---|---|
| A 静态保留（8 项） | Input.touchCount(102,106) / GetTouch(i)(108) / PlaneWithinPolygon(150) / EventSystem.current.RaycastAll(132) / Resources.Load("Marker")(167) 全在；GetTouch(0) 仅注释行 105；FakeX/Y/Z 0 匹配；git diff ApiConfig 为空 + BaseUrl 仍 `http://192.168.5.29:8080` | PASS |
| B 颜色规则 | 代码级证据（方法全文）：high→Color.red / low→Color.green / default 含 medium/null/空/未知→Color.yellow；resolved→new Color(0.5,0.5,0.5,0.35) 且判定优先于优先级 | PASS（代码级） |
| C 编译 | `Unity.exe -batchmode -quit -logFile Logs/bonus1-import.log`：`errorCS=0`；`CompileScripts: 9629.006ms` + quit 计数 1 | PASS |
| D 回归 | CreatePrimitive 仅 1 处 + `if (marker == null)` 复用(161/163)；拦截 a/c 与 uiHits.Count>0(126/133/141)；提交拦截文案/interactable=false/失败保留输入全在；git diff --stat 仅 2 文件，React/Go/数据文件零改动 | PASS |
| E git | diff --check 空输出（无空白错误）；commit `bonus1-marker-color` | PASS |

- 仍未验证（UNVERIFIED）：Play 模式/真机上标记实际颜色显示——需人工运行；提交成功后 marker 变色、resolved 灰半透明的运行时效果未跑。
- 下一步：Play 模式或装机人工验证颜色 → 通过后再打包。

## [21] 2026-10-31 Bonus 1 复核修正：resolved 真半透明（渲染状态切换）+ 新标记状态重置（编译通过，未打 APK）

**发现**：[20] 只改 alpha，但 Marker.mat 为 Standard Opaque（本轮读资产原文：`_Mode: 0`、`_SrcBlend: 1`、`_DstBlend: 0`、`_ZWrite: 1`、`m_ValidKeywords: []`、`m_CustomRenderQueue: -1`），Opaque 下 alpha 无效，半透明未被证明有效。另：单标记复用时旧 resolved 灰色会污染新上报的标记。
**更正 [20] 记录**：其 E 栏写的 `commit bonus1-marker-color` 为提交 message，真实 hash 是 `fe46893`（以 git log 为准，此处按实更正，不改历史条目）。

**Bonus 1 代码修正**（只改 1 个文件 Assets/Scripts/PlaneTapPlacer.cs；SubmitPanel.cs/Marker.mat 零改动）：
- 新增材质样式唯一应用入口 `ApplyMarkerVisualStyle(Color color, bool resolved)`：resolved=true → `_Mode=3`、`_SrcBlend=5`(SrcAlpha)、`_DstBlend=10`(OneMinusSrcAlpha)、`_ZWrite=0`、Enable `_ALPHABLEND_ON`、Disable `_ALPHATEST_ON`/`_ALPHAPREMULTIPLY_ON`、renderQueue=3000(Transparent)、color=(0.5,0.5,0.5,0.35)；false → `_Mode=0`、1、0、`_ZWrite=1`、三 Keyword 全关、renderQueue=2000(Geometry)。只作用于 `markerMaterialInstance`。
- 新增 `RefreshMarkerStyle()` 内部收敛；`SetCurrentPriority`/`ApplyIssueStyle`/marker 创建处三处调用统一走它（原直接 `.color=` 赋值已移除）。
- 修复二：`HandleTap` 里真实命中平面、准备更新 marker 位置时重置 `currentStatus="open"`（保留 currentPriority），并打日志；marker 已存在分支重刷样式。**提交失败分支无任何重置/改色**（失败保留原标记行为不变）。
- 保留项逐一核对：ColorForPriority/ColorForState/Input.touchCount/GetTouch(i)/RaycastAll/PlaneWithinPolygon/单标记复用/BaseUrl/三重拦截 全在（见下表）。

| 验证 | 原始输出 | 结论 |
|---|---|---|
| 静态检查 | GetTouch(i)=152；GetTouch(0) 仅注释 149；PlaneWithinPolygon=194；RaycastAll=176；FakeX/Y/Z 无匹配；ColorForPriority=41/ColorForState=52/单标记复用 213-215；git diff ApiConfig 空且 BaseUrl 仍 `http://192.168.5.29:8080`；git status 仅 ` M Assets/Scripts/PlaneTapPlacer.cs`（React/Go/issues.json 不在列表） | PASS |
| Marker.mat 未改 | git status 无该文件；资产仍 `_Mode: 0`...（未被改成动态颜色资产） | PASS |
| 编译 | `Unity.exe -batchmode -quit -projectPath ... -logFile Logs/bonus1b-import.log`：`errorCS=0`；`CompileScripts: 1446.367ms` + quit 计数 1 | PASS |
| 真机测试 | **未执行**（本轮不打 APK、不跑 Play 模式） | UNVERIFIED |

- 涉及文件：Assets/Scripts/PlaneTapPlacer.cs（+~50 行）；docs/PROGRESS.md、AI_LOG.md（追加）。
- **当前结论：PARTIAL**——代码/静态/编译通过；resolved 半透明与状态重置的运行时效果 UNVERIFIED。
- 下一步：Play 模式或装机人工验证 → 通过后再打包；Bonus 2 未开始。

## [22] 2026-10-31 Bonus 1 运行时验证轮：新 APK 产出（含 e9ca53d 后代码），装机/Play 验证 UNVERIFIED

**目标**：验证 high/medium/low 运行时颜色、提交成功/失败行为、resolved 入口；不新增业务功能、不开始 Bonus 2。

**第一步 基础确认**：完整读取 PlaneTapPlacer/SubmitPanel/IssueApi/IssueModels/Marker.mat/AI_LOG/PROGRESS；`git status --short --untracked-files=all` 空输出、`git log -5 --oneline`（f8d5466→e9ca53d→fe46893→b3c0ad8→e4a55dc）、`git diff --check` 空 → 无未提交源码修改，满足打包前置。

**第二步 构建**（源码 = HEAD f8d5466，包含 e9ca53d 之后全部代码；构建后 `git log -1` 仍 f8d5466）：

| 项 | 原始输出 | 结论 |
|---|---|---|
| 命令 | `D:\Unity job\2022.3.62f3c1\Editor\Unity.exe -batchmode -quit -projectPath ... -executeMethod S5Build.Build -logFile Logs/bonus1-runtime-build.log`（后台 + 90s 轮询 1 次命中） | — |
| BUILD_RESULT | `BUILD_RESULT=Succeeded size=352820390 errors=0`（第 946 行） | PASS |
| error CS | 计数 0 | PASS |
| APK | `Builds/mvp-ar.apk` 18,478,786 字节（15:17 新生成，非旧包 18,474,394）；S5Build.cs 硬编码路径，按指令未为改名大范围改代码，构建后复制为 `Builds/bonus1-runtime.apk` 同大小 | PASS |
| 退出证据 | 日志无 "Batchmode quit successfully" 行（S5Build 用 EditorApplication.Exit 直接退出，历来如此）；BUILD_RESULT 已写 + APK 落盘 + Unity.exe 进程已消失 | PASS（按构建类判据） |

**第三步 high/medium/low 运行时颜色**：**UNVERIFIED**——本机无 adb（`where adb` 无输出）、无已连接 Android 设备；Play 模式需 GUI 编辑器与 AR 运行环境，agent headless 无法执行。未将任何静态检查写成运行时 PASS，未伪造截图/日志。装机验证步骤已写入交付报告交用户执行（A 默认黄 / B high 红+重放+提交 / C low 绿+重放+提交 / D medium 恢复黄）。

**第四步 提交失败不破坏样式**：**UNVERIFIED**（需真机或 GUI Play 模式；安全起见未擅停本项目后端）。代码级静态确认：SubmitPanel.OnSubmitFinished 失败分支仅 `SetResult(result.errorMessage)`，不碰 marker、不清输入；IssueApi 失败路径均走 Fail() 返回可读消息。

**第五步 resolved 测试入口**：grep 全工程 "PATCH" 0 匹配（Unity 从未调用 PATCH 状态接口）；Assets/Editor 仅 S4TestRunner/S5Build/S5MarkerAsset/S5Setup，无 resolved 注入入口；POST /api/issues 默认 open → **当前项目无自然触发 resolved 的入口**。未手工改 backend/data/issues.json、未伪造截图。静态代码路径 PASS（[21] 已证），resolved 真实运行时效果保持 **UNVERIFIED**。未在无运行时证据下加 SetOverrideTag。

**第六步 回归检查**：GetTouch(i)=152 / GetTouch(0) 仅注释行 149 / PlaneWithinPolygon=194 / EventSystem.current.RaycastAll=176 / `marker == null` 单标记复用=213 / FakeX/Y/Z 0 匹配 / git diff ApiConfig 空且 BaseUrl 仍 `http://192.168.5.29:8080` / React、Go、backend/data 不在 git status → **全 PASS**；构建日志 error CS=0。

- 本轮修改文件：仅 docs/PROGRESS.md、AI_LOG.md（台账追加；**零源码改动**）。
- **当前结论：PARTIAL**——构建 PASS（新 APK 含 e9ca53d 后代码）；运行时颜色/失败/全 resolved 效果 UNVERIFIED，等装机人工验证；Bonus 2 未开始。
- 下一步：用户安装 Builds/bonus1-runtime.apk 按报告 A-D 步骤验证颜色并回报结果。

## [23] 2026-10-31 Bonus 1 人工运行时验收归档（纯文档轮，零源码改动）

1. **日期**：2026-10-31
2. **目标**：归档 Bonus 1 最终人工运行时验收结果 + 台账一致性检查；不修改功能代码、不开始 Bonus 2。
3. **证据来源**：用户实际安装 APK 后的**人工观察**（用户未提供截图/视频，本条不含任何截图路径，未伪造任何日志）。
4. **APK 路径**：`Builds/bonus1-runtime.apk`（[22] 轮构建产物，18,478,786 字节，源码 f8d5466 含 e9ca53d 后代码；本轮未重新打包）。
5. **medium/high/low 实际颜色**：medium = 黄色 PASS；high = 红色 PASS；low = 绿色 PASS。
6. **重新点击平面后**：颜色正常 PASS（仅为用户观察到的普通颜色重放结果，**不构成** "resolved → 新平面 open 重置" 的验证）。
7. **提交成功后**：颜色正常 PASS。
8. **提交失败回归**：marker 是否保留 UNVERIFIED；输入是否保留 UNVERIFIED（用户未测试）。
9. **resolved 灰色半透明**：UNVERIFIED（用户未测试；Unity 当前无自然触发 resolved 的业务入口——无 PATCH 调用、POST 默认 open）。
10. 本轮未修改任何 Unity 源码（`git status` 空输出起始与结束一致，仅本文件与 AI_LOG.md 追加）。
11. 本轮未重新打 APK。
12. Bonus 2：未开始。
13. **当前结论**：
    - 主路径运行时验证：**PASS**；
    - Bonus 1 总体：**PARTIAL**；
    - 提交失败回归：**UNVERIFIED**；resolved 灰色半透明：**UNVERIFIED**；
    - Bonus 2：未开始。

**台账一致性检查**：[20]（代码实现，编译通过未打 APK）、[21]（resolved 渲染状态修正 + 状态重置）、[22]（新 APK 产出，当时运行时全 UNVERIFIED）与本条（用户人工验收归档）链条一致、结论递进无矛盾；[22] 的"等装机人工验证"由本条闭环。历史条目未覆盖、未删改。

## [24] 2026-10-31 Bonus 2 实现轮：本地规则智能判级（编译通过，未打 APK）

**目标**：按已评审设计实现"轻量本地规则智能判级"——本地关键词规则给出可解释建议、用户可采用/可手动覆盖；不加网络、不改契约、不动 Bonus 1 逻辑。开工检查：`git status` 空、HEAD=42dfda8、`git diff --check` 空。

**改动（3 文件，PlaneTapPlacer/IssueApi/IssueModels/ApiConfig/React/Go 零改动）**：
- **新建 `Assets/Scripts/LocalPriorityRules.cs`**（纯 C# 静态类，零 Unity/网络依赖）：`PrioritySuggestion {priority, reason, matched}` + `Evaluate(title, description)`。词表：high={漏电,火灾,燃气,触电,坍塌,坠落,人身,危险,紧急,重大} / medium={故障,异常,损坏,渗水,噪音,失灵,停用,卡住,影响使用,无法} / low={划痕,轻微,建议,优化,观感,灰尘,清洁,摆放,标识不清,美化}。冲突策略：先标题后描述；同文本 high>medium>low 命中即返回（不计分）；归一化 Trim+ToLowerInvariant（中文子串匹配）；双空返回 matched=false；全程 try/catch 异常回退 matched=false。命中原因含命中词（可解释）。
- **SubmitPanel.cs**（+116 行）：新增 `suggestionText`/`adoptButton` 字段；`SetPriority` 拆分手动锁定语义（手动点按钮 → `manualLocked=true`）+ 共用 `ApplyPriority`（内含原颜色联动 SetCurrentPriority，Bonus 1 路径不变）；`SetPriorityInitial`（启动默认中，不锁定）；`OnAdoptSuggestion`（Array.IndexOf→ApplyPriority，越界安全）；输入变化 `OnInputChanged` 置脏 + `Update` 内 0.4s 防抖评估；`EvaluateSuggestionNow` try/catch 兜底；`RefreshSuggestionDisplay` 显示"建议:高|原因…"或引导文案并开关 adopt 按钮；手动锁定后前缀"已手动选择｜建议"。**提交路径未动**：仍 `PriorityValues[priorityIndex]`；成功分支 ApplyIssueStyle 未动；失败分支保留输入未动。
- **SubmitUIBootstrap.cs**（+15/−2）：新增 `SuggestionText`（420,450，560×28，淡黄）+ `AdoptSuggestionButton`"采用"（1010,450，120×28，初始 interactable=false）；两 InputField onValueChanged → `panel.OnInputChanged()`；启动改调 `SetPriorityInitial(1)`。新控件在 Canvas/GraphicRaycaster 下 → 自动被 RaycastAll 拦截，AR 点击不受影响。

| 验证 | 原始输出 | 结论 |
|---|---|---|
| 静态回归 10 项 | GetTouch(i)=152 / GetTouch(0) 仅注释 149 / PlaneWithinPolygon=194 / RaycastAll=176 / ApplyIssueStyle=247 仍在成功分支 / Fake 0 匹配 / BaseUrl 未变 / 按钮仍绑 SetPriority(Bootstrap 83) / PriorityValues 仍 low,medium,high / diff --stat 仅 SubmitPanel+SubmitUIBootstrap（LocalPriorityRules 为新文件 ??） | PASS |
| Bonus 1 保护 | PlaneTapPlacer.cs 不在 git status（零改动）；SetCurrentPriority 调用链不变；提交成功 ApplyIssueStyle 不变 | PASS |
| 契约保护 | IssueModels/IssueApi/ApiConfig 零改动；无新网络调用；priority 枚举未变 | PASS |
| 编译 | `Unity.exe -batchmode -quit -logFile Logs/bonus2-import.log`：`errorCS=0`；`328: CompileScripts: 4399.496ms` + quit 计数 1 | PASS |

- **本轮结论：代码/静态/编译 PASS；运行时建议效果 UNVERIFIED（未打 APK、未跑 Play 模式）**。
- 涉及文件：Assets/Scripts/LocalPriorityRules.cs（新）、SubmitPanel.cs、SubmitUIBootstrap.cs、docs/PROGRESS.md、AI_LOG.md。
- Bonus 2 未完成项：装机/Play 验证建议刷新、采用、手动覆盖、防抖。
- 下一步：commit → 等用户指令（装机验证或继续调整）。

## [25] 2026-10-31 Bonus 2 增量修正与规则验证：三项偏差修正 + 14/14 规则测试真实执行 PASS（编译 PASS，未打 APK）

**目标**：修正 [24] 实现的三项已确认偏差（标题先命中早返回 / 缺关键词 砸·急·漏水 / 多余 0.4s debounce），并用真实执行（非静态阅读）验证规则；不重做功能、不改 e12988e/e62b7ff 历史、不打 APK。

**基线保护**：开工读 5 文件 + git 原始输出——status 空 / log -3 = e62b7ff, e12988e, 42dfda8 / `git show --stat e12988e` = AI_LOG.md +13、Assets/Scripts/LocalPriorityRules.cs +89（新）、SubmitPanel.cs +116、SubmitUIBootstrap.cs +15/−2、docs/PROGRESS.md +21；`git show --stat e62b7ff` = LocalPriorityRules.cs.meta +11；diff --check 空。未用 reset/amend/rebase/checkout/clean/restore。

**修正（git diff --stat：2 files +17/−33，恰在允许范围内；SubmitUIBootstrap.cs 无需改——OnInputChanged 名字未变）**：
- **偏差1 冲突处理**：`Scan(t) ?? Scan(d)` → 标题+描述逐级共同判断（high 任一命中即 high → 否则 medium 任一 → 否则 low；同级双命中先扫标题取原因词，只影响文案不改等级）；旧单文本 `Scan` 删除；类注释同步更新。
- **偏差2 关键词**：HighKeywords += 砸、急（12 词）；MediumKeywords += 漏水（11 词）；grep 确认 漏水 不在 LowKeywords（10 词未动）；无其他增删。
- **偏差3 去 debounce**：删 suggestionDirty/suggestionEvalAt/SuggestionDebounceSeconds 三字段 + `Update()`；`OnInputChanged` 直接同步调 `EvaluateSuggestionNow()`；grep "Debounce|suggestionDirty|unscaledTime" = 0 残留。

**规则测试（实际执行 LocalPriorityRules.Evaluate）**：项目无测试框架（manifest.json grep test=0；Assets/Editor 仅 S4TestRunner/S5Build/S5MarkerAsset/S5Setup）。用 Unity 自带 Mono（`Editor\Data\MonoBleedingEdge`）mcs 直接编译**项目真实源文件** `Assets/Scripts/LocalPriorityRules.cs` + 沙箱 harness（mvp\ruletests\RuleTests.cs，项目目录外不入库）→ mcs EXIT=0 → mono 执行 RUN_EXIT=0。10 指定场景 + 4 关键词场景：

| # | 输入（标题/描述） | 实际 priority | matched | reason 命中词 | 期望 | 结论 |
|---|---|---|---|---|---|---|
| T1 | 配电箱漏电 / 空 | high | True | 漏电 | high | PASS |
| T2 | 空调异常噪音 / 空 | medium | True | 异常 | medium | PASS |
| T3 | 墙面轻微划痕 / 空 | low | True | 划痕 | low | PASS |
| T4 | 墙面划痕 / 发现配电箱漏电 | high | True | 漏电 | high | PASS |
| T5 | 空调故障 / 建议优化外观 | medium | True | 故障 | medium | PASS |
| T6 | 巡检记录 / 普通巡检记录 | "" | False | 未命中关键词 | matched=false | PASS |
| T7 | "" / "" | "" | False | ""（不崩） | matched=false | PASS |
| T8 | null / null | "" | False | ""（不崩） | matched=false | PASS |
| T9 | 墙面轻微划痕 / 设备故障并有漏电危险 | high | True | 漏电 | high | PASS |
| T10 | 照明设备异常 / 存在坠落危险 | high | True | 坠落 | high | PASS |
| KA_ZA | 指示牌被砸坏 / 空 | high | True | 砸 | high | PASS |
| KA_JI | 火情紧急 / 空 | high | True | 紧急 | high | PASS |
| KA_LOUSHUI | 天花板漏水 / 空 | medium | True | 漏水 | medium | PASS |
| KA_LOUSHUI_LOW | 轻微划痕 / 地面漏水 | medium | True | 漏水 | 漏水不在 low | PASS |

→ **规则测试：PASS（14/14，真实执行项目源文件）**。

| 验证 | 原始输出 | 结论 |
|---|---|---|
| 不变式（第三节） | PriorityValues 单定义（21）+3 使用，无第二套映射；SetPriority→manualLocked=true→ApplyPriority(41-45)；OnAdoptSuggestion→ApplyPriority 不置 manual(55-68)、非法/无匹配提前 return；提交 priority=PriorityValues[priorityIndex](199)；输入变化只写 lastSuggestion 不碰 priorityIndex | PASS |
| Bonus 1 兼容性 | GetTouch(i)=152 / GetTouch(0) 仅注释 149 / PlaneWithinPolygon=194 / RaycastAll=176 / ApplyIssueStyle 118↔成功分支 231 / SetCurrentPriority 106↔141；PlaneTapPlacer/IssueApi/IssueModels/ApiConfig diff 空；BaseUrl 仍 192.168.5.29:8080 | PASS（静态证据） |
| 编译 | Logs/bonus2b-import.log：errorCS=0；`313: CompileScripts: 1613.299ms`；quit=1；BUILD_RESULT=无（本轮仅编译未打包，属预期） | PASS |
| git | diff --check EXIT=0 空；status 仅 2 文件 M；diff --name-only = LocalPriorityRules.cs + SubmitPanel.cs | PASS（范围符合） |

- 涉及文件：Assets/Scripts/LocalPriorityRules.cs、SubmitPanel.cs、AI_LOG.md、docs/PROGRESS.md（仅追加更正记录，历史未覆盖）。
- **当前结论：规则测试 PASS / 编译 PASS / 静态不变式+Bonus 1 兼容性 PASS / Play·真机 UNVERIFIED；本轮未打 APK。**
- 下一步：commit（fix(bonus2) 增量修正）→ 等用户指令（装机验证建议刷新/采用/手动覆盖，或继续调整）。
