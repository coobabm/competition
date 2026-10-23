# E-C 文本清单

正文来源：终局网络讨论_来源与对白清单_20261019.md §4–6。三个稳定 ID，每封9个双语段落（问候、7段正文、署名），全文逐段原样提取。

- `zhou_open_door` / `online`
- `zhou_copies` / `public_model`
- `zhou_unpressed_button` / `local_only`

资源的 `source=public_article` 表示游戏原创公开文章，不是私人寄送、现实人物原文或公开历史文献。三个 LP 可选插段原样存档；默认不显示，未提供经验证事实投影时不可插入。

## 原清单 UI 文案

- UI-SAVE：保存本篇到游戏邮箱 / Save this letter to the game inbox
- UI-SAVE-NOTE：只保存公开文章的本地副本。不发送邮箱地址，也不会告诉作者你选择了什么。 / This saves a local copy of a public article. It sends no email address and does not tell the author what you chose.
- UI-SENDER：老周 · 公开来信（本地保存） / Lao Zhou · Public letter (saved locally)
- UI-CONTINUE：继续尾声：保存来信并前往邮箱 / Continue the epilogue: save the letter and open the inbox
- UI-WARN：终局来信：双击可先阅读。读完后点击“读完，保存并返回标题”才会通关；中途可返回桌面。 / Ending letter: double-click to read first. This ending completes only when you choose “Finish reading, save and return to title.” You may return to the desktop while reading.
- UI-CONFIRM：打开终局来信？读完并确认后，本结局会保存为已通关，并返回标题。现在只打开信件；你仍可中途退出。 / Open the ending letter? Once you finish and confirm, this ending will be saved as completed and the game will return to the title screen. This action only opens the letter; you may leave while reading.
- UI-READ：先读信 / Read first
- UI-LATER：暂不打开 / Not now
- UI-FINISH：读完，保存并返回标题 / Finish reading, save and return to title
- UI-BACK：先回桌面，稍后继续 / Back to desktop; continue later
- UI-FAIL：没有保存成功，尚未通关。请重试。 / Saving failed. This ending is not complete yet. Please retry.
- UI-REPLAY：返回结局档案 / Return to endings
- UI-HISTORY：往期讨论 · 未阅读不代表错过任务 / Past discussions · Unread posts are not missed tasks
- UI-FICTION：游戏内报道 · 人物与事件为虚构 / In-game report · Fictional people and events
- UI-PRIVATE：本机备忘 · 仅自己可见 / Local note · Only visible to you

## 本包新增控件及状态文案（完整列举）

- UI-OPEN：打开来信 / Open letter
- UI-PUBLIC：公开文章 · 游戏原创 / Public article · Original game writing
- UI-SAVED：老周的公开来信已保存到游戏邮箱。 / Lao Zhou’s public letter has been saved to the game inbox.
- UI-UNREAD：未打开 / Unopened
- UI-UNFINISHED：未读完 / Unfinished
- UI-ARCHIVE：只读回看 / Read-only archive
- UI-SAVING：正在保存…… / Saving…
- UI-SAVE-FAIL：未能保存来信，请重试。尚未通关。 / The letter was not saved. Please retry. This ending is not complete yet.
- UI-OPEN-FAIL：未能保存阅读状态，请重试。尚未通关。 / Reading progress could not be saved. Please retry. This ending is not complete yet.
- UI-UNAVAILABLE：当前无法完成本结局，可先阅读或返回桌面。 / This ending cannot be completed yet. You may read or return to the desktop.
- UI-COMPLETED：已保存完成记录。 / Completion record saved.
- UI-SMALLER：小字 / Smaller text
- UI-LARGER：大字 / Larger text
- UI-ARTICLE-SOURCE：公开文章，由你保存到游戏邮箱。 / A public article, saved by you to the game inbox.
- UI-OPEN-TITLE：打开终局来信？ / Open the ending letter?

WORK-MAIL 采用来源§5给定全文，最小补丁在原毕业邮件槽原位替换；未新增人物身份或现实发送行为。
退稿来源标签新增：虚构投稿系统 / Fictional submission system；正文不重写。

## 三封公开信全文（与资源逐段相同）

### zhou_open_door · 开着的门 / The Door Left Open

**greeting**

读到这里的朋友：

To anyone reading this,

**p01**

今天又有一篇新论文，大家忙着争论，它会把什么变快，什么变便宜。我想起的却是另一件小事：一个工具第一次被陌生人用上以后，它的作者就不再是唯一能够说“差不多了”的人。

There is another new paper today. People are debating what it might make faster or cheaper. I keep thinking of something smaller: once a stranger starts using a tool, its author is no longer the only person entitled to say, “That will do.”

**p02**

做演示的时候，我们往往挑自己熟悉的问题。门一打开，进来的却是各人的日子：一份赶着交的说明，一句怕译错的话，一个不想再重复敲的表格。那一点省下来的工夫是真的。不能因为还有风险，就装作它对别人毫无用处。

For demonstrations, we tend to choose familiar questions. Once the door opens, what comes in is other people's everyday life: a description due soon, a sentence they are afraid of mistranslating, a form they do not want to type again. The time saved is real. The existence of risks should not make us pretend the tool is of no use to anyone.

**p03**

可是，答得顺的那句话也可能被信了，来不及核对的人也可能把它交出去。作者觉得只是一个小错，落到另一个人的事情里，未必还是小错。开一扇门，不能只算有多少人进来，还得想，有人拿着错误回来时，门后面有没有人。

But a fluent sentence may also be believed. Someone without time to check may pass it on. What seems a small mistake to its author may not be small in another person's situation. Opening a door is not only about counting who comes in. It is also about whether someone will be there when a user returns with an error.

**p04**

我越来越在意那些不好看的地方：一页没修完的已知问题，一条解释为什么暂时不回答的说明，一个真的有人看的更正入口。它们不如漂亮的演示容易传播，却让“我们会继续改”这句话有了落脚的地方。开关也好，限制也好，都不是写下就会自动尽责的东西，总有人得在合适的时候用它。

I find myself paying more attention to the untidy parts: an unfinished list of known problems, an explanation of why a request cannot be answered yet, a correction channel that somebody actually reads. They travel less easily than a polished demonstration, but they give “we will keep improving it” something solid to stand on. A switch or a limit does not fulfil its purpose merely by existing. Someone must still use it at the right time.

**p05**

如果一个工具要持续对外服务，作者就得容许别人提出自己没准备好听的问题。也得容许自己承认：这件事现在还做不好。把某一项停下来修，不必等于否定从前做过的一切；让它一直开着，也不自动等于勇敢。

If a tool is to remain available, its author must allow other people to ask questions they may not be ready to hear. They must also be able to admit that something does not work well enough yet. Pausing one feature for repairs need not erase everything that came before. Keeping it running does not automatically make the decision brave.

**p06**

今天的论文不会替任何人完成这些工作。它只把一些可能性又往前推了一点。以后仍会有普通的夜晚，消息一条条过来，维护的人想睡觉，使用的人想把事情做完。愿开着门的人记得，门外站着的不是一串访问量，而是各自要过日子的人。

Today's paper will not do this work for anyone. It moves some possibilities a little further forward. There will still be ordinary nights: messages arriving, a maintainer wanting sleep, users wanting to finish what they started. I hope those who keep a door open remember that the people outside it are more than a visitor count. They have lives to get on with.

**p07**

写到这里，我去把自己拖了几天的一条更正补上。再大的话，最后也得落回这一点。

Having written this, I am going to finish a correction I have been putting off for days. Even the largest statements eventually come back to something that small.

**signature**

老周

Lao Zhou

### zhou_copies · 离开手的副本 / A Copy Beyond Your Hands

**greeting**

读到这里的朋友：

To anyone reading this,

**p01**

我见过有人在文件夹里把一个名字改成“最终版”，过几天又在后面添一个“新”。文件还在自己手上时，这算个笑话。副本到了别人那里，名字就不只是自己的备忘了，它可能是别人判断能不能信的全部线索。

I have seen people name a file “final,” then add “new” a few days later. While the file stays on one computer, that is a small joke. Once copies reach other people, the name is no longer merely a private reminder. It may be their only clue about whether to trust what they have.

**p02**

把东西公开有一种很实在的好处：别人可以看，可以试，可以指出作者一个人没见过的问题。原来只能站在门外等的人，有机会动手改。有人补一份说明，有人找到一个错处，有人把它带到完全不同的用途里。这些事不该被轻轻一句“不可控”全部抹掉。

Making something public has a practical value. Other people can inspect it, try it, and identify problems its author never encountered alone. People who could only wait outside gain a chance to work on it themselves. One writes better instructions, another finds an error, and someone else uses it for something entirely different. Calling all of that “uncontrollable” would leave out much that matters.

**p03**

但副本不会因为作者改变了想法，就自己走回来。有人会认真留下来源和修改记录，也可能有人只转发一个文件。后来出了问题，下载的人未必知道自己拿的是哪一版。原作者既不能替所有后来的决定负责，也不能假装第一次交出去时，说明写成什么样毫无关系。

But copies do not return on their own when an author changes their mind. Some people will preserve the source and change history carefully. Others may pass along only a file. If trouble follows, a downloader may not know which version they have. The original author cannot be responsible for every later decision. Nor can they pretend that the instructions accompanying the first release made no difference.

**p04**

所以我想劝愿意公开的人，除了文件，也把边界写出来：它从哪里来，在哪些地方试过，哪些事情没有把握，别人可以怎样使用和修改，问题应该到哪里记录。说明不可能拦住每一次误用，但它能让愿意认真做事的人少猜一点。许可也不是装饰；说“大家都可以用”之前，要先弄清楚自己到底有权交出什么。

So I would ask anyone preparing a public release to publish its boundaries along with the files: where it came from, what has been tested, where uncertainty remains, how others may use and modify it, and where problems can be recorded. Instructions cannot prevent every misuse. They can spare careful people some guesswork. A licence is not decoration either. Before saying “anyone may use this,” establish what you actually have the right to give away.

**p05**

更难的一点也许是承认，别人真会把它改成作者不喜欢的样子。分享若只允许赞同，就仍然像一种批准。可放下独占，也不等于放下判断。可以说明不同意，可以维护自己的版本，可以把来路讲清楚，而不必声称所有副本都仍听自己指挥。

Perhaps the harder part is accepting that someone really will change it in ways its author dislikes. Sharing that permits only agreement still resembles a permission slip. Yet giving up exclusive control does not mean giving up judgment. An author can disagree, maintain their own version, and clarify its history without claiming that every copy remains under their command.

**p06**

今天有新的研究公开，明天会有人试着复现。我喜欢这种你给我一段台阶、我再补上一段的事情。只愿每一段台阶旁边，也有人留下哪里还不稳的记号。

New research becomes public today; tomorrow someone will try to reproduce it. I like the way one person builds a step and another adds the next. I only hope someone also leaves a mark beside the places that are not yet secure.

**p07**

东西离开手以后，作者的工作没有全结束，只是其中一部分开始由别人接着写了。能看见这一点，大概才真正开始理解“公开”两个字的分量。

Once something leaves its author's hands, the work has not entirely ended. Part of it has begun to be written by others. Recognising that may be where understanding the weight of a public release begins.

**signature**

老周

Lao Zhou

### zhou_unpressed_button · 还没有按下的按钮 / The Button Not Yet Pressed

**greeting**

读到这里的朋友：

To anyone reading this,

**p01**

新东西一出来，最响亮的声音常常是“赶紧”。赶紧试，赶紧发，赶紧把位置占下来。还有一种声音没那么容易写成新闻：我想再看一看。说这句话的人，未必是不相信自己做的东西，也可能正因为知道一点它的毛病，才没法痛快地点头。

When something new appears, the loudest word is often “hurry.” Hurry to try it, release it, and claim a place. Another sentence is less likely to become a headline: “I want to look a little longer.” Someone who says it may still believe in what they have made. Knowing some of its flaws can be precisely what makes an easy yes impossible.

**p02**

不发布不会让所有难题消失。世界不会因为一个人的犹豫停下，别的人仍会试，仍会做，仍会在今天这样的早晨放出新的论文。等着用工具的人，也不会因为作者谨慎，就自动少一份着急。所以等待不能只剩下一个姿态，最好能说清楚，还在等什么。

Not releasing does not make every difficult question disappear. The world will not pause for one person's hesitation. Other people will keep experimenting, building, and publishing papers on mornings like this. Those waiting for a useful tool will not automatically feel less urgency because its author is cautious. Waiting should be more than a posture. It helps to name what is still being waited for.

**p03**

也许是一个老问题换种问法就答错，也许是说得很肯定却找不到根据，也许是还没有安排好出错以后怎样纠正。把这些问题写下来，给它们留一段真正能工作的时间，和把文件关上、盼着以后自己变好，是不同的事。继续观察也该有事情可做，有可以重新判断的依据。

Perhaps an old problem produces a wrong answer when rephrased. Perhaps an answer sounds certain but has no support. Perhaps there is still no clear way to correct mistakes once they reach someone else. Writing those problems down and setting aside time to work on them is different from closing a folder and hoping it will improve by itself. Continued observation should involve work and evidence on which a later judgment can rest.

**p04**

我不想把迟一点说成天然高尚。没有人能保证拖到某一天，就终于不会伤到谁。同样，我也不想把迟一点说成胆小。别人等着看热闹的时候，承认自己还没准备好，有时比交出一个漂亮的演示更费力。

I do not want to call delay inherently noble. Nobody can promise that waiting until a particular day will remove every chance of harm. Nor do I want to call it cowardice. When others are eager for a spectacle, admitting that something is not ready can take more effort than presenting a polished demonstration.

**p05**

有些东西只在小范围里陪人做一件事，也有它的分量。它不必因为没有站到热闹的地方，就算白做了。但若某天准备重新开门，也别只因为周围的声音更大了。看看当初留下的问题，是否真的少了几项，剩下的是否能诚实地告诉使用它的人。

Something that helps a small number of people with one task can still matter. Remaining outside the spotlight does not make the work worthless. But if the door is opened later, let it be for more than the growing noise outside. Look back at the original questions. Have some actually been resolved? Can the remaining ones be explained honestly to the people who will use it?

**p06**

这封信不能替谁判断哪一天合适。今天的论文也不能。它们至多让人多一个角度，多一个可以继续试的办法。决定还在做事的人手里，日子也还得接着过。

This letter cannot choose the right day for anyone. Neither can today's paper. At most, they offer another perspective or another method to try. The decision remains with the people doing the work, and ordinary life continues alongside it.

**p07**

我写完时，窗外已经有人赶早班。电脑可以先留在那里，人不必每一晚都陪它亮到天明。明天还想得动，也是继续把事情做好的一个条件。

As I finish, people outside are leaving for the early shift. The computer can stay where it is. A person does not have to keep it company until dawn every night. Being able to think again tomorrow is also part of doing the work well.

**signature**

老周

Lao Zhou

## 可选证据插段全文（默认不显示）

**LP1 / disclosed_online**

你在消息里说，准备让别人使用那个服务。我读到了。上面谈维护的几段，可以当成我对这件事的回答；具体怎么做，仍要看你准备公开的说明和实际测试。

I read your message saying you plan to let others use the service. The passages about maintenance are my response to that. The practical details still depend on the notes you publish and the tests you have actually done.

**LP2 / disclosed_public_package**

你说准备把模型包公开，我读到了。若你愿意给出说明页，我可以就那页写明的内容提意见。页面以外的事，我不会假装已经了解。

I read that you plan to make the model package public. If you choose to share its documentation page, I can comment on what it actually says. I will not pretend to know what lies beyond it.

**LP4 / disclosed_delay**

你说想再等一等，我记下了。我不替这个决定打分；如果以后愿意谈，可以从你还没弄清的一个问题开始。

I read that you want to wait a little longer. I won't grade that decision. If you want to talk later, we can begin with one question you still haven't resolved.

## 工作背景邮件完整替换稿

**WORK-MAIL** 发件人“行政同事 / Office administration”；日期保留5月22日（2016）；主题“本周值班表核对 / Please check this week's duty roster”。

中文：各位同事：\n\n本周值班表已放在共享文件夹。请核对自己的班次；如需调整，请先和换班同事确认，再回复行政登记。\n\n离开工位前请检查电源和门窗。\n\n行政

English: Colleagues,\n\nThis week's duty roster is in the shared folder. Please check your shifts. If you need a change, agree with the colleague swapping with you and reply so administration can update the record.\n\nPlease check power switches and windows before leaving your desk.\n\nOffice administration

