#include "timer.h"

static volatile uint32_t sys_tick = 0;

// TIM2 ??????
void TIM2_IRQHandler(void)
{
    if (TIM_GetITStatus(TIM2, TIM_IT_Update) != RESET)
    {
        sys_tick++;
        TIM_ClearITPendingBit(TIM2, TIM_IT_Update);
    }
}

// ??????(1ms ??)
void Timer_Init(void)
{
    TIM_TimeBaseInitTypeDef TIM_TimeBaseStructure;   // ????????
    
    // ?? TIM2 ??
    RCC_APB1PeriphClockCmd(RCC_APB1Periph_TIM2, ENABLE);
    
    // ?? TIM2 ? 1ms ?? (72MHz / 72 = 1MHz, ?? 1000 = 1ms)
    TIM_TimeBaseStructure.TIM_Period = 1000 - 1;
    TIM_TimeBaseStructure.TIM_Prescaler = 72 - 1;
    TIM_TimeBaseStructure.TIM_ClockDivision = TIM_CKD_DIV1;
    TIM_TimeBaseStructure.TIM_CounterMode = TIM_CounterMode_Up;
    TIM_TimeBaseInit(TIM2, &TIM_TimeBaseStructure);
    
    // ????????,????
    TIM_ClearFlag(TIM2, TIM_FLAG_Update);
    TIM_ITConfig(TIM2, TIM_IT_Update, ENABLE);
    
    // ???????
    NVIC_SetPriority(TIM2_IRQn, 1);
    NVIC_EnableIRQ(TIM2_IRQn);
    
    // ?????
    TIM_Cmd(TIM2, ENABLE);
}

// ??????(??)
uint32_t GetTick(void)
{
    return sys_tick;
}