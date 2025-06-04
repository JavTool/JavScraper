using System.Drawing;
using System.Windows.Forms;

namespace JavScraper.App
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            tabControlMain = new TabControl();
            tabPageMain = new TabPage();
            button8 = new Button();
            button7 = new Button();
            button6 = new Button();
            button5 = new Button();
            button4 = new Button();
            label8 = new Label();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            richTextBox1 = new RichTextBox();
            textBoxNamingPreviewMultiple = new TextBox();
            textBoxNamingRuleMultiple = new TextBox();
            textBoxNamingPreview = new TextBox();
            textBoxNamingRule = new TextBox();
            txtFailedOutputDir = new TextBox();
            txtSuccessfulOutputDir = new TextBox();
            txtInputDir = new TextBox();
            btnTest = new Button();
            btnStop = new Button();
            btnStart = new Button();
            chkGenerateNFO = new CheckBox();
            chkDownGallery = new CheckBox();
            chkDownCover = new CheckBox();
            tabPageOrganizingMode = new TabPage();
            tabPageFixMetadata = new TabPage();
            label11 = new Label();
            richTextBox2 = new RichTextBox();
            button2 = new Button();
            label10 = new Label();
            textBox1 = new TextBox();
            tabPageCropImage = new TabPage();
            mainTablePanel = new TableLayoutPanel();
            controlPanel = new TableLayoutPanel();
            labelCropMode = new Label();
            comboBoxCropMode = new ComboBox();
            label9 = new Label();
            comboBoxCoverType = new ComboBox();
            previewPanel = new TableLayoutPanel();
            pictureBoxBackdrop = new PictureBox();
            pictureBoxFanart = new PictureBox();
            pictureBoxPoster = new PictureBox();
            pictureBoxThumb = new PictureBox();
            buttonPanel = new TableLayoutPanel();
            button3 = new Button();
            button1 = new Button();
            labelCoverType = new Label();
            selectButton = new Button();
            cropButton = new Button();
            topPanel = new Panel();
            bottomPanel = new Panel();
            previewBox = new PictureBox();
            dropPanel = new Panel();
            dropLabel = new Label();
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            toolToolStripMenuItem = new ToolStripMenuItem();
            optionToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            labelPoster = new Label();
            labelThumb = new Label();
            labelBackdrop = new Label();
            labelFanart = new Label();
            tabControlMain.SuspendLayout();
            tabPageMain.SuspendLayout();
            tabPageFixMetadata.SuspendLayout();
            tabPageCropImage.SuspendLayout();
            mainTablePanel.SuspendLayout();
            controlPanel.SuspendLayout();
            previewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxBackdrop).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxFanart).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPoster).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxThumb).BeginInit();
            buttonPanel.SuspendLayout();
            topPanel.SuspendLayout();
            bottomPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)previewBox).BeginInit();
            dropPanel.SuspendLayout();
            menuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // tabControlMain
            // 
            tabControlMain.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControlMain.Controls.Add(tabPageMain);
            tabControlMain.Controls.Add(tabPageOrganizingMode);
            tabControlMain.Controls.Add(tabPageFixMetadata);
            tabControlMain.Controls.Add(tabPageCropImage);
            tabControlMain.Location = new Point(2, 28);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(944, 510);
            tabControlMain.TabIndex = 0;
            // 
            // tabPageMain
            // 
            tabPageMain.Controls.Add(button8);
            tabPageMain.Controls.Add(button7);
            tabPageMain.Controls.Add(button6);
            tabPageMain.Controls.Add(button5);
            tabPageMain.Controls.Add(button4);
            tabPageMain.Controls.Add(label8);
            tabPageMain.Controls.Add(label7);
            tabPageMain.Controls.Add(label6);
            tabPageMain.Controls.Add(label5);
            tabPageMain.Controls.Add(label4);
            tabPageMain.Controls.Add(label3);
            tabPageMain.Controls.Add(label2);
            tabPageMain.Controls.Add(label1);
            tabPageMain.Controls.Add(richTextBox1);
            tabPageMain.Controls.Add(textBoxNamingPreviewMultiple);
            tabPageMain.Controls.Add(textBoxNamingRuleMultiple);
            tabPageMain.Controls.Add(textBoxNamingPreview);
            tabPageMain.Controls.Add(textBoxNamingRule);
            tabPageMain.Controls.Add(txtFailedOutputDir);
            tabPageMain.Controls.Add(txtSuccessfulOutputDir);
            tabPageMain.Controls.Add(txtInputDir);
            tabPageMain.Controls.Add(btnTest);
            tabPageMain.Controls.Add(btnStop);
            tabPageMain.Controls.Add(btnStart);
            tabPageMain.Controls.Add(chkGenerateNFO);
            tabPageMain.Controls.Add(chkDownGallery);
            tabPageMain.Controls.Add(chkDownCover);
            tabPageMain.Location = new Point(4, 26);
            tabPageMain.Name = "tabPageMain";
            tabPageMain.Padding = new Padding(3);
            tabPageMain.Size = new Size(936, 480);
            tabPageMain.TabIndex = 0;
            tabPageMain.Text = "标准模式";
            tabPageMain.UseVisualStyleBackColor = true;
            // 
            // button8
            // 
            button8.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button8.Location = new Point(878, 42);
            button8.Name = "button8";
            button8.Size = new Size(50, 23);
            button8.TabIndex = 8;
            button8.Text = "打开";
            button8.UseVisualStyleBackColor = true;
            // 
            // button7
            // 
            button7.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button7.Location = new Point(878, 71);
            button7.Name = "button7";
            button7.Size = new Size(50, 23);
            button7.TabIndex = 8;
            button7.Text = "打开";
            button7.UseVisualStyleBackColor = true;
            // 
            // button6
            // 
            button6.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button6.Location = new Point(878, 100);
            button6.Name = "button6";
            button6.Size = new Size(50, 23);
            button6.TabIndex = 8;
            button6.Text = "打开";
            button6.UseVisualStyleBackColor = true;
            // 
            // button5
            // 
            button5.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button5.Location = new Point(878, 130);
            button5.Name = "button5";
            button5.Size = new Size(50, 23);
            button5.TabIndex = 7;
            button5.Text = "预设";
            button5.UseVisualStyleBackColor = true;
            // 
            // button4
            // 
            button4.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button4.Location = new Point(878, 188);
            button4.Name = "button4";
            button4.Size = new Size(50, 23);
            button4.TabIndex = 6;
            button4.Text = "预设";
            button4.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(86, 251);
            label8.Name = "label8";
            label8.Size = new Size(32, 17);
            label8.TabIndex = 5;
            label8.Text = "日志";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(26, 222);
            label7.Name = "label7";
            label7.Size = new Size(92, 17);
            label7.TabIndex = 5;
            label7.Text = "命名预览（多）";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(26, 161);
            label6.Name = "label6";
            label6.Size = new Size(92, 17);
            label6.TabIndex = 5;
            label6.Text = "命名预览（单）";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(26, 190);
            label5.Name = "label5";
            label5.Size = new Size(92, 17);
            label5.TabIndex = 5;
            label5.Text = "命名规则（多）";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(26, 132);
            label4.Name = "label4";
            label4.Size = new Size(92, 17);
            label4.TabIndex = 5;
            label4.Text = "命名规则（单）";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(38, 103);
            label3.Name = "label3";
            label3.Size = new Size(80, 17);
            label3.TabIndex = 5;
            label3.Text = "失败输出目录";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(38, 74);
            label2.Name = "label2";
            label2.Size = new Size(80, 17);
            label2.TabIndex = 5;
            label2.Text = "成功输出目录";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(62, 45);
            label1.Name = "label1";
            label1.Size = new Size(56, 17);
            label1.TabIndex = 5;
            label1.Text = "输入目录";
            // 
            // richTextBox1
            // 
            richTextBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBox1.Location = new Point(124, 248);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new Size(804, 226);
            richTextBox1.TabIndex = 4;
            richTextBox1.Text = "";
            // 
            // textBoxNamingPreviewMultiple
            // 
            textBoxNamingPreviewMultiple.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxNamingPreviewMultiple.Location = new Point(124, 219);
            textBoxNamingPreviewMultiple.Name = "textBoxNamingPreviewMultiple";
            textBoxNamingPreviewMultiple.ReadOnly = true;
            textBoxNamingPreviewMultiple.Size = new Size(804, 23);
            textBoxNamingPreviewMultiple.TabIndex = 3;
            // 
            // textBoxNamingRuleMultiple
            // 
            textBoxNamingRuleMultiple.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxNamingRuleMultiple.Location = new Point(124, 188);
            textBoxNamingRuleMultiple.Name = "textBoxNamingRuleMultiple";
            textBoxNamingRuleMultiple.Size = new Size(748, 23);
            textBoxNamingRuleMultiple.TabIndex = 3;
            // 
            // textBoxNamingPreview
            // 
            textBoxNamingPreview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxNamingPreview.Location = new Point(124, 158);
            textBoxNamingPreview.Name = "textBoxNamingPreview";
            textBoxNamingPreview.ReadOnly = true;
            textBoxNamingPreview.Size = new Size(804, 23);
            textBoxNamingPreview.TabIndex = 3;
            // 
            // textBoxNamingRule
            // 
            textBoxNamingRule.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            textBoxNamingRule.Location = new Point(124, 129);
            textBoxNamingRule.Name = "textBoxNamingRule";
            textBoxNamingRule.Size = new Size(748, 23);
            textBoxNamingRule.TabIndex = 3;
            // 
            // txtFailedOutputDir
            // 
            txtFailedOutputDir.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFailedOutputDir.Location = new Point(124, 100);
            txtFailedOutputDir.Name = "txtFailedOutputDir";
            txtFailedOutputDir.Size = new Size(748, 23);
            txtFailedOutputDir.TabIndex = 3;
            // 
            // txtSuccessfulOutputDir
            // 
            txtSuccessfulOutputDir.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSuccessfulOutputDir.Location = new Point(124, 71);
            txtSuccessfulOutputDir.Name = "txtSuccessfulOutputDir";
            txtSuccessfulOutputDir.Size = new Size(748, 23);
            txtSuccessfulOutputDir.TabIndex = 3;
            // 
            // txtInputDir
            // 
            txtInputDir.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtInputDir.Location = new Point(124, 42);
            txtInputDir.Name = "txtInputDir";
            txtInputDir.Size = new Size(748, 23);
            txtInputDir.TabIndex = 3;
            // 
            // btnTest
            // 
            btnTest.Location = new Point(485, 13);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(50, 23);
            btnTest.TabIndex = 2;
            btnTest.Text = "测试";
            btnTest.UseVisualStyleBackColor = true;
            btnTest.Click += btnTest_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(429, 13);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(50, 23);
            btnStop.TabIndex = 2;
            btnStop.Text = "停止";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(373, 13);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(50, 23);
            btnStart.TabIndex = 1;
            btnStart.Text = "开始";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // chkGenerateNFO
            // 
            chkGenerateNFO.AutoSize = true;
            chkGenerateNFO.Checked = true;
            chkGenerateNFO.CheckState = CheckState.Checked;
            chkGenerateNFO.Location = new Point(286, 15);
            chkGenerateNFO.Name = "chkGenerateNFO";
            chkGenerateNFO.Size = new Size(81, 21);
            chkGenerateNFO.TabIndex = 0;
            chkGenerateNFO.Text = "生成 NFO";
            chkGenerateNFO.UseVisualStyleBackColor = true;
            // 
            // chkDownGallery
            // 
            chkDownGallery.AutoSize = true;
            chkDownGallery.Checked = true;
            chkDownGallery.CheckState = CheckState.Checked;
            chkDownGallery.Location = new Point(205, 15);
            chkDownGallery.Name = "chkDownGallery";
            chkDownGallery.Size = new Size(75, 21);
            chkDownGallery.TabIndex = 0;
            chkDownGallery.Text = "下载图片";
            chkDownGallery.UseVisualStyleBackColor = true;
            // 
            // chkDownCover
            // 
            chkDownCover.AutoSize = true;
            chkDownCover.Checked = true;
            chkDownCover.CheckState = CheckState.Checked;
            chkDownCover.Location = new Point(124, 15);
            chkDownCover.Name = "chkDownCover";
            chkDownCover.Size = new Size(75, 21);
            chkDownCover.TabIndex = 0;
            chkDownCover.Text = "下载封面";
            chkDownCover.UseVisualStyleBackColor = true;
            // 
            // tabPageOrganizingMode
            // 
            tabPageOrganizingMode.Location = new Point(4, 26);
            tabPageOrganizingMode.Name = "tabPageOrganizingMode";
            tabPageOrganizingMode.Padding = new Padding(3);
            tabPageOrganizingMode.Size = new Size(936, 480);
            tabPageOrganizingMode.TabIndex = 2;
            tabPageOrganizingMode.Text = "整理模式";
            tabPageOrganizingMode.UseVisualStyleBackColor = true;
            // 
            // tabPageFixMetadata
            // 
            tabPageFixMetadata.AllowDrop = true;
            tabPageFixMetadata.Controls.Add(label11);
            tabPageFixMetadata.Controls.Add(richTextBox2);
            tabPageFixMetadata.Controls.Add(button2);
            tabPageFixMetadata.Controls.Add(label10);
            tabPageFixMetadata.Controls.Add(textBox1);
            tabPageFixMetadata.Location = new Point(4, 26);
            tabPageFixMetadata.Name = "tabPageFixMetadata";
            tabPageFixMetadata.Size = new Size(936, 480);
            tabPageFixMetadata.TabIndex = 3;
            tabPageFixMetadata.Text = "修复数据";
            tabPageFixMetadata.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(16, 142);
            label11.Name = "label11";
            label11.Size = new Size(68, 17);
            label11.TabIndex = 11;
            label11.Text = "操作日志：";
            // 
            // richTextBox2
            // 
            richTextBox2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBox2.Location = new Point(90, 142);
            richTextBox2.Name = "richTextBox2";
            richTextBox2.ReadOnly = true;
            richTextBox2.Size = new Size(838, 329);
            richTextBox2.TabIndex = 10;
            richTextBox2.Text = "";
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button2.Location = new Point(878, 12);
            button2.Name = "button2";
            button2.Size = new Size(50, 23);
            button2.TabIndex = 9;
            button2.Text = "打开";
            button2.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(16, 15);
            label10.Name = "label10";
            label10.Size = new Size(68, 17);
            label10.TabIndex = 7;
            label10.Text = "输入目录：";
            // 
            // textBox1
            // 
            textBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox1.Location = new Point(90, 12);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(782, 23);
            textBox1.TabIndex = 6;
            // 
            // tabPageCropImage
            // 
            tabPageCropImage.AllowDrop = true;
            tabPageCropImage.Controls.Add(mainTablePanel);
            tabPageCropImage.Location = new Point(4, 26);
            tabPageCropImage.Name = "tabPageCropImage";
            tabPageCropImage.Padding = new Padding(10);
            tabPageCropImage.Size = new Size(936, 480);
            tabPageCropImage.TabIndex = 1;
            tabPageCropImage.Text = "裁切封面";
            tabPageCropImage.UseVisualStyleBackColor = true;
            // 
            // mainTablePanel
            // 
            mainTablePanel.ColumnCount = 1;
            mainTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            mainTablePanel.Controls.Add(controlPanel, 0, 0);
            mainTablePanel.Controls.Add(previewPanel, 0, 1);
            mainTablePanel.Controls.Add(buttonPanel, 0, 2);
            mainTablePanel.Dock = DockStyle.Fill;
            mainTablePanel.Location = new Point(10, 10);
            mainTablePanel.Name = "mainTablePanel";
            mainTablePanel.Padding = new Padding(5);
            mainTablePanel.RowCount = 3;
            mainTablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            mainTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            mainTablePanel.Size = new Size(916, 460);
            mainTablePanel.TabIndex = 0;
            // 
            // controlPanel
            // 
            controlPanel.ColumnCount = 4;
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            controlPanel.ColumnStyles.Add(new ColumnStyle());
            controlPanel.Controls.Add(labelCropMode, 2, 0);
            controlPanel.Controls.Add(comboBoxCropMode, 3, 0);
            controlPanel.Controls.Add(label9, 0, 0);
            controlPanel.Controls.Add(comboBoxCoverType, 1, 0);
            controlPanel.Dock = DockStyle.Fill;
            controlPanel.Location = new Point(8, 8);
            controlPanel.Name = "controlPanel";
            controlPanel.RowCount = 1;
            controlPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            controlPanel.Size = new Size(900, 29);
            controlPanel.TabIndex = 0;
            // 
            // labelCropMode
            // 
            labelCropMode.Anchor = AnchorStyles.Left;
            labelCropMode.AutoSize = true;
            labelCropMode.Location = new Point(223, 6);
            labelCropMode.Name = "labelCropMode";
            labelCropMode.Size = new Size(68, 17);
            labelCropMode.TabIndex = 2;
            labelCropMode.Text = "裁切方式：";
            labelCropMode.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // comboBoxCropMode
            // 
            comboBoxCropMode.Anchor = AnchorStyles.Left;
            comboBoxCropMode.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxCropMode.Items.AddRange(new object[] { "右侧", "中间", "左侧" });
            comboBoxCropMode.Location = new Point(303, 3);
            comboBoxCropMode.Name = "comboBoxCropMode";
            comboBoxCropMode.Size = new Size(120, 25);
            comboBoxCropMode.TabIndex = 3;
            // 
            // label9
            // 
            label9.Anchor = AnchorStyles.Left;
            label9.AutoSize = true;
            label9.Location = new Point(3, 6);
            label9.Name = "label9";
            label9.Size = new Size(68, 17);
            label9.TabIndex = 4;
            label9.Text = "封面类型：";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // comboBoxCoverType
            // 
            comboBoxCoverType.Anchor = AnchorStyles.Left;
            comboBoxCoverType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxCoverType.FormattingEnabled = true;
            comboBoxCoverType.Items.AddRange(new object[] { "竖版封面（2:3）", "横版封面（16:9）", "方形封面（1:1）" });
            comboBoxCoverType.Location = new Point(83, 3);
            comboBoxCoverType.Name = "comboBoxCoverType";
            comboBoxCoverType.Size = new Size(120, 25);
            comboBoxCoverType.TabIndex = 5;
            // 
            // previewPanel
            // 
            previewPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            previewPanel.BackColor = Color.LightGray;
            previewPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            previewPanel.ColumnCount = 2;
            previewPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            previewPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            previewPanel.Controls.Add(pictureBoxBackdrop, 0, 0);
            previewPanel.Controls.Add(pictureBoxFanart, 1, 0);
            previewPanel.Controls.Add(pictureBoxPoster, 0, 1);
            previewPanel.Controls.Add(pictureBoxThumb, 1, 1);
            previewPanel.Location = new Point(8, 43);
            previewPanel.Name = "previewPanel";
            previewPanel.RowCount = 2;
            previewPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            previewPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            previewPanel.Size = new Size(900, 374);
            previewPanel.TabIndex = 1;
            // 
            // pictureBoxBackdrop
            // 
            pictureBoxBackdrop.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBoxBackdrop.Location = new Point(4, 4);
            pictureBoxBackdrop.Name = "pictureBoxBackdrop";
            pictureBoxBackdrop.Size = new Size(442, 179);
            pictureBoxBackdrop.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxBackdrop.TabIndex = 0;
            pictureBoxBackdrop.TabStop = false;
            // 
            // pictureBoxFanart
            // 
            pictureBoxFanart.Dock = DockStyle.Fill;
            pictureBoxFanart.Location = new Point(453, 4);
            pictureBoxFanart.Name = "pictureBoxFanart";
            pictureBoxFanart.Size = new Size(443, 179);
            pictureBoxFanart.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxFanart.TabIndex = 1;
            pictureBoxFanart.TabStop = false;
            // 
            // pictureBoxPoster
            // 
            pictureBoxPoster.Dock = DockStyle.Fill;
            pictureBoxPoster.Location = new Point(4, 190);
            pictureBoxPoster.Name = "pictureBoxPoster";
            pictureBoxPoster.Size = new Size(442, 180);
            pictureBoxPoster.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxPoster.TabIndex = 2;
            pictureBoxPoster.TabStop = false;
            // 
            // pictureBoxThumb
            // 
            pictureBoxThumb.Dock = DockStyle.Fill;
            pictureBoxThumb.Location = new Point(453, 190);
            pictureBoxThumb.Name = "pictureBoxThumb";
            pictureBoxThumb.Size = new Size(443, 180);
            pictureBoxThumb.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxThumb.TabIndex = 3;
            pictureBoxThumb.TabStop = false;
            // 
            // buttonPanel
            // 
            buttonPanel.ColumnCount = 3;
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            buttonPanel.Controls.Add(button3, 1, 0);
            buttonPanel.Controls.Add(button1, 2, 0);
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.Location = new Point(8, 423);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.RowCount = 1;
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            buttonPanel.Size = new Size(900, 29);
            buttonPanel.TabIndex = 2;
            // 
            // button3
            // 
            button3.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            button3.Location = new Point(703, 3);
            button3.Name = "button3";
            button3.Size = new Size(94, 25);
            button3.TabIndex = 1;
            button3.Text = "选择文件";
            button3.UseVisualStyleBackColor = true;
            button3.Click += SelectButton_Click;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            button1.Location = new Point(803, 3);
            button1.Name = "button1";
            button1.Size = new Size(94, 25);
            button1.TabIndex = 0;
            button1.Text = "裁切封面";
            button1.UseVisualStyleBackColor = true;
            button1.Click += CropButton_Click;
            // 
            // labelCoverType
            // 
            labelCoverType.Anchor = AnchorStyles.Left;
            labelCoverType.AutoSize = true;
            labelCoverType.Location = new Point(3, 7);
            labelCoverType.Name = "labelCoverType";
            labelCoverType.Size = new Size(68, 17);
            labelCoverType.TabIndex = 0;
            labelCoverType.Text = "封面类型：";
            labelCoverType.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // selectButton
            // 
            selectButton.Anchor = AnchorStyles.Right;
            selectButton.AutoSize = true;
            selectButton.Location = new Point(691, 39);
            selectButton.MinimumSize = new Size(100, 25);
            selectButton.Name = "selectButton";
            selectButton.Size = new Size(100, 27);
            selectButton.TabIndex = 1;
            selectButton.Text = "选择文件";
            // 
            // cropButton
            // 
            cropButton.Anchor = AnchorStyles.Right;
            cropButton.AutoSize = true;
            cropButton.Enabled = false;
            cropButton.Location = new Point(797, 39);
            cropButton.MinimumSize = new Size(100, 25);
            cropButton.Name = "cropButton";
            cropButton.Size = new Size(100, 27);
            cropButton.TabIndex = 2;
            cropButton.Text = "裁切封面";
            // 
            // topPanel
            // 
            topPanel.Controls.Add(labelCoverType);
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(200, 100);
            topPanel.TabIndex = 3;
            // 
            // bottomPanel
            // 
            bottomPanel.Controls.Add(selectButton);
            bottomPanel.Controls.Add(cropButton);
            bottomPanel.Location = new Point(0, 0);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Size = new Size(200, 100);
            bottomPanel.TabIndex = 4;
            // 
            // previewBox
            // 
            previewBox.Dock = DockStyle.Fill;
            previewBox.Location = new Point(3, 3);
            previewBox.Name = "previewBox";
            previewBox.Size = new Size(930, 474);
            previewBox.SizeMode = PictureBoxSizeMode.Zoom;
            previewBox.TabIndex = 3;
            previewBox.TabStop = false;
            // 
            // dropPanel
            // 
            dropPanel.AllowDrop = true;
            dropPanel.BackColor = Color.FromArgb(240, 240, 240);
            dropPanel.BorderStyle = BorderStyle.FixedSingle;
            dropPanel.Controls.Add(dropLabel);
            dropPanel.Dock = DockStyle.Fill;
            dropPanel.Location = new Point(3, 3);
            dropPanel.Name = "dropPanel";
            dropPanel.Size = new Size(930, 428);
            dropPanel.TabIndex = 0;
            // 
            // dropLabel
            // 
            dropLabel.AllowDrop = true;
            dropLabel.BackColor = Color.Transparent;
            dropLabel.Dock = DockStyle.Fill;
            dropLabel.Location = new Point(0, 0);
            dropLabel.Name = "dropLabel";
            dropLabel.Size = new Size(928, 426);
            dropLabel.TabIndex = 0;
            dropLabel.Text = "将图片文件拖拽到此处\n或点击选择文件";
            dropLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // menuStrip
            // 
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem, toolToolStripMenuItem, helpToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(946, 25);
            menuStrip.TabIndex = 1;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(39, 21);
            fileToolStripMenuItem.Text = "File";
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(47, 21);
            viewToolStripMenuItem.Text = "View";
            // 
            // toolToolStripMenuItem
            // 
            toolToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { optionToolStripMenuItem });
            toolToolStripMenuItem.Name = "toolToolStripMenuItem";
            toolToolStripMenuItem.Size = new Size(46, 21);
            toolToolStripMenuItem.Text = "Tool";
            // 
            // optionToolStripMenuItem
            // 
            optionToolStripMenuItem.Name = "optionToolStripMenuItem";
            optionToolStripMenuItem.Size = new Size(116, 22);
            optionToolStripMenuItem.Text = "Option";
            optionToolStripMenuItem.Click += optionToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(47, 21);
            helpToolStripMenuItem.Text = "Help";
            // 
            // statusStrip
            // 
            statusStrip.Location = new Point(0, 538);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(946, 22);
            statusStrip.TabIndex = 2;
            statusStrip.Text = "statusStrip1";
            // 
            // labelPoster
            // 
            labelPoster.Dock = DockStyle.Bottom;
            labelPoster.Location = new Point(3, 217);
            labelPoster.Name = "labelPoster";
            labelPoster.Size = new Size(462, 25);
            labelPoster.TabIndex = 3;
            labelPoster.Text = "封面图 (poster)";
            labelPoster.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelThumb
            // 
            labelThumb.Dock = DockStyle.Bottom;
            labelThumb.Location = new Point(471, 217);
            labelThumb.Name = "labelThumb";
            labelThumb.Size = new Size(456, 25);
            labelThumb.TabIndex = 4;
            labelThumb.Text = "缩略图 (thumb)";
            labelThumb.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelBackdrop
            // 
            labelBackdrop.Dock = DockStyle.Bottom;
            labelBackdrop.Location = new Point(3, 431);
            labelBackdrop.Name = "labelBackdrop";
            labelBackdrop.Size = new Size(462, 25);
            labelBackdrop.TabIndex = 5;
            labelBackdrop.Text = "背景图 (backdrop)";
            labelBackdrop.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelFanart
            // 
            labelFanart.Dock = DockStyle.Bottom;
            labelFanart.Location = new Point(471, 431);
            labelFanart.Name = "labelFanart";
            labelFanart.Size = new Size(456, 25);
            labelFanart.TabIndex = 6;
            labelFanart.Text = "艺术图 (fanart)";
            labelFanart.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(946, 560);
            Controls.Add(statusStrip);
            Controls.Add(tabControlMain);
            Controls.Add(menuStrip);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip;
            Margin = new Padding(4);
            Name = "FormMain";
            Text = "Jav Scraper Tools";
            tabControlMain.ResumeLayout(false);
            tabPageMain.ResumeLayout(false);
            tabPageMain.PerformLayout();
            tabPageFixMetadata.ResumeLayout(false);
            tabPageFixMetadata.PerformLayout();
            tabPageCropImage.ResumeLayout(false);
            mainTablePanel.ResumeLayout(false);
            controlPanel.ResumeLayout(false);
            controlPanel.PerformLayout();
            previewPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxBackdrop).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxFanart).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPoster).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxThumb).EndInit();
            buttonPanel.ResumeLayout(false);
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            bottomPanel.ResumeLayout(false);
            bottomPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)previewBox).EndInit();
            dropPanel.ResumeLayout(false);
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private void CreatePreviewCell(int row, int col, PictureBox pictureBox, Label label, string text)
        {
            TableLayoutPanel cell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Margin = new Padding(0),
                BackColor = Color.White
            };

            cell.RowStyles.Add(new RowStyle(SizeType.Percent, 90F));
            cell.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.BackColor = Color.White;
            pictureBox.Margin = new Padding(5);

            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Text = text;
            label.BackColor = Color.White;
            label.Margin = new Padding(0);

            cell.Controls.Add(pictureBox, 0, 0);
            cell.Controls.Add(label, 0, 1);

            previewPanel.Controls.Add(cell, col, row);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPageMain;
        private System.Windows.Forms.TabPage tabPageCropImage;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.CheckBox chkGenerateNFO;
        private System.Windows.Forms.CheckBox chkDownGallery;
        private System.Windows.Forms.CheckBox chkDownCover;
        private System.Windows.Forms.TextBox textBoxNamingPreview;
        private System.Windows.Forms.TextBox textBoxNamingRule;
        private System.Windows.Forms.TextBox txtFailedOutputDir;
        private System.Windows.Forms.TextBox txtSuccessfulOutputDir;
        private System.Windows.Forms.TextBox txtInputDir;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TextBox textBoxNamingRuleMultiple;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBoxNamingPreviewMultiple;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripMenuItem optionToolStripMenuItem;
        private Panel dropPanel;
        private Label dropLabel;
        private Button selectButton;
        private Button cropButton;
        private PictureBox previewBox;
        private ComboBox comboBoxCoverType1;
        private Label labelCoverType;
        private TableLayoutPanel previewPanel;
        //private PictureBox posterPreviewBox;
        //private PictureBox thumbPreviewBox;
        //private PictureBox backdropPreviewBox;
        //private PictureBox fanartPreviewBox;
        private Label labelPoster;
        private Label labelThumb;
        private Label labelBackdrop;
        private Label labelFanart;
        private Panel topPanel;
        private Panel bottomPanel;
        private TableLayoutPanel mainTablePanel;
        private TableLayoutPanel controlPanel;
        private Label labelCropMode;
        private ComboBox comboBoxCropMode;
        private TableLayoutPanel buttonPanel;
        private Button button3;
        private Button button1;
        private Label label9;
        private ComboBox comboBoxCoverType;
        private PictureBox pictureBoxBackdrop;
        private PictureBox pictureBoxFanart;
        private PictureBox pictureBoxPoster;
        private PictureBox pictureBoxThumb;
        private TabPage tabPageOrganizingMode;
        private TabPage tabPageFixMetadata;
        private Button button2;
        private Label label10;
        private TextBox textBox1;
        private Label label11;
        private RichTextBox richTextBox2;
    }
}

