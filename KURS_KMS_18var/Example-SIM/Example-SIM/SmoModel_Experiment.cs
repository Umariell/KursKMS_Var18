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
            return Day > M + 1;
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
            // TODO: вся инициализация с цифрами только здесь
            #region Параметры модели

            TV = 30;
            VV = 40;
            PP = 4;
            //PNP = 7.98;
            //PPZ = 2.34;
            TP = 7;
            TF = TP * 30;

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

            // TODO: параметры генераторов!
            NormalGenerator_VDS1.Mx = 25;
            NormalGenerator_VDS1.Sigma = 4;
            NormalGenerator_VDS2.Mx = 21;
            NormalGenerator_VDS2.Sigma = 7;

            (NormalGenerator_VDS1.BPN as GeneratedBaseRandomStream).Seed = 54 * seed;
            (NormalGenerator_VDS2.BPN as GeneratedBaseRandomStream).Seed = 89 * seed;
            (UniformGenerator_TVost1.BPN as GeneratedBaseRandomStream).Seed = 51 * seed;
            (UniformGenerator_TVost2.BPN as GeneratedBaseRandomStream).Seed = 16 * seed;

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
                Shops[i].ProductLossRequestCurrent.Value = 0;            // 
                Shops[i].ProductUnrealizedCurrent.Value = 0;         // начальный объем пролежанного товара в i-том магазине
                Shops[i].ProductUnmetDemandCurrent.Value = 0;    // начальный объем неудовлетворенного спроса на товар в i-том магазине
                Shops[i].HasSendRequest.Value = false;
                Shops[i].RequestsTotalCount.Value = 0;

            }
            SVST.Value = 0;
            SVSTP.Value = 0;

            // Cброс сборщиков статистики

            foreach (var collector in GetObjectsByType<StatCollector>())
                collector.ResetCollector();

            //Печать заголовка строки состояния модели
            var DayNumber = 0;
            TraceModel(DayNumber);

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

        //Действия по окончанию прогона
        public override void FinishModelling(int variantCount, int runCount)
        {
            Tracer.AnyTrace("");
            Tracer.TraceOut("==============================================================");
            Tracer.TraceOut("============Статистические результаты моделирования===========");
            Tracer.TraceOut("==============================================================");
            Tracer.AnyTrace("");
            Tracer.TraceOut("Время моделирования: " + string.Format("{0:0.00}", Time));

            for (int i = 0; i < N; i++)
            {
                // TODO: вывести суммарные потери магазина, а не средние.
                Tracer.TraceOut("Средние дневные потери от пролеживания товара в  " + i + "- ом магазине: " + (Shops[i].ProductUnrealizedCurrent.Value * PP / M));
                Tracer.TraceOut("Средние дневные потери от неудовлетворенного спроса в  " + i + "-ом магазине: " + (Shops[i].ProductUnmetDemandCurrent.Value * PNP / M));
                //Tracer.TraceOut("Средние дневные потери от подачи заявок в " + i + "-ом магазине: " + (SKZ[i].Value * PPZ / M));
            }
            // TODO: вывести суммарные потери обоих магазинов

            Tracer.TraceOut("Суммарные дневные потери торговой системы: " + ((Shops[0].ProductUnrealizedCurrent.Value * PP / M) + (Shops[1].ProductUnrealizedCurrent.Value * PP / M)
                                                                           + (Shops[0].ProductUnmetDemandCurrent.Value * PNP / M) + (Shops[1].ProductUnmetDemandCurrent.Value * PNP / M)
                                                                           + (Shops[0].RequestsTotalCount.Value * PPZ / M) + (Shops[1].RequestsTotalCount.Value * PPZ / M)));
        }


        /// <summary>
        /// Печать строки состояния.
        /// </summary>
        void TraceModel(int dayNumber)
        {
            Tracer.TraceOut("==============================================================");
            Tracer.TraceOut("Номер дня: " + dayNumber);
            Tracer.TraceOut("Текущий объем товара в первом магазине: " + Shops[0].ProductAmountCurrent.Value + "; во втором магазине: " + Shops[1].ProductAmountCurrent.Value);
            Tracer.TraceOut("Текущий объем пролёжанного товара: " + Shops[0].ProductUnrealizedCurrent.Value + "; во втором магазине: " + Shops[1].ProductUnrealizedCurrent.Value);
            // TODO: спрос в магазине!
            Tracer.TraceOut("Суммарный объем неудовлетворенного спроса в первом магазине: " + Shops[0].ProductUnmetDemandCurrent.Value + "; во втором магазине: " + Shops[1].ProductUnmetDemandCurrent.Value);
            Tracer.TraceOut("Cуммарный объем спроса за день: " + SVST.Value);
            Tracer.TraceOut("Была ли подана заявка в первом магазине: " + Shops[0].HasSendRequest.Value + "; во втором магазине: " + Shops[1].HasSendRequest.Value);
            Tracer.TraceOut("Суммарное количество поданных заявок в первом магазине: " + Shops[0].RequestsTotalCount.Value + "; во втором магазине: " + Shops[1].RequestsTotalCount.Value);
            Tracer.TraceOut("==============================================================");
        }
    }
}