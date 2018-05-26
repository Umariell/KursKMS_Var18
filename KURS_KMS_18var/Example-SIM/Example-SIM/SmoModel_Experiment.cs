using CommonModel.Kernel;
using CommonModel.RandomStreamProducing;
using CommonModel.StatisticsCollecting;
using System;

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
            return Time > TP;
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

        //Задание начального состояния модели для нового варианта модели
        public override void SetNextVariant(int variantCount)
        {
            #region Параметры модели

            TV = 60;
            VV = 100;
            PP = 3.50;
            PNP = 7.98;
            PPZ = 2.34;
            TF = 7;
            TP = 14400000;

            #endregion



            #region Установка параметров законов распределения

            int seed = 1;   //System.DateTime.Now.Second * System.DateTime.Now.Millisecond;

            (NormalGenerator_VDS1.BPN as GeneratedBaseRandomStream).Seed    = 54 * seed;
            (NormalGenerator_VDS2.BPN as GeneratedBaseRandomStream).Seed    = 89 * seed;
            (UniformGenerator_TVost1.BPN as GeneratedBaseRandomStream).Seed = 51 * seed;
            (UniformGenerator_TVost2.BPN as GeneratedBaseRandomStream).Seed = 16 * seed;

            #endregion
        }

        #region Задание начальных параметров моделирования

        public override void StartModelling(int variantCount, int runCount)
        {

            //Задание начальных значений модельных переменных и объектов
                                        
            for (int i=0; i<N; i++)
            {
                Shops[i].ProductAmountCurrent.Value = VV;   // начальный объем товара в i-том магазине
                Shops[i].ProductDemandCurrent.Value = 0;    // начальный объем спроса на товар в i-том магазине
                SVP[i].Value = 0;
                SVNS[i].Value = 0;
                SKZ[i].Value = 0;
                Flag[i].Value = false;
                
            }
            SVST.Value = 0;
           

            // Cброс сборщиков статистики

            foreach (var collector in GetObjectsByType<StatCollector>())
                collector.ResetCollector();

            //Печать заголовка строки состояния модели
            TraceModel();

            //Планирование начальных событий


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
            

            for (int i=0; i<N; i++)
            {

                Tracer.TraceOut("Средние дневные потери от пролеживания товара в  " + i + "- ом магазине: " + (SVP[i].Value * PP / Time));
                Tracer.TraceOut("Средние дневные потери от неудовлетворенного спроса в  " + i + "-ом магазине: " + (SVNS[i].Value * PNP / Time));
                Tracer.TraceOut("Средние дневные потери от подачи заявок в " + i + "-ом магазине: " + (SKZ[i].Value * PPZ / Time));

            }
            Tracer.TraceOut("Суммарные дневные потери торговой системы: " + ((SVP[0].Value  * PP  / Time) + (SVP[1].Value  * PP  / Time) +
                                                                             (SVNS[0].Value * PNP / Time) + (SVNS[1].Value * PNP / Time) +
                                                                             (SKZ[0].Value  * PPZ / Time) + (SKZ[1].Value  * PPZ / Time))) ;

        }


        //Печать строки состояния
        void TraceModel()
        {
            Tracer.TraceOut("VTT[0].Value: " + VTT[0].Value + "VTT[1].Value: " + VTT[1].Value);

            Tracer.TraceOut("SVP[0].Value: " + SVP[0].Value + "SVP[1].Value: " + SVP[1].Value);

            Tracer.TraceOut("SVNS[0].Value: " + SVNS[0].Value + "SVNS[1].Value: " + SVNS[1].Value);

            Tracer.TraceOut("SVST.Value: " + SVST.Value);

            Tracer.TraceOut("Flag[0].Value: " + Flag[0].Value + "Flag[1].Value: " + Flag[1].Value); // TОDO: как вывести дни? (см. описание переменной Flag)

            Tracer.TraceOut("SKZ[0].Value: " + SKZ[0].Value + "SKZ[1].Value: " + SKZ[1].Value);

            Tracer.TraceOut("==============================================================");
        }

    }
}