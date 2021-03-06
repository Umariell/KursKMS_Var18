﻿using CommonModel.Kernel;
using System;

namespace Model_Lab
{
    public partial class SmoModel : Model
    {
        /// <summary>
        /// Класс для события 1 - окончание рабочего дня
        /// </summary>
        public class K1 : TimeModelEvent<SmoModel>
        {
            #region Атрибуты события

            /// <summary>
            /// Номер текущего дня
            /// </summary>
            public int DayNumber { get; set; }

            #endregion
            // Алгоритм обработки события            
            protected override void HandleEvent(ModelEventArgs args)
            {
                for (int i = 0; i < N; i++)
                {
                    if (i == 0)
                        Model.Shops[i].ProductDemandCurrent.Value = (int)Model.NormalGenerator_VDS1.GenerateValue();
                    if (i == 1)
                        Model.Shops[i].ProductDemandCurrent.Value = (int)Model.NormalGenerator_VDS2.GenerateValue();

                    // Если спрос превысил текущий объем товара в магазине
                    if (Model.Shops[i].ProductAmountCurrent.Value < Model.Shops[i].ProductDemandCurrent.Value)
                    {
                        // Неудовлетворенный спрос
                        Model.Shops[i].ProductUnmetDemandCurrent.Value = Math.Abs(Model.Shops[i].ProductDemandCurrent.Value - Model.Shops[i].ProductAmountCurrent.Value);
                    }
                    else
                    {
                        // Если спрос меньше текущего объема товара, то вычитаем из объема товара объем спроса
                        Model.Shops[i].ProductAmountCurrent.Value -= Model.Shops[i].ProductDemandCurrent.Value;
                        Model.Shops[i].ProductUnrealizedCurrent.Value = Model.Shops[i].ProductAmountCurrent.Value;
                    }

                    if (Model.Shops[i].ProductAmountCurrent.Value < Model.TV && Model.Shops[i].HasSendRequest == 0)
                    {
                        double deltaTime = i == 0 ? Model.ZR_TV_ValueGenerator1.GenerateValue() : Model.ZR_TV_ValueGenerator2.GenerateValue();
                        var k2Event = new K2
                        {
                            ShopNumber = i,
                            DayOfSupply = DayNumber
                        };
                        Model.Shops[i].HasSendRequest.Value = 1;
                        Model.Shops[i].RequestsTotalCountCurrent.Value++;
                        Model.Shops[i].RequestsTotalCountAll.Value++;
                        Model.PlanEvent(k2Event, DayNumber + deltaTime);

                        // Занесение в файл трассировки записи о запланированном событии
                        Model.Tracer.PlanEventTrace(k2Event,
                                                    DayNumber + deltaTime,
                                                    k2Event.ShopNumber);
                    }

                    Model.Shops[i].ProductDemandAll.Value += Model.Shops[i].ProductDemandCurrent.Value;               // спроса на товар
                    Model.Shops[i].ProductUnmetDemandAll.Value += Model.Shops[i].ProductUnmetDemandCurrent.Value;     // неудовлетворенного спроса на товар
                    Model.Shops[i].ProductUnrealizedAll.Value += Model.Shops[i].ProductUnrealizedCurrent.Value;       // пролежанного товара
                }

                //Планирование следующего события окончания рабочего дня; НО!!!!
                //если время кончилось, планируем событие 3
                Model.Day++;
                if (Model.Day <= M)
                {
                    var k1Event = new K1
                    {
                        DayNumber = Model.Day
                    };
                    Model.PlanEvent(k1Event,
                                    1);
                    Model.Tracer.PlanEventTrace(k1Event,
                                                k1Event.DayNumber,
                                                "VS[" + Model.Shops[0].ProductDemandCurrent.Value + "," + Model.Shops[1].ProductDemandCurrent.Value + "]");
                }
                if (Model.Day % 7 == 0) // == 1
                {
                    var k3Event = new K3 { NumberOfWeek = Model.Day / 7 };
                    Model.PlanEvent(k3Event, 0);
                    // Занесение в файл трассировки записи о запланированном событии
                    Model.Tracer.PlanEventTrace(k3Event,
                                                Model.Day / 7,
                                                Model.SVSTP.Value,
                                                Model.SVST.Value);
                }
                Model.TraceModel(DayNumber);
            }
        }

        /// <summary>
        /// Класс для события 2 - пополнение товарного запаса в магазине Nмаг
        /// Момент подачи заявки на склад
        /// </summary>//(тут была точка останова)
        public class K2 : TimeModelEvent<SmoModel>
        {
            #region Атрибуты события

            /// <summary>
            /// Номер магазина.
            /// </summary>
            public int ShopNumber { get; set; }

            /// <summary>
            /// День подачи заявки из магазина в ОС.
            /// </summary>
            public int DayOfSupply { get; set; }

            #endregion

            // Алгоритм обработки события            
            protected override void HandleEvent(ModelEventArgs args)
            {
                // Выполнить поставку товара в магазин с номером ShopNumber
                Model.Shops[ShopNumber].ProductAmountCurrent.Value += Model.VV;
                Model.Shops[ShopNumber].HasSendRequest.Value = 0;
                Model.SVST.Value += Model.VV;
            }
        }

        /// <summary>
        /// Класс для события 3 - Окончание фиксированного интервала времени сбора статистики по объему поставок
        /// </summary>
        public class K3 : TimeModelEvent<SmoModel>
        {
            #region Атрибуты события

            /// <summary>
            /// Номер недели.
            /// </summary>
            public int NumberOfWeek { get; set; }

            #endregion

            protected override void HandleEvent(ModelEventArgs args)
            {
                Model.SVSTP.Value = Model.SVST.Value;
                Model.Tracer.AnyTrace("TF №: " + NumberOfWeek, "\tSVST: " + Model.SVST.Value, "\tSVSTP: " + Model.SVSTP.Value);
                Model.SVST.Value = 0;
            }
        }
    }
}