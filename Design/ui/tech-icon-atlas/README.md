# 《我的电脑活了》科技图标全量设计交接

## 范围与打开方式

直接打开 `index.html`，无需服务器或 fetch。211 个节点 / 186 个聚合 entry：覆盖审计报告的 203 / 178，再从 YY/persona_packs.json 读取 8 个动态性格题包。设计者模式有隐藏内容剧透。搜索、语义分类、24/32/48/96 px、悬停/键盘聚焦、已发现/锁定/神秘、减少动画均可预览。

## 文件

- manifest.json：全部节点映射与动画参数；base 按真实运行时 entry 查找，overlay 按 node id。
- icons/：105 张原 PNG（对应 127 个节点）和 81 张新绘 SVG（对应 84 个节点）。原 PNG 原样复制且逐字节 SHA256 核验；本目录不会回写 Assets。
- overlays/：211 张透明 SVG 信号叠层。允许同知识主题复用母题；quiz 根据题目关联的实际知识节点选择，而不是序号。深度/宽度多个等级保留聚合 entry 基图。
- generate.py / template.html：可复现源；在项目根运行 `python Design/ui/tech-icon-atlas/generate.py`。新增节点加入权威报告或动态 persona 资源后，补充 SPECIAL 语义映射。断言当前211节点为本版覆盖门槛，扩展目录时同步更新。
- validation.json：静态校验结果。未启动 Unity 或浏览器，视觉观感和 Unity 集成仍需人工验收。

## 视觉与运动规则

原图保持不变。无原图时采用透明底几何图案，不以数字编号作为图案；有网络、卷积、回环、注意力、优化、数据、棋盘、人格、图鉴、小批量、求导、日历、网页、话筒、笔、报纸等语义母题。人格题包用文体符号而非真人肖像；国际象棋与五子棋分开；小批量、自动求导按当前 UI 文案而非历史 ID 含义。

叠层 viewBox 为 128×128；2 单位实线核心 + 5 单位、12% 透明光晕，无 blur/filter。按128px原尺寸解释2px/5px，预览缩放时随图标比例缩放。信号在 phasePath 上运行1000ms，再静止隐藏1600ms，共2600ms。起始/结束轻淡入淡出，基图不动、不呼吸、不缩放，不移动按钮命中区域，无悬浮音效。移出或失焦移除 run，立即复位；后台 visibilitychange 清除运动。普通锁定整体降亮35%，仍可预览低亮信号；神秘态用问号并隐藏名称/基图/叠层。减少动画与系统 prefers-reduced-motion 都取消运动，仅保留静态描线。

## Unity 实施要点

SVG 是设计交换稿，Unity 不应直接把 SVG 内的 CSS 当运行组件。本稿 SVG 文件本身只存透明路径；预览的 CSS 位于 HTML。程序按 manifest 的 phasePath、activeMs/restMs 用 UI mesh/LineRenderer 或离线路径采样重建信号，可先把新基础 SVG 光栅化为项目规格 Sprite。保持现有 PNG 的导入、九宫格和授权，不替换生产资源。图标视觉层关闭 raycastTarget；UI 按钮保持固定命中盒。PointerEnter/Select 启动，PointerExit/Deselect/OnDisable/后台中断复位；减少动画直接显示静态描线。聚合卡按 entry 取 base，别误把 s.d3 等级 ID 当缺失资源；显示下一等级时可选其专属 overlay。

## 来源与授权

既有图像来自本项目 Assets/LingGuangV05/Resources/LingGuangV05/Icons/tech，其素材授权沿用项目，本稿不新增授权声明。新增 SVG 是本稿直接编写的几何图形，未使用外部素材。名称与含义读取 XgTechTreeCopy、XgCatalog、XgTrainingLessons、XgBoardPhenomena 及 persona_packs；manifest 的 catalogSource 标记静态报告或动态包来源。

末次静态校验：81 个新增 entry 的基础 SVG 去除 title 后，跨 entry 完全相同几何组为 0。共用知识母题另附真实语义附件：问答/分层/裁剪、导入箭头/报头、棋谱/落点、手写/杂文、验证码/热图、书脊/链接、一步/两步棋路等。
小尺寸交接：128设计单位下的2/5描边在24px预览会缩为0.375/0.94px，可能偏弱。Unity实施时可按屏幕像素保持核心至少1px、外沿不超过2px且保持低透明度，避免覆盖整个图案；小尺寸允许简化路径。最终清晰度仍需实际UI视觉验收。
