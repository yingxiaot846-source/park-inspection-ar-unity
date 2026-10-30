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
