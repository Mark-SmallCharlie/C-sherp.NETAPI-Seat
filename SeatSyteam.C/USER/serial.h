#ifndef __SERIAL_H
#define __SERIAL_H

#include "stm32f10x.h"

void Serial_Init(uint8_t usart_num);
void Serial_SendByte(uint8_t usart_num, uint8_t data);
void Serial_SendString(uint8_t usart_num, const char *str);  
void Serial_Printf(uint8_t usart_num, char *format, ...);

extern char USART1_RX_BUF[256];
extern uint16_t USART1_RX_LEN;
extern uint8_t USART1_RX_FINISH;

extern char USART2_RX_BUF[256];
extern uint16_t USART2_RX_LEN;
extern uint8_t USART2_RX_FINISH;

#endif