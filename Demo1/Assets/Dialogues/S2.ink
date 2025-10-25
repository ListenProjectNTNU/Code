// 第二場景：焦慮的化身
// ====================================
// ① 遭遇怪物
// ====================================

主角：「！！！！！！」 
#speaker:主角
#portrait:default
#layout:left
// （眼前的學姊變成了一個怪物。）

（怪物嘴裡發出細小的呢喃聲，令人不適。） 
#speaker:主角
#portrait:default
#layout:left
// 音效：怪物低語
#scene:monster_whisper

（隨著怪物逼近，主角開始越來越慌亂，突然）
#speaker:主角
#portrait:default
#layout:left
// 音效變大聲；怪物向前移動指定距離；主角後退指定距離
#scene:monster_approach

// ====================================
// ② 耳機介入
// ====================================

（耳機發出聲音）
#speaker:主角
#portrait:default
#layout:left
// 恐怖音效響起；音效與角色都停下；耳機小精靈聲出現
#scene:stop_all_for_headphone

耳機：「按下 'Q 或 C 或 R' 攻擊」
#speaker:耳機
#portrait:default
#layout:right
#scene:start_battle
// 怪物與主角解除暫停，讓玩家可操作（戰鬥開始）

// ====================================
// （此區為戰鬥階段）
// 玩家操控主角擊殺怪物後，會自動觸發下段對話
// ====================================

#scene:end_battle
（怪物消失後，掉落了一個微微發出聲音的物體，聽起來像學姊的聲音。）
#speaker:主角
#portrait:default
#layout:left
#scene:item_drop

// ====================================
// ③ 耳機說明階段
// ====================================

耳機：「你剛剛擊倒的是你心中的焦慮，這裡是你的心理世界」
#speaker:耳機
#portrait:default
#layout:right

耳機：「但因為你的焦慮還沒完全消除，所以你必須在擊倒更多焦慮，才有機會回到現實世界」
#speaker:耳機
#portrait:default
#layout:right

耳機：「怪物掉落的是『對話碎片』，在你在心理世界時現實世界還是在流動，對話碎片是你暫時擊敗焦慮時聽到的現實世界的聲音，也是你成長的證明，可以讓你在心理世界的能力提升」
#speaker:耳機
#portrait:default
#layout:right

耳機：「靠近後按下 E 可以拾取」
#speaker:耳機
#portrait:default
#layout:right
#scene:enable_pickup

耳機：「喔對了～忘記自我介紹。我是你的守護靈，我會在你在心理世界時提供你協助」
#speaker:耳機
#portrait:default
#layout:right

耳機：「那現在我們一起想辦法回到現實世界吧」
#speaker:耳機
#portrait:default
#layout:right
#scene:end_tutorial
// 結束對話狀態，玩家恢復可控制角色

// ====================================
// ④ 前往傳送門
// ====================================

#scene:reach_final_gate_trigger
耳機：「太好了，你已經擊敗你心中大多的焦慮了，但是還不夠徹底」
#speaker:耳機
#portrait:default
#layout:right

耳機：「繼續往前面探索吧，耳機。」
#speaker:耳機
#portrait:default
#layout:right
#scene:enable_portal
// 結束對話，玩家可操作角色走到門前觸發轉場（進入第三場景）

#scene:end_scene2
