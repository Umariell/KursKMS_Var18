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
        /// Количество магазинов
        /// </summary>
        public const int N = 2;

        /// <summary>
        /// Количество дней
        /// </summary>
        public const int M = 28;

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
        /// потери от нереализованной прибыли(руб/ед.т.)
        /// </summary>
        public int PNP;

        /// <summary>
        /// потери от подачи заявки на пополнение товарного запаса(руб/ед.т.)
        /// </summary>
        public int PPZ;

        /// <summary>
        /// временной интервал сбора статистики по суммарному объему спроса с оптового склада(дн.)
        /// </summary>
        public int TF = M;

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
        /// Текущий суммарный объем поставок товара с оптового склада.
        /// </summary>
        public TRealVar SVST;

        /// <summary>
        /// Cуммарный объем поставок товар (за все время)
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
            /// Суммарный объем спроса на товар магазина.
            /// </summary>
            public TIntVar ProductDemandAll { get; set; }

            /// <summary>
            /// Текущий объем неудовлетворенного спроса на товар магазина.
            /// </summary>
            public TRealVar ProductUnmetDemandCurrent { get; set; }

            /// <summary>
            /// Суммарный объем неудовлетворенного спроса на товар магазина.
            /// </summary>
            public TRealVar ProductUnmetDemandAll { get; set; }

            /// <summary>
            /// Текущий объем пролёжанного (нереализованного) товара.
            /// </summary>
            public TRealVar ProductUnrealizedCurrent { get; set; }

            /// <summary>
            /// Суммарный объем пролёжанного (нереализованного) товара.
            /// </summary>
            public TRealVar ProductUnrealizedAll { get; set; }

            /// <summary>
            /// идентификатор подачи очередной заявки на пополнение в магазине в день № t1 (1 - заявка подана, 0 -заявка не подана)
            /// </summary>
            public TIntVar HasSendRequest { get; set; }

            /// <summary>
            /// Количество заявок на пополнение товара.
            /// </summary>
            public TRealVar RequestsTotalCountCurrent { get; set; }

            /// <summary>
            /// Суммарное количество заявок на пополнение товара.
            /// </summary>
            public TRealVar RequestsTotalCountAll { get; set; }

            /// <summary>
            /// МО.
            /// </summary>
            public double Mx { get; set; }

            /// <summary>
            /// Сигма.
            /// </summary>
            public double Sigma { get; set; }
        }
        #region Класс магазина с переменными состояния

        /// <summary>
        /// Массив магазинов.
        /// </summary>
        public Shop[] Shops { get; set; }
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
        public Variance<double>[] Variance_SDP_PNP;
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

        /// <summary>
        /// суммарный объем текущего спроса на товар с оптового скалада за очередной фикс. интервал времени 
        /// </summary>
        public Variance<double> Variance_SVSTP;
        public Min<double> Min_SVSTP;
        public Max<double> Max_SVSTP;

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
            #region Инициализация переменных состояния модели
            Shops = new Shop[N];
            for (int i = 0; i < N; i++)
            {
                Shops[i] = new Shop();
                Shops[i].ProductAmountCurrent = InitModelObject<TIntVar>("Текущий объем товара в(во) " + i + "-ом магазине ");
                Shops[i].ProductDemandCurrent = InitModelObject<TIntVar>("Текущий объем спроса на товар в(во) " + i + "-ом магазине ");
                Shops[i].ProductUnmetDemandCurrent = InitModelObject<TRealVar>("Текущий объем неудовлетворенного спроса в(во) " + i + "-ом магазине ");
                Shops[i].ProductUnrealizedCurrent = InitModelObject<TRealVar>("Текущий объем пролежанного товара в(во) " + i + "-ом магазине ");
                Shops[i].HasSendRequest = InitModelObject<TIntVar>("Идентификатор подачи заявки в(во) " + i + "-ом магазине ");
                Shops[i].RequestsTotalCountCurrent = InitModelObject<TRealVar>("Текцщее количество поданных заявок на пополнение товара  в(во) " + i + "-ом магазине ");
                Shops[i].RequestsTotalCountAll = InitModelObject<TRealVar>("Суммарное количество поданных заявок на пополнение товара  в(во) " + i + "-ом магазине ");
                Shops[i].ProductDemandAll = InitModelObject<TIntVar>("Суммарный объем спроса на товар в(во) " + i + "-ом магазине ");
                Shops[i].ProductUnmetDemandAll = InitModelObject<TRealVar>("Суммарный объем неудовлетворенного спроса в(во) " + i + "-ом магазине ");
                Shops[i].ProductUnrealizedAll = InitModelObject<TRealVar>("Суммарный объем пролежанного товара в(во) " + i + "-ом магазине ");
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

            #endregion

            #region Инициализация сборщиков статистики и подключение их к переменным

            //хранение пролеживаемого товара
            Variance_SDP_PP = InitModelObjectArray<Variance<double>>(N, "Сборщик статистики: средние дневные потери от пролеживания товара в i-том магазине");
            Variance_SDP_PP[0].ConnectOnSet(Shops[0].ProductUnrealizedCurrent);
            Variance_SDP_PP[1].ConnectOnSet(Shops[1].ProductUnrealizedCurrent);
            Max_SDP_PP = InitModelObjectArray<Max<double>>(N, "Максимум потерь от пролеживания товара в i-том магазине");
            Max_SDP_PP[0].ConnectOnSet(Shops[0].ProductUnrealizedCurrent);
            Max_SDP_PP[1].ConnectOnSet(Shops[1].ProductUnrealizedCurrent);
            Min_SDP_PP = InitModelObjectArray<Min<double>>(N, "Минимум потерь от пролеживания товара в i-том магазине");
            Min_SDP_PP[0].ConnectOnSet(Shops[0].ProductUnrealizedCurrent);
            Min_SDP_PP[1].ConnectOnSet(Shops[1].ProductUnrealizedCurrent);

            //нереализованная прибыль от неудовлетворенного спроса
            Variance_SDP_PNP = InitModelObjectArray<Variance<double>>(N, "Сборщик статистики: средние дневные потери от неудовлетворенного спроса в i-том магазине");
            Variance_SDP_PNP[0].ConnectOnSet(Shops[0].ProductUnmetDemandCurrent);
            Variance_SDP_PNP[1].ConnectOnSet(Shops[1].ProductUnmetDemandCurrent);
            Max_SDP_PNP = InitModelObjectArray<Max<double>>(N, "Максимум потерь от нереализованной прибыли в i-том магазине");
            Max_SDP_PNP[0].ConnectOnSet(Shops[0].ProductUnmetDemandCurrent);
            Max_SDP_PNP[1].ConnectOnSet(Shops[1].ProductUnmetDemandCurrent);
            Min_SDP_PNP = InitModelObjectArray<Min<double>>(N, "Минимум потерь от нереализованной прибыли в i-том магазине");
            Min_SDP_PNP[0].ConnectOnSet(Shops[0].ProductUnmetDemandCurrent);
            Min_SDP_PNP[1].ConnectOnSet(Shops[1].ProductUnmetDemandCurrent);

            //потери от подачи заявок
            Variance_SDP_PPZ = InitModelObjectArray<Variance<double>>(N, "Сборщик статистики: средние дневные потери от подачи заявок в i-том магазине");
            Variance_SDP_PPZ[0].ConnectOnSet(Shops[0].RequestsTotalCountCurrent);
            Variance_SDP_PPZ[1].ConnectOnSet(Shops[1].RequestsTotalCountCurrent);
            Max_SDP_PPZ = InitModelObjectArray<Max<double>>(N, "Максимум потерь от подачи заявок в i-том магазине");
            Max_SDP_PPZ[0].ConnectOnSet(Shops[0].RequestsTotalCountCurrent);
            Max_SDP_PPZ[1].ConnectOnSet(Shops[1].RequestsTotalCountCurrent);
            Min_SDP_PPZ = InitModelObjectArray<Min<double>>(N, "Минимум потерь от подачи заявок в i-том магазине");
            Min_SDP_PPZ[0].ConnectOnSet(Shops[0].RequestsTotalCountCurrent);
            Min_SDP_PPZ[1].ConnectOnSet(Shops[1].RequestsTotalCountCurrent);

            // суммарные средние дневные потери торговой системы
            Variance_SSDS = InitModelObject<Variance<double>>("Сборщик статистики: суммарные средние дневные потери торговой системы");

            // SVSTP
            Variance_SVSTP = InitModelObject<Variance<double>>("Сборщик статистики: МО объема поставок со склада в магазины.");
            Variance_SVSTP.ConnectOnSet(SVSTP);
            Max_SVSTP = InitModelObject<Max<double>>("Максимум объема поставок со склада в магазины.");
            Max_SVSTP.ConnectOnSet(SVSTP);
            Min_SVSTP = InitModelObject<Min<double>>("Минимум объема поставок со склада в магазины.");
            Min_SVSTP.ConnectOnSet(SVSTP);
            #endregion
        }
        #endregion
    }
    #endregion
}