using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModel.StatisticsCollecting;
using CommonModel.RandomStreamProducing;
using CommonModel.Collections;
using CommonModel.Kernel;
using CommonModel.Serialization;
using System.Runtime.Serialization;

namespace Model_Lab
{
    public partial class SmoModel : Model
    {
        #region Параметры модели

        /// <summary>
        /// количество магазинов
        /// </summary>
        public const int N = 2;

        /// <summary>
        /// количество дней
        /// </summary>
        public const int M = 7;

        ///// <summary>
        ///// мат. ожидание дневного спроса в i-том магазине
        ///// </summary>
        //public double[]  MO_DS =  new double[N];

        ///// <summary>
        ///// СКО дневного спроса в i-том магазине
        ///// </summary>
        //public double[]  SKO_DS = new double[N];

        /// <summary>
        /// ряд распределения, двумерный массив, характеризующий закон распределения времени восстановления товарного запаса,
        /// элементы которого P[i, j] – вероятность, 
        /// с которой возникает подача заявки из i-го магазина через 
        /// j-ый день в ОС(i= 1, … N; j=1,2...7)
        /// </summary>
        public double[,] ZR_TV =  new double[N,M];


        //Во всех магазинах предполагаются одинаковыми следующие параметры:

        /// <summary>
        /// точка восстановления
        /// </summary>
        public int TV;

        /// <summary>
        /// объём восстановления
        /// </summary>
        public int VV;

        /// <summary>
        /// потери от хранения пролежанной продукции(руб/ед.т.)
        /// </summary>
        public double PP;

        /// <summary>
        /// потери от нереализуемой прибыли(руб/ед.т.)
        /// </summary>
        public double PNP;

        /// <summary>
        /// потери от подачи заявки на пополнение товарного запаса(руб/ед.т.)
        /// </summary>
        public double PPZ;

        /// <summary>
        /// временной интервал сбора статистики по суммарному объему спроса с оптового склада(дн.)
        /// </summary>
        public int TF;

        /// <summary>
        /// время прогона ИМ 
        /// </summary>
        public int TP;

        #endregion

        #region Переменные состояния модели

        /// <summary>
        ///  текущий объем товара в i-ом магазине
        /// </summary>
        public TIntVar [] VTT;

        /// <summary>
        /// суммарный объем пролежанного товара в i-ом магазине
        /// </summary>
        public TIntVar [] SVP;

        /// <summary>
        /// суммарный объем неудовлетворенного спроса в i-ом магазине
        /// </summary>
        public TIntVar [] SVNS; 

        /// <summary>
        /// идентификатор подачи очередной заявки на пополнение в i-ом магазине в день № t1 (true - заявка подана, false -заявка не подана)
        /// </summary>
        public TBoolVar[] Flag;

        /// <summary>
        /// суммарное количество поданных заявок на пополнение товара в i-ом магазине
        /// </summary>
        public TIntVar [] SKZ;

        /// <summary>
        /// текущий суммарный объем спроса на товар
        /// </summary>
        public TRealVar SVST;

        #endregion

        #region Дополнительные структуры

        /// <summary>
        /// Класс магазина.
        /// </summary>
        public class Shop
        {
            /// <summary>
            /// Номер магазина.
            /// </summary>
            public int Number { get; set; }

            /// <summary>
            /// Текущий объем товара в магазине.
            /// </summary>
            public TIntVar ProductAmountCurrent { get; set; } //TODO: переменная состояния(?) в чем отличие от VTT

            /// <summary>
            /// Список значений объема товара по дням.
            /// </summary>
            public List<TIntVar> ProductAmountAll { get; set; }


            /// <summary>
            /// Текущий объем спроса на товар магазина.
            /// </summary>
            public TIntVar ProductDemandCurrent { get; set; }

            /// <summary>
            /// Список значений объемов спроса на товар по дням.
            /// </summary>
            public List<TIntVar> ProductDemandAll { get; set; }


            /// <summary>
            /// Текущий объем неудовлетворенного спроса на товар магазина.
            /// </summary>
            public TIntVar ProductUnmetDemandCurrent { get; set; }

            /// <summary>
            /// Список значений объемов неудовлетворенного спроса на товар по дням.
            /// </summary>
            public List<TIntVar> ProductUnmetDemandAll { get; set; }


            /// <summary>
            /// Объем последней поставки.
            /// </summary>
            public TIntVar SupplyAmountLast { get; set; }

            /// <summary>
            /// Список значений объема поставок по дням.
            /// </summary>
            public List<TIntVar> SupplyAmountAll { get; set; }

            /// <summary>
            /// МО.
            /// </summary>
            public double Mx { get; set; }

            /// <summary>
            /// Сигма.
            /// </summary>
            public double Sigma { get; set; }
        }

        /// <summary>
        /// Массив магазинов.
        /// </summary>
        public Shop[] Shops { get; set; }

        /// <summary>
        /// Заявка на пополнение товара в магазине.
        /// </summary>
        public class RequestForReplenishment
        {
            /// <summary>
            /// Номер заявки.
            /// </summary>
            public int Num { get; set; }

            /// <summary>
            /// День фиксации заявки.
            /// </summary>
            public int TimeEnter;
        }
        #endregion

        #region Cборщики статистики

        /// <summary>
        /// средние дневные потери от пролеживания товара в i-том магазине
        /// </summary>
        public Variance<double> Variance_SDP_PP;
        public Min<double>[] Min_SDP_PP;
        public Max<double>[] Max_SDP_PP;

        /// <summary>
        /// средние дневные потери от неудовлетворенного спроса в i-том магазине
        /// </summary>
        public Variance<double> Variance_SDP_PNP;
        public Min<double>[] Min_SDP_PNP;
        public Max<double>[] Max_SDP_PNP;

        /// <summary>
        /// средние дневные потери от подачи заявок в i-том магазине
        /// </summary>
        public Variance<int> Variance_SDP_PPZ;
        public Min<double>[] Min_SDP_PPZ;
        public Max<double>[] Max_SDP_PPZ;

        /// <summary>
        /// суммарные средние дневные потери торговой системы
        /// </summary>
        public Variance<double> Variance_SSDS; // TODO: уточнить:нужен ли для него макс. и мин.?


        #endregion

        #region Генераторы ПСЧ

        /// <summary>
        /// генератор объема дневного спроса для первого магазина
        /// </summary>
        NormalStream NormalGenerator_VDS1;

        /// <summary>
        /// генератор объема дневного спроса для второго магазина
        /// </summary>
        NormalStream NormalGenerator_VDS2;

        //Генератор равномерного распределения. Генерируем величину от 0 до 1 и смотрим - какое событие выполнилось.
        //Например: в первый магазин будет заявка в 4 день с вероятностью 0.214, в 5 день - 0.786.
        //Генератор выдал 0.4. Это больше 0.214, так что считаем, что заявка пришла в 5 день.

        /// <summary>
        /// генератор времени восстановления товарного запаса для первого магазина
        /// </summary>
        UniformStream UniformGenerator_TVost1;

        /// <summary>
        /// генератор времени восстановления товарного запаса для второго магазина
        /// </summary>
        UniformStream UniformGenerator_TVost2;

        
        /// <summary>
        /// Ряд распределения для времени восстановления товарного запаса в первом магазине
        /// </summary>
        public DiscreteStream<double> ZR_TV_ValueGenerator1;

        /// <summary>
        /// Ряд распределения для времени восстановления товарного запаса во втором магазине
        /// </summary>
        public DiscreteStream<double> ZR_TV_ValueGenerator2;

        #endregion

        #region Инициализация объектов модели

        public SmoModel(Model parent, string name) : base(parent, name)
        {
            # region Инициализация массива магазинов
            Shops = new Shop[N];
            #endregion


            #region Инициализация переменных состояния модели
            for (int i = 0; i < N; i++)
            {
                VTT [i] = InitModelObject<TIntVar> ("Текущий объем товара в(во) " + i + "-ом магазине ");
                SVP [i] = InitModelObject<TIntVar> ("Суммарный объем пролежанного товара в(во) " + i + "-ом магазине ");
                SVNS[i] = InitModelObject<TIntVar> ("Суммарный объем неудовлетворенного спроса в(во) " + i + "-ом магазине ");
                Flag[i] = InitModelObject<TBoolVar>("Идентификатор подачи заявки в(во) " + i + "-ом магазине ");
                SKZ [i] = InitModelObject<TIntVar> ("Суммарное количество поданных заявок на пополнение товара  в(во) " + i + "-ом магазине ");
            }


            // Ряд распределения для времени восстановления товарного запаса в первом магазине
            ZR_TV_ValueGenerator1 = InitModelObject<DiscreteStream<double>>("Генератор 'Ряд распределения для времени восстановления товарного запаса в первом магазине': ");
            ZR_TV_ValueGenerator1.Table = new Dictionary<double, double>();
            ZR_TV_ValueGenerator1.Table.Add(0, 0.000);
            ZR_TV_ValueGenerator1.Table.Add(1, 0.000);
            ZR_TV_ValueGenerator1.Table.Add(2, 0.004);
            ZR_TV_ValueGenerator1.Table.Add(3, 0.110);
            ZR_TV_ValueGenerator1.Table.Add(4, 0.786);
            ZR_TV_ValueGenerator1.Table.Add(5, 0.100);
            ZR_TV_ValueGenerator1.Table.Add(6, 0.000);

            // Ряд распределения для времени восстановления товарного запаса во втором магазине
            ZR_TV_ValueGenerator2 = InitModelObject<DiscreteStream<double>>("Генератор 'Ряд распределения для времени восстановления товарного запаса во втором магазине': ");
            ZR_TV_ValueGenerator2.Table = new Dictionary<double, double>();
            ZR_TV_ValueGenerator2.Table.Add(0, 0.180);
            ZR_TV_ValueGenerator2.Table.Add(1, 0.609);
            ZR_TV_ValueGenerator2.Table.Add(2, 0.201);
            ZR_TV_ValueGenerator2.Table.Add(3, 0.010);
            ZR_TV_ValueGenerator2.Table.Add(4, 0.000);
            ZR_TV_ValueGenerator2.Table.Add(5, 0.000);
            ZR_TV_ValueGenerator2.Table.Add(6, 0.000);
        
            #endregion

            #region Инициализация генераторов потоков

            NormalGenerator_VDS1    = InitModelObject<NormalStream> ("генератор потока 'объем дневного спроса в первом магазине'");
            NormalGenerator_VDS2    = InitModelObject<NormalStream> ("генератор потока 'объем дневного спроса во втором магазине'");
            UniformGenerator_TVost1 = InitModelObject<UniformStream>("генератор потока 'время восстановления товарного запаса в первом магазине'");
            UniformGenerator_TVost2 = InitModelObject<UniformStream>("генератор потока 'время восстановления товарного запаса во втором магазине'");

            #endregion

            #region Инициализация сборщиков статистики и подключение их к переменным

            //хранение пролеживаемого товара
            Variance_SDP_PP = InitModelObject<Variance<double>>("Сборщик статистики: средние дневные потери от пролеживания товара в i-том магазине");
            Variance_SDP_PP.ConnectOnSet(PP);//TODO: что с этим (и еще в остальных двух вариантах) делать
            Max_SDP_PP = InitModelObjectArray<Max<double>>(2,"Максимум потерь от пролеживания товара в i-том магазине");//TODO: макс и мин для каждого верно?
            Min_SDP_PP = InitModelObjectArray<Min<double>>(2,"Минимум потерь от пролеживания товара в i-том магазине" );

            //нереализуемая прибыль от неудовлетворенного спроса
            Variance_SDP_PNP = InitModelObject<Variance<double>>("Сборщик статистики: средние дневные потери от неудовлетворенного спроса в i-том магазине");
            Variance_SDP_PNP.ConnectOnSet(PNP);
            Max_SDP_PNP = InitModelObjectArray<Max<double>>(2, "Максимум потерь от нереализуемой прибыли в i-том магазине");
            Min_SDP_PNP = InitModelObjectArray<Min<double>>(2, "Минимум потерь от нереализуемой прибыли в i-том магазине" );

            //потери от подачи заявок
            Variance_SDP_PPZ = InitModelObject<Variance<int>>("Сборщик статистики: средние дневные потери от подачи заявок в i-том магазине");
            Variance_SDP_PPZ.ConnectOnSet(PPZ);
            Max_SDP_PPZ = InitModelObjectArray<Max<double>>(2, "Максимум потерь от подачи заявок в i-том магазине");
            Min_SDP_PPZ = InitModelObjectArray<Min<double>>(2, "Минимум потерь от подачи заявок в i-том магазине" );

            // суммарные средние дневные потери торговой системы
            Variance_SSDS = InitModelObject<Variance<double>>("Сборщик статистики: суммарные средние дневные потери торговой системы");

            #endregion
        }

        #endregion
    }
}