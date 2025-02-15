//VAR has_A = false
VAR has_B = false
VAR has_C = false

Hello there, this is a test. #speaker:NPC#portrait:dr_green_neutral#layout:left
I am a NPC.
Hi, I am Player. Nice to see you. #speaker:Player#portrait:ms_yellow_neutral#layout:right
There is a question. Plese choose your answer. #speaker:NPC#portrait:dr_green_neutral#layout:left
*[A]#speaker:Player#portrait:ms_yellow_neutral#layout:right
Is A.
->After_Questions
*{has_B}[B] #speaker:Player#portrait:ms_yellow_happy#layout:right
Is B.
->After_Questions
*{has_C}It is [C], so it is C.#speaker:Player#portrait:ms_yellow_sad#layout:right
->After_Questions

===After_Questions===
Finished the choose.#speaker:NPC#portrait:dr_green_neutral#layout:left
-> END