using System;

namespace Model_Lab
{
    class Program
    {
        static void Main(string[] args)
        {
            //Создаем модель
            var smoModel = new SmoModel(null, "Модель СМО");

            try
            {
                //Запускаем модель
                smoModel.PERFORM();
            }
            catch (Exception e)
            {
                //выводим сообщение об ошибке, если есть
                smoModel.Tracer.TraceOut(e.Message);
            }

            //сохраняем трассировку в файл
            smoModel.Tracer.OutStream.Flush();
        }
    }
}