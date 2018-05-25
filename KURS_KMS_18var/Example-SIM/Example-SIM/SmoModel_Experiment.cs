using CommonModel.Kernel;
using CommonModel.RandomStreamProducing;
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
 

            #endregion


            #region Установка параметров законов распределения

            int seed = 1;   //System.DateTime.Now.Second * System.DateTime.Now.Millisecond;

            (NormalGenerator_VDS1.BPN as GeneratedBaseRandomStream).Seed    = 54 * seed;
            (NormalGenerator_VDS2.BPN as GeneratedBaseRandomStream).Seed    = 89 * seed;
            (UniformGenerator_TVost1.BPN as GeneratedBaseRandomStream).Seed = 51 * seed;
            (UniformGenerator_TVost2.BPN as GeneratedBaseRandomStream).Seed = 16 * seed;

            #endregion
        }

        public override void StartModelling(int variantCount, int runCount)
        {

            #region Задание начальных значений модельных переменных и объектов

                                                       
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
            #endregion

            #region Cброс сборщиков статистики



            #endregion

            //Печать заголовка строки состояния модели
            TraceModel();

            #region Планирование начальных событий

            // TODO: подсмотреть в аналогичный код на https://github.com/Talrandel/KMS/blob/master/Modelling.Lab1/SMOModel_Experiment.cs

            #endregion


        }

        //Действия по окончанию прогона
        public override void FinishModelling(int variantCount, int runCount)
        {
            Tracer.TraceOut("==============================================================");
            Tracer.TraceOut("============Статистические результаты моделирования===========");
            Tracer.TraceOut("==============================================================");
           
            Tracer.TraceOut("Время моделирования: " + Time);

            Tracer.TraceOut("Средние дневные потери от пролеживания товара в  1-ом магазине: " + (SVP[0].Value*PP / Time));
            Tracer.TraceOut("Средние дневные потери от пролеживания товара во 2-ом магазине: " + (SVP[1].Value*PP / Time));

            Tracer.TraceOut("Средние дневные потери от неудовлетворенного спроса в  1-ом магазине: " + (SVNS[0].Value * PNP / Time));
            Tracer.TraceOut("Средние дневные потери от неудовлетворенного спроса во 2-ом магазине: " + (SVNS[1].Value * PNP / Time));

            Tracer.TraceOut("Средние дневные потери от подачи заявок в  1-ом магазине: " + (SKZ[0].Value * PPZ / Time));
            Tracer.TraceOut("Средние дневные потери от подачи заявок во 2-ом магазине: " + (SKZ[1].Value * PPZ / Time));

            Tracer.TraceOut("Суммарные дневные потери торговой системы: " + (SDP_VP[0] + SDP_NS[0]+ SDP_VP[1] + SDP_NS[1]));
            /*
            * problem
            */

        }

        //Печать строки состояния
        void TraceModel()
        {
            /*
            * problem
            */
            // TODO: подсмотреть в аналогичный код на https://github.com/Talrandel/KMS/blob/master/Modelling.Lab1/SMOModel_Experiment.cs
        }

    }
}