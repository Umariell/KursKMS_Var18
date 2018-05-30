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

            public int DayNumber { get; set; }

            #endregion

            // Алгоритм обработки события            
            protected override void HandleEvent(ModelEventArgs args)
            {
                //Model.TraceModel(DayNumber);

                for (int i = 0; i < N; i++)
                {
                    Model.Shops[i].ProductDemandCurrent.Value = (int)Model.NormalGenerator_VDS1.GenerateValue();
                    // Записать объем товара за i-тый день в соответствующий список у магазина
                    //Model.Shops[i].ProductAmountAll.Add(Model.Shops[i].ProductAmountCurrent);

                    // Записать объем спроса за i-тый день в соответствующий список у магазина
                    //Model.Shops[i].ProductDemandAll.Add(Model.Shops[i].ProductDemandCurrent);

                    // Записать объем неудовлетворенного спроса за i-тый день в соответствующий список у магазина
                    //Model.Shops[i].ProductUnmetDemandAll.Add(Model.Shops[i].ProductUnmetDemandCurrent);

                    // Записать объем пролежанного товара за i-тый день в соответствующий список у магазина
                    //Model.Shops[i].ProductUnrealizedAll.Add(Model.Shops[i].ProductUnrealizedCurrent);

                    // Записать объем потерь от подачи заявки за i-тый день в соответствующий список у магазина
                    //Model.Shops[i].ProductLossRequestAll.Add(Model.Shops[i].ProductLossRequestCurrent);

                    // Если спрос превысил текущий объем товара в магазине
                    if (Model.Shops[i].ProductAmountCurrent.Value < Model.Shops[i].ProductDemandCurrent.Value)
                    {
                        // TODO: аналогично для остальных переменных

                        //неудовлетворенный спрос
                        Model.Shops[i].ProductUnmetDemandCurrent.Value = Math.Abs(Model.Shops[i].ProductDemandCurrent.Value - Model.Shops[i].ProductAmountCurrent.Value);
                        //Model.Shops[i].ProductUnmetDemandAll.Add(Model.Shops[i].ProductUnmetDemandCurrent);

                        //Model.Shops[i].ProductUnrealizedAll.Add(Model.Shops[i].ProductUnrealizedCurrent);// TODO: выходит, что он высчитывается так же, как неуд.спрос и по сути равен ему?

                        //объем потерь от подачи заявки
                        Model.Shops[i].ProductLossRequestCurrent.Value = Math.Abs(Model.Shops[i].ProductDemandCurrent.Value - Model.Shops[i].ProductAmountCurrent.Value);
                        //Model.Shops[i].ProductLossRequestAll.Add(Model.Shops[i].ProductLossRequestCurrent);

                        // Планирование события 1 - появление заявки в СМО
                        // Задание интервала времени (для каждого магазина) через который наступит событие
                        double deltaTime = i == 0 ? Model.ZR_TV_ValueGenerator1.GenerateValue() : Model.ZR_TV_ValueGenerator2.GenerateValue();
                        //var deltaTime1 = Model.ZR_TV_ValueGenerator1.GenerateValue();
                        //var deltaTime2 = Model.ZR_TV_ValueGenerator2.GenerateValue();

                        var k2Event = new K2()
                        {
                            ShopNumber = i
                        };
                        Model.Shops[i].HasSendRequest.Value = true;
                        Model.Shops[i].RequestsTotalCount.Value++;
                        Model.PlanEvent(k2Event, DayNumber + deltaTime);
                        // Занесение в файл трассировки записи о запланированном событии
                        Model.Tracer.PlanEventTrace(k2Event,
                                                    Model.Time + deltaTime,
                                                    k2Event.ShopNumber);
                    }
                    else
                        // Если спрос меньше текущего объема товара, то вычитаем из объема товара объем спроса
                        Model.Shops[i].ProductAmountCurrent.Value -= Model.Shops[i].ProductDemandCurrent.Value;

                    //пролежанный товар
                    Model.Shops[i].ProductUnrealizedCurrent.Value = Math.Abs(Model.Shops[i].ProductAmountCurrent.Value - Model.Shops[i].ProductDemandCurrent.Value);

                    // Считаем убытки от пролёжанного товара за текущий день в магазине
                    Model.Shops[i].ProductUnrealizedCurrent.Value = Model.Shops[i].ProductAmountCurrent.Value * Model.PP;
                    //Model.Shops[i].ProductUnrealizedAll.Add(Model.Shops[i].ProductUnrealizedCurrent);

                    // Увеличиваем текущий суммарный объем спроса на товар
                    Model.SVST.Value += Model.Shops[i].ProductDemandCurrent.Value;

                    // Генерируем спрос в магазине на следующий день
                    Model.Shops[i].ProductDemandCurrent.Value = (int)Model.NormalGenerator_VDS1.GenerateValue();
                    //Model.Shops[i].ProductDemandAll.Add(Model.Shops[i].ProductDemandCurrent);

                    Model.TraceModel(DayNumber);
                }
                Model.SVSTP.Value += Model.SVST.Value;
                Model.SVST.Value = 0;

                //Model.Time++;
                //Планирование следующего события окончания рабочего дня
                ++DayNumber;
                Model.Day++;
                var k1Event = new K1()
                {
                    DayNumber = DayNumber
                };
                Model.PlanEvent(k1Event, DayNumber);
                // Занесение в файл трассировки записи о запланированном событии
                Model.Tracer.PlanEventTrace(k1Event,
                                      DayNumber);
            }
        }

        /// <summary>
        /// Класс для события 2 - пополнение товарного запаса в магазине Nмаг
        /// Момент подачи заявки на склад
        /// </summary>
        public class K2 : TimeModelEvent<SmoModel>
        {
            #region Атрибуты события

            // TODO: учесть нумерацию в массиве (индекс с 0) и номер магазина в событии
            public int ShopNumber { get; set; }

            /// <summary>
            /// День поставки товара со склада в магазин.
            /// </summary>
            public int DayOfSupply { get; set; }

            #endregion

            // Алгоритм обработки события            
            protected override void HandleEvent(ModelEventArgs args)
            {
                // Запланировать поставку товара в магазин с номером ShopNumber
                Model.Shops[ShopNumber].SupplyAmountLast.Value = Model.VV;
                Model.Shops[ShopNumber].ProductAmountCurrent.Value += Model.VV;
                Model.Shops[ShopNumber].HasSendRequest.Value = false;
                //Model.Shops[ShopNumber].SupplyAmountAll.Add(Model.Shops[ShopNumber].SupplyAmountLast);

                // TODO: пересчитать еще значения для магазина (мо, сигма, объем товара и т.д.)
                // TODO: вывести состояние модели
                //Model.TraceModel(DayOfSupply);
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

            }
        }
    }
}