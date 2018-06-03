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
                    if (Model.Shops[i].ProductAmountCurrent.Value < Model.Shops[i].ProductDemandCurrent.Value)
                    {
                        // Неудовлетворенный спрос
                        Model.Shops[i].ProductUnmetDemandCurrent.Value = Math.Abs(Model.Shops[i].ProductDemandCurrent.Value - Model.Shops[i].ProductAmountCurrent.Value);

                        // Объем потерь от подачи заявки
                        Model.Shops[i].ProductLossRequestCurrent.Value = (int)Model.NormalGenerator_PP_Loss.GenerateValue();

                        // Планирование события 1 - появление заявки в СМО
                        // Задание интервала времени (для каждого магазина) через который наступит событие
                        double deltaTime = i == 0 ? Model.ZR_TV_ValueGenerator1.GenerateValue() : Model.ZR_TV_ValueGenerator2.GenerateValue();

                        var k2Event = new K2
                        {
                            ShopNumber = i,
                            DayOfSupply = DayNumber
                        };
                        Model.Shops[i].HasSendRequest.Value = true;
                        Model.Shops[i].RequestsTotalCount.Value++;
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

                        // Количество пролежанного товара
                        Model.Shops[i].ProductUnrealizedCurrent.Value = Model.Shops[i].ProductAmountCurrent.Value;

                    }
                    // Увеличиваем текущий суммарный объем спроса на товар
                    Model.SVST.Value += Model.Shops[i].ProductDemandCurrent.Value;

                    Model.Shops[i].ProductDemandAll.Value += Model.Shops[i].ProductDemandCurrent.Value;
                    Model.Shops[i].ProductLossRequestAll.Value += Model.Shops[i].ProductLossRequestCurrent.Value;
                    Model.Shops[i].ProductUnmetDemandAll.Value += Model.Shops[i].ProductUnmetDemandCurrent.Value;
                    Model.Shops[i].ProductUnrealizedAll.Value += Model.Shops[i].ProductUnrealizedCurrent.Value;
                }
                Model.TraceModel(DayNumber);
                Model.SVSTP.Value += Model.SVST.Value;
                Model.SVST.Value = 0;
                
                //Планирование следующего события окончания рабочего дня; НО!!!!
                //если время кончилось, планируем событие 3
                Model.Day++;
                if (Model.Day <= M)
                {
                    var k1Event = new K1
                    {
                        DayNumber = Model.Day
                    };
                    Model.PlanEvent(k1Event, k1Event.DayNumber);
                    // Занесение в файл трассировки записи о запланированном событии
                    Model.Tracer.PlanEventTrace(k1Event,
                                          k1Event.DayNumber);
                }
                else
                {
                    var k3Event = new K3();
                    Model.PlanEvent(k3Event, Model.Day);
                    // Занесение в файл трассировки записи о запланированном событии
                    Model.Tracer.PlanEventTrace(k3Event,
                                          Model.Day);

                }
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
                Model.Shops[ShopNumber].HasSendRequest.Value = false;
                Model.TraceRequest(DayOfSupply, ShopNumber);
            }
        }

        /// <summary>
        /// Класс для события 3 - Окончание фиксированного интервала времени сбора статистики по объему поставок
        /// </summary>
        public class K3 : TimeModelEvent<SmoModel>
        {
            #region Атрибуты события

            #endregion

            protected override void HandleEvent(ModelEventArgs args)
            {
                //Model.TraceK3();
            }
        }
    }
}