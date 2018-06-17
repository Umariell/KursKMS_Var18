using CommonModel.Kernel;
using CommonModel.RandomStreamProducing;
using CommonModel.StatisticsCollecting;

namespace Model_Lab
{
    public partial class SmoModel : Model
    {
        /// <summary>
        /// Условие завершения прогона модели True - завершить прогон. По умолчанию false. 
        /// </summary>
        /// <param name="variantCount"></param>
        /// <param name="runCount"></param>
        /// <returns></returns>
        public override bool MustStopRun(int variantCount, int runCount)
        {
            return Day > M;
        }

        /// <summary>
        /// Установка метода перебора вариантов модели.
        /// </summary>
        /// <param name="variantCount"></param>
        /// <returns></returns>
        public override bool MustPerformNextVariant(int variantCount)
        {
            // Используем один вариант модели.
            return variantCount < 1;
        }

        /// <summary>
        /// Определение необходимости продолжить выполнение прогонов модели.
        /// </summary>
        /// <param name="variantCount"></param>
        /// <param name="runCount"></param>
        /// <returns>true - продолжить выполнение ПРОГОНОВ модели; Иначе - false.</returns>
        public override bool MustPerformNextRun(int variantCount, int runCount)
        {
            return runCount < 1;
        }

        /// <summary>
        /// Задание начального состояния модели для нового варианта модели
        /// </summary>
        /// <param name="variantCount"></param>
        public override void SetNextVariant(int variantCount)
        {
            #region Параметры модели

            VV = 100;
            TV = 70;

            PP = 10;
            PNP = 140;
            PPZ = 1440;

            TP = 2000;
            for (int i = 0; i < N; i++)
            {
                Shops[i].Mx = 20;
                Shops[i].Sigma = 4;
            }

            Day = 1;

            #endregion

            #region Установка параметров законов распределения

            int seed = 1;   //System.DateTime.Now.Second * System.DateTime.Now.Millisecond;

            ZR_TV_ValueGenerator1.Table.Add(0, 0.000);
            ZR_TV_ValueGenerator1.Table.Add(1, 0.500);
            ZR_TV_ValueGenerator1.Table.Add(2, 0.604);
            ZR_TV_ValueGenerator1.Table.Add(3, 0.710);
            ZR_TV_ValueGenerator1.Table.Add(4, 0.886);
            ZR_TV_ValueGenerator1.Table.Add(5, 0.900);
            ZR_TV_ValueGenerator1.Table.Add(6, 1.000);

            ZR_TV_ValueGenerator2.Table.Add(0, 0.180);
            ZR_TV_ValueGenerator2.Table.Add(1, 0.209);
            ZR_TV_ValueGenerator2.Table.Add(2, 0.401);
            ZR_TV_ValueGenerator2.Table.Add(3, 0.710);
            ZR_TV_ValueGenerator2.Table.Add(4, 0.800);
            ZR_TV_ValueGenerator2.Table.Add(5, 0.900);
            ZR_TV_ValueGenerator2.Table.Add(6, 1.000);

            // значения генераторов зависящие от Shops[i].Mx и Sigma
            NormalGenerator_VDS1.Mx = Shops[0].Mx;
            NormalGenerator_VDS1.Sigma = Shops[0].Sigma;
            NormalGenerator_VDS2.Mx = Shops[1].Mx;
            NormalGenerator_VDS2.Sigma = Shops[1].Sigma;

            (NormalGenerator_VDS1.BPN as GeneratedBaseRandomStream).Seed = 119 * seed;
            (NormalGenerator_VDS2.BPN as GeneratedBaseRandomStream).Seed = 197 * seed;
            (UniformGenerator_TVost1.BPN as GeneratedBaseRandomStream).Seed = 130 * seed;
            (UniformGenerator_TVost2.BPN as GeneratedBaseRandomStream).Seed = 237 * seed;
            #endregion
        }


        public override void StartModelling(int variantCount, int runCount)
        {
            #region Задание начальных параметров моделирования
            //Задание начальных значений модельных переменных и объектов
            for (int i = 0; i < N; i++)
            {
                Shops[i].ProductAmountCurrent.Value = VV;           // начальный объем товара в i-том магазине
                Shops[i].ProductDemandCurrent.Value = 0;            // начальный объем спроса на товар в i-том магазине
                Shops[i].ProductUnrealizedCurrent.Value = 0;        // начальный объем пролежанного товара в i-том магазине
                Shops[i].ProductUnmetDemandCurrent.Value = 0;       // начальный объем неудовлетворенного спроса на товар в i-том магазине
                Shops[i].HasSendRequest.Value = 0;                  // начальное состояние по наличию заявок
                Shops[i].RequestsTotalCountCurrent.Value = 0;       // начальное кол-во заявок

            }
            SVSTP.Value = 0;
            SVST.Value = 0;

            // Cброс сборщиков статистики

            foreach (var collector in GetObjectsByType<StatCollector>())
                collector.ResetCollector();

            //Печать заголовка строки состояния модели
            TraceModelHeader();
            //Печать заголовка строки состояния модели
            var DayNumber = 0;

            //Планирование начальных событий
            // И вывод СБС
            var k1Event = new K1()
            {
                DayNumber = DayNumber
            };
            PlanEvent(k1Event, DayNumber);
            Tracer.PlanEventTrace(k1Event, DayNumber + 1);
            var k3Event = new K3 { NumberOfWeek = 0 };
            PlanEvent(k3Event, 0);
            Tracer.PlanEventTrace(k3Event,
                                        1);
            Tracer.AnyTrace();
            Tracer.AnyTrace();
            TraceModel(DayNumber++);
            #endregion
        }
        /// <summary>
        /// Действия по окончанию прогона
        /// </summary>
        /// <param name="variantCount"></param>
        /// <param name="runCount"></param>
        public override void FinishModelling(int variantCount, int runCount)
        {
            Tracer.TraceOut("===============================================================================");
            Tracer.TraceOut("============ Статистические результаты моделирования ==========================");
            Tracer.TraceOut("===============================================================================");
            Tracer.AnyTrace("");
            Tracer.TraceOut("======= I. Сбор статистики по средним дневным потерям торговой системы ========");
            Tracer.AnyTrace("");
            //Tracer.AnyTrace("");
            Tracer.AnyTrace("1. Сбор статистики средней дневной потери по каждому магазину (руб)");
            Tracer.TraceOut("   средние дн. потери в первом магазине:  " + ((Shops[0].ProductUnrealizedAll.Value * PP / M) + (Shops[0].ProductUnmetDemandAll.Value * PNP / M) + (Shops[0].RequestsTotalCountAll.Value * PPZ / M)));
            Tracer.TraceOut("   средние дн. потери во втором магазине: " + ((Shops[1].ProductUnrealizedAll.Value * PP / M) + (Shops[1].ProductUnmetDemandAll.Value * PNP / M) + (Shops[1].RequestsTotalCountAll.Value * PPZ / M)));
            Tracer.AnyTrace("");
            //Tracer.AnyTrace("");
            Tracer.TraceOut("2. Сбор статистики по средним дневным потерям по всей системе (руб): SSDS = "
                + ((Shops[0].ProductUnrealizedAll.Value * PP / M)
                + (Shops[1].ProductUnrealizedAll.Value * PP / M)
                + (Shops[0].ProductUnmetDemandAll.Value * PNP / M)
                + (Shops[1].ProductUnmetDemandAll.Value * PNP / M)
                + (Shops[0].RequestsTotalCountAll.Value * PPZ / M)
                + (Shops[1].RequestsTotalCountAll.Value * PPZ / M)));
            Tracer.AnyTrace("");
            //Tracer.AnyTrace("");
            Tracer.TraceOut("3. Сбор статистики по средним дневным потерям по отдельным составляющим (руб): ");
            Tracer.AnyTrace("");
            Tracer.TraceOut("       в системе: -------------------------------------------------------------");
            Tracer.TraceOut("           средние потери от пролеживания товара:       " + ((Shops[0].ProductUnrealizedAll.Value * PP / M) + (Shops[1].ProductUnrealizedAll.Value * PP / M)));
            Tracer.TraceOut("           средние потери от неудовлетворенного спроса: " + ((Shops[0].ProductUnmetDemandAll.Value * PNP / M) + (Shops[1].ProductUnmetDemandAll.Value * PNP / M)));
            Tracer.TraceOut("           средние потери от подачи заявки:             " + ((Shops[0].RequestsTotalCountAll.Value * PPZ / M) + (Shops[1].RequestsTotalCountAll.Value * PPZ / M)));
            //Tracer.AnyTrace("");
            Tracer.TraceOut("       в первом магазине: -----------------------------------------------------");
            Tracer.TraceOut("           средние потери от пролеживания товара SDP_VP[0] = " + (Shops[0].ProductUnrealizedAll.Value * PP / M));
            Tracer.TraceOut("               МО:      " + (Variance_SDP_PP[0].Mx * PP));
            //Tracer.TraceOut("           макс:    " + (Max_SDP_PP[0].Stat * PP));//вместо минимакса Хохо "приказал" делать МО
            //Tracer.TraceOut("           мин:     " + (Min_SDP_PP[0].Stat * PP));
            Tracer.TraceOut("           средние потери от неудовлетворенного спроса SDP_NS[0] = " + (Shops[0].ProductUnmetDemandAll.Value * PNP / M));
            Tracer.TraceOut("               МО:      " + (Variance_SDP_PNP[0].Mx * PNP));
            //Tracer.TraceOut("           макс:    " + (Max_SDP_PNP[0].Stat * PNP));
            //Tracer.TraceOut("           мин:     " + (Min_SDP_PNP[0].Stat * PNP));
            Tracer.TraceOut("           средние потери от подачи заявки SDP_PZ[0] = " + (Shops[0].RequestsTotalCountAll.Value * PPZ / M));
            Tracer.TraceOut("               МО:      " + (Variance_SDP_PPZ[0].Mx * PPZ / M));
            //Tracer.TraceOut("           макс:    " + (Max_SDP_PPZ[0].Stat * PPZ / M));
            //Tracer.TraceOut("           мин:     " + (Min_SDP_PPZ[0].Stat * PPZ / M));
            //Tracer.AnyTrace("");
            Tracer.TraceOut("       во втором магазине: ----------------------------------------------------");
            Tracer.TraceOut("           средние потери от пролеживания товара SDP_VP[1] = " + (Shops[1].ProductUnrealizedAll.Value * PP / M));
            Tracer.TraceOut("               МО:      " + (Variance_SDP_PP[1].Mx * PP));
            //Tracer.TraceOut("           макс:    " + (Max_SDP_PP[1].Stat * PP));
            //Tracer.TraceOut("           мин:     " + (Min_SDP_PP[1].Stat * PP));
            Tracer.TraceOut("           средние потери от неудовлетворенного спроса SDP_NS[1] = " + (Shops[1].ProductUnmetDemandAll.Value * PNP / M));
            Tracer.TraceOut("               МО:      " + (Variance_SDP_PNP[1].Mx * PNP));
            //Tracer.TraceOut("           макс:    " + (Max_SDP_PNP[1].Stat * PNP));
            //Tracer.TraceOut("           мин:     " + (Min_SDP_PNP[1].Stat * PNP));
            Tracer.TraceOut("           средние потери от подачи заявки SDP_PZ[1] = " + (Shops[1].RequestsTotalCountAll.Value * PPZ / M));
            Tracer.TraceOut("               МО:      " + (Variance_SDP_PPZ[1].Mx * PPZ / M));
            //Tracer.TraceOut("           макс:    " + (Max_SDP_PPZ[1].Stat * PPZ / M));
            //Tracer.TraceOut("           мин:     " + (Min_SDP_PPZ[1].Stat * PPZ / M));
            Tracer.AnyTrace("");
            Tracer.AnyTrace("");
            Tracer.AnyTrace("");
            Tracer.TraceOut("======== II. Сбор статистики по суммарным потерям в торговой системе ==========");
            Tracer.AnyTrace("");
            Tracer.TraceOut(" МО суммарного объема поставок с оптового склада по всем магазинам: " + Variance_SVSTP.Mx);
        }


        /// <summary>
        /// Печать заголовка
        /// </summary>
        void TraceModelHeader()
        {
            Tracer.TraceOut("========================================================================");
            Tracer.TraceOut("======================= Запущена модель ================================");
            Tracer.TraceOut("========================================================================");
            //Tracer.AnyTrace("");
            Tracer.AnyTrace("Параметры модели:");
            //Tracer.AnyTrace("");
            Tracer.AnyTrace("Количество магазинов: " + N);
            //Tracer.AnyTrace("");
            Tracer.AnyTrace("МО дневного спроса в i-ом магазине: " + Shops[0].Mx);
            //Tracer.AnyTrace("");
            Tracer.AnyTrace("СКО дневного спроса в i-ом магазине: " + Shops[0].Sigma);
            //Tracer.AnyTrace("");
            Tracer.AnyTrace("Точка восстановления: " + TV);
            //Tracer.AnyTrace("");
            Tracer.AnyTrace("Объем восстановления: " + VV);
            //Tracer.AnyTrace("");
            Tracer.AnyTrace("Потери от хранения пролежанной продукции PP: " + PP);
            //Tracer.AnyTrace("");
            Tracer.AnyTrace("Потери от нереализованной прибыли PNP: " + PNP);
            //Tracer.AnyTrace("");
            Tracer.AnyTrace("Потери от подачи заявки на пополнение товарного запаса PPZ: " + PPZ);
            Tracer.AnyTrace("Фиксированный интервал времени TF: 7 дн.");
            Tracer.AnyTrace("Время прогона имитационной модели TP: " + M);
            Tracer.AnyTrace("");
            Tracer.AnyTrace("Начальное состояние модели:");
        }

        /// <summary>
        /// Печать строки состояния.
        /// </summary>
        void TraceModel(int dayNumber)
        {
            Tracer.AnyTrace("SVP[" + Shops[0].ProductUnrealizedCurrent.Value + "," + Shops[1].ProductUnrealizedCurrent.Value + "]",
                            "SVNS[" + Shops[0].ProductUnmetDemandCurrent.Value + "," + Shops[1].ProductUnmetDemandCurrent.Value + "]",
                            "SKZ[" + Shops[0].RequestsTotalCountCurrent.Value + "," + Shops[1].RequestsTotalCountCurrent.Value + "]",
                            "VTT[" + Shops[0].ProductAmountCurrent.Value + "," + Shops[1].ProductAmountCurrent.Value + "]",
                            "Flag[" + Shops[0].HasSendRequest.Value + "," + Shops[1].HasSendRequest.Value + "]");
            Tracer.AnyTrace("");
            Tracer.AnyTrace("");
        }
    }
}