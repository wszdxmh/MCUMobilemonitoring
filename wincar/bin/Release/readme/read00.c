void USART1_IRQHandler(void)
{
	static uint8_t i = 0;
  static uint8_t buf[3] = {0xff, 0xff, 0xff};
	if(USART_GetITStatus(USART1, USART_IT_RXNE) != RESET)
	{
		buf[i] = USART_ReceiveData(USART1);
    USART_ClearITPendingBit(USART1,USART_IT_RXNE);
		i++;
	}
  if(i == 3)
  {
    i=0;
    if(buf[0] == 0x00 && buf[1] == 0x01 && buf[2] == 0xff)
    {
      Positioning_Left += 10;
      Positioning_Right = (uint16_t)(3405-1.029 * Positioning_Left);
      Send_PC(0x00, Positioning_Left);
      delay_ms(40);
      Send_PC(0x01, Positioning_Right);
    }
    else if(buf[0] == 0x00 && buf[1] == 0x02 && buf[2] == 0xff)
    {
      Positioning_Left -= 10;
      Positioning_Right = (uint16_t)(3405 - (1.0)*(1.029 * Positioning_Left));
      Send_PC(0x00, Positioning_Left);
      delay_ms(40);
      Send_PC(0x01, Positioning_Right);
    }
    else if(buf[0] == 0x01 && buf[1] == 0x04 && buf[2] == 0xfe)
    {
      //Positioning_Left -= 19;
      Positioning_Right += 10;
      //Send_PC(0x00, Positioning_Left);
      //delay_ms(50);
      Send_PC(0x01, Positioning_Right);
    }
    else if(buf[0] == 0x01 && buf[1] == 0x08 && buf[2] == 0xfe)
    {
      //Positioning_Left += 19;
      Positioning_Right -= 10;
      //Send_PC(0x00, Positioning_Left);
      //delay_ms(50);
      Send_PC(0x01, Positioning_Right);
    }
    else if(buf[0] == 0x02 && buf[1] == 0x10 && buf[2] == 0xfd)
    {
      Lift += 10;
      Send_PC(0x02, Lift);
    }
    else if(buf[0] == 0x02 && buf[1] == 0x20 && buf[2] == 0xfd)
    {
      Lift -= 10;
      Send_PC(0x02, Lift);
    }
    else if(buf[0] == 0x03 && buf[1] == 0x40 && buf[2] == 0xfc)
    {
      Flat += 10;
      Send_PC(0x03, Flat);
    }
    else if(buf[0] == 0x03 && buf[1] == 0x80 && buf[2] == 0xfc)
    {
      Flat -= 10;
      Send_PC(0x03, Flat);
    }
    else if(buf[0] == 0x04  && buf[2] == 0xfb)
    {
      Flag = 1;
      Time = buf[1];
    }
  }
}