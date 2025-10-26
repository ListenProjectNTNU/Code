// ====================================
// knot 1: 戰鬥前對話
// ====================================

=== battle_before ===
「！！！！！！」 #speaker:家豪#portrait:default#layout:left

（怪物嘴裡發出細小的呢喃聲，令人不適。） #speaker:家豪#portrait:default#layout:left#scene:monster_whisper

（隨著怪物逼近，主角開始越來越慌亂，突然）#speaker:家豪#portrait:default#layout:left#scene:monster_approach

（耳機發出聲音）#speaker:耳機#portrait:default#layout:left#scene:stop_all_for_headphone

「按下 'Q 或 C 或 R' 攻擊，WASD移動」#speaker:耳機#portrait:default#layout:right#scene:start_battle

// === 戰鬥階段開始 ===
// 此處 Unity 控制遊戲進入戰鬥模式，Ink 暫停
-> END



// ====================================
// knot 2: 戰鬥後對話
// ====================================

=== battle_after ===
（怪物消失後，掉落了一個微微發出聲音的物體，聽起來像學姊的聲音。）#speaker:家豪#portrait:default#layout:left#scene:item_drop

「你剛剛擊倒的是你心中的焦慮，這裡是你的心理世界」#speaker:耳機#portrait:default#layout:right

「但因為你的焦慮還沒完全消除，所以你必須在擊倒更多焦慮，才有機會回到現實世界」#speaker:耳機#portrait:default#layout:right

「怪物掉落的是『對話碎片』，在你在心理世界時現實世界還是在流動」#speaker:耳機#portrait:default#layout:right

「對話碎片是你暫時擊敗焦慮時聽到的現實世界的聲音，也是你成長的證明，可以讓你在心理世界的能力提升」#speaker:耳機#portrait:default#layout:right

「靠近後按下 E 可以拾取」#speaker:耳機#portrait:default#layout:right

「喔對了～忘記自我介紹。我是你的守護靈，我會在你在心理世界時提供你協助」#speaker:耳機#portrait:default#layout:right

「那現在我們一起想辦法回到現實世界吧」#speaker:耳機#portrait:default#layout:right#scene:end_tutorial

// 此處玩家重新可操作角色
-> END



// ====================================
// knot 3: 傳送門前對話
// ====================================

=== before_portal ===
「太好了，你已經擊敗你心中大多的焦慮了，但是還不夠徹底」#speaker:耳機#portrait:default#layout:right

「繼續往前面探索吧！加油！。」#speaker:耳機#portrait:default#layout:right#scene:enable_portal

// 玩家可操作，觸發進入下一場景
-> END

#scene:end_scene2
