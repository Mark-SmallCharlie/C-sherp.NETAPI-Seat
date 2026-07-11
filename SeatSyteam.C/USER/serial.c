#include "serial.h"
#include <stdarg.h>
#include <stdio.h>
#include <string.h>

char USART1_RX_BUF[256] = {0};
uint16_t USART1_RX_LEN = 0;
uint8_t USART1_RX_FINISH = 0;

char USART2_RX_BUF[256] = {0};
uint16_t USART2_RX_LEN = 0;
uint8_t USART2_RX_FINISH = 0;
//静态辅助函数
static void USART_Config(USART_TypeDef* USARTx, uint32_t baud)
{
    USART_InitTypeDef USART_InitStructure;
    USART_InitStructure.USART_BaudRate = baud;
    USART_InitStructure.USART_WordLength = USART_WordLength_8b;
    USART_InitStructure.USART_StopBits = USART_StopBits_1;
    USART_InitStructure.USART_Parity = USART_Parity_No;
    USART_InitStructure.USART_HardwareFlowControl = USART_HardwareFlowControl_None;
    USART_InitStructure.USART_Mode = USART_Mode_Tx | USART_Mode_Rx;
    USART_Init(USARTx, &USART_InitStructure);
    USART_Cmd(USARTx, ENABLE);
}
//串口初始化
void Serial_Init(uint8_t usart_num)
{
    GPIO_InitTypeDef GPIO_InitStructure;
    
    if (usart_num == 1)
    {
        RCC_APB2PeriphClockCmd(RCC_APB2Periph_USART1 | RCC_APB2Periph_GPIOA, ENABLE);
        GPIO_InitStructure.GPIO_Pin = GPIO_Pin_9;
        GPIO_InitStructure.GPIO_Mode = GPIO_Mode_AF_PP;
        GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;
        GPIO_Init(GPIOA, &GPIO_InitStructure);
        GPIO_InitStructure.GPIO_Pin = GPIO_Pin_10;
        GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IN_FLOATING;
        GPIO_Init(GPIOA, &GPIO_InitStructure);
        USART_Config(USART1, 115200);
        USART_ITConfig(USART1, USART_IT_RXNE, ENABLE);
        NVIC_EnableIRQ(USART1_IRQn);
        NVIC_SetPriority(USART1_IRQn, 1);
    }
    else if (usart_num == 2)
    {
        RCC_APB1PeriphClockCmd(RCC_APB1Periph_USART2, ENABLE);
        RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOA, ENABLE);
        GPIO_InitStructure.GPIO_Pin = GPIO_Pin_2;
        GPIO_InitStructure.GPIO_Mode = GPIO_Mode_AF_PP;
        GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;
        GPIO_Init(GPIOA, &GPIO_InitStructure);
        GPIO_InitStructure.GPIO_Pin = GPIO_Pin_3;
        GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IN_FLOATING;
        GPIO_Init(GPIOA, &GPIO_InitStructure);
        USART_Config(USART2, 115200);
        USART_ITConfig(USART2, USART_IT_RXNE, ENABLE);
        NVIC_EnableIRQ(USART2_IRQn);
        NVIC_SetPriority(USART2_IRQn, 1);
    }
}
//发送函数
void Serial_SendByte(uint8_t usart_num, uint8_t data)
{
    if (usart_num == 1)
    {
        USART_SendData(USART1, data);
        while (USART_GetFlagStatus(USART1, USART_FLAG_TXE) == RESET);
    }
    else if (usart_num == 2)
    {
        USART_SendData(USART2, data);
        while (USART_GetFlagStatus(USART2, USART_FLAG_TXE) == RESET);
    }
}

void Serial_SendString(uint8_t usart_num, const char *str)
{
    while (*str)
    {
        Serial_SendByte(usart_num, *str++);
    }
}

void Serial_Printf(uint8_t usart_num, char *format, ...)
{
    char buf[128];
    va_list args;
    va_start(args, format);
    vsprintf(buf, format, args);
    va_end(args);
    Serial_SendString(usart_num, buf);
}
//中断处理函数
void USART1_IRQHandler(void)
{
    uint8_t ch;
    if (USART_GetITStatus(USART1, USART_IT_RXNE) != RESET)
    {
        ch = USART_ReceiveData(USART1);
        if (USART1_RX_LEN < sizeof(USART1_RX_BUF) - 1)
        {
            USART1_RX_BUF[USART1_RX_LEN++] = ch;
            USART1_RX_BUF[USART1_RX_LEN] = '\0';
        }
        if (ch == '\n') USART1_RX_FINISH = 1;
        USART_ClearITPendingBit(USART1, USART_IT_RXNE);
    }
}

void USART2_IRQHandler(void)
{
    uint8_t ch;
    if (USART_GetITStatus(USART2, USART_IT_RXNE) != RESET)
    {
        ch = USART_ReceiveData(USART2);
        if (USART2_RX_LEN < sizeof(USART2_RX_BUF) - 1)
        {
            USART2_RX_BUF[USART2_RX_LEN++] = ch;
            USART2_RX_BUF[USART2_RX_LEN] = '\0';
        }
        if (ch == '\n') USART2_RX_FINISH = 1;
        USART_ClearITPendingBit(USART2, USART_IT_RXNE);
    }
}