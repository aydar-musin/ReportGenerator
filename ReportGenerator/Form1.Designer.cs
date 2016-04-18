namespace ReportGenerator
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.panelButtons = new System.Windows.Forms.Panel();
            this.StateLabel = new System.Windows.Forms.Label();
            this.StartStopButton = new System.Windows.Forms.Button();
            this.SettingsBox = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.ErrorsEmailTextBox = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.OrdersEmailPasstextBox = new System.Windows.Forms.TextBox();
            this.OrdersEmailtextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.IntervalNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.CookieTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.LogTextBox = new System.Windows.Forms.TextBox();
            this.ManualGroupBox = new System.Windows.Forms.GroupBox();
            this.TestTextBox = new System.Windows.Forms.TextBox();
            this.TestButton = new System.Windows.Forms.Button();
            this.ProxyCheckBox = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.ProxyTestButton = new System.Windows.Forms.Button();
            this.panelButtons.SuspendLayout();
            this.SettingsBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.IntervalNumericUpDown)).BeginInit();
            this.ManualGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelButtons
            // 
            this.panelButtons.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelButtons.Controls.Add(this.StateLabel);
            this.panelButtons.Controls.Add(this.StartStopButton);
            this.panelButtons.Location = new System.Drawing.Point(12, 12);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(468, 35);
            this.panelButtons.TabIndex = 0;
            // 
            // StateLabel
            // 
            this.StateLabel.AutoSize = true;
            this.StateLabel.Location = new System.Drawing.Point(84, 11);
            this.StateLabel.Name = "StateLabel";
            this.StateLabel.Size = new System.Drawing.Size(16, 13);
            this.StateLabel.TabIndex = 1;
            this.StateLabel.Text = "...";
            // 
            // StartStopButton
            // 
            this.StartStopButton.Location = new System.Drawing.Point(3, 7);
            this.StartStopButton.Name = "StartStopButton";
            this.StartStopButton.Size = new System.Drawing.Size(75, 21);
            this.StartStopButton.TabIndex = 0;
            this.StartStopButton.Text = "Start";
            this.StartStopButton.UseVisualStyleBackColor = true;
            this.StartStopButton.Click += new System.EventHandler(this.StartStopButton_Click);
            // 
            // SettingsBox
            // 
            this.SettingsBox.Controls.Add(this.ProxyTestButton);
            this.SettingsBox.Controls.Add(this.ProxyCheckBox);
            this.SettingsBox.Controls.Add(this.ManualGroupBox);
            this.SettingsBox.Controls.Add(this.label5);
            this.SettingsBox.Controls.Add(this.ErrorsEmailTextBox);
            this.SettingsBox.Controls.Add(this.label4);
            this.SettingsBox.Controls.Add(this.label3);
            this.SettingsBox.Controls.Add(this.OrdersEmailPasstextBox);
            this.SettingsBox.Controls.Add(this.OrdersEmailtextBox);
            this.SettingsBox.Controls.Add(this.label2);
            this.SettingsBox.Controls.Add(this.IntervalNumericUpDown);
            this.SettingsBox.Controls.Add(this.CookieTextBox);
            this.SettingsBox.Controls.Add(this.label1);
            this.SettingsBox.Location = new System.Drawing.Point(12, 53);
            this.SettingsBox.Name = "SettingsBox";
            this.SettingsBox.Size = new System.Drawing.Size(468, 301);
            this.SettingsBox.TabIndex = 1;
            this.SettingsBox.TabStop = false;
            this.SettingsBox.Text = "Настройки";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 95);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(116, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Email для сообщений:";
            // 
            // ErrorsEmailTextBox
            // 
            this.ErrorsEmailTextBox.Location = new System.Drawing.Point(128, 92);
            this.ErrorsEmailTextBox.Name = "ErrorsEmailTextBox";
            this.ErrorsEmailTextBox.Size = new System.Drawing.Size(120, 20);
            this.ErrorsEmailTextBox.TabIndex = 10;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(254, 69);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Пароль:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 69);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Email заказов:";
            // 
            // OrdersEmailPasstextBox
            // 
            this.OrdersEmailPasstextBox.Location = new System.Drawing.Point(308, 66);
            this.OrdersEmailPasstextBox.Name = "OrdersEmailPasstextBox";
            this.OrdersEmailPasstextBox.Size = new System.Drawing.Size(154, 20);
            this.OrdersEmailPasstextBox.TabIndex = 7;
            // 
            // OrdersEmailtextBox
            // 
            this.OrdersEmailtextBox.Location = new System.Drawing.Point(88, 66);
            this.OrdersEmailtextBox.Name = "OrdersEmailtextBox";
            this.OrdersEmailtextBox.Size = new System.Drawing.Size(160, 20);
            this.OrdersEmailtextBox.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(157, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Интервал обновления (мин) : ";
            // 
            // IntervalNumericUpDown
            // 
            this.IntervalNumericUpDown.Location = new System.Drawing.Point(169, 14);
            this.IntervalNumericUpDown.Name = "IntervalNumericUpDown";
            this.IntervalNumericUpDown.Size = new System.Drawing.Size(120, 20);
            this.IntervalNumericUpDown.TabIndex = 3;
            this.IntervalNumericUpDown.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // CookieTextBox
            // 
            this.CookieTextBox.Location = new System.Drawing.Point(49, 40);
            this.CookieTextBox.Name = "CookieTextBox";
            this.CookieTextBox.Size = new System.Drawing.Size(413, 20);
            this.CookieTextBox.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Cookie: ";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // LogTextBox
            // 
            this.LogTextBox.Location = new System.Drawing.Point(486, 12);
            this.LogTextBox.Multiline = true;
            this.LogTextBox.Name = "LogTextBox";
            this.LogTextBox.ReadOnly = true;
            this.LogTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.LogTextBox.Size = new System.Drawing.Size(305, 342);
            this.LogTextBox.TabIndex = 2;
            // 
            // ManualGroupBox
            // 
            this.ManualGroupBox.Controls.Add(this.label6);
            this.ManualGroupBox.Controls.Add(this.TestButton);
            this.ManualGroupBox.Controls.Add(this.TestTextBox);
            this.ManualGroupBox.Location = new System.Drawing.Point(9, 165);
            this.ManualGroupBox.Name = "ManualGroupBox";
            this.ManualGroupBox.Size = new System.Drawing.Size(453, 130);
            this.ManualGroupBox.TabIndex = 12;
            this.ManualGroupBox.TabStop = false;
            this.ManualGroupBox.Text = "Ручной режим";
            // 
            // TestTextBox
            // 
            this.TestTextBox.Location = new System.Drawing.Point(79, 29);
            this.TestTextBox.Name = "TestTextBox";
            this.TestTextBox.Size = new System.Drawing.Size(115, 20);
            this.TestTextBox.TabIndex = 0;
            // 
            // TestButton
            // 
            this.TestButton.Location = new System.Drawing.Point(200, 29);
            this.TestButton.Name = "TestButton";
            this.TestButton.Size = new System.Drawing.Size(55, 20);
            this.TestButton.TabIndex = 1;
            this.TestButton.Text = "OK";
            this.TestButton.UseVisualStyleBackColor = true;
            this.TestButton.Click += new System.EventHandler(this.TestButton_Click);
            // 
            // ProxyCheckBox
            // 
            this.ProxyCheckBox.AutoSize = true;
            this.ProxyCheckBox.Checked = true;
            this.ProxyCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ProxyCheckBox.Location = new System.Drawing.Point(9, 126);
            this.ProxyCheckBox.Name = "ProxyCheckBox";
            this.ProxyCheckBox.Size = new System.Drawing.Size(138, 17);
            this.ProxyCheckBox.TabIndex = 13;
            this.ProxyCheckBox.Text = "Использовать прокси";
            this.ProxyCheckBox.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 32);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(68, 13);
            this.label6.TabIndex = 14;
            this.label6.Text = "ОГРН/ИНН:";
            // 
            // ProxyTestButton
            // 
            this.ProxyTestButton.Location = new System.Drawing.Point(153, 122);
            this.ProxyTestButton.Name = "ProxyTestButton";
            this.ProxyTestButton.Size = new System.Drawing.Size(125, 23);
            this.ProxyTestButton.TabIndex = 14;
            this.ProxyTestButton.Text = "Проверить прокси";
            this.ProxyTestButton.UseVisualStyleBackColor = true;
            this.ProxyTestButton.Click += new System.EventHandler(this.ProxyTestButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(799, 366);
            this.Controls.Add(this.LogTextBox);
            this.Controls.Add(this.SettingsBox);
            this.Controls.Add(this.panelButtons);
            this.Name = "Form1";
            this.Text = "Form1";
            this.panelButtons.ResumeLayout(false);
            this.panelButtons.PerformLayout();
            this.SettingsBox.ResumeLayout(false);
            this.SettingsBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.IntervalNumericUpDown)).EndInit();
            this.ManualGroupBox.ResumeLayout(false);
            this.ManualGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button StartStopButton;
        private System.Windows.Forms.Label StateLabel;
        private System.Windows.Forms.GroupBox SettingsBox;
        private System.Windows.Forms.TextBox LogTextBox;
        private System.Windows.Forms.TextBox CookieTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown IntervalNumericUpDown;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox ErrorsEmailTextBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox OrdersEmailPasstextBox;
        private System.Windows.Forms.TextBox OrdersEmailtextBox;
        private System.Windows.Forms.CheckBox ProxyCheckBox;
        private System.Windows.Forms.GroupBox ManualGroupBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button TestButton;
        private System.Windows.Forms.TextBox TestTextBox;
        private System.Windows.Forms.Button ProxyTestButton;
    }
}

