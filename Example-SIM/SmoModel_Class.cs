using System.Collections.Generic;
using CommonModel.StatisticsCollecting;
using CommonModel.RandomStreamProducing;
using CommonModel.Kernel;

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


        /// <summary>
        /// ряд распределения, двумерный массив, характеризующий закон распределения времени восстановления товарного запаса,
        /// элементы которого P[i, j] – вероятность, 
        /// с которой возникает подача заявки из i-го магазина через 
        /// j-ый день в ОС(i= 1, … N; j=1,2...7)
        /// </summary>
        public double[,] ZR_TV = new double[N, M];


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
        public int PP;

        /// <summary>
        /// потери от нереализуемой прибыли(руб/ед.т.)
        /// </summary>
        public int PNP;

        /// <summary>
        /// потери от подачи заявки на пополнение товарного запаса(руб/ед.т.)
        /// </summary>
        public int PPZ;

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
        /// Номер дня.
        /// </summary>
        public int Day { get; set; }

        /// <summary>
        /// суммарное количество поданных заявок на пополнение товара в i-ом магазине
        /// </summary>
        public int[] SKZ;

        /// <summary>
        /// текущий суммарный объем спроса на товар (за день)
        /// </summary>
        public TRealVar SVST;

        /// <summary>
        /// Cуммарный объем спроса на товар (за все время)
        /// </summary>
        public TRealVar SVSTP;

        /// <summary>
        /// Суммарный объем нереализованного товара
        /// </summary>
        public TIntVar SVP; 

        #endregion

        #region Класс магазина с переменными состояния

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
            public TIntVar ProductAmountCurrent { get; set; }


            /// <summary>
            /// Текущий объем спроса на товар магазина.
            /// </summary>
            public TIntVar ProductDemandCurrent { get; set; }


            /// <summary>
            /// Текущий объем неудовлетворенного спроса на товар магазина.
            /// </summary>
            public TIntVar ProductUnmetDemandCurrent { get; set; }


            /// <summary>
            /// Текущий объем пролёжанного (нереализованного) товара.
            /// </summary>
            public TRealVar ProductUnrealizedCurrent { get; set; }


            /// <summary>
            /// Текущий объем потерь от подачи заявки.
            /// </summary>
            public TRealVar ProductLossRequestCurrent { get; set; }


            /// <summary>
            /// Объем последней поставки.
            /// </summary>
            public TIntVar SupplyAmountLast { get; set; }

            /// <summary>
            /// идентификатор подачи очередной заявки на пополнение в магазине в день № t1 (true - заявка подана, false -заявка не подана)
            /// </summary>
            public TBoolVar HasSendRequest { get; set; }

            /// <summary>
            /// Количество заявок на пополнение товара.
            /// </summary>
            public TIntVar RequestsTotalCount { get; set; }

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
        public Variance<double>[] Variance_SDP_PP;
        public Min<double>[] Min_SDP_PP;
        public Max<double>[] Max_SDP_PP;

        /// <summary>
        /// средние дневные потери от неудовлетворенного спроса в i-том магазине
        /// </summary>
        public Variance<int>[] Variance_SDP_PNP;
        public Min<double>[] Min_SDP_PNP;
        public Max<double>[] Max_SDP_PNP;

        /// <summary>
        /// средние дневные потери от подачи заявок в i-том магазине
        /// </summary>
        public Variance<double>[] Variance_SDP_PPZ;
        public Min<double>[] Min_SDP_PPZ;
        public Max<double>[] Max_SDP_PPZ;

        /// <summary>
        /// суммарные средние дневные потери торговой системы
        /// </summary>
        public Variance<double> Variance_SSDS;
        public Min<double>[] Min_SSDS;
        public Max<double>[] Max_SSDS;


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

        /// <summary>
        /// генератор размера потерь от подачи заявки
        /// </summary>
        NormalStream NormalGenerator_PP_Loss;

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
                Shops[i] = new Shop();
                Shops[i].ProductAmountCurrent = InitModelObject<TIntVar>("Текущий объем товара в(во) " + i + "-ом магазине ");
                Shops[i].ProductDemandCurrent = InitModelObject<TIntVar>("Текущий объем спроса на товар в(во) " + i + "-ом магазине ");
                Shops[i].ProductLossRequestCurrent = InitModelObject<TRealVar>("Текущий объем потерь от подачи заявки в(во) " + i + "-ом магазине ");
                Shops[i].ProductUnrealizedCurrent = InitModelObject<TRealVar>("Текущий объем пролежанного товара в(во) " + i + "-ом магазине ");
                Shops[i].ProductUnmetDemandCurrent = InitModelObject<TIntVar>("Текущий объем неудовлетворенного спроса в(во) " + i + "-ом магазине ");
                Shops[i].HasSendRequest = InitModelObject<TBoolVar>("Идентификатор подачи заявки в(во) " + i + "-ом магазине ");
                Shops[i].RequestsTotalCount = InitModelObject<TIntVar>("Суммарное количество поданных заявок на пополнение товара  в(во) " + i + "-ом магазине ");
                Shops[i].SupplyAmountLast = InitModelObject<TIntVar>("Объем последней поставки в(во) " + i + "-ом магазине ");

            }

            SVST = InitModelObject<TRealVar>("текущий суммарный объем спроса на товар (за день)");
            SVSTP = InitModelObject<TRealVar>("Cуммарный объем спроса на товар (за все время)");

            // Ряд распределения для времени восстановления товарного запаса в первом магазине
            ZR_TV_ValueGenerator1 = InitModelObject<DiscreteStream<double>>("Генератор 'Ряд распределения для времени восстановления товарного запаса в первом магазине': ");
            ZR_TV_ValueGenerator1.Table = new Dictionary<double, double>();

            // Ряд распределения для времени восстановления товарного запаса во втором магазине
            ZR_TV_ValueGenerator2 = InitModelObject<DiscreteStream<double>>("Генератор 'Ряд распределения для времени восстановления товарного запаса во втором магазине': ");
            ZR_TV_ValueGenerator2.Table = new Dictionary<double, double>();

            #endregion

            #region Инициализация генераторов потоков

            NormalGenerator_VDS1 = InitModelObject<NormalStream>("генератор потока 'объем дневного спроса в первом магазине'");
            NormalGenerator_VDS2 = InitModelObject<NormalStream>("генератор потока 'объем дневного спроса во втором магазине'");
            UniformGenerator_TVost1 = InitModelObject<UniformStream>("генератор потока 'время восстановления товарного запаса в первом магазине'");
            UniformGenerator_TVost2 = InitModelObject<UniformStream>("генератор потока 'время восстановления товарного запаса во втором магазине'");
            NormalGenerator_PP_Loss = InitModelObject<NormalStream>("генератор потока 'объём потерь при подаче заявки на восстановление'");

            #endregion

            #region Инициализация сборщиков статистики и подключение их к переменным

            //хранение пролеживаемого товара
            Variance_SDP_PP = InitModelObjectArray<Variance<double>>(N, "Сборщик статистики: средние дневные потери от пролеживания товара в i-том магазине");
            Variance_SDP_PP[0].ConnectOnSet(Shops[0].ProductUnrealizedCurrent);
            Variance_SDP_PP[1].ConnectOnSet(Shops[1].ProductUnrealizedCurrent);
            Max_SDP_PP = InitModelObjectArray<Max<double>>(N, "Максимум потерь от пролеживания товара в i-том магазине");
            Min_SDP_PP = InitModelObjectArray<Min<double>>(N, "Минимум потерь от пролеживания товара в i-том магазине");

            //нереализуемая прибыль от неудовлетворенного спроса
            Variance_SDP_PNP = InitModelObjectArray<Variance<int>>(N, "Сборщик статистики: средние дневные потери от неудовлетворенного спроса в i-том магазине");
            Variance_SDP_PNP[0].ConnectOnSet(Shops[0].ProductUnmetDemandCurrent);
            Variance_SDP_PNP[1].ConnectOnSet(Shops[1].ProductUnmetDemandCurrent);
            Max_SDP_PNP = InitModelObjectArray<Max<double>>(N, "Максимум потерь от нереализуемой прибыли в i-том магазине");
            Min_SDP_PNP = InitModelObjectArray<Min<double>>(N, "Минимум потерь от нереализуемой прибыли в i-том магазине");

            //потери от подачи заявок
            Variance_SDP_PPZ = InitModelObjectArray<Variance<double>>(N, "Сборщик статистики: средние дневные потери от подачи заявок в i-том магазине");
            Variance_SDP_PPZ[0].ConnectOnSet(Shops[0].ProductLossRequestCurrent); 
            Variance_SDP_PPZ[1].ConnectOnSet(Shops[1].ProductLossRequestCurrent);
            Max_SDP_PPZ = InitModelObjectArray<Max<double>>(N, "Максимум потерь от подачи заявок в i-том магазине");
            Min_SDP_PPZ = InitModelObjectArray<Min<double>>(N, "Минимум потерь от подачи заявок в i-том магазине");

            // суммарные средние дневные потери торговой системы
            Variance_SSDS = InitModelObject<Variance<double>>("Сборщик статистики: суммарные средние дневные потери торговой системы");

            #endregion
        }

        #endregion
    }
}