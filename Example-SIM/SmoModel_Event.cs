using CommonModel.Kernel;
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
                    if (i == 1) // else
                        Model.Shops[i].ProductDemandCurrent.Value = (int)Model.NormalGenerator_VDS2.GenerateValue();

                    // Если спрос превысил текущий объем товара в магазине
                    //if ((Model.Shops[i].ProductAmountCurrent.Value < Model.Shops[i].ProductDemandCurrent.Value)&& (Model.Shops[i].HasSendRequest.Value == false))
                    //{
                    if (Model.Shops[i].ProductAmountCurrent.Value < Model.Shops[i].ProductDemandCurrent.Value)
                    {

                        // Неудовлетворенный спрос
                        Model.Shops[i].ProductUnmetDemandCurrent.Value = Math.Abs(Model.Shops[i].ProductDemandCurrent.Value - Model.Shops[i].ProductAmountCurrent.Value);

                        // Планирование события 1 - появление заявки в СМО
                        // Задание интервала времени (для каждого магазина) через который наступит событие
                        //TODO: кажется, дельта работает не так. Пополнение у всех магазинов почему-то происходит строго через через день, хотя это значение должно варироваться в соответствии с ZR_TV
                        double deltaTime = i == 0 ? Model.ZR_TV_ValueGenerator1.GenerateValue() : Model.ZR_TV_ValueGenerator2.GenerateValue(); 
                        var k2Event = new K2
                        {
                            ShopNumber = i,
                            DayOfSupply = DayNumber
                        };
                        Model.Shops[i].HasSendRequest.Value = 1;
                        Model.Shops[i].RequestsTotalCountCurrent.Value = 1;
                        Model.PlanEvent(k2Event, DayNumber + deltaTime);

                        // Занесение в файл трассировки записи о запланированном событии
                        Model.Tracer.PlanEventTrace(k2Event, 
                                                    DayNumber + deltaTime,
                                                    k2Event.ShopNumber);
                    }
                    else
                    {
                        // Если спрос меньше текущего объема товара, то вычитаем из объема товара объем спроса
                        Model.Shops[i].ProductAmountCurrent.Value -= Model.Shops[i].ProductDemandCurrent.Value;

                        if (Model.Shops[i].ProductAmountCurrent.Value < 0)
                            Model.Shops[i].ProductUnrealizedCurrent.Value = 0;
                        else
                            Model.Shops[i].ProductUnrealizedCurrent.Value = Model.Shops[i].ProductAmountCurrent.Value;

                    }

                    // Увеличиваем текущий суммарный объем 
                    //Model.SVST.Value += Model.Shops[i].ProductDemandCurrent.Value;

                    Model.Shops[i].ProductDemandAll.Value += Model.Shops[i].ProductDemandCurrent.Value;               // спроса на товар
                    if (Model.Shops[i].HasSendRequest.Value == 1)
                        Model.Shops[i].RequestsTotalCountAll.Value += Model.Shops[i].RequestsTotalCountCurrent.Value; // заявок на пополнение товара
                    Model.Shops[i].ProductUnmetDemandAll.Value += Model.Shops[i].ProductUnmetDemandCurrent.Value;     // неудовлетворенного спроса на товар
                    Model.Shops[i].ProductUnrealizedAll.Value += Model.Shops[i].ProductUnrealizedCurrent.Value;       // пролежанного товара
                }

                
                
                
                
                //Планирование следующего события окончания рабочего дня; НО!!!!
                //если время кончилось, планируем событие 3
                Model.Day++;
                if ((Model.Day <= 28))
                {
                    var k1Event = new K1
                    {
                        DayNumber = Model.Day
                    };
                    Model.PlanEvent(k1Event,  
                                    k1Event.DayNumber);
                    Model.Tracer.PlanEventTrace(k1Event, k1Event.DayNumber, "VS[0," + Model.Shops[0].ProductDemandCurrent.Value + "]" + " \tVS[1," + Model.Shops[1].ProductDemandCurrent.Value + "]");

                    // Занесение в файл трассировки записи о запланированном событии
                    //Model.Tracer.PlanEventTrace(k1Event, 
                    //                            k1Event.DayNumber);
                    //Model.Tracer.PlanEventTrace(k1Event, "VS[0," + Model.Shops[0].ProductDemandCurrent.Value+"]");
                    //Model.Tracer.PlanEventTrace(k1Event, "VS[1," + Model.Shops[1].ProductDemandCurrent.Value + "]");

                }
                else
                {
                    var k3Event = new K3();
                    Model.SVSTP.Value = Model.SVST.Value; 
                    Model.SVST.Value = 0;
                    Model.PlanEvent(k3Event, Model.Day);
                    // Занесение в файл трассировки записи о запланированном событии
                    Model.Tracer.PlanEventTrace(k3Event,
                                          Model.Day, Model.SVSTP.Value);

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

            public int ShopNumber { get; set; }

            /// <summary>
            /// День подачи заявки из магазина в ОС
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

            public int NumberOfWeek { get; set; }
            public double SVSTP_K3 { get; set; }


            #endregion

            protected override void HandleEvent(ModelEventArgs args)
            {
                
                
            }
        }
    }
}