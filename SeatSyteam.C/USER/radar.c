//#include "radar.h"

////雷达引脚定义
//#define RADAR1_PIN  GPIO_Pin_0   // seat_1 -> PB0
//#define RADAR2_PIN  GPIO_Pin_1   // seat_2 -> PB1
//#define RADAR3_PIN  GPIO_Pin_5   // seat_3 -> PB5
//#define RADAR4_PIN  GPIO_Pin_6   // seat_4 -> PB6
//#define RADAR_PORT  GPIOB
////全局数组，存储每个座位的数据
//uint8_t seat_status[RADAR_NUM] = {0};
////静态数组，本文件可见，用于保存上一次读取的状态
//static uint8_t last_status[RADAR_NUM] = {0xFF, 0xFF, 0xFF, 0xFF};
////雷达引脚初始化
//void Radar_Init(void)
//{
//    GPIO_InitTypeDef GPIO_InitStructure;
//    RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOB, ENABLE);
//    GPIO_InitStructure.GPIO_Pin = RADAR1_PIN | RADAR2_PIN | RADAR3_PIN | RADAR4_PIN;
//    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IPU;   // 设置引脚为上拉输入
//    GPIO_Init(RADAR_PORT, &GPIO_InitStructure);
//}
////读取所有雷达状态
//void Radar_ReadAll(void)
//{
//    seat_status[0] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR1_PIN) ? 1 : 0;
//    seat_status[1] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR2_PIN) ? 1 : 0;
//    seat_status[2] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR3_PIN) ? 1 : 0;
//    seat_status[3] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR4_PIN) ? 1 : 0;
//}
////检测状态变化
//uint8_t Radar_CheckChange(void)
//{
//    uint8_t current[RADAR_NUM];
//    uint8_t changed = 0;
//    uint8_t i;
////读取所有引脚电平到current数组
//    current[0] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR1_PIN) ? 1 : 0;
//    current[1] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR2_PIN) ? 1 : 0;
//    current[2] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR3_PIN) ? 1 : 0;
//    current[3] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR4_PIN) ? 1 : 0;
////遍历所有座位，将current[i]与last_status[i]比较，如果不同则更新last_status[i]与seat_status[i]
//    for (i = 0; i < RADAR_NUM; i++)
//    {
//        if (current[i] != last_status[i])
//        {
//            last_status[i] = current[i];
//            seat_status[i] = current[i];
//						//有变化则chanfed置为1
//            changed = 1;
//        }
//    }
//		//返回changed
//    return changed;
//}
#include "radar.h"

#define RADAR1_PIN  GPIO_Pin_0
#define RADAR2_PIN  GPIO_Pin_1
#define RADAR3_PIN  GPIO_Pin_5
#define RADAR4_PIN  GPIO_Pin_6
#define RADAR_PORT  GPIOB

uint8_t seat_status[RADAR_NUM] = {0};
static uint8_t last_status[RADAR_NUM] = {0xFF, 0xFF, 0xFF, 0xFF};

void Radar_Init(void)
{
    GPIO_InitTypeDef GPIO_InitStructure;
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOB, ENABLE);
    GPIO_InitStructure.GPIO_Pin = RADAR1_PIN | RADAR2_PIN | RADAR3_PIN | RADAR4_PIN;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IPU; 
    GPIO_Init(RADAR_PORT, &GPIO_InitStructure);
}

void Radar_ReadAll(void)
{
    seat_status[0] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR1_PIN) ? 1 : 0;
    seat_status[1] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR2_PIN) ? 1 : 0;
    seat_status[2] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR3_PIN) ? 1 : 0;
    seat_status[3] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR4_PIN) ? 1 : 0;
}

uint8_t Radar_CheckChange(void)
{
    uint8_t current[RADAR_NUM];
    uint8_t changed = 0;
    uint8_t i;

    current[0] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR1_PIN) ? 1 : 0;
    current[1] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR2_PIN) ? 1 : 0;
    current[2] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR3_PIN) ? 1 : 0;
    current[3] = GPIO_ReadInputDataBit(RADAR_PORT, RADAR4_PIN) ? 1 : 0;

    for (i = 0; i < RADAR_NUM; i++)
    {
        if (current[i] != last_status[i])
        {
            last_status[i] = current[i];
            seat_status[i] = current[i];
            changed = 1;
        }
    }
    return changed;
}