
namespace RDPServer
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
            this.components = new System.ComponentModel.Container();
            this.checkBoxControl = new System.Windows.Forms.CheckBox();
            this.checkBoxAuto = new System.Windows.Forms.CheckBox();
            this.status = new System.Windows.Forms.Label();
            this.listAudio = new System.Windows.Forms.ListBox();
            this.FPSControl = new System.Windows.Forms.NumericUpDown();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.FPSControl)).BeginInit();
            this.SuspendLayout();
            // 
            // checkBoxControl
            // 
            this.checkBoxControl.AutoSize = true;
            this.checkBoxControl.Location = new System.Drawing.Point(13, 13);
            this.checkBoxControl.Name = "checkBoxControl";
            this.checkBoxControl.Size = new System.Drawing.Size(98, 17);
            this.checkBoxControl.TabIndex = 0;
            this.checkBoxControl.Text = "Remote control";
            this.checkBoxControl.UseVisualStyleBackColor = true;
            // 
            // checkBoxAuto
            // 
            this.checkBoxAuto.AutoSize = true;
            this.checkBoxAuto.Checked = true;
            this.checkBoxAuto.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxAuto.Location = new System.Drawing.Point(13, 37);
            this.checkBoxAuto.Name = "checkBoxAuto";
            this.checkBoxAuto.Size = new System.Drawing.Size(136, 17);
            this.checkBoxAuto.TabIndex = 1;
            this.checkBoxAuto.Text = "Allow auto-connections";
            this.checkBoxAuto.UseVisualStyleBackColor = true;
            // 
            // status
            // 
            this.status.AutoSize = true;
            this.status.Location = new System.Drawing.Point(13, 61);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(76, 13);
            this.status.TabIndex = 2;
            this.status.Text = "not connected";
            // 
            // listAudio
            // 
            this.listAudio.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listAudio.FormattingEnabled = true;
            this.listAudio.Location = new System.Drawing.Point(169, 13);
            this.listAudio.Name = "listAudio";
            this.listAudio.Size = new System.Drawing.Size(120, 95);
            this.listAudio.TabIndex = 3;
            // 
            // FPSControl
            // 
            this.FPSControl.Location = new System.Drawing.Point(16, 82);
            this.FPSControl.Name = "FPSControl";
            this.FPSControl.Size = new System.Drawing.Size(120, 20);
            this.FPSControl.TabIndex = 4;
            this.FPSControl.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(296, 114);
            this.Controls.Add(this.FPSControl);
            this.Controls.Add(this.listAudio);
            this.Controls.Add(this.status);
            this.Controls.Add(this.checkBoxAuto);
            this.Controls.Add(this.checkBoxControl);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.FPSControl)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxControl;
        private System.Windows.Forms.CheckBox checkBoxAuto;
        private System.Windows.Forms.Label status;
        private System.Windows.Forms.ListBox listAudio;
        private System.Windows.Forms.NumericUpDown FPSControl;
        private System.Windows.Forms.Timer timer1;
    }
}

