#include "delay.h"

void Delay_us(uint32_t us)
{
    uint32_t ticks = us * 72;   
    SysTick->LOAD = ticks - 1;
    SysTick->VAL = 0;
    SysTick->CTRL = 0x05;       
    while ((SysTick->CTRL & 0x00010000) == 0);
    SysTick->CTRL = 0x04;       
}

void Delay_ms(uint32_t ms)
{
    while (ms--) Delay_us(1000);
}

void Delay_s(uint32_t s)
{
    while (s--) Delay_ms(1000);
}