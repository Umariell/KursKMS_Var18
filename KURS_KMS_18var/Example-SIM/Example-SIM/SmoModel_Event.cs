using CommonModel.Kernel;

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
                Model.TraceModel();

                for (int i = 0; i < N; i++)
                {
                    // Записать объем товара за i-тый день в соответствующий список у магазина
                    Model.Shops[i].ProductAmountAll.Add(Model.Shops[i].ProductAmountCurrent);

                    // Записать объем спроса за i-тый день в соответствующий список у магазина
                    Model.Shops[i].ProductDemandAll.Add(Model.Shops[i].ProductDemandCurrent);//TODO: текщий обхем спроса надо моделировать. Как?

                    // Пересчитать текущее количество товара в магазине при необходимости
                    Model.Shops[i].ProductAmountCurrent.Value = Model.VV; //TODO: надо ли?

                    // Если спрос превысил текущий объем товара в магазине
                    if (Model.Shops[i].ProductAmountCurrent.Value < Model.Shops[i].ProductDemandCurrent.Value)
                    {
                        // Планирование события 1 - появление заявки в СМО
                        // Задание интервала времени (для каждого магазина) через который наступит событие
                        var deltaTime1 = Model.ZR_TV_ValueGenerator1.GenerateValue();
                        var deltaTime2 = Model.ZR_TV_ValueGenerator2.GenerateValue();

                        // TODO: если разница спроса и объема товара меньше 0, то планируем событие К2
                        if (Model.Shops[i].ProductAmountCurrent.Value>Model.Shops[i].ProductDemandCurrent.Value)
                        {
                            
                            var k2Event = new K2()
                            {
                                ShopNumber = i
                            };
                        }
                        Model.PlanEvent(k2Event, deltaTime1, deltaTime2);
                        // Занесение в файл трассировки записи о запланированном событии
                        Model.Tracer.PlanEventTrace(k1Event, Model.Time + deltaTime1, Model.Time + deltaTime2, k1Event.Id, Model.RequestsIncomming[0].Value, Model.RequestsIncomming[0].Value, Model.RequestsLost[0].Value);
                        Model.TraceModel();
                    }

                }
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

            #endregion

            // Алгоритм обработки события            
            protected override void HandleEvent(ModelEventArgs args)
            {

                // Запланировать поставку товара в магазин с номером ShopNumber
                Model.Shops[ShopNumber].SupplyAmountLast.Value = Model.VV;
                Model.Shops[ShopNumber].SupplyAmountAll.Add(Model.Shops[ShopNumber].SupplyAmountLast);
                
                // TODO: пересчитать еще значения для магазина (мо, сигма, объем товара и т.д.)
                // TODO: вывести состояние модели

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