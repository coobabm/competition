# 深色按钮白色直角底片

已查看截图 `LocalVerification/SelectionIsolation/20261018-113408-772/classic-footer-1400x900-wrong.png`。答题按钮四角为浅灰，返回小游戏为明显白色矩形边角，与外层Frame的rim颜色吻合。

证据链：
- XgUi.Button深色根Hit创建时Color.clear；真实Face迁入Visual并RoundSurface(radius12)。反馈代码仅更改Transform，未发现题卡刷新给Hit改色。
- ZhongbaoSkin.Outlined原先 `Panel(frame,rim)` 仍是无sprite的满矩形；按钮内缩1px。现在内部圆角露出外矩形四角。
- XgLabelPage创建yes/no传XgDark.Line（半透明白），所以是较暗灰角；XgLabelPage.DeskGame创建gameToggle仍传ZhongbaoSkin.Line（浅色实体线），所以出现亮白角。

候选补丁 `dark-outlined-corners.patch` 只改ZhongbaoSkin.Outlined：深色外Frame也用圆角13（内12+1px），默认浅色Line映射深色Line；显式非默认rim保持原色，浅色工厂外框/行为不改。保留Frame图形及AnswerFocusOutline，不把焦点提示禁掉；不动命中框、输入、PressFeedback、布局或题目逻辑。

apply --check通过。按要求未编译、未启动Unity、未修改生产文件。后续检查同截图正常/按下/错误反馈/键盘focus状态：外角应透出页面底色，框线仍在，浅色众包按钮保持原样。外Frame保持固定布局，Visual缩放时外框不随动，这是现有外框结构，独立于这次白角修复。
