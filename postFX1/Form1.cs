using System;
using System.Diagnostics;
using System.Windows.Forms;
using NvAPIWrapper; // Пакет NvAPIWrapper.Net
using NvAPIWrapper.Display; // Тут живет класс Display

namespace postFX1
{
    public partial class Form1 : Form
    {
        // --- ИСПРАВЛЕНИЕ 1: Объявляем переменную здесь, чтобы её видели все методы ---
        private Display[] _displays;
        private Timer _hueTimer;
        public int nass;

        public Form1()
        {
            InitializeComponent();
            LoadMonitors();

            _hueTimer = new Timer();
            _hueTimer.Interval = 20; // Скорость смены (50 мс). Чем меньше, тем быстрее.
            _hueTimer.Tick += HueTimer_Tick;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            label1.Text = trackBar1.Value.ToString();

            var selectedDisplay = _displays[cmbMonitors.SelectedIndex];
            var vibranceControl = selectedDisplay.DigitalVibranceControl;
            int currentLevel = vibranceControl.CurrentLevel;

            label1.Text = currentLevel.ToString();
            textBox1.Text = currentLevel.ToString(); // Добавлено: синхронизация при загрузке
            nass = currentLevel;
            trackBar1.Value = nass;
        }

        private void HueTimer_Tick(object sender, EventArgs e)
        {
            // Проверка на всякий случай
            if (cmbMonitors.SelectedIndex < 0 || _displays == null) return;

            try
            {
                var selectedDisplay = _displays[cmbMonitors.SelectedIndex];

                var vibranceControl = selectedDisplay.HUEControl;

                if (vibranceControl.CurrentAngle >= 359)
                {
                    vibranceControl.CurrentAngle = 0;
                }
                else
                {
                    vibranceControl.CurrentAngle += 1;
                }
            }
            catch
            {
                // Если ошибка — выключаем таймер, чтобы не спамить ошибками
                _hueTimer.Stop();
                checkBox1.Checked = false;
            }
        }

        private void LoadMonitors()
        {
            try
            {
                NVIDIA.Initialize();

                // Получаем список дисплеев
                _displays = Display.GetDisplays();

                cmbMonitors.Items.Clear();

                if (_displays.Length == 0)
                {
                    MessageBox.Show("Мониторы NVIDIA не найдены (возможно, работает встройка Intel?).");
                    return;
                }

                foreach (var display in _displays)
                {
                    cmbMonitors.Items.Add(display.Name);
                }

                cmbMonitors.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Проверка, что монитор выбран и список не пуст
            if (cmbMonitors.SelectedIndex < 0 || _displays == null) return;

            // Получаем конкретный монитор из списка
            var selectedDisplay = _displays[cmbMonitors.SelectedIndex];

            try
            {
                var vibranceControl = selectedDisplay.DigitalVibranceControl;

                int currentLevel = vibranceControl.CurrentLevel;

                if (currentLevel == nass)
                {
                    vibranceControl.CurrentLevel = 50;
                }
                else
                {
                    vibranceControl.CurrentLevel = nass;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка переключения: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            //try
            //{
            //    var selectedDisplay = _displays[cmbMonitors.SelectedIndex];
            //    var vibranceControl = selectedDisplay.DigitalVibranceControl;
            //    var HUEControl = selectedDisplay.HUEControl;
            //    vibranceControl.CurrentLevel = 50;
            //    HUEControl.CurrentAngle = 0;
            //    NVIDIA.Unload();
            //}
            //catch { /* Игнорируем ошибки при закрытии */ }

            //base.OnFormClosing(e);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (cmbMonitors.SelectedIndex < 0) return;

            if (checkBox1.Checked)
            {
                _hueTimer.Start();
            }
            else
            {
                _hueTimer.Stop();

                try
                {
                    var selectedDisplay = _displays[cmbMonitors.SelectedIndex];
                    selectedDisplay.HUEControl.CurrentAngle = 0;
                }
                catch { }
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            nass = trackBar1.Value;
            label1.Text = trackBar1.Value.ToString();
            textBox1.Text = trackBar1.Value.ToString(); // Добавлено: обновление текста при скролле
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var selectedDisplay = _displays[cmbMonitors.SelectedIndex];
            var vibranceControl = selectedDisplay.DigitalVibranceControl;
            int currentLevel = vibranceControl.CurrentLevel;
            vibranceControl.CurrentLevel = 50;
            trackBar1.Value = 50;

            label1.Text = trackBar1.Value.ToString();
            textBox1.Text = trackBar1.Value.ToString(); // Добавлено: сброс текста в 50
        }

        // --- НОВАЯ ЛОГИКА TEXTBOX (вместо TextChanged) ---

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            // Применяем настройки только при нажатии Enter
            if (e.KeyCode == Keys.Enter)
            {
                if (int.TryParse(textBox1.Text, out int newValue))
                {
                    // Ограничиваем рамками ползунка
                    if (newValue < trackBar1.Minimum) newValue = trackBar1.Minimum;
                    if (newValue > trackBar1.Maximum) newValue = trackBar1.Maximum;

                    // Обновляем UI
                    trackBar1.Value = newValue;
                    nass = newValue;
                    label1.Text = newValue.ToString();
                    textBox1.Text = newValue.ToString();

                    // Применяем к дисплею
                    if (cmbMonitors.SelectedIndex >= 0 && _displays != null)
                    {
                        try
                        {
                            var selectedDisplay = _displays[cmbMonitors.SelectedIndex];
                            selectedDisplay.DigitalVibranceControl.CurrentLevel = newValue;
                        }
                        catch { }
                    }
                }
                e.SuppressKeyPress = true; // Убираем звук ошибки Windows
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Разрешаем вводить только цифры и стирать (Backspace)
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}