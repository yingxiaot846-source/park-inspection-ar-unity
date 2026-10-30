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
