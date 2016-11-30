using Rainfall.Redirection.Attributes;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace Rainfall
{
    [TargetType(typeof(TerrainManager))]
    internal class WaterLevelDetour:TerrainManager
    {
        [RedirectMethod]
        new public float WaterLevel(Vector2 position)
        {
            int num = Mathf.FloorToInt((position.x + 8640f) * 16f);
            int num2 = Mathf.FloorToInt((position.y + 8640f) * 16f);
            int num3 = Mathf.Clamp(num >> 8, 0, 1080);
            int num4 = Mathf.Clamp((num >> 8) + 1, 0, 1080);
            int num5 = Mathf.Clamp(num2 >> 8, 0, 1080);
            int num6 = Mathf.Clamp((num2 >> 8) + 1, 0, 1080);
            int num7 = num5 * 1081 + num3;
            int num8 = num6 * 1081 + num3;
            int num9 = num5 * 1081 + num4;
            int num10 = num6 * 1081 + num4;
            int num11 = 1000000;
            int num12 = 0;
            int num13 = 0;
            int num14 = 0;
            int num15 = 0;
            WaterSimulation.Cell[] array = this.WaterSimulation.BeginRead();
            try
            {
                int height = (int)array[num7].m_height;
                if (height != 0)
                {
                   
                    num12 = (int)this.BlockHeights[num7] + height;
                    if (num12 < num11)
                    {
                        num11 = num12;
                    }
                }
                height = (int)array[num8].m_height;
                if (height != 0)
                {
                    num13 = (int)this.BlockHeights[num8] + height;
                    if (num13 < num11)
                    {
                        num11 = num13;
                    }
                }
                height = (int)array[num9].m_height;
                if (height != 0)
                {
                    num14 = (int)this.BlockHeights[num9] + height;
                    if (num14 < num11)
                    {
                        num11 = num14;
                    }
                }
                height = (int)array[num10].m_height;
                if (height != 0)
                {
                    num15 = (int)this.BlockHeights[num10] + height;
                    if (num15 < num11)
                    {
                        num11 = num15;
                    }
                }
            }
            finally
            {
                this.WaterSimulation.EndRead();
            }
            //Debug.Log("[RF].WLD Pos.x = " + position.x.ToString() + " pos.z = " + position.y.ToString());
            //Debug.Log("[RF].WLD num 11 is " + num11.ToString());
            if (num11 == 1000000)
            {
                return 0f;
            }
            if (num12 == 0)
            {
                num12 = num11;
            }
            if (num13 == 0)
            {
                num13 = num11;
            }
            if (num14 == 0)
            {
                num14 = num11;
            }
            if (num15 == 0)
            {
                num15 = num11;
            }
            int num16 = num12 + ((num13 - num12) * (num2 & 255) >> 8);
            int num17 = num14 + ((num15 - num14) * (num2 & 255) >> 8);
            int num18 = num16 + ((num17 - num16) * (num & 255) >> 8);
            int num19 = (int)this.RawHeights2[num7];
            int num20 = (int)this.RawHeights2[num8];
            int num21 = (int)this.RawHeights2[num9];
            int num22 = (int)this.RawHeights2[num10];
            int num23 = num19 + ((num20 - num19) * (num2 & 255) >> 8);
            int num24 = num21 + ((num22 - num21) * (num2 & 255) >> 8);
            int num25 = num23 + ((num24 - num23) * (num & 255) >> 8);
            
            //Begin NonStock Code
            int miny;
            int avgy;
            int maxy;
            this.CalculateAreaHeight(position.x, position.y, position.x, position.y, out miny, out avgy, out maxy);
            num25 = miny;
            //Debug.Log("[RF].WLD Pos.x = " + position.x.ToString() + " pos.z = " + position.y.ToString());
            //Debug.Log("[RF].WLD num 18 is " + num18.ToString() + " num 25 is " + num25.ToString());
            
            
            if (num18 - num25 >= -16)//End Nonstock Code
            {
                return (float)num18 * 0.015625f;
            }
            return 0f;
        }

        [RedirectMethod]
        new public bool HasWater(Vector2 position)
        {
            int num = Mathf.FloorToInt((position.x + 8640f) * 16f);
            int num2 = Mathf.FloorToInt((position.y + 8640f) * 16f);
            int num3 = Mathf.Clamp(num >> 8, 0, 1080);
            int num4 = Mathf.Clamp((num >> 8) + 1, 0, 1080);
            int num5 = Mathf.Clamp(num2 >> 8, 0, 1080);
            int num6 = Mathf.Clamp((num2 >> 8) + 1, 0, 1080);
            int num7 = num5 * 1081 + num3;
            int num8 = num6 * 1081 + num3;
            int num9 = num5 * 1081 + num4;
            int num10 = num6 * 1081 + num4;
            int num11 = 1000000;
            int num12 = 0;
            int num13 = 0;
            int num14 = 0;
            int num15 = 0;
            WaterSimulation.Cell[] array = this.WaterSimulation.BeginRead();
            try
            {
                int height = (int)array[num7].m_height;
                if (height != 0)
                {
                    num12 = (int)this.BlockHeights[num7] + height;
                    if (num12 < num11)
                    {
                        num11 = num12;
                    }
                }
                height = (int)array[num8].m_height;
                if (height != 0)
                {
                    num13 = (int)this.BlockHeights[num8] + height;
                    if (num13 < num11)
                    {
                        num11 = num13;
                    }
                }
                height = (int)array[num9].m_height;
                if (height != 0)
                {
                    num14 = (int)this.BlockHeights[num9] + height;
                    if (num14 < num11)
                    {
                        num11 = num14;
                    }
                }
                height = (int)array[num10].m_height;
                if (height != 0)
                {
                    num15 = (int)this.BlockHeights[num10] + height;
                    if (num15 < num11)
                    {
                        num11 = num15;
                    }
                }
            }
            finally
            {
                this.WaterSimulation.EndRead();
            }
            if (num11 == 1000000)
            {
                return false;
            }
            if (num12 == 0)
            {
                num12 = num11;
            }
            if (num13 == 0)
            {
                num13 = num11;
            }
            if (num14 == 0)
            {
                num14 = num11;
            }
            if (num15 == 0)
            {
                num15 = num11;
            }
            int num16 = num12 + ((num13 - num12) * (num2 & 255) >> 8);
            int num17 = num14 + ((num15 - num14) * (num2 & 255) >> 8);
            int num18 = num16 + ((num17 - num16) * (num & 255) >> 8);
            int num19 = (int)this.RawHeights2[num7];
            int num20 = (int)this.RawHeights2[num8];
            int num21 = (int)this.RawHeights2[num9];
            int num22 = (int)this.RawHeights2[num10];
            int num23 = num19 + ((num20 - num19) * (num2 & 255) >> 8);
            int num24 = num21 + ((num22 - num21) * (num2 & 255) >> 8);
            int num25 = num23 + ((num24 - num23) * (num & 255) >> 8);
            //Begin NonStock Code
            int miny;
            int avgy;
            int maxy;
            this.CalculateAreaHeight(position.x, position.y, position.x, position.y, out miny, out avgy, out maxy);
            num25 = miny;
            
            return num18 - num25 >= -16;
            //End Nonstock Code
        }


    }
}
