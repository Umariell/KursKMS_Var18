using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;
using CommonModel.Kernel;
using CommonModel.Collections;
using CommonModel.Serialization;
using Model_Lab;

namespace VisualPss
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Threading.Thread modelThread;

        /// <summary>
        /// Конструктор главного окна программы визуализации
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            ///Инициализация объекта пользовательской модели
            SmoModel model = new SmoModel(null, "Многоуровневая модель");
            ///

            ///Передача объекта пользовательской модели в объект визуализации
            ModelTreeControl.SimModel = model;
            ///
        }

        private void LoadModelButton_Click_1(object sender, RoutedEventArgs e)
        {
            if (ModelTreeControl.SimModel != null && ModelTreeControl.SimModel.ModelState == ModelStateEnum.Run)
            {
                MessageBoxResult ans = MessageBox.Show("Невозможно загрузить модель, так другая модель находится в процессе прогона. Остановить модель?", "Ошибка при загрузке модели", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (ans == MessageBoxResult.Yes)
                    ModelTreeControl.SimModel.StopModel(true, false);
                else
                    return;
            }

            if (ModelTreeControl.SimModel.ModelState == ModelStateEnum.Paused)
            {
                MessageBoxResult ans = MessageBox.Show("Невозможно загрузить модель, так другая модель находится в режиме паузы!", "Ошибка при загрузке модели", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog();

            dialog.DefaultExt = "xml";
            dialog.Filter = "XML-файл сериализованной модели | *.xml";
            dialog.Title = "Загрузить сериализованную модель";

            Nullable<bool> result = dialog.ShowDialog();
            if (result == true)
            {
                ModelTreeControl.SimModel = (Model)ModelSerializer.ModelLoadFromXml(dialog.FileName, ModelTreeControl.SimModel);
                if (ModelTreeControl.SimModel != null)
                {
                    MessageBox.Show("Файл модели \n" + dialog.FileName + "\n успешно загружен", "Модель загружена", MessageBoxButton.OK, MessageBoxImage.Information);

                }
                else
                    MessageBox.Show("Ошибка при загрузке модели!", "Модель не была загружена", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ModelTreeControl.SimModel == null)
            {
                MessageBox.Show("Модель не была загружена!", "Невозможно сохранить модель", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ModelTreeControl.SimModel.ModelState == ModelStateEnum.Run)
            {
                MessageBoxResult ans = MessageBox.Show("Невозможно сохранить модель, так как она находится в процессе прогона. Остановить модель?", "Ошибка при сохранении модели", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (ans == MessageBoxResult.Yes)
                {
                    if (ModelTreeControl.SimModel.ModelState == ModelStateEnum.Run)
                        ModelTreeControl.SimModel.StopModel(true, false);
                }
                else
                    return;
            }

            /*
            if (ModelTreeControl.SimModel.ModelState == ModelStateEnum.Paused)
            {
                MessageBoxResult ans = MessageBox.Show("Невозможно сохранить модель, так как она находится в режиме паузы!", "Ошибка при сохранении модели", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            */

            SaveFileDialog dialog = new SaveFileDialog();

            dialog.DefaultExt = "xml";
            dialog.Filter = "XML-файл сериализованной модели | *.xml";
            dialog.Title = "Сохранить модель";
            dialog.FileName = ModelTreeControl.SimModel.Name;
            dialog.AddExtension = false;

            Nullable<bool> result = dialog.ShowDialog();
            if (result == true)
            {
                if (ModelSerializer.ModelSaveAsXml(ModelTreeControl.SimModel, dialog.FileName))
                    MessageBox.Show("Модель успешно сохранена в \n" + dialog.FileName, "Модель сохранена", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Ошибка при сохранении модели!", "Модель не была сохранена", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void StartModelButton_Click(object sender, RoutedEventArgs e)
        {
            int delayTime = Int32.Parse(DelayText.Text);

            if (delayTime > 100 || delayTime < 0)
            {
                MessageBox.Show("Скорость прогона должна быть задана в пределе от 0 до 100!", "Неверно задана задержка скорости прогона", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (modelThread == null || ModelTreeControl.SimModel.ModelState == ModelStateEnum.Stopped
                || ModelTreeControl.SimModel.ModelState == ModelStateEnum.EmergencyStopped)
            {
                //modelThread = new System.Threading.Thread(ModelTreeControl.SimModel.PERFORM);

                modelThread = new System.Threading.Thread(() => ModelTreeControl.SimModel.PERFORM(delayTime));
                modelThread.Start();
                ModelTreeControl.SimModel.AfterEndModelling += new ModelRunEventHandler(SimModel_AfterEndModelling);
            }
            else
            {
                if (ModelTreeControl.SimModel.ModelState == ModelStateEnum.Run)
                    MessageBox.Show("Модель уже запущена!", "Невозможно запустить модель", MessageBoxButton.OK, MessageBoxImage.Error);
                if (ModelTreeControl.SimModel.ModelState == ModelStateEnum.Paused)
                {
                    //modelThread = new System.Threading.Thread(ModelTreeControl.SimModel.ContinueModelPerforme);
                    modelThread = new System.Threading.Thread(() => ModelTreeControl.SimModel.ContinueModelPerforme(delayTime));
                    modelThread.Start();
                }
            }
        }

        private void PauseModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (modelThread == null
                || ModelTreeControl.SimModel.ModelState == ModelStateEnum.Stopped
                || ModelTreeControl.SimModel.ModelState == ModelStateEnum.EmergencyStopped)
            {
                MessageBox.Show("Модель не запущена на исполнение!", "Невозможно приостановить модель", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (ModelTreeControl.SimModel.ModelState == ModelStateEnum.Paused)
            {
                MessageBox.Show("Модель уже приостановлена!", "Невозможно приостановить модель", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                ModelTreeControl.SimModel.PauseModel();
            }
        }

        private void StopModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (modelThread == null
                || ModelTreeControl.SimModel.ModelState == ModelStateEnum.Stopped
                || ModelTreeControl.SimModel.ModelState == ModelStateEnum.EmergencyStopped)
            {
                MessageBox.Show("Модель не была запущена на исполнение!", "Невозможно остановить модель", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (ModelTreeControl.SimModel.ModelState == ModelStateEnum.Paused)
            {
                MessageBox.Show("Модель приостановлена!", "Невозможно приостановить модель", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                ModelTreeControl.SimModel.StopModel(true, false);
                ModelTreeControl.SimModel.AfterEndModelling -= SimModel_AfterEndModelling;
            }
        }

        void SimModel_AfterEndModelling(object sender, ModelRunArgs args)
        {
            MessageBox.Show("Прогон модели завершен!");
            ModelTreeControl.SimModel.AfterEndModelling -= SimModel_AfterEndModelling;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ModelTreeControl.SimModel != null && ModelTreeControl.SimModel.ModelState == ModelStateEnum.Run)
            {
                MessageBoxResult ans = MessageBox.Show("Прогон модели не был завершен. Вы уверены, что хотите выйти?", "Модель запущена на исполнение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (ans == MessageBoxResult.Yes)
                {
                    ModelTreeControl.SimModel.StopModel(true, true);
                    e.Cancel = false;
                }
                else
                    e.Cancel = true;
            }
        }

        private bool CheckDelayInput(string text, out int result)
        {
            if (Int32.TryParse(text, out result))
                return true;
            else
                return false;
        }

        private void DelayText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int result;
            if (!CheckDelayInput(e.Text, out result))
            {
                e.Handled = true;
            }
        }

        private void ModelTreeControl_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
