# A 作者身份后续补丁

依赖已审阅 ending-small-A-reviewed.patch 的结果；单独应用本目录 ending-small-A-author-followup.patch，不重放 A 整包。

修正原 A 中保留旧早期 prompt 导致的身份冲突：老周在 YY 和贴吧始终知道自己是作者，早期不得主动点破身份、玩家拥有程序或未披露私事；没有最终私下邀请/读信问话的实际凭据时不得主动推进测试发布。最终实际联系门禁与其它 NPC 旧规则不变。没有注入任何玩家训练/设备/成绩数据。

仅三文件：LaoZhouQuestions.cs 添加中英 EarlyPersonaRule；LlmPersonas.cs 的老周早期分支使用专属规则；TiebaHub.cs 的老周早期分支使用同一规则。其它联系人继续使用旧 Era/匿名规则。

已对已合入 A 的真实生产通过 git apply --check（生产为 LF；补丁按真实字节生成，避免 Windows 写文本时转成 CRLF），逐段静态核对；没有运行 Unity、模型或 Player，没有宣称实际模型一定遵循提示词。补丁 SHA256：679ebb43d9dcb7db13686a69334f2e3207f14522349b86ec547f2d9d77d4af6b。
