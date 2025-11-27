// ========== Scene 3 ==========
// 一進場就是對話

=== battle_before ===
// 對話模式 (boss 不啟用)
「……聽得見嗎？這裡的反應很強，應該是……核心。」#speaker:耳機#portrait:default#layout:left##scene:enter_scene3

// Boss 啟用，但在對話模式結束才可正常移動
「小心，那是……！」#speaker:耳機#portrait:default#layout:left#scene:appear_boss

// 播放怪物音效，對話結束後關掉音效，並啟用 boss 的 AI 讓 Boss 自由移動
「逃不了的。你只是把自己關得更深。」#speaker:怪物#portrait:default#layout:left

// ==================
// 遊戲時間：戰鬥結束後地圖中心出現傳送門，並且進入對話
// ==================
-> END

=== battle_after ===
// 播放耳機聲
「很好……你可以回來了。」#speaker:耳機#portrait:default#layout:left#scene:stop_all_for_headphone

「現在你心中的焦慮暫時都被消除了，可以返回現實世界了。」#speaker:耳機#portrait:default#layout:left

// ==================
// 遊戲模式：讓玩家自己走進傳送門，接到第四幕
// ==================
-> END


