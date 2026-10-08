using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace Monitel.Mal.Context.CIM16.Xml.GOSTSO
{
    public static class Forms
    {
        public static void CreateFormIgnoredObj(string text)
        {
            System.Windows.Forms.Application.EnableVisualStyles();

            // Создание формы
            using (var form = new Form())
            {
                int formHeight = 600;
                int formWidth = 800;

                form.Text = "Игнорированные объекты"; // Заголовок окна
                form.ClientSize = new Size(formWidth, formHeight); // Размер окна
                form.FormBorderStyle = FormBorderStyle.FixedSingle;
                form.MaximizeBox = false;
                form.StartPosition = FormStartPosition.CenterScreen; // Центрируем
                form.BackColor = Color.White; // Чистый белый фон
                form.Font = new Font("Segoe UI", 9);

                // Элемент TextBox для вывода текста
                var textBox = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    Width = formWidth - 20,
                    Height = formHeight - 60,
                    Location = new Point(10, 10),
                    ScrollBars = ScrollBars.Both,
                    WordWrap = true,
                    Font = new Font("Consolas", 10),
                    Padding = new Padding(10),
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.FromArgb(215, 215, 215), // Светло-серый фон
                    Text = text // Содержимое для отображения
                };

                // Кнопка для копирования содержимого в буфер обмена
                var copyButton = new Button
                {
                    Text = "Скопировать",
                    Width = formWidth - 20,
                    Height = 30,
                    Location = new Point(10, formHeight - 40),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(100, 100, 100), // Серый
                    FlatStyle = FlatStyle.Flat,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right
                };

                // Обработка события клика кнопки
                copyButton.Click += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        Clipboard.SetText(textBox.Text); // Копирует текст в буфер обмена
                        copyButton.Text = "Скопировано";
                        copyButton.BackColor = Color.FromArgb(10, 161, 88); // Зелёный
                        form.Refresh();
                        System.Threading.Thread.Sleep(1000);
                        copyButton.Text = "Скопировать";
                        copyButton.BackColor = Color.FromArgb(100, 100, 100);
                    }
                };

                // Добавляем компоненты на форму
                form.Controls.Add(copyButton);
                form.Controls.Add(textBox);


                // Запускаем форму
                System.Windows.Forms.Application.Run(form);
            }
        }
    }
}
