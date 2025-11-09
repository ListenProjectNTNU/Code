//動畫 走在沒有開門的走廊png loop裡
按下空白鍵下一句#speaker:提示#scene:start

（估計大學也跟高中一樣吧？只要繼續當個邊緣人就不會受到傷害。）#speaker:家豪#portrait:default#layout:right

（不想參加新生訓練的家豪，被媽媽強行拎到大學門口，邊說著：「大學是新的開始，要體驗人生！」 ）#speaker:家豪#portrait:default#layout:left
//動畫 打破沒有開門的走廊png loop 下一張走廊png有開門

（雖心有不滿，但也不想忤逆媽媽，戴上耳機一邊平靜自己的心情一邊前往教室。）#speaker:家豪#portrait:default#layout:left#scene:corridor_withDoor
//動畫 走到有開門的地方
（走到教室門口還沒平復好心情，突然，後面傳來一道聲音—— ）#speaker:家豪#portrait:default#layout:left

// 動畫 畫面快速閃白後回到正盛時學姊出現
「嘿！是新生嗎？怎麼不進去？」 #speaker:學姊#portrait:default#layout:left

（轉過頭，一位笑得燦爛的學姊正站在他身後） #speaker:家豪#portrait:default#layout:left#scene:fox_appear

「我……我……這就進去！」#speaker:家豪#portrait:default#layout:right#scene:player_turnBack

//動畫：畫面切到滿人教室PNG  音效：人群吵雜聲
（看向教室內，教室內，每張桌子都坐滿了人，大家有說有笑地聊著天。主角感到無地自容，視線開始因為緊張而模糊。） #speaker:家豪#portrait:default#layout:left#scene:ClassRoom_Start

//動畫閃白後回到在走廊的場景，並且身後出現學姊(啟動FOX物件) 關掉剛剛的音效
「嘿！剛剛的新生！」（突然，學姊的聲音再次響起—— ）#speaker:學姊#portrait:default#layout:left#scene:ClassRoom_End

//動畫閃紅
（對上學姊燦爛的笑容，臉上的血液再次沸騰） #speaker:家豪#portrait:default#layout:left

「我剛剛對了名單，你是最後一個到的人，我是你新訓期間的隊輔。」 #speaker:學姊#portrait:default#layout:left

//動畫忽明忽暗+雜訊 音效：耳鳴
（聽著學姊的話語，視線越來越模糊，開始忽明忽暗——） #speaker:家豪#portrait:default#layout:left#scene:fade_out

//切換到第二場景 關掉耳鳴
（再度睜眼時，主角已經不在教室內，眼前的學姊也變成了不可名狀的生物……） #speaker:家豪#portrait:default#layout:left