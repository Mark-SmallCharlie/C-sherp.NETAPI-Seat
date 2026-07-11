#ifndef __RADAR_H
#define __RADAR_H

#include "stm32f10x.h"
//定义雷达数量
#define RADAR_NUM   4
//声明外部全局变量
extern uint8_t seat_status[RADAR_NUM];
//函数声明
void Radar_Init(void);
void Radar_ReadAll(void);
uint8_t Radar_CheckChange(void);

#endif