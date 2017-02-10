//
//STM32F103VCT6串口收发协议例程
//

/**
  ************************************
  * @brief  串口发送协议
  * @param  header:头码  date:16位整形数据
  * @retval None
	************************************
*/
void Send_PC(uint8_t header, uint16_t date)
{
  uint8_t buf[4];
  buf[0] = header;
  buf[1] = date >> 8;
  buf[2] = date & 0xff;
  buf[3] = ~header;
  for(uint8_t i=0; i<4; i++)
  {
    while(USART_GetFlagStatus(USART1,USART_FLAG_TC)==RESET);
    USART_SendData(USART1, buf[i]);
  }
}

//动作枚举定义
typedef enum ACT
{
	POSITIONING_PLUS = 0x0001ff00,
	POSITIONING_MINUS = 0x0002ff00,
	LIFT_PLUS = 0X0101FE00,
	LIFT_MINUS = 0X0102FE00,
	FLAT_PLUS = 0X0201FE00,
	FLAT_MINUS = 0X0202FE00,
	TIME_PLUS = 0X0401FB00,
	TIME_MINUS = 0X0402FB00,
	TURN_FLAG_PLUS = 0X0403FB00,
	TURN_FLAG_MINUS = 0X0404FB00,
	TURN_FLAG_0 = 0X0405FB00,
	MODE_1 = 0X0502FA00,
	MODE_2 = 0X0503FA00,
	MODE_3 = 0X0504FA00,
	MODE_4 = 0X0501FA00,
	CATCH = 0X0507FA00,
	PUT = 0X0508FA00
}ACT;

//定义联合体
typedef union MSG{
	uint8_t buf[4];
	ACT act;
}MSG;

void USART1_IRQHandler(void)
{
	static uint8_t i = 0;
  static MSG msg = {0xff,0xff,0xff,0x00};
  Flag_Manipulator = 1;//接收到数据标志位
	if(USART_GetFlagStatus(USART1, USART_IT_RXNE) != RESET)
	{
		msg.buf[i] = USART_ReceiveData(USART1);
    USART_ClearITPendingBit(USART1,USART_IT_RXNE);
		i++;
	}
  if(i == 3)
  {
    i=0;
		msg.act = (long)((msg.buf[0]<<(8*3)) | (msg.buf[1]<<(8*2)) | (msg.buf[2]<<8));
		switch(msg.act)
		{
			case POSITIONING_PLUS:			
				Positioning_Left += 5;
				Positioning_Right = (uint16_t)(3405-1.029 * Positioning_Left);
				break;			
			case POSITIONING_MINUS:			
				Positioning_Left -= 5;
				Positioning_Right = (uint16_t)(3405-1.029 * Positioning_Left);
				break;			
			case LIFT_PLUS:Lift += 5;break;
			case LIFT_MINUS:Lift -= 5;break;
			case FLAT_PLUS:Flat += 5;break;
			case FLAT_MINUS:Flat -= 10;break;
			case TIME_PLUS:Time++;break;
			case TIME_MINUS:Time--;break;
			case TURN_FLAG_PLUS:Turn_Flag++;break;
			case TURN_FLAG_MINUS:Turn_Flag--;break;
			case TURN_FLAG_0:Turn_Flag = 0;Time = 0;break;
			case MODE_1:Mode = 1;break;
			case MODE_2:Mode = 2;break;
			case MODE_3:Mode = 3;break;
			case MODE_4:Mode = 4;break;
			case CATCH:Mode = 5;break;
			case PUT:Mode = 6;break;
			default:break;
		}
}

/**
  ************************************
  * @brief  控制函数
  * @param  None
  * @retval None
	************************************
*/
void Test_Ultrasonic(void)
{
  uint16_t value_cm = 0;
  while(1)
  {
    if(Time == 0 && Turn_Flag == 0)
      Run_Stop();
		switch(Mode)
		{
			case 1:		
				Lift_Flat(Catch_On_Lift, Catch_On_Flat);
				Positioning(Catch_On_Positioning_Left, Catch_On_Positioning_Right, MANIPULATOR_DELAY_TIME);
				Mode = 0;
				break;
			case 2:
				Lift_Flat(Catch_In_Lift, Catch_In_Flat);
				Positioning(Catch_In_Positioning_Left, Catch_In_Positioning_Right, MANIPULATOR_DELAY_TIME);
				Mode = 0;
				break;
			case 3:			
				Lift_Flat(Catch_Under_Lift, Catch_Under_Flat);
				Positioning(Catch_Under_Positioning_Left, Catch_Under_Positioning_Right, MANIPULATOR_DELAY_TIME);
				Mode = 0;
				break;			
			case 4:Put_Catch("Catch", "Highest");Mode = 0;break;
			case 5:Catch_Put(Catch);Mode = 0;break;
			case 6:Catch_Put(Put);Mode = 0;break;
			default:Mode = 0;break;
		}
		TIM_SetCompare4(TIM3,Positioning_Left);//PB1
		TIM_SetCompare3(TIM3,Positioning_Right);//PB0
		TIM_SetCompare1(TIM3,Lift);//PB4
		TIM_SetCompare1(TIM4,Flat);//PB6
    value_cm = Ultrasonic_Mean_Value(5999, 72);
    Send_PC(0x00, value_cm);
  }
}