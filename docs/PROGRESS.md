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
