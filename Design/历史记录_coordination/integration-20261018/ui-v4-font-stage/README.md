# UI v4 字体交付准备（仅staging）

已从官方来源下载并抽取指定静态字重；实际字体、许可URL/大小/SHA256/字体name表见SOURCE_MANIFEST.json。MiSans官方包227.88MB含多字重多格式，只需抽取的8.12MB Regular TTF；不要把整包/网页缓存加入游戏或GitHub。Inter官方发布4.1中静态Regular字体name表是Version4.001（保留原样，如实记录）；Source Han SerifSC Regular来自Adobe2.003R标签。

## 许可结论与必须保留事项

MiSans官网PDF已下载且提取全文为MiSans-license-extracted.txt。第2条明确允许使用字体创作App等作品并分发/出售；禁止字体改编二次开发及单独再分发；要求在软件中注明使用MiSans，并在字体副本中保留版权与协议。字体源文件保持逐字节不变，不做子集化、不发布独立字体下载或字体包；TMP资产仅作为本游戏内部渲染资源。必须把FONT_CREDITS里的MiSans说明接进游戏的关于/制作名单（仅外部文本不够“在软件中注明”），并把MiSans-license.pdf、字体版权及其他两份许可纳入发行包。

Source Han Serif与Inter使用SIL OFL1.1，允许随软件打包分发，须保留版权/许可且不得单独售卖字体；保留源文件不改。字体本身不改许可。本次不把项目整体改成OFL。

## 主会话最小导入

1. 仅把MiSans-Regular.ttf、SourceHanSerifSC-Regular.otf、Inter-Regular.ttf及MiSans-license.pdf、SourceHanSerif-LICENSE.txt、Inter-LICENSE.txt、FONT_CREDITS.txt、SOURCE_MANIFEST.json放Assets/LingGuangV05/Fonts/UiV4。源字节合计约31.54MiB，构建压缩后实际体积待BuildReport。
2. UiV4FontAssets.cs放Assets/LocalBuild/Editor。运行UiV4FontAssets.Build生成Resources/LingGuangV05/Fonts/UiV4下3个共享TMP角色。只写专属目录，不修改全局TMP配置、桌面字体或开场字体；重复运行保留现有GUID，源字体不一致时报错；材质/图集均保存为子资产。动态图集保留小批预热字，字体正文/自由回复按需增页；生成输出含缺字及Inter数字advance。
3. XgUiV4Fonts.cs放XingGuang/Desktop。它只提供显式角色加载，不会自动影响现有界面；在XingGuangView.Build创建.exe专属ui时把font参数换成XgUiV4Fonts.Sans??font，勿在XgUi全局构造器里替换（其他应用也用它）。阅读页正文显式Reading，数字控件显式Numbers+FixedDigits（纯数字，richText=true）。场景外其他应用和开场保持原样。
4. 将已有Noto CJK必要时接在Sans尾部fallback（以主会话核验的现有TMP资产为准，不创建第二份巨大Noto源）；不能给Reading/Sans相互循环fallback。生僻字与玩家自由输入在实际角色链验证。

## 状态/测试边界

下载与许可核对完成；源码生成器/加载器已准备，尚未编译/执行（遵守主会话性能采样期间禁止重CPU）。未写主项目，未启动Unity。生成器不是完整UI接入；主会话仍需运行、解决缺字符号、检查数字等宽/换行、加入实际游戏内致谢、查看脱离开发机字体环境的Player与BuildReport。对外包里只复制所用字体许可证/说明，不附staging zip。
