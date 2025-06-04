using System.Drawing;
using System.Windows.Forms;

namespace JavScraper.App
{
    partial class ImageCropForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            dropPanel = new Panel();
            dropLabel = new Label();
            selectButton = new Button();
            cropButton = new Button();
            //previewBox = new PictureBox();
            dropPanel.SuspendLayout();
            //((System.ComponentModel.ISupportInitialize)previewBox).BeginInit();
            SuspendLayout();
            // 
            // dropPanel
            // 
            dropPanel.Controls.Add(dropLabel);
            dropPanel.Location = new Point(106, 122);
            dropPanel.Name = "dropPanel";
            dropPanel.Size = new Size(200, 100);
            dropPanel.TabIndex = 0;
            // 
            // dropLabel
            // 
            dropLabel.Location = new Point(31, 24);
            dropLabel.Name = "dropLabel";
            dropLabel.Size = new Size(100, 23);
            dropLabel.TabIndex = 0;
            // 
            // selectButton
            // 
            selectButton.Location = new Point(308, 65);
            selectButton.Name = "selectButton";
            selectButton.Size = new Size(75, 23);
            selectButton.TabIndex = 1;
            selectButton.Click += SelectButton_Click;
            // 
            // cropButton
            // 
            cropButton.Location = new Point(484, 169);
            cropButton.Name = "cropButton";
            cropButton.Size = new Size(75, 23);
            cropButton.TabIndex = 2;
            cropButton.Click += CropButton_Click;
            //// 
            //// previewBox
            //// 
            //previewBox.Location = new Point(475, 314);
            //previewBox.Name = "previewBox";
            //previewBox.Size = new Size(100, 50);
            //previewBox.TabIndex = 3;
            //previewBox.TabStop = false;
            // 
            // ImageCropForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 561);
            Controls.Add(dropPanel);
            Controls.Add(selectButton);
            Controls.Add(cropButton);
            //Controls.Add(previewBox);
            Name = "ImageCropForm";
            Text = "ImageCropForm";
            dropPanel.ResumeLayout(false);
            //((System.ComponentModel.ISupportInitialize)previewBox).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel dropPanel;
        private Label dropLabel;
        private Button selectButton;
        private Button cropButton;
        //private PictureBox previewBox;
    }
}