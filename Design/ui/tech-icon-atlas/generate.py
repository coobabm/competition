from pathlib import Path
import json,re,shutil,hashlib,html,xml.etree.ElementTree as ET
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
CORE=ROOT/'Assets/LingGuangV05/XingGuang/Core'
SOURCE=ROOT/'Assets/LingGuangV05/Resources/LingGuangV05/Icons/tech'
REPORT=ROOT/'LocalVerification/DesignPass/TechnologyCatalogCoverage/report.json'
text='\n'.join(p.read_text(encoding='utf-8-sig') for p in CORE.glob('*.cs'))
names=dict(re.findall(r'\{\s*"([\w.]+)",\s*new Copy\("([^"]+)"',text))
for ident,name in re.findall(r'id\s*=\s*"([\w.]+)"\s*,\s*name\s*=\s*"([^"]+)"',text): names.setdefault(ident,name)
for ident,name in re.findall(r'P\("([\w.]+)",\s*\d+,\s*"([^"]+)"',text):names['ph.'+ident]=name
for ident,name in re.findall(r'L\("([\w.]+)",\s*\d+,\s*"([^"]+)"',text):names['lesson.'+ident]=name
mini_path=ROOT/'Assets/LingGuangV05/Resources/LingGuangV05/Teaching/minigame_packs.json'
for pack in json.loads(mini_path.read_text(encoding='utf-8-sig'))['packs']:names.setdefault('pack.'+pack['id'],pack['name'])
names['pack.poem_next']='唐诗接字'
quiz={}
quiz_triggers={}
for line in text.splitlines():
 if 'new XgStageQuiz{' in line:
  fields=dict(re.findall(r'(id|q|node|trigger)="([^"]*)"',line)); names['quiz.'+fields['id']]=fields['q'];quiz_triggers['quiz.'+fields['id']]=fields.get('trigger','');quiz['quiz.'+fields['id']]=fields.get('node') or {'gap':'train.holdout','stage_enter':'rnn'}.get(fields.get('trigger'),'sft')
for i,n in enumerate(['是或否','多选一','看图说词','短句','长记忆与翻译','全部放开'],1):names['ab.'+str(i)]='能力 · '+n
# Each motif describes a signal route; all geometry is authored here, without external assets.
M={
'network':('M24 36L64 64L104 36M24 92L64 64L104 92','<circle cx="24" cy="36" r="7"/><circle cx="24" cy="92" r="7"/><circle cx="64" cy="64" r="9"/><circle cx="104" cy="36" r="7"/><circle cx="104" cy="92" r="7"/>','输入汇入神经元，再分流输出'),
'depth':('M30 30H94V50H34V74H94V98H34','<path d="M28 30H100M28 50H100M28 74H100M28 98H100"/>','沿网络层逐层传递'),
'width':('M20 64H42M42 64L70 30H108M42 64H108M42 64L70 98H108','<path d="M100 22V38M100 56V72M100 90V106"/>','由一条输入展开为并行通道'),
'convolution':('M30 38H78V66H50V90H98','<path d="M24 24H104V104H24ZM24 50H104M24 78H104M50 24V104M78 24V104"/><rect x="27" y="27" width="30" height="30" rx="3"/>','小卷积窗口沿图像格点扫描'),
'recurrent':('M28 64H92C112 64 112 28 82 28H46C18 28 18 64 40 64M92 64V98H42','<rect x="42" y="48" width="44" height="32" rx="5"/><path d="M42 88V108M86 88V108"/>','记忆信号回环并通过门控'),
'attention':('M24 30L64 84L104 30M64 84V22M24 56L64 84L104 56','<rect x="18" y="20" width="12" height="12"/><rect x="58" y="16" width="12" height="12"/><rect x="98" y="20" width="12" height="12"/><rect x="54" y="84" width="20" height="20"/>','多个词位汇聚到当前关注位置'),
'optimizer':('M24 32L42 64L62 84L82 90L104 94','<path d="M22 26Q64 110 108 30M20 106H110"/><path d="M70 92L81 90L77 80"/>','梯度沿损失谷底逐步收敛'),
'activation':('M22 96H50L82 32H106','<path d="M22 18V108H110M20 64H108"/>','信号跨过激活阈值'),
'data':('M32 44H96V64H32V84H96','<path d="M22 28H52L62 38H108V102H22Z"/><path d="M34 53H85M34 74H85"/>','数据在资料夹中的记录行流动'),
'clean':('M30 32L54 62V96H74V62L98 32','<path d="M24 26H104L74 64V102H54V64ZM78 86L88 96L106 74"/>','筛除异常记录，保留可用样本'),
'tool':('M26 68H48V42H80V68H104','<rect x="40" y="34" width="48" height="50" rx="6"/><path d="M28 22V42M64 14V34M100 22V42M28 84V106M64 84V114M100 84V106"/>','工具接收输入并处理输出'),
'automation':('M24 38H88V62H40V90H104','<path d="M80 28L92 38L80 48M96 80L108 90L96 100"/><rect x="24" y="52" width="28" height="22" rx="3"/>','任务进入队列并自动送出'),
'board':('M26 90H50V66H74V42H98','<path d="M24 24H104V104H24ZM24 50H104M24 78H104M50 24V104M78 24V104"/><circle cx="50" cy="78" r="8"/><circle cx="78" cy="50" r="8"/>','沿棋盘候选走法搜索'),
'persona':('M30 100V78Q30 62 48 60Q24 30 52 24Q82 20 80 50L94 62L78 68V98','<path d="M50 84L66 96L88 76"/>','性格轮廓内联结偏好与选择'),
'atlas':('M24 30Q44 20 64 34Q84 20 104 30V96Q84 86 64 100Q44 86 24 96Z','<path d="M64 34V100M32 46H54M32 60H54M74 46H96M74 60H96"/>','成长记录在书页之间归档'),
'curve':('M22 94L38 90L50 82L62 85L76 56L90 40L106 34','<path d="M20 20V108H110M26 36H100"/>','训练曲线由低到高积累进步'),
'gap':('M24 90L48 66L72 42L104 30M24 98L48 86L72 84L104 82','<path d="M20 20V108H110M88 44V72M82 48L88 42L94 48"/>','训练与验证路径分离，显示泛化差距'),
'fade':('M104 36L84 50L64 64L44 78L24 92','<path d="M92 26V48M72 40V64M52 54V80M32 70V96"/>','反向信号经过各层逐渐衰弱'),
'branch':('M24 64H52L80 30H106M52 64L80 98H106','<path d="M78 26L98 46M98 26L78 46M78 86L88 96L108 76"/>','区分条件并检验两条可能路径'),
'bottleneck':('M24 30L58 58H70L104 30M24 98L58 70H70L104 98','<path d="M20 22L56 54V74L20 108M108 22L72 54V74L108 108"/>','信息穿过狭窄的编码瓶颈'),
'language':('M24 40H56L74 88H106','<path d="M20 26H58V62H32L20 72ZM70 66H108V102H96L84 112V102H70Z"/>','源语言编码后生成目标语言'),
'math':('M24 64H104M64 24V104','<path d="M24 26H44M34 16V36M84 88H106M84 100H106"/>','运算符连接输入与计算结果'),
'audio':('M24 64H38V40H50V90H64V22H78V98H90V50H104','<path d="M20 108H108"/>','节拍信号穿过不同强度的频带')}

M.update({
'batch':('M26 34H66V50H42V66H82V90H104','<rect x="20" y="24" width="42" height="42" rx="3"/><rect x="34" y="38" width="42" height="42" rx="3"/><rect x="48" y="52" width="42" height="42" rx="3"/>','多张样本卡汇成一次小批量更新'),
'derivative':('M24 94Q56 94 68 62T106 26','<path d="M20 22V106H108M34 100L96 22"/>','沿曲线求局部切线斜率'),
'calendar':('M30 46H98V80H66V100','<rect x="22" y="28" width="84" height="78" rx="5"/><path d="M22 48H106M44 20V36M84 20V36M42 72L56 86L86 58"/>','日期格内提示已知的重要纪念日'),
'chess':('M34 100H96L84 82L78 56L90 38L70 22L48 36L34 64H56','<path d="M34 100V108H98V100M62 38H66M38 62L52 72"/>','马形棋子提示国际象棋战术搜索'),
'browser':('M28 46H100V74H50V96','<rect x="20" y="24" width="88" height="82" rx="5"/><path d="M20 44H108M30 34H34M42 34H46M54 34H58M50 94L62 78L70 100"/>','网页入口内浏览学习资料'),
'probability':('M26 98V76H48V56H70V34H94V98','<path d="M20 108H110M24 22L104 102"/>','比较不同结果的概率分布'),
'guide':('M28 36H100M28 64H100M28 92H100','<path d="M44 24V48M84 52V76M60 80V104"/><rect x="20" y="18" width="88" height="92" rx="4"/>','三个配置滑块显示预训练设置'),
'microphone':('M48 28V60Q48 82 64 82T80 60V28','<rect x="46" y="20" width="36" height="60" rx="18"/><path d="M32 52V62Q32 96 64 96T96 62V52M64 96V110M48 110H80"/>','话筒声路表示相声段子的口语节奏'),
'pen':('M28 100L62 62L100 24','<path d="M28 100L38 76L84 22L106 24L104 46L52 90ZM62 62L90 62M40 104H106"/>','笔锋写下杂文的论点'),
'news':('M28 36H100V56H28V78H100','<rect x="20" y="22" width="88" height="86" rx="3"/><path d="M30 34H98M30 50H62V80H30ZM72 52H98M72 66H98M30 94H98"/>','新闻稿从标题到正文逐行展开'),
'hearttext':('M30 40Q30 20 48 24L64 40L80 24Q104 20 100 44L64 80Z','<path d="M28 94H100M40 106H88"/>','情感文本把语句连接到心形意象'),
'debate':('M20 36H76V68H50L30 84V68H20M76 52H108V98H90L78 110V98H58','<path d="M32 48H64M70 82H96"/>','两个意见框来回回应'),
'answer':('M26 36H100V82H62L40 100V82H26','<path d="M44 56L56 68L82 44"/>','对话框内出现参考回答'),
'cup':('M36 32V76Q36 96 64 96T92 76V32','<path d="M30 32H96M92 42H108V72H92M26 108H104M52 18V26M76 18V26"/>','热饮与短句表示温暖励志文体')
})
SPECIAL={'pack.mnist_write':'pen','pack.captcha':'convolution','pack.meme_word':'hearttext','pack.poem_next':'language','pack.headline_swipe':'news','pack.review_swipe':'answer','pack.chinglish':'language','label.raise':'curve','label.coop':'branch','label.brain':'network','label.hard':'branch','label.audit':'clean','label.parallel':'width','label.trapmemory':'atlas','label.captcha':'convolution','label.auto':'automation','spider.acquire':'browser','spider.fast':'optimizer','spider.browse':'browser','spider.autoanswer':'automation','train.checkpoint':'atlas','train.ledger':'math','daily.slang':'debate','backprop':'batch','chainrule':'derivative','daily.care':'calendar','daily.dice':'probability','daily.surf':'browser','books.library':'browser','secret.6':'guide','pack.chess_mate1':'chess','pack.chess_mate2':'chess','label.ai':'answer','persona.daily':'language','persona.xiangsheng':'microphone','persona.zhihu':'answer','persona.qiongyao':'hearttext','persona.luxun':'pen','persona.xinwen':'news','persona.tieba':'debate','persona.jitang':'cup'}

M.update({
'overflow':('M24 98L44 82L60 50L72 24M84 24L104 44M104 24L84 44','<path d="M20 18V108H110M22 30H66M72 62V88M72 98V102"/>','更新幅度过大冲出有效范围，提示裁剪或恢复'),
'setback':('M24 94L44 74L64 48L84 78L104 66','<path d="M20 20V108H110M76 76L86 80L88 68"/>','曲线出现单轮回落，随后继续训练'),
'plateau':('M24 94L44 60L64 44H104','<path d="M20 20V108H110M24 30H108"/>','成绩接近结构天花板，平台段停止上升'),
'emerge':('M24 92H50V64H78V36H104','<path d="M20 100H108M88 20L92 28L104 30L94 38L96 50L86 44L76 50L78 38L70 30L82 28Z"/>','多个能力要求达标后出现新的能力星标'),
'predict':('M20 54H48V74H76V54H108','<rect x="20" y="38" width="24" height="36"/><rect x="52" y="58" width="24" height="36"/><path d="M86 30H110V70H86ZM90 90H106"/>','已有词元连接到待预测的下一个词元'),
'instruction':('M24 34H72V64H104V94H56','<path d="M20 22H80V56H20ZM48 72H108V108H48M62 88L72 98L94 80"/>','指令问题与示范回答配对，学习跟答'),
'feedback':('M24 34H80V68H104V96H48V68H24Z','<path d="M36 46H64M64 84L76 94L94 76"/>','反馈回流强化迎合的回答偏好'),
'review':('M32 28H88V100H32V28M88 52H108V84H88','<path d="M44 44H76M44 60H76M44 76H60M94 76L104 84L112 76"/>','回顾题目记录，再次检验是否学会'),
'power':('M24 94H46L64 64L84 24H106','<path d="M20 20V108H110M34 38L48 24L62 38"/>','指数曲线表示幂与指数的增长'),
'integral':('M92 24Q72 14 68 42L60 88Q56 114 36 104','<path d="M28 96Q50 92 68 70T106 34M40 94V106M56 86V106M72 70V106M88 50V106"/>','曲线下的分区累积表示微积分面积'),
'groupmath':('M24 64H48M36 52V76M80 54L102 76M102 54L80 76','<path d="M20 28Q8 64 20 100M112 28Q124 64 112 100M56 42H72M56 86H72"/>','括号组织加乘等多步混合运算')
})
SPECIAL.update({'lesson.nan':'overflow','quiz.q1d':'overflow','quiz.q3c':'overflow','lesson.regress':'setback','lesson.ceiling':'plateau','lesson.emergence':'emerge','ph.emergence':'emerge','quiz.q6a':'predict','quiz.q6c':'instruction','quiz.q6d':'feedback','data.review':'review','arith.mixed':'groupmath','arith.powers':'power','arith.calculus':'integral','arith.auto':'automation'})

spider='<ellipse cx="64" cy="66" rx="18" ry="22"/><circle cx="64" cy="38" r="10"/><path d="M48 54L28 42L18 24M46 64L24 60L12 48M46 76L24 84L18 104M80 54L100 42L110 24M82 64L104 60L116 48M82 76L104 84L110 104"/>'
M.update({
'spider-home':('M64 28V88M48 66H80',spider+'<path d="M52 104H76"/>','蜘蛛驻留工具入口，等待采集任务'),
'spider-fast':('M16 100L50 90L88 86L114 72',spider+'<path d="M14 90H32M10 100H26M18 110H36"/>','蜘蛛侧面短速度线提示处理加速'),
'spider-page':('M28 22H98V100H78',spider+'<path d="M84 14H114V104H84M84 30H110M86 92L106 74"/>','蜘蛛抓取旁边网页并翻至下一页'),
'spider-answer':('M32 96L50 108L98 90',spider+'<path d="M30 100L46 114L100 92"/>','蜘蛛处理题目后在下方确认作答'),
'slots8':('M26 36H102V64H26V92H102','<path d="M20 22H108V106H20ZM42 22V106M64 22V106M86 22V106M20 64H108"/>','八个装备位置逐格展开'),
'slots12':('M26 36H102V64H26V92H102','<path d="M20 22H108V106H20ZM42 22V106M64 22V106M86 22V106M20 50H108M20 78H108"/>','十二个装备位置逐格展开'),
'ledger':('M34 32H100V52H34V72H100V92H34','<path d="M22 20H108V108H22ZM38 20V108M72 32V96M30 44H98M30 64H98M30 84H98"/>','记账本分列记录收入与耗电'),
'checkpoint':('M28 34H94V94H28Z','<rect x="20" y="22" width="88" height="84" rx="4"/><rect x="38" y="30" width="44" height="24"/><path d="M38 102V70H90V102M50 82L62 94L80 74"/>','保存当前训练快照并可恢复最佳版本')
})
SPECIAL.update({'spider.acquire':'spider-home','spider.fast':'spider-fast','spider.browse':'spider-page','spider.autoanswer':'spider-answer','relic.slots8':'slots8','relic.slots12':'slots12','train.ledger':'ledger','train.checkpoint':'checkpoint'})

M.update({
'bargain':('M24 44L58 24H104V80L72 104L24 58Z','<circle cx="86" cy="40" r="6"/><path d="M40 66H76M64 54L76 66L64 78"/>','价格标签内的下调箭头表示显卡折价'),
'sudoku':('M26 26H102V102H26Z','<path d="M20 20H108V108H20ZM48 20V108M80 20V108M20 48H108M20 80H108M28 28H40V40H28ZM88 56H100V68H88ZM56 88H68V100H56Z"/>','数独宫格以空格与已填位置表示约束'),
'cardsmath':('M24 88L48 40L80 40L104 88','<path d="M16 46L40 32L64 84L40 98ZM40 26H64V88H40ZM68 26H92V88H68ZM88 32L112 46L88 98L64 84M56 54H76M66 44V64"/>','四张扑克牌组合算式，表示凑数题'),
'poetry':('M30 26V98M52 26V82M74 26V98M96 26V62','<path d="M22 18H106V108H22M88 90H104M96 82V98"/>','竖排诗句留出一个待续的字位'),
'proofread':('M24 34H104M24 58H80M24 82H104','<path d="M58 50L76 68M76 50L58 68M48 100L64 90L80 100"/>','标出句中错误并补入修正，表示中式英语改错'),
'monitor':('M24 70H42L52 44L64 88L76 58H104','<rect x="18" y="24" width="92" height="72" rx="4"/><path d="M50 108H78M64 96V108"/>','监控屏显示训练波动与异常风险')
})
SPECIAL.update({'daily.bargain':'bargain','pack.sudoku':'sudoku','pack.make24':'cardsmath','pack.poem_next':'poetry','pack.chinglish':'proofread','train.monitor':'monitor'})

# Semantic accessories differentiate entries sharing a visual grammar.
ACCESSORIES={
'quiz.q1a':'M8 40L18 50L30 30',
'quiz.q2a':'M8 34H30M8 44H30M8 54H30',
'quiz.q1d':'M12 58L30 36M12 58H24M12 58V46',
'quiz.q3c':'M10 38H30M10 54H30M20 32V60',
'quiz.q1c':'M8 36H28V60H8ZM12 42H24M12 50H24',
'quiz.q3d':'M8 36L28 56M28 36L8 56',
'quiz.q3a':'M8 34H28V54H8ZM8 44H28M18 34V54',
'quiz.q4a':'M8 34H30V52H20L12 60V52H8Z',
'daily.news':'M24 12H104M38 8V16M90 8V16',
 'train.data':'M64 12V42M50 28L64 42L78 28',
 'board.logic1':'M78 78H98M88 68V88',
 'gamedata':'M12 12H42V38H12ZM18 20H36M18 28H36',
 'nextmove':'M82 86H110M100 76L110 86L100 96',
 'pack.captcha':'M12 12L20 34L30 14M36 14L30 34H42M10 24L44 20',
 'train.weights':'M10 12H22V24H10ZM26 12H38V24H26ZM10 28H22V40H10ZM26 28H38V40H26M14 16H18M30 16V20M14 32L18 36',
 'pack.mnist_write':'M12 16Q28 4 28 18L12 32H30',
 'persona.luxun':'M12 10H42V38H12ZM18 18H36M18 26H36',
 'pack.meme_word':'M10 10H40V38H10ZM16 20H18M30 20H32M18 28Q24 34 32 28',
 'persona.qiongyao':'M84 94H116V114H104L94 122V114H84Z',
 'daily.slang':'M10 10H34M10 18H28M10 26H34M18 6V32',
 'persona.tieba':'M84 10H116V32H104L94 40V32H84M92 20H108',
 'pack.chess_mate1':'M10 104H30L42 92M36 92H42V98',
 'pack.chess_mate2':'M10 108H28V88H46L54 80M48 80H54V86',
 'label.ai':'M10 10H34V32H10ZM16 20L22 26L30 14',
 'pack.review_swipe':'M10 16H38M16 10L10 16L16 22M32 10L38 16L32 22',
 'persona.zhihu':'M88 106H116M94 100L102 92L110 100M102 92V118',
 'pack.headline_swipe':'M10 112H38M16 106L10 112L16 118M32 106L38 112L32 118',
 'persona.xinwen':'M88 10H116V34H88ZM92 18H112M92 26H108',
 'books.library':'M8 8H18V38H8ZM20 8H30V38H20ZM32 10L42 8L48 36L38 38Z',
 'daily.surf':'M10 20L20 10Q30 8 30 18L22 26M20 22L28 14M24 20Q34 18 34 28L24 38'
}

def family(id):
 if id in SPECIAL:return SPECIAL[id]
 if id in quiz:return family(quiz[id])
 if re.match(r'[sv]\.d',id):return 'depth'
 if re.match(r'[sv]\.w',id):return 'width'
 if id in ['lenet','alexnet','vgg','googlenet','textcnn','augment','caption','train.weights']:return 'convolution'
 if id in ['rnn','lstm','gru','irnn','ph.amnesia','ph.forget']:return 'recurrent'
 if id in ['attention','transformer','multihead','position','ph.emergence','lesson.emergence']:return 'attention'
 if id in ['seq2seq','wordvec','subword','sft','cot','ph.translation','ab.4','ab.5']:return 'language'
 if id in ['momentum','adam','rmsprop','shared.lr','lrschedule','warmup','gradclip','lesson.small_step','lesson.nan']:return 'optimizer'
 if id in ['relu','sigmoid','step','bias']:return 'activation'
 if id in ['dataclean','data.review','ph.inbreeding','lesson.noise']:return 'clean'
 if id in ['ph.vanish','lesson.vanishing','ph.deeper']:return 'fade'
 if id in ['ph.memorize','lesson.overfit','train.holdout','dropout','ph.shortcut']:return 'gap'
 if id in ['ph.bottleneck','ph.serial','lesson.ceiling']:return 'bottleneck'
 if id in ['ph.xor','ab.1','ab.2','ph.hallucinate']:return 'branch'
 if id in ['ph.grok','lesson.grokking','lesson.regress','train.curve','train.monitor','earlystop']:return 'curve'
 if id.startswith('board.') or id in ['nextmove','go.pack','gamedata'] or 'chess' in id or 'sudoku' in id:return 'board'
 if id.startswith('persona.') and id!='persona.archive':return 'language'
 if id.startswith('persona.') or id in ['ph.drift','ph.sycophancy','daily.care']:return 'persona'
 if id.startswith('arith.') or 'make24' in id or id=='arith.pack':return 'math'
 if id=='daily.rhythm':return 'audio'
 if id.startswith(('label.','spider.')) or id=='arith.auto':return 'automation'
 if id.endswith('.pack') or id.startswith('pack.') or id in ['train.data','daily.news','daily.surf']:return 'data'
 if id.startswith(('ph.','lesson.','ab.')) or id in ['books.library','secret.6']:return 'atlas'
 if id in ['perceptron','mlp','weights','learnrule','features','resnet','residual','transfer','brain.window','chainrule','backprop']:return 'network'
 return 'tool'

(OUT/'icons').mkdir(exist_ok=True);(OUT/'overlays').mkdir(exist_ok=True)
rows=[]
report_nodes=json.loads(REPORT.read_text(encoding='utf-8-sig'))['nodes']
persona_path=ROOT/'Assets/LingGuangV05/Resources/LingGuangV05/YY/persona_packs.json'
persona_names={p['id']:p['name'] for p in json.loads(persona_path.read_text(encoding='utf-8-sig'))['packs']}
for key,value in persona_names.items():names['persona.'+key]=value
report_nodes += [dict(id='persona.'+k,entry='persona.'+k,dynamic=True) for k in persona_names]
for node in report_nodes:
 ident=node['id'];stem=ident.replace('.','_');fam=family(ident);route,glyph,meaning=M[fam]
 if ident.startswith('quiz.'):
  glyph+='<path d="M92 12H118V34H108L100 42V34H92ZM100 20Q104 14 110 20Q112 24 106 26M106 30V31"/>'
  trigger=quiz_triggers.get(ident,'')
  marker={'stage_enter':'M8 12H24L20 8M24 12L20 16','gap':'M8 12L18 8L26 10M8 14L18 20L26 22','nan':'M8 8L22 22M22 8L8 22','plateau':'M8 22L16 12H26','deep_flat':'M8 8H26M8 14H22M8 20H18','shift':'M8 12H24M20 8L24 12L20 16','long_fail':'M8 10H26M8 16H20M8 22H12','deep_worse':'M8 8L16 14L26 24','translate_fail':'M8 8H16V24M20 8H28V24','serial':'M8 8V14H16V20H24','noise':'M8 20L12 8L18 22L24 12'}.get(trigger,'M8 10H24V24H8Z')
  glyph+='<path d="'+marker+'"/>'
 if ident.startswith('lesson.'):
  glyph+='<path d="M92 14Q100 10 106 16Q112 10 120 14V34Q112 30 106 36Q100 30 92 34ZM106 16V36"/>'
 if ident in ACCESSORIES:glyph+='<path d="'+ACCESSORIES[ident]+'"/>'
 name=names.get(ident)
 if not name and re.match(r'[sv]\.[dw]',ident):name=('视觉' if ident[0]=='v' else '序列')+('网络深度' if ident[2]=='d' else '网络宽度')+' · '+ident[3:]
 if not name and ident.endswith('.pack'):name=names.get(ident[:-5],ident[:-5])+'数据包'
 if not name:name={'persona.archive':'性格档案','pack.sudoku':'数独','pack.make24':'凑二十四','pack.headline_swipe':'标题判断','pack.mnist_write':'手写数字','pack.chess_mate1':'国际象棋一步杀','pack.captcha':'验证码','pack.meme_word':'表情配词','pack.poem_next':'唐诗接字','pack.chess_mate2':'国际象棋两步杀','pack.review_swipe':'评论判断','pack.chinglish':'中式英语'}.get(ident,ident)
 source=SOURCE/(node['entry'].replace('.','_')+'.png');base='icons/'+node['entry'].replace('.','_')+('.png' if source.exists() else '.svg')
 if source.exists():shutil.copy2(source,OUT/base)
 else:
  (OUT/base).write_text(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128"><title>{html.escape(name)}</title><g fill="none" stroke="#6b9899" stroke-width="3" stroke-linecap="round" stroke-linejoin="round">{glyph}<path d="{route}"/></g></svg>',encoding='utf-8')
 overlay='overlays/'+stem+'.svg'
 svg=f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" aria-hidden="true"><g fill="none" stroke="#8eddd0" stroke-linecap="round" stroke-linejoin="round"><path class="signal halo" d="{route}" pathLength="100" stroke-width="5" opacity=".12"/><path class="signal core" d="{route}" pathLength="100" stroke-width="2"/></g></svg>'
 (OUT/overlay).write_text(svg,encoding='utf-8')
 rows.append(dict(id=ident,entry=node['entry'],name=name,base=base,overlay=overlay,family=fam,animation=meaning+'；仅图标内信号移动，悬停或聚焦启动，移开复位。',phasePath=route,activeMs=1000,restMs=1600,catalogSource='YY/persona_packs.json (dynamic)' if node.get('dynamic') else 'TechnologyCatalogCoverage/report.json',sourceType='project-original-png' if source.exists() else 'authored-semantic-svg',baseLookup='entry',sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest() if source.exists() else None))
manifest=dict(schemaVersion=1,authority=str(REPORT.relative_to(ROOT)).replace('\\','/'),nodeCount=len(rows),entryCount=len(set(x['entry'] for x in rows)),nodes=rows)
(OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
# Inline overlays keep the preview usable via file:// and allow host CSS to own animation.
overlays={r['id']:(OUT/r['overlay']).read_text(encoding='utf-8') for r in rows}
page=(OUT/'template.html').read_text(encoding='utf-8').replace('__MANIFEST__',json.dumps(manifest,ensure_ascii=False)).replace('__OVERLAYS__',json.dumps(overlays,ensure_ascii=False))
(OUT/'index.html').write_text(page,encoding='utf-8')
assert len(rows)==len({r['id'] for r in rows})==211
for r in rows:
 assert (OUT/r['base']).exists() and (OUT/r['overlay']).exists()
 ET.parse(OUT/r['overlay'])
 if r['base'].endswith('.svg'):ET.parse(OUT/r['base'])
 else:assert hashlib.sha256((OUT/r['base']).read_bytes()).hexdigest()==r['sourceSha256']
used={r['base'] for r in rows}|{r['overlay'] for r in rows}
for folder in ['icons','overlays']:
 for stale in (OUT/folder).iterdir():
  if stale.suffix in ['.svg','.png'] and str(stale.relative_to(OUT)).replace('\\','/') not in used:stale.unlink()
assert 'fetch(' not in page and '<filter' not in page
geometry_entries={}
for r in rows:
 if r['base'].endswith('.svg'):
  geometry=re.sub(r'<title>.*?</title>','',(OUT/r['base']).read_text(encoding='utf-8'))
  geometry_entries.setdefault(geometry,set()).add(r['entry'])
assert all(len(entries)==1 for entries in geometry_entries.values()), 'Different entries share identical new base geometry'
summary=dict(duplicateNewBaseGeometryGroups=0,nodes=len(rows),entries=manifest['entryCount'],originalPng=sum(r['sourceType']=='project-original-png' for r in rows),authoredSvg=sum(r['sourceType']=='authored-semantic-svg' for r in rows),families={f:sum(r['family']==f for r in rows) for f in sorted(M) if any(r['family']==f for r in rows)},unresolvedNames=[r['id'] for r in rows if r['name']==r['id']],checks=['211 unique IDs and references present (203 report + 8 dynamic persona)','SVG XML parses','No identical new base geometry across different entries (title excluded)','Original PNG SHA256 unchanged','No fetch or SVG blur filters','Only Design/ui/tech-icon-atlas written'])
(OUT/'validation.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(summary,ensure_ascii=False,indent=2))
