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
            return Day > 31;
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
            TV = 30;

            PP = 10;
            PNP = 140;
            PPZ = 340;

            TP = 1000;
            //TF = TP * 30;
            for (int i=0; i<N; i++)
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
            NormalGenerator_VDS1.Mx         = Shops[0].Mx;
            NormalGenerator_VDS1.Sigma      = Shops[0].Sigma;
            NormalGenerator_VDS2.Mx         = Shops[1].Mx;
            NormalGenerator_VDS2.Sigma      = Shops[1].Sigma;



            (NormalGenerator_VDS1.BPN as GeneratedBaseRandomStream).Seed = 140 * seed;
            (NormalGenerator_VDS2.BPN as GeneratedBaseRandomStream).Seed = 287 * seed;
            (UniformGenerator_TVost1.BPN as GeneratedBaseRandomStream).Seed = 300 * seed;
            (UniformGenerator_TVost2.BPN as GeneratedBaseRandomStream).Seed = 589 * seed;

            #endregion
        }

        #region Задание начальных параметров моделирования

        public override void StartModelling(int variantCount, int runCount)
        {
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
            TraceModel(DayNumber++);

            //Планирование начальных событий
            var k1Event = new K1()
            {
                DayNumber = DayNumber
            };
            PlanEvent(k1Event, DayNumber);
            // Занесение в файл трассировки записи о запланированном событии
            Tracer.PlanEventTrace(k1Event, DayNumber);
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
            Tracer.TraceOut("======= I. Сбор статистики средней дневной потери торговой системы ============");
            Tracer.AnyTrace("");
            Tracer.AnyTrace("1. Сбор статистики средней дневной потери по каждому магазину (руб)");
            Tracer.TraceOut("   в первом магазине:  " + ((Shops[0].ProductUnrealizedAll.Value * PP / M) + (Shops[0].ProductUnmetDemandAll.Value * PNP / M) + (Shops[0].RequestsTotalCountAll.Value * PPZ / M)));
            Tracer.TraceOut("   во втором магазине: " + ((Shops[1].ProductUnrealizedAll.Value * PP / M) + (Shops[1].ProductUnmetDemandAll.Value * PNP / M) + (Shops[1].RequestsTotalCountAll.Value * PPZ / M)));
            Tracer.AnyTrace("");
            Tracer.TraceOut("2. Сбор статистики средней дневной потери по всей системе (руб): SSDS = "
                + ((Shops[0].ProductUnrealizedAll.Value * PP / M)
                + (Shops[1].ProductUnrealizedAll.Value * PP / M)
                + (Shops[0].ProductUnmetDemandAll.Value * PNP / M)
                + (Shops[1].ProductUnmetDemandAll.Value * PNP / M)
                + (Shops[0].RequestsTotalCountAll.Value * PPZ / M)
                + (Shops[1].RequestsTotalCountAll.Value * PPZ / M)));
            Tracer.AnyTrace("");
            Tracer.TraceOut("3. Сбор статистики средней дневной потери по отдельным составляющим (руб): ");
            Tracer.TraceOut("  в системе: -----------------------------------------------------");
            Tracer.TraceOut("       потери от пролеживания товара:       " + ((Shops[0].ProductUnrealizedAll.Value * PP / M)+ (Shops[1].ProductUnrealizedAll.Value * PP / M)));
            Tracer.TraceOut("       потери от неудовлетворенного спроса: " + ((Shops[0].ProductUnmetDemandAll.Value * PNP / M) + (Shops[1].ProductUnmetDemandAll.Value * PNP / M)));
            Tracer.TraceOut("       потери от подачи заявки:             " + ((Shops[0].RequestsTotalCountAll.Value * PPZ / M) + (Shops[1].RequestsTotalCountAll.Value * PPZ / M)));
            Tracer.TraceOut("  в первом магазине: ---------------------------------------------");
            Tracer.TraceOut("       потери от пролеживания товара SDP_VP[0] = " + ((Shops[0].ProductUnrealizedAll.Value * PP) / M));
            Tracer.TraceOut("           макс:    " + (Max_SDP_PP[0].Stat* PP));
            Tracer.TraceOut("           мин:     " + (Min_SDP_PP[0].Stat* PP));
            Tracer.TraceOut("       потери от неудовлетворенного спроса SDP_NS[0] = " + (Shops[0].ProductUnmetDemandAll.Value * PNP / M));
            Tracer.TraceOut("           макс:    " + (Max_SDP_PNP[0].Stat* PNP));
            Tracer.TraceOut("           мин:     " + (Min_SDP_PNP[0].Stat* PNP));
            Tracer.TraceOut("       потери от подачи заявки SDP_PZ[0] = " + (Shops[0].RequestsTotalCountAll.Value * PPZ / M));
            Tracer.TraceOut("           макс:    " + (Max_SDP_PPZ[0].Stat * PPZ));
            Tracer.TraceOut("           мин:     " + (Min_SDP_PPZ[0].Stat * PPZ));
            Tracer.TraceOut("  во втором магазине: ---------------------------------------------");
            Tracer.TraceOut("       потери от пролеживания товара SDP_VP[1] = " + (Shops[1].ProductUnrealizedAll.Value * PP / M));
            Tracer.TraceOut("           макс:    " + (Max_SDP_PP[1].Stat * PP));
            Tracer.TraceOut("           мин:     " + (Min_SDP_PP[1].Stat * PP));
            Tracer.TraceOut("       потери от неудовлетворенного спроса SDP_NS[1] = " + (Shops[1].ProductUnmetDemandAll.Value * PNP / M));
            Tracer.TraceOut("           макс:    " + (Max_SDP_PNP[1].Stat * PNP));
            Tracer.TraceOut("           мин:     " + (Min_SDP_PNP[1].Stat * PNP));
            Tracer.TraceOut("       потери от подачи заявки SDP_PZ[1] = " + (Shops[1].RequestsTotalCountAll.Value * PPZ / M));
            Tracer.TraceOut("           макс:    " + (Max_SDP_PPZ[1].Stat * PPZ));
            Tracer.TraceOut("           мин:     " + (Min_SDP_PPZ[1].Stat * PPZ));



            Tracer.AnyTrace("");
            Tracer.AnyTrace("");
            Tracer.TraceOut("======== II. Сбор статистики суммарной потери в торговой системе ==============");

            //Tracer.AnyTrace("Суммарный объем спроса на товар (ед.т.)");
            //Tracer.TraceOut("в первом магазине: " + (Shops[0].ProductDemandAll.Value) + "  во втором магазине: " + (Shops[1].ProductDemandAll.Value));
            //Tracer.TraceOut("в системе: " + ((Shops[0].ProductDemandAll.Value) + (Shops[1].ProductDemandAll.Value)));            
            Tracer.AnyTrace("");
            Tracer.TraceOut("SVSTP:"+SVSTP.Value);
            //Tracer.TraceOut("Max_SVSTP: " + (Max_SVSTP.Stat * SVST));
            //Tracer.TraceOut("Min_SVSTP: " + (Min_SVSTP.Stat * SVST));
            Tracer.TraceOut("кол-во заявок, поданных в первом магазине: " + Shops[0].RequestsTotalCountAll.Value);
            Tracer.TraceOut("кол-во заявок, поданных во втором магазине: " + Shops[1].RequestsTotalCountAll.Value);
            Tracer.TraceOut("ППЗ: " + PPZ);


        }
        ///// <summary>
        ///// Действия по окончанию прогона
        ///// </summary>
        ///// <param name="variantCount"></param>
        ///// <param name="runCount"></param>
        //public override void FinishModelling(int variantCount, int runCount)
        //{
        //    Tracer.AnyTrace("");
        //    Tracer.TraceOut("К3: Окончание фикс. интервала времени сбора статистики по объему поставок (" + M + " д.)");
        //    Tracer.TraceOut("===============================================================================");
        //    Tracer.TraceOut("============Статистические результаты моделирования============================");
        //    Tracer.TraceOut("===============================================================================");
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Средние дневные потери от пролеживания товара за день");
        //    Tracer.TraceOut("в первом магазине: " + (Shops[0].ProductUnrealizedCurrent.Value * PP / M) + "  во втором магазине: " + (Shops[1].ProductUnrealizedCurrent.Value * PP / M));
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Максимальные дневные потери от пролеживания товара за день");
        //    Tracer.TraceOut("в первом магазине: " + Max_SDP_PP[0].Stat + "  во втором магазине: " + Max_SDP_PP[1].Stat);
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Минимальные дневные потери от пролеживания товара за день");
        //    Tracer.TraceOut("в первом магазине: " + Min_SDP_PP[0].Stat + "  во втором магазине: " + Min_SDP_PP[1].Stat);
        //    Tracer.AnyTrace("---");
        //    Tracer.AnyTrace("Средние дневные потери от неудовлетворенного спроса за день");
        //    Tracer.TraceOut("в первом магазине: " + (Shops[0].ProductUnmetDemandCurrent.Value * PNP / M) + "  во втором магазине: " + (Shops[1].ProductUnmetDemandCurrent.Value * PNP / M));
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Максимальные дневные потери от пролеживания товара за день");
        //    Tracer.TraceOut("в первом магазине: " + Max_SDP_PNP[0].Stat + "  во втором магазине: " + Max_SDP_PNP[1].Stat);
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Минимальные потери от нереализованной прибыли");
        //    Tracer.TraceOut("в первом магазине: " + Min_SDP_PNP[0].Stat + "  во втором магазине: " + Min_SDP_PNP[1].Stat);
        //    Tracer.AnyTrace("---");
        //    Tracer.AnyTrace("Средние дневные потери от подачи заявок за день");
        //    Tracer.TraceOut("в первом магазине: " + (Shops[0].ProductLossRequestCurrent.Value * PPZ / M) + "  во втором магазине: " + (Shops[1].ProductLossRequestCurrent.Value * PPZ / M));
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Максимальные дневные потери от пот подачи заявок за день");
        //    Tracer.TraceOut("в первом магазине: " + Max_SDP_PPZ[0].Stat + "  во втором магазине: " + Max_SDP_PPZ[1].Stat);
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Минимальные потери от от подачи заявок за день");
        //    Tracer.TraceOut("в первом магазине: " + Min_SDP_PPZ[0].Stat + "  во втором магазине: " + Min_SDP_PPZ[1].Stat);
        //    Tracer.AnyTrace("");
        //    Tracer.TraceOut("=============================================================================");
        //    Tracer.AnyTrace("");
        //    Tracer.TraceOut("Суммарные средние дневные потери торговой системы за день: "
        //        + ((Shops[0].ProductUnrealizedAll.Value * PP / M)
        //        + (Shops[1].ProductUnrealizedAll.Value * PP / M)
        //        + (Shops[0].ProductUnmetDemandAll.Value * PNP / M)
        //        + (Shops[1].ProductUnmetDemandAll.Value * PNP / M)
        //        + (Shops[0].RequestsTotalCount.Value * PPZ / M)
        //        + (Shops[1].RequestsTotalCount.Value * PPZ / M)));
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Суммарный объем спроса на товар");
        //    Tracer.TraceOut("в первом магазине: " + (Shops[0].ProductDemandAll.Value) + "  во втором магазине: " + (Shops[1].ProductDemandAll.Value));
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Суммарные количество пролежанного товара");
        //    Tracer.TraceOut("в первом магазине: " + (Shops[0].ProductUnrealizedAll.Value) + "  во втором магазине: " + (Shops[1].ProductUnrealizedAll.Value));
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Суммарные дневные потери от пролеживания товара");
        //    Tracer.TraceOut("в первом магазине: " + (Shops[0].ProductUnrealizedAll.Value * PP) + "  во втором магазине: " + (Shops[1].ProductUnrealizedAll.Value * PP));
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Суммарные дневные потери от неудовлетворенного спроса");
        //    Tracer.TraceOut("в первом магазине: " + (Shops[0].ProductUnmetDemandAll.Value * PNP) + "  во втором магазине: " + (Shops[1].ProductUnmetDemandAll.Value * PNP));
        //    Tracer.AnyTrace("");
        //    Tracer.AnyTrace("Суммарные дневные потери от подачи заявок");
        //    Tracer.TraceOut("в первом магазине: " + (Shops[0].RequestsTotalCount.Value * PPZ) + "  во втором магазине: " + (Shops[1].RequestsTotalCount.Value * PPZ));
        //    Tracer.AnyTrace("");

        //    Tracer.TraceOut("Суммарные дневные потери торговой системы: " + ((Shops[0].ProductUnrealizedCurrent.Value * PP) + (Shops[1].ProductUnrealizedCurrent.Value * PP)
        //                                                               + (Shops[0].ProductUnmetDemandCurrent.Value * PNP) + (Shops[1].ProductUnmetDemandCurrent.Value * PNP)
        //                                                               + (Shops[0].RequestsTotalCount.Value * PPZ) + (Shops[1].RequestsTotalCount.Value * PPZ)));


        //}

        /// <summary>
        /// Печать заголовка
        /// </summary>
        void TraceModelHeader()
        {
            Tracer.TraceOut("========================================================================");
            Tracer.TraceOut("======================= Запущена модель ================================");
            Tracer.TraceOut("========================================================================");
            Tracer.AnyTrace("");
            Tracer.AnyTrace("Параметры модели:");
            Tracer.AnyTrace("");
            Tracer.AnyTrace("Количество магазинов: " + N);
            Tracer.AnyTrace("");
            Tracer.AnyTrace("МО дневного спроса в i-ом магазине: " + Shops[0].Mx);
            Tracer.AnyTrace("");
            Tracer.AnyTrace("СКО дневного спроса в i-ом магазине: " + Shops[0].Sigma);
            Tracer.AnyTrace("");
            Tracer.AnyTrace("Точка восстановления: " + TV);
            Tracer.AnyTrace("");
            Tracer.AnyTrace("Объем восстановления: " + VV);
            Tracer.AnyTrace("");
            Tracer.AnyTrace("Начальное состояние модели:");
           
            //    Tracer.AnyTrace("SVP[0] = " + Shops[0].ProductUnrealizedAll.Value,
            //                    "SVNS[0] = " + Shops[0].ProductUnmetDemandAll.Value,
            //                    "SKZ[0] = " + Shops[0].RequestsTotalCountAll.Value,
            //                    "VTT[0] = " + Shops[0].ProductAmountCurrent.Value,
            //                    "Flag[0] = " + Shops[0].HasSendRequest.Value);
            //Tracer.AnyTrace("SVP[1] = " + Shops[1].ProductUnrealizedAll.Value,
            //                "SVNS[1] = " + Shops[1].ProductUnmetDemandAll.Value,
            //                "SKZ[1] = " + Shops[1].RequestsTotalCountAll.Value,
            //                "VTT[1] = " + Shops[1].ProductAmountCurrent.Value,
            //                "Flag[1] = " + Shops[1].HasSendRequest.Value,
            //                "  SVST = " + SVST.Value);
            //Tracer.AnyTrace("");
            //Tracer.TraceOut("==============================================================");
            Tracer.AnyTrace("");
           
        }

        /// <summary>
        /// Печать строки состояния.
        /// </summary>
        void TraceModel(int dayNumber)
        {
            //Tracer.TraceOut("===============================================================================");

            Tracer.AnyTrace("SVP[0,"  + Shops[0].ProductUnrealizedCurrent.Value + "]",
                            "SVNS[0," + Shops[0].ProductUnmetDemandCurrent.Value + "]",
                            "SKZ[0,"  + Shops[0].RequestsTotalCountCurrent.Value + "]",
                            "VTT[0,"  + Shops[0].ProductAmountCurrent.Value + "]",
                            "Flag[0," + Shops[0].HasSendRequest.Value + "]");

            Tracer.AnyTrace("SVP[1,"  + Shops[1].ProductUnrealizedCurrent.Value + "]",
                            "SVNS[1," + Shops[1].ProductUnmetDemandCurrent.Value + "]",
                            "SKZ[1,"  + Shops[1].RequestsTotalCountCurrent.Value + "]",
                            "VTT[1,"  + Shops[1].ProductAmountCurrent.Value + "]",
                            "Flag[1," + Shops[1].HasSendRequest.Value + "]",
                            "SVST = "+ SVST.Value);
            Tracer.AnyTrace("");
            Tracer.AnyTrace("");
        }

        /// <summary>
        /// Печать трассировки о том, когда была послана заявка и какой объем товара стал после пополнения
        /// </summary>
        void TraceRequest(int dayOfSupply, int shopNumber)
        {
            //Tracer.TraceOut("===============================================================================");
            //Tracer.TraceOut("К2: Пополнение товарного запаса в магазине " + (shopNumber + 1));
            //Tracer.TraceOut("Номер дня подачи заявки: " + dayOfSupply);
            //Tracer.TraceOut("Текущий объем товара в магазине: " + Shops[shopNumber].ProductAmountCurrent.Value);
            //Tracer.TraceOut("Объем поставки (восстановления) товара в магазин: " + VV);

            //Tracer.TraceOut("===============================================================================");
        }
    }
}